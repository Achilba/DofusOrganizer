using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DofusTeamOrganizer.Services;
using DofusTeamOrganizer.ViewModels;
using DofusTeamOrganizer.Views;

namespace DofusTeamOrganizer;

public partial class App : Application
{
    private Mutex _mutex;
    private HotkeyService _hotkeys;
    private MainViewModel _vm;
    private MainWindow _main;
    private MiniWindow _mini;
    private SongeMiniWindow _songeMini;
    private bool _songeMiniActive;
    private Storage _storage;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Champs numériques (Tag = "SelectAll") : tout le texte est sélectionné quand on clique ou qu'on arrive dessus,
        // pour que la saisie remplace la valeur au lieu de s'ajouter à elle (100 + 50 = 10050).
        EventManager.RegisterClassHandler(typeof(TextBox), UIElement.GotKeyboardFocusEvent,
            new KeyboardFocusChangedEventHandler((s, a) => { if (s is TextBox t && t.Tag as string == "SelectAll") t.SelectAll(); }));
        EventManager.RegisterClassHandler(typeof(TextBox), UIElement.PreviewMouseLeftButtonDownEvent,
            new MouseButtonEventHandler((s, a) =>
            {
                if (s is TextBox t && t.Tag as string == "SelectAll" && !t.IsKeyboardFocusWithin) { t.Focus(); a.Handled = true; }
            }));

        _mutex = new Mutex(true, "DofusTeamOrganizer.SingleInstance", out bool first);
        if (!first)
        {
            MessageBox.Show("DOFUS Team Organizer est déjà lancé (Ctrl+Alt+O pour l'afficher).", "DOFUS Team Organizer");
            Shutdown();
            return;
        }

        Updater.CleanupOld();
        _storage = new Storage();
        DispatcherUnhandledException += (_, args) =>
        {
            _storage.LogError(args.Exception.ToString());
            MessageBox.Show("Une erreur est survenue, elle a été notée dans :\n" + _storage.ErrorLogPath, "DOFUS Team Organizer");
            args.Handled = true;
        };

        // 1. Données + sauvegarde quotidienne AVANT d'ouvrir la moindre fenêtre
        var data = _storage.Load(out string warning);
        if (data.AutoBackup) _storage.DailyBackup(data);

        _hotkeys = new HotkeyService();
        _vm = new MainViewModel(_storage, data, warning, _hotkeys);
        _vm.SaveNow();

        _main = new MainWindow { DataContext = _vm };
        _mini = new MiniWindow { DataContext = _vm };
        _songeMini = new SongeMiniWindow { DataContext = _vm };

        _vm.ToggleVisibilityRequested += ToggleVisibility;
        _vm.MiniModeRequested += SetMini;
        _vm.SongeMiniRequested += SetSongeMini;
        _main.Closed += (_, _) => Quit();

        _vm.PatchNotesRequested += ShowPatchNotes;

        if (data.StartInMini) SetMini(true); else SetMini(false);

        _ = _vm.CheckUpdateAsync(silent: true);
        // Revérifie toutes les 5 minutes tant que l'appli est ouverte (la pastille « Paramètres » apparaît alors toute seule)
        var updateTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
        updateTimer.Tick += (_, _) => _ = _vm.CheckUpdateAsync(silent: true);
        updateTimer.Start();

        // Notes de version : affichées une fois après chaque mise à jour
        if (_vm.ShouldShowPatchNotes)
        {
            _vm.MarkPatchNotesSeen();
            ShowPatchNotes();
        }
    }

    private void ShowPatchNotes()
    {
        var w = new PatchNotesWindow();
        if (_main.IsVisible) w.Owner = _main;
        // Fermer les notes ne doit pas réduire l'appli : on remet la fenêtre principale au premier plan.
        w.Closed += (_, _) =>
        {
            if (!_main.IsVisible) return;
            if (_main.WindowState == WindowState.Minimized) _main.WindowState = WindowState.Normal;
            _main.Activate();
        };
        w.Show();
    }

    /// <summary>Barre Mini réservée aux songes (on revient à l'appli avec son bouton de retour).</summary>
    private void SetSongeMini(bool on)
    {
        if (!on) { SetMini(false); return; }
        _main.Hide();
        _mini.Hide();
        _songeMiniActive = true;
        _songeMini.Show();
    }

    private void SetMini(bool mini)
    {
        _songeMini.Hide();
        _songeMiniActive = false;
        _vm.Data.StartInMini = mini;
        _vm.RequestSave();
        if (mini) { _main.Hide(); _mini.Show(); _mini.Topmost = _vm.Data.MiniAlwaysOnTop; }
        else { _mini.Hide(); _main.Show(); _main.Activate(); }
    }

    private void ToggleVisibility()
    {
        if (_main.IsVisible || _mini.IsVisible || _songeMini.IsVisible) { _main.Hide(); _mini.Hide(); _songeMini.Hide(); }
        else if (_songeMiniActive) _songeMini.Show();
        else SetMini(_vm.Data.StartInMini);
    }

    /// <summary>Fermeture demandée par la mise à jour automatique.</summary>
    public void ExitForUpdate() => Quit();

    private void Quit()
    {
        try { _vm?.SaveNow(); } catch { }
        _hotkeys?.Dispose();
        Shutdown();
    }
}
