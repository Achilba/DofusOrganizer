using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using DofusTeamOrganizer.Models;
using DofusTeamOrganizer.Services;

namespace DofusTeamOrganizer.ViewModels;

public class NavItem : ObservableObject
{
    private bool _active;
    public NavItem(string key, string label, Geometry icon) { Key = key; Label = label; Icon = icon; }
    public Geometry Icon { get; }
    public string Key { get; }
    public string Label { get; }
    public bool IsActive { get => _active; set => Set(ref _active, value); }
    private bool _badge;
    public bool Badge { get => _badge; set => Set(ref _badge, value); }
}

/// <summary>Une ligne de la liste des raccourcis dans Paramètres.</summary>
public class ShortcutRow : ObservableObject
{
    private string _keyText = "";
    private bool _capturing;
    private string _message = "";

    public ShortcutRow(string action) { Action = action; Label = ShortcutDefs.Label(action); }

    public string Action { get; }
    public string Label { get; }
    public string KeyText { get => _keyText; set { if (Set(ref _keyText, value)) Raise(nameof(ButtonText)); } }
    public bool Capturing { get => _capturing; set { if (Set(ref _capturing, value)) Raise(nameof(ButtonText)); } }
    public string Message { get => _message; set => Set(ref _message, value); }
    public string ButtonText => _capturing ? "Appuie sur une touche…" : _keyText;
}

/// <summary>Une pastille de thème dans Paramètres.</summary>
public class ThemeRow : ObservableObject
{
    private bool _selected;
    public ThemeRow(ThemeDef def)
    {
        Key = def.Key;
        Name = def.Name;
        AccentBrush = Freeze(def.Accent);
        SurfaceBrush = Freeze(def.Surface);
        BackgroundBrush = Freeze(def.Background);
    }
    private static System.Windows.Media.SolidColorBrush Freeze(System.Windows.Media.Color c)
    {
        var b = new System.Windows.Media.SolidColorBrush(c);
        b.Freeze();
        return b;
    }
    public string Key { get; }
    public string Name { get; }
    public System.Windows.Media.Brush AccentBrush { get; }
    public System.Windows.Media.Brush SurfaceBrush { get; }
    public System.Windows.Media.Brush BackgroundBrush { get; }
    public bool Selected { get => _selected; set => Set(ref _selected, value); }
}

public class NoteHit
{
    public string Key { get; set; }
    public string Snippet { get; set; }
}

public class TaskItem : ObservableObject
{
    private bool _done;
    private readonly Action<TaskItem> _toggled;
    public TaskItem(int line, string text, bool done, Action<TaskItem> toggled) { Line = line; Text = text; _done = done; _toggled = toggled; }
    public int Line { get; }
    public string Text { get; }
    public bool Done { get => _done; set { if (Set(ref _done, value)) _toggled?.Invoke(this); } }
}

public class MainViewModel : ObservableObject
{
    public Storage Storage { get; }
    public AppData Data { get; private set; }
    public HotkeyService Hotkeys { get; }

    private readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(600) };
    private readonly DispatcherTimer _toastTimer = new() { Interval = TimeSpan.FromMilliseconds(2500) };
    private readonly DispatcherTimer _undoTimer = new() { Interval = TimeSpan.FromMilliseconds(4000) };
    private readonly DispatcherTimer _pollTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };

    private string _page = "home";
    private string _toast = "";
    private string _undoText = "";
    private Action _undoAction;
    private string _status = "";
    private string _lastCharacter = "";
    private string _selectedNote = "Commun";

    public event Action ToggleVisibilityRequested;
    public event Action<bool> MiniModeRequested; // true = afficher la mini-barre
    public event Action MiniSettingsChanged;
    public event Action UiScaleChanged;
    public event Action FloatNoteRequested;
    public RelayCommand FloatNoteCommand { get; private set; }

    public string[] ScaleChoices { get; } = { "90 %", "100 %", "110 %", "125 %" };
    public string ScaleChoice
    {
        get
        {
            var t = (int)Math.Round(Data.UiScale * 100) + " %";
            return Array.IndexOf(ScaleChoices, t) >= 0 ? t : "100 %";
        }
        set
        {
            if (value == null || value == ScaleChoice) return;
            if (!int.TryParse(value.Replace("%", "").Trim(), out var pct)) return;
            Data.UiScale = pct / 100.0;
            Raise();
            RequestSave();
            UiScaleChanged?.Invoke();
        }
    }
    public event Action<bool> SongeMiniRequested; // true = afficher la barre Mini des songes
    public void RequestSongeMini() => SongeMiniRequested?.Invoke(true);
    public event Action PatchNotesRequested;

    public ObservableCollection<ZaapEntry> Favorites { get; } = new();
    public ObservableCollection<DofusItem> MainDofus { get; } = new();
    public ObservableCollection<DofusItem> SpecialDofus { get; } = new();
    public ObservableCollection<string> NoteKeys { get; } = new();
    public ObservableCollection<NavItem> NavItems { get; } = new();
    /// <summary>Onglets du haut de la barre latérale (tous sauf Paramètres).</summary>
    public ObservableCollection<NavItem> TopNavItems { get; } = new();
    /// <summary>Onglet du bas de la barre latérale (Paramètres).</summary>
    public ObservableCollection<NavItem> BottomNavItems { get; } = new();

    // ── Raccourcis modifiables ──
    public ObservableCollection<ShortcutRow> ShortcutRows { get; } = new();
    public RelayCommand ShortcutClickCommand { get; private set; }
    public RelayCommand ResetShortcutsCommand { get; private set; }

    public ShortcutSpec GetShortcut(string action)
        => Data.Shortcuts != null && Data.Shortcuts.TryGetValue(action, out var s) && s != null && s.Vk > 0 ? s : ShortcutDefs.Default(action);

    private void BuildShortcutRows()
    {
        ShortcutRows.Clear();
        foreach (var a in ShortcutDefs.All)
            ShortcutRows.Add(new ShortcutRow(a) { KeyText = ShortcutDefs.Display(GetShortcut(a)) });
    }

    /// <summary>Clic sur une touche : la ligne attend la prochaine touche pressée.</summary>
    private void ShortcutClick(object p)
    {
        foreach (var r in ShortcutRows) { r.Capturing = false; r.Message = ""; }
        if (p is ShortcutRow row) row.Capturing = true;
    }

    /// <summary>Appelée par la page Paramètres quand une touche est pressée pendant la saisie.</summary>
    public void AssignShortcut(ShortcutRow row, int mods, int vk)
    {
        if (vk == 0x6A && mods == 0) vk = 0xDC; // * du pavé numérique = * (les deux sont enregistrés ensemble)

        string error = null;
        if (vk == 0x7B && mods == 0 && row.Action != ShortcutDefs.Invites)
            error = "F12 est réservée à « Copier les invitations » (Windows la refuse pour les autres raccourcis).";
        else if (row.Action == ShortcutDefs.Toggle && (mods & (1 | 2 | 8)) == 0)
            error = "Le raccourci global doit contenir Ctrl, Alt ou Windows, sinon il bloquerait cette touche partout.";
        else if (row.Action != ShortcutDefs.Toggle && (mods & (1 | 2 | 8)) == 0 && !ShortcutDefs.AllowedAlone(vk))
            error = "Une lettre ou un chiffre seul gênerait l'écriture dans DOFUS : choisis une touche F1…F12, * ou ajoute Ctrl / Alt.";
        else
        {
            var spec = new ShortcutSpec(mods, vk);
            foreach (var other in ShortcutDefs.All)
                if (other != row.Action && GetShortcut(other).Same(spec))
                { error = "Déjà utilisé par « " + ShortcutDefs.Label(other) + " »."; break; }
        }

        if (error != null) { row.Message = error; row.Capturing = false; return; }

        Data.Shortcuts ??= new();
        Data.Shortcuts[row.Action] = new ShortcutSpec(mods, vk);
        row.KeyText = ShortcutDefs.Display(Data.Shortcuts[row.Action]);
        row.Message = "";
        row.Capturing = false;
        RequestSave();
        Hotkeys.Apply(GetShortcut);
    }

    private void ResetShortcuts()
    {
        Data.Shortcuts?.Clear();
        BuildShortcutRows();
        RequestSave();
        Hotkeys.Apply(GetShortcut);
        ShowToast("Raccourcis rétablis");
    }

    // ── Thèmes de couleurs ──
    public ObservableCollection<ThemeRow> ThemeRows { get; } = new();
    public RelayCommand ThemeClickCommand { get; private set; }

    private void BuildThemeRows()
    {
        ThemeRows.Clear();
        foreach (var t in ThemeCatalog.All) ThemeRows.Add(new ThemeRow(t) { Selected = t.Key == ThemeCatalog.Get(Data.Theme).Key });
    }

    private void SetTheme(string key)
    {
        var def = ThemeCatalog.Get(key);
        Data.Theme = def.Key;
        ApplyTheme();
        foreach (var r in ThemeRows) r.Selected = r.Key == def.Key;
        RequestSave();
    }

    // ── Démarrage avec Windows ──
    public bool StartWithWindows
    {
        get => AutoStart.IsEnabled();
        set
        {
            if (AutoStart.IsEnabled() == value) return;
            if (!AutoStart.Set(value)) ShowToast("Windows a refusé la modification du démarrage automatique");
            else ShowToast(value ? "L'appli démarrera avec Windows" : "Démarrage automatique désactivé");
            Raise();
        }
    }

    public GlobalSearch Search { get; private set; }

    public CharactersViewModel CharactersPage { get; private set; }
    public RoutineViewModel Routines { get; private set; }
    public DungeonsViewModel Dungeons { get; private set; }
    public SongesViewModel Songes { get; private set; }
    public AlmanaxViewModel Almanax { get; private set; }

    public RelayCommand NavigateCommand { get; }
    public RelayCommand CopyInvitesCommand { get; }
    public RelayCommand CopyZaapCommand { get; }
    public RelayCommand ShowMiniCommand { get; }
    public RelayCommand ShowMainCommand { get; }
    public RelayCommand ExportCommand { get; }
    public RelayCommand CopyBackupCommand { get; }
    public RelayCommand ImportCommand { get; }
    public RelayCommand ResetAllCommand { get; }
    public RelayCommand OpenFolderCommand { get; }
    public RelayCommand UndoCommand { get; }
    public RelayCommand CopyErrorLogCommand { get; }
    public RelayCommand OpenRepoCommand { get; }
    public RelayCommand AccentPickCommand { get; }
    public string[] AccentSwatches { get; } = { "#F2B544", "#FF7A2F", "#F0544F", "#E86AA6", "#B57BFF", "#5FA8FF", "#35D0E0", "#2FD0B8", "#5FD97A", "#C7D94B" };
    public RelayCommand CopySupportInfoCommand { get; }
    public string DataFolderText => Storage.Dir;
    public RelayCommand PatchNotesCommand { get; }
    public RelayCommand PrepareNextWeekCommand { get; }
    public RelayCommand RetrySaveCommand { get; }
    public RelayCommand CheckUpdateCommand { get; }
    public RelayCommand InstallUpdateCommand { get; }

    // ───────── Mises à jour automatiques ─────────
    private UpdateInfo _update;
    private string _updateText = "";
    private bool _updateBusy;
    private string _notifiedVersion = "";
    public string UpdateText { get => _updateText; private set => Set(ref _updateText, value); }
    private void RaiseUpdate()
    {
        Raise(nameof(UpdateAvailable));
        Raise(nameof(UpdateVersionText));
        foreach (var n in NavItems) if (n.Key == "settings") n.Badge = UpdateAvailable;
    }
    /// <summary>Texte du bouton rouge de l'accueil, par exemple « Version 0.9.15 ».</summary>
    public string UpdateVersionText => _update == null ? "" : "Version " + _update.Version;
    public bool UpdateAvailable => _update != null && !_updateBusy;
    public bool UpdateBusy { get => _updateBusy; private set { if (Set(ref _updateBusy, value)) RaiseUpdate(); } }
    public string InstallUpdateLabel => _update == null ? "" : "Installer la version " + _update.Version;

    public async System.Threading.Tasks.Task CheckUpdateAsync(bool silent)
    {
        if (_updateBusy) return;
        UpdateBusy = true;
        if (!silent) UpdateText = "Recherche en cours…";
        try
        {
            _update = await new Updater(Storage.Dir).CheckAsync(PatchNotes.Current);
            RaiseUpdate();
            Raise(nameof(InstallUpdateLabel));
            if (_update != null)
            {
                UpdateText = "Version " + _update.Version + " disponible.";
                if (silent && _notifiedVersion != _update.Version && !Data.StartInMini && !Data.QuietMode)
                {
                    _notifiedVersion = _update.Version;
                    ShowToast("Mise à jour " + _update.Version + " disponible (Paramètres)");
                }
            }
            else if (!silent) UpdateText = "Tu as la dernière version (" + PatchNotes.Current + ").";
            else UpdateText = "";
        }
        catch (Exception ex)
        {
            _update = null;
            RaiseUpdate();
            // Sans connexion ou dépôt privé sans jeton : silencieux au lancement, explicite si demandé.
            UpdateText = silent ? "" : "Impossible de vérifier (connexion ou accès au dépôt) : " + ex.Message;
            Storage.LogError("Mise à jour : " + ex.Message);
        }
        finally { UpdateBusy = false; }
    }

    private async void InstallUpdate()
    {
        if (_update == null || _updateBusy) return;
        UpdateBusy = true;
        UpdateText = "Téléchargement de la version " + _update.Version + "…";
        try
        {
            SaveNow();
            await new Updater(Storage.Dir).DownloadAndReplaceAsync(_update);
            UpdateText = "Redémarrage…";
            ((App)Application.Current).ExitForUpdate();
        }
        catch (Exception ex)
        {
            UpdateText = "Échec de la mise à jour : " + ex.Message + " (rien n'a été modifié).";
            Storage.LogError("Mise à jour : " + ex);
            UpdateBusy = false;
        }
    }
    public bool SaveFailed { get; private set; }
    public string MemoryText => "Mémoire utilisée par l'appli : " + Math.Round(System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / 1048576.0) + " Mo";

    public string VersionText => "Version " + PatchNotes.Current;

    public MainViewModel(Storage storage, AppData data, string warning, HotkeyService hotkeys)
    {
        Storage = storage;
        Data = data;
        Hotkeys = hotkeys;
        Status = warning;
        _loadWarning = warning;

        // Thème de couleurs appliqué avant l'ouverture de toute fenêtre ; démarrage Windows : met le chemin à jour si l'appli a bougé
        ApplyTheme();
        ThemeClickCommand = new RelayCommand(p => SetTheme(p as string));
        BuildThemeRows();
        AutoStart.Refresh();

        foreach (var (k, l, ico) in new[] { ("home", "Accueil", "IcoHome"), ("chars", "Personnages", "IcoChars"), ("dofus", "Dofus", "IcoDofus"), ("almanax", "Almanax", "IcoCalendar"),
                                            ("dungeons", "Donjons", "IcoDungeon"), ("dreams", "Songes", "IcoMoon"), ("routines", "Tâches", "IcoRoutines"),
                                            ("notes", "Notes", "IcoNotes"), ("settings", "Paramètres", "IcoSettings") })
            NavItems.Add(new NavItem(k, l, Application.Current.TryFindResource(ico) as Geometry));
        foreach (var n in NavItems) (n.Key == "settings" ? BottomNavItems : TopNavItems).Add(n);
        UpdateNav();

        ShortcutClickCommand = new RelayCommand(ShortcutClick);
        ResetShortcutsCommand = new RelayCommand(ResetShortcuts);
        BuildShortcutRows();
        Hotkeys.Apply(GetShortcut);

        NavigateCommand = new RelayCommand(p => Page = (string)p);
        CopyInvitesCommand = new RelayCommand(CopyInvites);
        CopyZaapCommand = new RelayCommand(p => CopyZaap((ZaapEntry)p));
        ShowMiniCommand = new RelayCommand(() => MiniModeRequested?.Invoke(true));
        ShowMainCommand = new RelayCommand(() => MiniModeRequested?.Invoke(false));
        ExportCommand = new RelayCommand(Export);
        CopyBackupCommand = new RelayCommand(CopyBackup);
        ImportCommand = new RelayCommand(Import);
        ResetAllCommand = new RelayCommand(ResetAll);
        OpenFolderCommand = new RelayCommand(() => System.Diagnostics.Process.Start("explorer.exe", Storage.Dir));
        UndoCommand = new RelayCommand(RunUndo);
        RetrySaveCommand = new RelayCommand(SaveNow);
        CheckUpdateCommand = new RelayCommand(async () => await CheckUpdateAsync(false));
        InstallUpdateCommand = new RelayCommand(InstallUpdate);
        PatchNotesCommand = new RelayCommand(() => PatchNotesRequested?.Invoke());
        PrepareNextWeekCommand = new RelayCommand(() => { Almanax.ShowNextWeek(); Page = "almanax"; });
        OpenNoteCommand = new RelayCommand(p => OpenNote(p as NoteHit));
        AddTaskCommand = new RelayCommand(AddTask);
        ToggleSpecialCommand = new RelayCommand(() => SpecOpen = !SpecOpen);
        ToggleCompactCommand = new RelayCommand(() => MiniCompact = !MiniCompact);
        MoveUpCommand = new RelayCommand(p => Move(p as Character, -1));
        MoveDownCommand = new RelayCommand(p => Move(p as Character, +1));
        CopyAlmanaxZaapCommand = new RelayCommand(() => CopyZaap(ZaapData.All.First(z => z.Coord == "-5,-23")));
        OpenAlmanaxFromMiniCommand = new RelayCommand(() => { Page = "almanax"; MiniModeRequested?.Invoke(false); });
        FloatNoteCommand = new RelayCommand(() => FloatNoteRequested?.Invoke());
        AccentPickCommand = new RelayCommand(p =>
        {
            Data.AccentColor = p as string ?? "";
            ApplyTheme();
            Raise(nameof(AccentText));
            RequestSave();
        });
        OpenRepoCommand = new RelayCommand(() =>
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/" + Updater.Owner + "/" + Updater.Repo + "/releases") { UseShellExecute = true }); }
            catch { ShowToast("Impossible d'ouvrir le navigateur"); }
        });
        CopySupportInfoCommand = new RelayCommand(() =>
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("DOFUS Team Organizer " + PatchNotes.Current);
            sb.AppendLine("Windows : " + Environment.OSVersion.VersionString + " · .NET " + Environment.Version);
            sb.AppendLine("Dossier de données : " + Storage.Dir);
            sb.AppendLine("Personnages : " + Characters.Count + " · thème : " + Data.Theme + " · taille du texte : " + (int)Math.Round(Data.UiScale * 100) + " %");
            try
            {
                if (File.Exists(Storage.ErrorLogPath))
                {
                    var lines = File.ReadAllLines(Storage.ErrorLogPath);
                    sb.AppendLine("--- Dernières lignes du journal d'erreurs ---");
                    foreach (var l in lines.Skip(Math.Max(0, lines.Length - 40))) sb.AppendLine(l);
                }
            }
            catch { }
            ShowToast(CopyText(sb.ToString()) ? "Infos de support copiées" : "Copie impossible");
        });
        CopyErrorLogCommand = new RelayCommand(() =>
        {
            var text = File.Exists(Storage.ErrorLogPath) ? File.ReadAllText(Storage.ErrorLogPath) : "";
            if (text.Length == 0) ShowToast("Aucune erreur enregistrée");
            else ShowToast(CopyText(text) ? "Journal copié" : "Copie impossible");
        });

        TrackedObject.Changed += RequestSave;
        Character.FeatureChanged += OnFeatureChanged;
        FeatureColumns = new[]
        {
            new FeatureColumn(this, "Accueil", c => c.ShowHome, (c, v) => c.ShowHome = v),
            new FeatureColumn(this, "Succès DJ", c => c.AchTracked, (c, v) => c.AchTracked = v),
            new FeatureColumn(this, "Tâches", c => c.ShowTasks, (c, v) => c.ShowTasks = v),
            new FeatureColumn(this, "Songes", c => c.ShowDreams, (c, v) => c.ShowDreams = v),
            new FeatureColumn(this, "Notes", c => c.ShowNotes, (c, v) => c.ShowNotes = v),
        };

        BuildLists();
        UpdateOrder();
        CharactersPage = new CharactersViewModel(this);
        Dungeons = new DungeonsViewModel(this);
        Songes = new SongesViewModel(this);
        Almanax = new AlmanaxViewModel(this);
        Routines = new RoutineViewModel(this);
        Search = new GlobalSearch(this);
        RebuildTasks();

        _saveTimer.Tick += (_, _) => { _saveTimer.Stop(); SaveNow(); };
        _toastTimer.Tick += (_, _) => { _toastTimer.Stop(); Toast = ""; };
        _undoTimer.Tick += (_, _) => { _undoTimer.Stop(); UndoText = ""; _undoAction = null; };
        _pollTimer.Tick += (_, _) => Poll();
        _pollTimer.Start();

        hotkeys.Pressed += OnHotkey;
    }

    // ───────── Ordre des personnages ─────────

    private void UpdateOrder()
    {
        for (int i = 0; i < Characters.Count; i++)
        {
            Characters[i].Number = i + 1;
            Characters[i].IsMain = i == 0;
        }
        Raise(nameof(MainCharLabel));
        Raise(nameof(ConnectedText));
        RefreshHomeCharacters();
        foreach (var f in FeatureColumns ?? Array.Empty<FeatureColumn>()) f.Refresh();
    }

    // ───────── Fonctions par personnage (Paramètres) ─────────

    public FeatureColumn[] FeatureColumns { get; private set; }

    /// <summary>Personnages affichés sur l'accueil (ceux dont la case « Accueil » est cochée), dans l'ordre choisi.</summary>
    public ObservableCollection<Character> HomeCharacters { get; } = new();

    private void RefreshHomeCharacters()
    {
        var want = Characters.Where(c => c.ShowHome).ToList();
        if (HomeCharacters.SequenceEqual(want)) return;
        HomeCharacters.Clear();
        foreach (var c in want) HomeCharacters.Add(c);
    }

    private void OnFeatureChanged()
    {
        RefreshHomeCharacters();
        foreach (var f in FeatureColumns ?? Array.Empty<FeatureColumn>()) f.Refresh();
        BuildNoteKeys();
        Raise(nameof(SelectedNote));
        Raise(nameof(NoteText));
        Routines?.Refresh();
        Songes?.Strat.RefreshChars();
        RequestSave();
    }

    /// <summary>Glisser-déposer d'un personnage dans les Paramètres : le place à un nouvel endroit de l'ordre.</summary>
    public void MoveCharacterTo(Character c, int index)
    {
        int i = Characters.IndexOf(c);
        if (c == null || i < 0) return;
        index = Math.Max(0, Math.Min(index, Characters.Count - 1));
        if (i == index) return;
        Characters.Move(i, index);
        UpdateOrder();
        BuildNoteKeys();
        RequestSave();
    }

    private void Move(Character c, int dir)
    {
        if (c == null) return;
        int i = Characters.IndexOf(c), j = i + dir;
        if (i < 0 || j < 0 || j >= Characters.Count) return;
        Characters.Move(i, j);
        UpdateOrder();
        BuildNoteKeys();
        RequestSave();
    }

    /// <summary>Ouvre la fiche d'un personnage (clic sur une carte de l'accueil).</summary>
    public void OpenCharacter(Character c)
    {
        if (c == null) return;
        CharactersPage.Selected = c;
        Page = "chars";
    }

    // ───────── Navigation / état ─────────

    public string Page
    {
        get => _page;
        set
        {
            if (!Set(ref _page, value)) return;
            UpdateNav();
            Raise(nameof(ShowMiniShortcut));
            // La liste des personnages suivis peut avoir changé depuis la page Personnages.
            if (value == "dungeons") Dungeons?.Reload();
            if (value == "home") foreach (var c in Characters) c.RefreshCardInfo();
            if (value == "routines") Routines?.CheckResets();
            if (value == "dreams") Songes?.Strat.RefreshChars();
        }
    }

    /// <summary>Bouton « Mode Mini » du menu : toutes les pages sauf Paramètres.</summary>
    public bool ShowMiniShortcut => _page != "settings";

    private void UpdateNav()
    {
        foreach (var n in NavItems) n.IsActive = n.Key == _page;
    }

    public string Toast
    {
        get => _toast;
        private set
        {
            if (!Set(ref _toast, value)) return;
            Raise(nameof(ToastTitle));
            Raise(nameof(ToastDetail));
        }
    }
    /// <summary>« COPIÉ » quand le message commence par « Copié », sinon vide.</summary>
    public string ToastTitle => (_toast ?? "").StartsWith("Copié") ? "COPIÉ" : "";
    public string ToastDetail => (_toast ?? "").StartsWith("Copié") ? _toast.Substring(5).Trim() : _toast;
    public string UndoText { get => _undoText; private set => Set(ref _undoText, value); }
    public string Status { get => _status; set => Set(ref _status, value); }

    public bool SwapEnabled
    {
        get => Data.SwapEnabled;
        set { if (Data.SwapEnabled != value) { Data.SwapEnabled = value; Raise(); RequestSave(); } }
    }

    public bool MiniAlwaysOnTop
    {
        get => Data.MiniAlwaysOnTop;
        set { if (Data.MiniAlwaysOnTop != value) { Data.MiniAlwaysOnTop = value; Raise(); RequestSave(); MiniSettingsChanged?.Invoke(); } }
    }

    public double MiniOpacity
    {
        get => Data.MiniOpacity;
        set { if (Math.Abs(Data.MiniOpacity - value) > 0.001) { Data.MiniOpacity = value; Raise(); Raise(nameof(MiniOpacityText)); RequestSave(); MiniSettingsChanged?.Invoke(); } }
    }

    public bool AutoBackup
    {
        get => Data.AutoBackup;
        set { if (Data.AutoBackup != value) { Data.AutoBackup = value; Raise(); RequestSave(); } }
    }

    public string LastBackup => string.IsNullOrEmpty(Data.LastBackup) ? "aucune pour l'instant" : Data.LastBackup;

    public string MainCounter => MainDofus.Count(d => d.Done) + "/" + MainDofus.Count;
    public double MainPct => MainDofus.Count == 0 ? 0 : 100.0 * MainDofus.Count(d => d.Done) / MainDofus.Count;
    public string SpecialCounter => SpecialDofus.Count(d => d.Done) + "/" + SpecialDofus.Count;
    public double SpecialPct => SpecialDofus.Count == 0 ? 0 : 100.0 * SpecialDofus.Count(d => d.Done) / SpecialDofus.Count;
    public string SpecialCounterSpaced => SpecialDofus.Count(d => d.Done) + " / " + SpecialDofus.Count;

    private bool _specOpen = true;
    public bool SpecOpen { get => _specOpen; set => Set(ref _specOpen, value); }
    public RelayCommand ToggleSpecialCommand { get; private set; }

    public string ConnectedText => Characters.Count(c => c.Detected) + " / " + Characters.Count + " connectés";
    public string MainCharLabel => (Characters.FirstOrDefault()?.Name ?? "—") + " (principal)";
    public string DataIntactText => string.IsNullOrEmpty(_loadWarning) ? "Fichier de données vérifié au lancement : intact" : _loadWarning;
    public bool DataIntact => string.IsNullOrEmpty(_loadWarning);
    private readonly string _loadWarning;

    public double MiniOpacityPct => Math.Round(Data.MiniOpacity * 100);
    public string MiniOpacityText => Math.Round(Data.MiniOpacity * 100) + " %";

    public bool RememberMiniPosition
    {
        get => Data.RememberMiniPosition;
        set { if (Data.RememberMiniPosition != value) { Data.RememberMiniPosition = value; Raise(); RequestSave(); } }
    }

    public bool MiniCompact
    {
        get => Data.MiniCompact;
        set { if (Data.MiniCompact != value) { Data.MiniCompact = value; Raise(); Raise(nameof(MiniFull)); Raise(nameof(ShowMiniZaap)); Raise(nameof(ShowMiniAlmanax)); Raise(nameof(ShowMiniSeparator)); RequestSave(); MiniSettingsChanged?.Invoke(); } }
    }
    public bool MiniFull => !Data.MiniCompact;

    // Éléments de la barre Mini
    public bool MiniShowSwap { get => Data.MiniShowSwap; set { if (Data.MiniShowSwap != value) { Data.MiniShowSwap = value; Raise(); RequestSave(); } } }
    public bool MiniShowInvites { get => Data.MiniShowInvites; set { if (Data.MiniShowInvites != value) { Data.MiniShowInvites = value; Raise(); RequestSave(); } } }
    public bool MiniShowZaap
    {
        get => Data.MiniShowZaap;
        set { if (Data.MiniShowZaap != value) { Data.MiniShowZaap = value; Raise(); Raise(nameof(ShowMiniZaap)); Raise(nameof(ShowMiniSeparator)); RequestSave(); } }
    }
    public bool MiniShowAlmanax
    {
        get => Data.MiniShowAlmanax;
        set { if (Data.MiniShowAlmanax != value) { Data.MiniShowAlmanax = value; Raise(); Raise(nameof(ShowMiniAlmanax)); Raise(nameof(ShowMiniSeparator)); RequestSave(); } }
    }
    public bool ShowMiniZaap => MiniFull && Data.MiniShowZaap;
    public bool ShowMiniAlmanax => MiniFull && Data.MiniShowAlmanax;
    public bool ShowMiniSeparator => MiniFull && (Data.MiniShowZaap || Data.MiniShowAlmanax);

    /// <summary>Applique le thème choisi et, si elle existe, la couleur d'accent personnalisée.</summary>
    private void ApplyTheme()
    {
        ThemeCatalog.AccentOverride = Data.AccentColor ?? "";
        ThemeCatalog.Apply(Data.Theme);
    }

    public string AccentText => string.IsNullOrEmpty(Data.AccentColor) ? "Couleur d'accent : celle du thème" : "Couleur d'accent personnalisée : " + Data.AccentColor;

    // Copie automatique vers un dossier
    public bool AutoExternalCopy
    {
        get => Data.AutoExternalCopy;
        set
        {
            if (Data.AutoExternalCopy == value) return;
            if (value && (string.IsNullOrEmpty(Data.ExternalBackupDir) || !System.IO.Directory.Exists(Data.ExternalBackupDir)))
            {
                ShowToast("Choisis d'abord un dossier avec « Copier ailleurs »");
                Raise();
                return;
            }
            Data.AutoExternalCopy = value;
            Raise();
            ExternalBackupIfDue();
        }
    }

    public string ExternalCopyText =>
        string.IsNullOrEmpty(Data.ExternalBackupDir) ? "Aucun dossier choisi : utilise d'abord « Copier ailleurs »."
        : "Dossier : " + Data.ExternalBackupDir + " · dernière copie automatique : " + (string.IsNullOrEmpty(Data.LastExternalCopy) ? "aucune" : Data.LastExternalCopy);

    /// <summary>Fait la copie du jour si elle est activée et pas encore faite.</summary>
    public void ExternalBackupIfDue()
    {
        Storage.ExternalDaily(Data);
        Raise(nameof(ExternalCopyText));
        RequestSave();
    }

    // Cartes de la page d'accueil
    public bool HomeShowChars { get => !Data.HomeHideChars; set { if (Data.HomeHideChars == value) { Data.HomeHideChars = !value; Raise(); RequestSave(); } } }
    public bool HomeShowShortcuts { get => !Data.HomeHideShortcuts; set { if (Data.HomeHideShortcuts == value) { Data.HomeHideShortcuts = !value; Raise(); RequestSave(); } } }
    public bool HomeShowInvites { get => !Data.HomeHideInvites; set { if (Data.HomeHideInvites == value) { Data.HomeHideInvites = !value; Raise(); RequestSave(); } } }
    public bool HomeSwapBottom { get => Data.HomeSwapBottom; set { if (Data.HomeSwapBottom != value) { Data.HomeSwapBottom = value; Raise(); RequestSave(); } } }

    public bool QuietMode { get => Data.QuietMode; set { if (Data.QuietMode != value) { Data.QuietMode = value; Raise(); RequestSave(); } } }
    public RelayCommand ToggleCompactCommand { get; private set; }

    public RelayCommand MoveUpCommand { get; private set; }
    public RelayCommand MoveDownCommand { get; private set; }
    public RelayCommand CopyAlmanaxZaapCommand { get; private set; }
    public RelayCommand OpenAlmanaxFromMiniCommand { get; private set; }

    public ObservableCollection<Character> Characters => Data.Characters;

    // ───────── Listes ─────────

    private void BuildLists()
    {
        Favorites.Clear();
        foreach (var coord in Data.FavoriteZaaps)
        {
            var z = ZaapData.All.FirstOrDefault(a => a.Coord == coord);
            if (z != null) Favorites.Add(z);
        }

        MainDofus.Clear();
        SpecialDofus.Clear();
        var main = new (string, string, string)[]
        {
            ("Argenté", "#C9CED6", null), ("Cawotte", "#E8A24C", null), ("Dokoko", "#EDE6D3", null), ("Veilleurs", "#8C9BB5", null),
            ("Émeraude", "#2FBF71", null), ("Pourpre", "#9B4FD0", null), ("Domakuro", "#D9534F", null), ("Dorigami", "#F0EEE8", null),
            ("Turquoise", "#2CC4C4", null), ("DDG", "#8EDBF5", null), ("Ocre", "#D9A22A", null), ("Nébuleux", "#6A4FBF", null),
            ("Abyssal", "#2B5FA8", null), ("Tacheté", "#C98B5A", null), ("Ivoire", "#EFE7CF", null), ("Ébène", "#3A3A44", null),
            ("Vulbis", "#E26FA8", null), ("Forgelave", "#E8602C", null), ("Argenté Scintillant", "#DDE3EE", null), ("Cauchemar", "#5A2E6B", null),
            ("Dom de Pin", "#5E8F3E", null), ("Sylvestre", "#3F8F6B", null), ("DDE", "#6B7383", "3.8"), ("DDE Cristallin", "#7FC8E8", "3.8")
        };
        foreach (var (n, c, f) in main) MainDofus.Add(NewDofus(n, c, false, f));
        var spec = new (string, string)[]
        {
            ("Dolmanax", "#E8B04C"), ("Cacao", "#8B5A3C"), ("Dokille", "#C9503A"), ("Dofawa", "#C4A24D"),
            ("Dofoozbz", "#5BC0DE"), ("Kaliptus", "#4FA36A"), ("Dotruche", "#B9B3A5")
        };
        foreach (var (n, c) in spec) SpecialDofus.Add(NewDofus(n, c, true, null));

        BuildNoteKeys();
        Raise(nameof(MainCounter)); Raise(nameof(MainPct)); Raise(nameof(SpecialCounter)); Raise(nameof(SpecialPct)); Raise(nameof(SpecialCounterSpaced));
    }

    private void BuildNoteKeys()
    {
        NoteKeys.Clear();
        NoteKeys.Add("Commun");
        foreach (var c in Data.Characters.Where(x => x.ShowNotes)) NoteKeys.Add(c.Name);
        if (!NoteKeys.Contains(_selectedNote)) _selectedNote = "Commun";
    }

    private DofusItem NewDofus(string name, string color, bool special, string future)
        => new(name, color, special, future, Data.DofusDone.Contains(name), Data.DofusStarted.Contains(name), OnDofusChanged);

    private void OnDofusChanged(DofusItem item)
    {
        if (item.Done) { if (!Data.DofusDone.Contains(item.Name)) Data.DofusDone.Add(item.Name); }
        else Data.DofusDone.Remove(item.Name);
        if (item.Started) { if (!Data.DofusStarted.Contains(item.Name)) Data.DofusStarted.Add(item.Name); }
        else Data.DofusStarted.Remove(item.Name);
        Raise(nameof(MainCounter)); Raise(nameof(MainPct)); Raise(nameof(SpecialCounter)); Raise(nameof(SpecialPct)); Raise(nameof(SpecialCounterSpaced));
        RequestSave();
    }

    // ───────── Gestion des personnages (renommer, ajouter, supprimer) ─────────

    private static readonly string[] NewCharColors = { "#F2B544", "#7FD1B9", "#E8A07A", "#9DB7F5", "#E57C7C", "#B5C46B", "#C79BF2", "#6FC3DF" };

    /// <summary>À appeler après toute modification de la liste des personnages.</summary>
    private void CharactersChanged()
    {
        UpdateOrder();
        BuildNoteKeys();
        Raise(nameof(SelectedNote));
        Raise(nameof(NoteText));
        Dungeons?.Reload();
        RequestSave();
    }

    /// <summary>Renomme un personnage sans perdre ses cases de succès ni ses notes. Renvoie un message d'erreur, ou "" si tout va bien.</summary>
    public string RenameCharacter(Character c, string newName)
    {
        newName = (newName ?? "").Trim();
        if (c == null) return "";
        if (newName.Length == 0) return "Le nom ne peut pas être vide.";
        if (newName.Length > 40) return "Le nom est trop long (40 caractères au maximum).";
        if (string.Equals(newName, "Commun", StringComparison.OrdinalIgnoreCase)) return "Le nom « Commun » est réservé aux notes communes.";
        var old = c.Name;
        if (newName == old) return "";
        if (Characters.Any(x => x != c && string.Equals(x.Name, newName, StringComparison.OrdinalIgnoreCase)))
            return "Un autre personnage porte déjà ce nom.";

        if (Data.BossAch.Remove(old, out var ach)) Data.BossAch[newName] = ach;
        if (Data.Notes.Remove(old, out var note)) Data.Notes[newName] = note;
        bool noteSelected = _selectedNote == old;
        c.Name = newName;
        BuildNoteKeys();
        if (noteSelected) _selectedNote = newName;
        Raise(nameof(SelectedNote));
        Raise(nameof(NoteText));
        Raise(nameof(MainCharLabel));
        Dungeons?.Reload();
        Routines?.RenameChar(old, newName);
        Songes?.Strat.RenameChar(old, newName);
        Routines?.Refresh();
        RequestSave();
        return "";
    }

    /// <summary>Ajoute un personnage avec un nom libre (« Personnage 2 », etc.) que l'utilisateur renomme ensuite.</summary>
    public Character AddCharacter()
    {
        int n = Characters.Count + 1;
        string name;
        do { name = "Personnage " + n++; } while (Characters.Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)));
        var c = new Character { Name = name, ClassName = "Iop", Color = NewCharColors[Characters.Count % NewCharColors.Length], InSwap = true };
        Characters.Add(c);
        CharactersChanged();
        return c;
    }

    /// <summary>Supprime un personnage (au moins un doit rester), avec « Annuler » : ses succès et ses notes sont restaurés.</summary>
    public bool RemoveCharacter(Character c)
    {
        if (c == null || Characters.Count <= 1) return false;
        int idx = Characters.IndexOf(c);
        var name = c.Name;
        Data.BossAch.Remove(name, out var ach);
        Data.Notes.Remove(name, out var note);
        Characters.Remove(c);
        CharactersChanged();
        ShowUndo("Personnage supprimé : " + name, () =>
        {
            Characters.Insert(Math.Min(idx, Characters.Count), c);
            if (ach != null) Data.BossAch[name] = ach;
            if (note != null) Data.Notes[name] = note;
            CharactersChanged();
            CharactersPage.Reload();
        });
        return true;
    }

    // ───────── Notes ─────────

    private string _noteSearch = "";
    public ObservableCollection<NoteHit> NoteHits { get; } = new();
    public ObservableCollection<TaskItem> Tasks { get; } = new();
    public RelayCommand OpenNoteCommand { get; private set; }
    public RelayCommand AddTaskCommand { get; private set; }

    public string SelectedNote
    {
        get => _selectedNote;
        set { if (value != null && Set(ref _selectedNote, value)) { Raise(nameof(NoteText)); RebuildTasks(); } }
    }

    public string NoteText
    {
        get => Data.Notes.TryGetValue(_selectedNote, out var t) ? t : "";
        set
        {
            Data.Notes[_selectedNote] = value ?? "";
            Raise();
            RebuildTasks();
            RequestSave();
        }
    }

    public string NoteSearch
    {
        get => _noteSearch;
        set { if (Set(ref _noteSearch, value ?? "")) RebuildHits(); }
    }

    public bool HasNoteHits => NoteHits.Count > 0;
    public bool SearchActive => _noteSearch.Trim().Length > 0;
    public string TasksTitle => "Tâches · " + Tasks.Count(t => t.Done) + "/" + Tasks.Count + " faites";
    public bool HasTasks => Tasks.Count > 0;

    private static readonly Regex TaskLine = new(@"^\s*[-*]\s*\[( |x|X)\]\s?(.*)$");

    private void RebuildTasks()
    {
        Tasks.Clear();
        var lines = NoteText.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var m = TaskLine.Match(lines[i]);
            if (m.Success) Tasks.Add(new TaskItem(i, m.Groups[2].Value, m.Groups[1].Value != " ", OnTaskToggled));
        }
        Raise(nameof(TasksTitle));
        Raise(nameof(HasTasks));
    }

    private void OnTaskToggled(TaskItem t)
    {
        var lines = NoteText.Replace("\r\n", "\n").Split('\n');
        if (t.Line < 0 || t.Line >= lines.Length) return;
        lines[t.Line] = Regex.Replace(lines[t.Line], @"\[( |x|X)\]", t.Done ? "[x]" : "[ ]", RegexOptions.None);
        NoteText = string.Join("\n", lines);
    }

    private void AddTask()
    {
        var text = NoteText;
        NoteText = (text.Length > 0 && !text.EndsWith("\n") ? text + "\n" : text) + "- [ ] Nouvelle tâche";
    }

    private void RebuildHits()
    {
        NoteHits.Clear();
        var q = _noteSearch.Trim();
        if (q.Length > 0)
        {
            foreach (var key in NoteKeys)
            {
                if (!Data.Notes.TryGetValue(key, out var text) || string.IsNullOrEmpty(text)) continue;
                int idx = text.IndexOf(q, StringComparison.CurrentCultureIgnoreCase);
                if (idx < 0) continue;
                int start = Math.Max(0, idx - 24);
                var snip = text.Substring(start, Math.Min(text.Length - start, q.Length + 60)).Replace("\r", " ").Replace("\n", " ");
                NoteHits.Add(new NoteHit { Key = key, Snippet = (start > 0 ? "…" : "") + snip });
            }
        }
        Raise(nameof(HasNoteHits));
        Raise(nameof(SearchActive));
    }

    private void OpenNote(NoteHit h)
    {
        if (h == null) return;
        SelectedNote = h.Key;
        NoteSearch = "";
    }

    // ───────── Sauvegarde ─────────

    public void RequestSave() { _saveTimer.Stop(); _saveTimer.Start(); }

    public void SaveNow()
    {
        try
        {
            Storage.Save(Data);
            if (SaveFailed) { SaveFailed = false; Status = ""; Raise(nameof(SaveFailed)); }
        }
        catch (Exception ex)
        {
            SaveFailed = true;
            Raise(nameof(SaveFailed));
            Status = "Impossible d'enregistrer tes données (disque plein ou dossier verrouillé ?). Tes dernières modifications ne sont pas sauvegardées.";
            Storage.LogError(ex.ToString());
        }
    }

    public void ShowToast(string text)
    {
        Toast = text;
        _toastTimer.Stop();
        _toastTimer.Start();
    }

    /// <summary>Affiche « Annuler » pendant 4 s ; le clic exécute l'action d'annulation.</summary>
    public void ShowUndo(string text, Action undo)
    {
        _undoAction = undo;
        UndoText = text;
        _undoTimer.Stop();
        _undoTimer.Start();
    }

    private void RunUndo()
    {
        var a = _undoAction;
        _undoTimer.Stop();
        UndoText = "";
        _undoAction = null;
        a?.Invoke();
    }

    private void Export()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            FileName = "DofusTeamOrganizer-" + DateTime.Now.ToString("yyyyMMdd") + ".json",
            Filter = "Données (*.json)|*.json"
        };
        if (dlg.ShowDialog() != true) return;
        try { Storage.ExportTo(dlg.FileName, Data); ShowToast("Données exportées"); }
        catch (Exception ex) { Status = "Export impossible : " + ex.Message; }
    }

    private void CopyBackup()
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog { Title = "Choisir le dossier où copier la sauvegarde" };
        if (!string.IsNullOrEmpty(Data.ExternalBackupDir) && System.IO.Directory.Exists(Data.ExternalBackupDir)) dlg.InitialDirectory = Data.ExternalBackupDir;
        if (dlg.ShowDialog() != true) return;
        try
        {
            Storage.CopyTo(dlg.FolderName, Data);
            Data.ExternalBackupDir = dlg.FolderName;
            Raise(nameof(ExternalCopyText));
            RequestSave();
            ShowToast("Sauvegarde copiée");
        }
        catch (Exception ex) { Status = "Copie impossible : " + ex.Message; }
    }

    private void Import()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Données (*.json)|*.json" };
        if (dlg.ShowDialog() != true) return;
        var d = Storage.Import(dlg.FileName);
        if (d == null) { Status = "Ce fichier n'est pas un export valide : rien n'a été modifié."; return; }
        ApplyLoadedData(d, "Données importées");
    }

    /// <summary>Remplace toutes les données par celles de <paramref name="d"/> et rafraîchit tous les écrans.</summary>
    private void ApplyLoadedData(AppData d, string toast)
    {
        Data = d;
        foreach (var p in new[] { nameof(Data), nameof(SwapEnabled), nameof(MiniAlwaysOnTop), nameof(MiniOpacity), nameof(AutoBackup),
                                  nameof(LastBackup), nameof(NoteText), nameof(MainCounter), nameof(SpecialCounter), nameof(Characters),
                                  nameof(RememberMiniPosition), nameof(MiniCompact), nameof(MiniFull), nameof(MiniOpacityText), nameof(ScaleChoice),
                                  nameof(MiniShowSwap), nameof(MiniShowInvites), nameof(MiniShowZaap), nameof(MiniShowAlmanax), nameof(ShowMiniZaap), nameof(ShowMiniAlmanax), nameof(ShowMiniSeparator), nameof(QuietMode),
                                  nameof(AccentText), nameof(AutoExternalCopy), nameof(ExternalCopyText), nameof(HomeShowChars), nameof(HomeShowShortcuts), nameof(HomeShowInvites), nameof(HomeSwapBottom) })
            Raise(p);
        BuildThemeRows();
        ApplyTheme();
        BuildLists();
        UpdateOrder();
        CharactersPage.Reload();
        Dungeons.Reload();
        Songes.Reload();
        Almanax.Reload();
        Routines.Reload();
        Hotkeys.Apply(GetShortcut);
        MiniSettingsChanged?.Invoke();
        UiScaleChanged?.Invoke();
        ShowToast(toast);
    }

    public event Action WelcomeRequested;

    /// <summary>Remet l'appli comme une nouvelle installation. Une copie des données actuelles est gardée dans le dossier Backups.</summary>
    private void ResetAll()
    {
        var r1 = System.Windows.MessageBox.Show(
            "Toutes tes données seront effacées : personnages, succès, tâches, notes, songes, raccourcis, thème et réglages.\n\nL'appli redeviendra comme une nouvelle installation.\nUne copie de sécurité est gardée, tu pourras la restaurer avec « Importer une sauvegarde ».\n\nContinuer ?",
            "Tout réinitialiser", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning, System.Windows.MessageBoxResult.No);
        if (r1 != System.Windows.MessageBoxResult.Yes) return;
        var r2 = System.Windows.MessageBox.Show("Dernière confirmation : tout effacer maintenant ?", "Tout réinitialiser",
            System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning, System.Windows.MessageBoxResult.No);
        if (r2 != System.Windows.MessageBoxResult.Yes) return;
        try
        {
            Storage.Save(Data);
            var copy = System.IO.Path.Combine(Storage.BackupDir, "data-avant-reinitialisation-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json");
            System.IO.Directory.CreateDirectory(Storage.BackupDir);
            System.IO.File.Copy(Storage.FilePath, copy, true);
        }
        catch (Exception ex)
        {
            Status = "Réinitialisation annulée : la copie de sécurité a échoué (" + ex.Message + ")";
            return;
        }
        var fresh = new AppData { LastSeenVersion = PatchNotes.Current };
        Storage.Save(fresh);
        try { if (AutoStart.IsEnabled()) AutoStart.Set(false); } catch { }
        ApplyLoadedData(fresh, "Application réinitialisée");
        Raise(nameof(StartWithWindows));
        WelcomeRequested?.Invoke();
    }

    // ───────── Notes de version ─────────

    /// <summary>Premier lancement : données par défaut intactes (un seul personnage d'origine) et écran pas encore vu.</summary>
    public bool ShouldShowWelcome =>
        !Data.WelcomeDone && Characters.Count == 1 && Characters[0].Name == "Personnage 1";

    public void MarkWelcomeDone()
    {
        Data.WelcomeDone = true;
        RequestSave();
    }

    /// <summary>Applique les personnages saisis à l'écran de bienvenue : le premier remplace « Personnage 1 ».</summary>
    public void ApplyWelcome(IList<(string Name, string ClassName)> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            var c = i == 0 ? Characters[0] : AddCharacter();
            c.Name = list[i].Name;
            c.ClassName = list[i].ClassName;
        }
        CharactersChanged();
        RequestSave();
    }

    public bool ShouldShowPatchNotes => Data.LastSeenVersion != PatchNotes.Current;

    public void MarkPatchNotesSeen()
    {
        Data.LastSeenVersion = PatchNotes.Current;
        RequestSave();
    }

    // ───────── Presse-papiers ─────────

    public bool CopyText(string text)
    {
        for (int i = 0; i < 5; i++)
        {
            try { Clipboard.SetDataObject(text, true); return true; }
            catch (COMException) { Thread.Sleep(40); }
        }
        return false;
    }

    public void CopyWithToast(string text, string message)
        => ShowToast(CopyText(text) ? message : "Copie impossible (presse-papiers occupé)");

    public void CopyZaap(ZaapEntry z)
    {
        if (z == null) return;
        ShowToast(CopyText(z.Command) ? "Copié  " + z.Command : "Copie impossible (presse-papiers occupé)");
    }

    public void CopyInvites()
    {
        var names = Characters.Select(c => c.Name).ToList();
        var active = DofusWindows.ActiveCharacter(names);
        if (active == "") active = _lastCharacter;
        if (active == "") active = Characters.FirstOrDefault()?.Name ?? "";
        _lastCharacter = active;

        var found = DofusWindows.FindAll(names);
        var others = names.Where(n => n != active && found[n] != IntPtr.Zero).ToList();
        if (others.Count == 0) { ShowToast("Aucun autre personnage à inviter"); return; }

        var cmd = string.Join(" ; ", others.Select(n => "/invite " + n));
        ShowToast(CopyText(cmd) ? "Copié  " + cmd : "Copie impossible (presse-papiers occupé)");
    }

    // ───────── DOFUS : détection, raccourcis ─────────

    private void Poll()
    {
        try
        {
            var found = DofusWindows.FindAll(Characters.Select(c => c.Name));
            foreach (var c in Characters)
                c.Detected = found.TryGetValue(c.Name, out var h) && h != IntPtr.Zero;
            Raise(nameof(ConnectedText));
            if (_page == "settings") Raise(nameof(MemoryText));

            Hotkeys.SetContextual(DofusWindows.IsForegroundDofus(), Data.SwapEnabled);

            var active = DofusWindows.ActiveCharacter(Characters.Select(c => c.Name));
            if (active != "") _lastCharacter = active;
        }
        catch (Exception ex) { Storage.LogError("Poll : " + ex); }
    }

    private void OnHotkey(int id)
    {
        switch (id)
        {
            case HotkeyService.ToggleId: ToggleVisibilityRequested?.Invoke(); break;
            case HotkeyService.InvitesId: CopyInvites(); break;
            case HotkeyService.MainCharId:
                var first = Characters.FirstOrDefault();
                if (first != null) Activate(first.Name);
                break;
            case HotkeyService.NextId:
            case HotkeyService.NextAltId: Cycle(+1); break;
            case HotkeyService.PrevId: Cycle(-1); break;
        }
    }

    private void Activate(string name)
    {
        if (DofusWindows.Activate(DofusWindows.Find(name))) _lastCharacter = name;
    }

    private void Cycle(int dir)
    {
        if (!Data.SwapEnabled) return;
        var team = Characters.Where(c => c.InSwap).Select(c => c.Name).ToList();
        if (team.Count == 0) return;

        var found = DofusWindows.FindAll(team);
        var active = DofusWindows.ActiveCharacter(team);
        int index = team.IndexOf(active);
        if (index < 0) index = dir > 0 ? -1 : 0;

        for (int i = 0; i < team.Count; i++)
        {
            index = (index + dir + team.Count) % team.Count;
            var h = found[team[index]];
            if (h != IntPtr.Zero)
            {
                DofusWindows.Activate(h);
                _lastCharacter = team[index];
                return;
            }
        }
    }
}
