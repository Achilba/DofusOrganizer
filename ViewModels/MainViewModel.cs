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
    public event Action PatchNotesRequested;

    public ObservableCollection<ZaapEntry> Favorites { get; } = new();
    public ObservableCollection<DofusItem> MainDofus { get; } = new();
    public ObservableCollection<DofusItem> SpecialDofus { get; } = new();
    public ObservableCollection<string> NoteKeys { get; } = new();
    public ObservableCollection<NavItem> NavItems { get; } = new();

    public CharactersViewModel CharactersPage { get; private set; }
    public DungeonsViewModel Dungeons { get; private set; }
    public SongesViewModel Songes { get; private set; }
    public AlmanaxViewModel Almanax { get; private set; }

    public RelayCommand NavigateCommand { get; }
    public RelayCommand CopyInvitesCommand { get; }
    public RelayCommand CopyZaapCommand { get; }
    public RelayCommand ShowMiniCommand { get; }
    public RelayCommand ShowMainCommand { get; }
    public RelayCommand ExportCommand { get; }
    public RelayCommand ImportCommand { get; }
    public RelayCommand OpenFolderCommand { get; }
    public RelayCommand UndoCommand { get; }
    public RelayCommand CopyErrorLogCommand { get; }
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
        foreach (var n in NavItems) if (n.Key == "settings") n.Badge = UpdateAvailable;
    }
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
                if (silent && _notifiedVersion != _update.Version)
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

        foreach (var (k, l, ico) in new[] { ("home", "Accueil", "IcoHome"), ("chars", "Personnages", "IcoChars"), ("dofus", "Dofus", "IcoDofus"), ("almanax", "Almanax", "IcoCalendar"),
                                            ("dungeons", "Donjons", "IcoDungeon"), ("dreams", "Songes", "IcoMoon"),
                                            ("notes", "Notes", "IcoNotes"), ("settings", "Paramètres", "IcoSettings") })
            NavItems.Add(new NavItem(k, l, Application.Current.TryFindResource(ico) as Geometry));
        UpdateNav();

        NavigateCommand = new RelayCommand(p => Page = (string)p);
        CopyInvitesCommand = new RelayCommand(CopyInvites);
        CopyZaapCommand = new RelayCommand(p => CopyZaap((ZaapEntry)p));
        ShowMiniCommand = new RelayCommand(() => MiniModeRequested?.Invoke(true));
        ShowMainCommand = new RelayCommand(() => MiniModeRequested?.Invoke(false));
        ExportCommand = new RelayCommand(Export);
        ImportCommand = new RelayCommand(Import);
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
        CopyErrorLogCommand = new RelayCommand(() =>
        {
            var text = File.Exists(Storage.ErrorLogPath) ? File.ReadAllText(Storage.ErrorLogPath) : "";
            if (text.Length == 0) ShowToast("Aucune erreur enregistrée");
            else ShowToast(CopyText(text) ? "Journal copié" : "Copie impossible");
        });

        TrackedObject.Changed += RequestSave;

        BuildLists();
        UpdateOrder();
        CharactersPage = new CharactersViewModel(this);
        Dungeons = new DungeonsViewModel(this);
        Songes = new SongesViewModel(this);
        Almanax = new AlmanaxViewModel(this);
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

    // ───────── Navigation / état ─────────

    public string Page
    {
        get => _page;
        set { if (Set(ref _page, value)) UpdateNav(); }
    }

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
        set { if (Data.MiniCompact != value) { Data.MiniCompact = value; Raise(); Raise(nameof(MiniFull)); RequestSave(); MiniSettingsChanged?.Invoke(); } }
    }
    public bool MiniFull => !Data.MiniCompact;
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
        foreach (var c in Data.Characters) NoteKeys.Add(c.Name);
        if (!NoteKeys.Contains(_selectedNote)) _selectedNote = "Commun";
    }

    private DofusItem NewDofus(string name, string color, bool special, string future)
        => new(name, color, special, future, Data.DofusDone.Contains(name), OnDofusChanged);

    private void OnDofusChanged(DofusItem item)
    {
        if (item.Done) { if (!Data.DofusDone.Contains(item.Name)) Data.DofusDone.Add(item.Name); }
        else Data.DofusDone.Remove(item.Name);
        Raise(nameof(MainCounter)); Raise(nameof(MainPct)); Raise(nameof(SpecialCounter)); Raise(nameof(SpecialPct)); Raise(nameof(SpecialCounterSpaced));
        RequestSave();
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

    private void Import()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Données (*.json)|*.json" };
        if (dlg.ShowDialog() != true) return;
        var d = Storage.Import(dlg.FileName);
        if (d == null) { Status = "Ce fichier n'est pas un export valide : rien n'a été modifié."; return; }
        Data = d;
        foreach (var p in new[] { nameof(Data), nameof(SwapEnabled), nameof(MiniAlwaysOnTop), nameof(MiniOpacity), nameof(AutoBackup),
                                  nameof(LastBackup), nameof(NoteText), nameof(MainCounter), nameof(SpecialCounter), nameof(Characters),
                                  nameof(RememberMiniPosition), nameof(MiniCompact), nameof(MiniFull), nameof(MiniOpacityText) })
            Raise(p);
        BuildLists();
        UpdateOrder();
        CharactersPage.Reload();
        Dungeons.Reload();
        Songes.Reload();
        Almanax.Reload();
        MiniSettingsChanged?.Invoke();
        ShowToast("Données importées");
    }

    // ───────── Notes de version ─────────

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
