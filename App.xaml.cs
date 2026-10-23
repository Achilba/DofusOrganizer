using System.Threading;
using System.Windows;
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
    private Storage _storage;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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

        _vm.ToggleVisibilityRequested += ToggleVisibility;
        _vm.MiniModeRequested += SetMini;
        _main.Closed += (_, _) => Quit();

        _vm.PatchNotesRequested += ShowPatchNotes;

        if (data.StartInMini) SetMini(true); else SetMini(false);

        _ = _vm.CheckUpdateAsync(silent: true);
        // Revérifie toutes les 30 minutes tant que l'appli est ouverte (la pastille « Paramètres » apparaît alors toute seule)
        var updateTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMinutes(30) };
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
        w.Show();
    }

    private void SetMini(bool mini)
    {
        _vm.Data.StartInMini = mini;
        _vm.RequestSave();
        if (mini) { _main.Hide(); _mini.Show(); _mini.Topmost = _vm.Data.MiniAlwaysOnTop; }
        else { _mini.Hide(); _main.Show(); _main.Activate(); }
    }

    private void ToggleVisibility()
    {
        if (_main.IsVisible || _mini.IsVisible) { _main.Hide(); _mini.Hide(); }
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
