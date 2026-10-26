using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using DofusTeamOrganizer.Models;

namespace DofusTeamOrganizer.ViewModels;

/// <summary>Une ligne « - [ ] étape » du texte d'un run, affichée comme une case à cocher.</summary>
public class CheckItem : ObservableObject
{
    private bool _checked;
    private readonly Action<CheckItem, bool> _toggled;

    public CheckItem(int line, string label, bool isChecked, Action<CheckItem, bool> toggled)
    {
        Line = line; Label = label; _checked = isChecked; _toggled = toggled;
    }

    public int Line { get; }
    public string Label { get; }
    public bool IsChecked
    {
        get => _checked;
        set { if (Set(ref _checked, value)) _toggled?.Invoke(this, value); }
    }
}

/// <summary>Contenu d'un fichier de run partagé (bouton Exporter / Importer).</summary>
public class RunFile
{
    public string Name { get; set; } = "";
    public string Text { get; set; } = "";
    public List<RunFileSlot> Slots { get; set; } = new();
}

public class RunFileSlot
{
    public string Char { get; set; } = "";
    public string Stuff { get; set; } = "";
}

/// <summary>Vue « Strat » de la page Songes : des runs types avec équipe (ordre d'initiative), stuffs, texte libre et compteur.</summary>
public class StratViewModel : ObservableObject
{
    private static readonly Regex CheckRe = new(@"^- \[( |x|X)\] (.*)$");

    private readonly MainViewModel _main;
    private StratRun _selected;
    private bool _reading;
    private bool _templatesOpen;
    private bool _buildingChecks;
    private bool _toggling;

    public ObservableCollection<string> CharNames { get; } = new();
    public ObservableCollection<CheckItem> Checks { get; } = new();
    public RelayCommand ToggleTemplatesCommand { get; }
    public RelayCommand AddRunCommand { get; }
    public RelayCommand DeleteRunCommand { get; }
    public RelayCommand DuplicateRunCommand { get; }
    public RelayCommand CopyRunCommand { get; }
    public RelayCommand ExportRunCommand { get; }
    public RelayCommand ImportRunCommand { get; }
    public RelayCommand ToggleReadingCommand { get; }
    public RelayCommand IncCommand { get; }
    public RelayCommand DecCommand { get; }
    public RelayCommand AddSlotCommand { get; }
    public RelayCommand RemoveSlotCommand { get; }
    public RelayCommand OpenStuffCommand { get; }

    public StratViewModel(MainViewModel main)
    {
        _main = main;
        StratSlot.Resolver = name => _main.Characters.FirstOrDefault(c => c.Name == name)?.Stuffs;
        StratSlot.ClassResolver = name => _main.Characters.FirstOrDefault(c => c.Name == name)?.ClassName;
        ToggleTemplatesCommand = new RelayCommand(() => { TemplatesOpen = !TemplatesOpen; });
        AddRunCommand = new RelayCommand(p => AddRun(p as string));
        DeleteRunCommand = new RelayCommand(DeleteRun);
        DuplicateRunCommand = new RelayCommand(DuplicateRun);
        CopyRunCommand = new RelayCommand(() => { if (_selected != null) _main.CopyWithToast(_selected.Summary(), "Strat copiée"); });
        ExportRunCommand = new RelayCommand(ExportRun);
        ImportRunCommand = new RelayCommand(ImportRun);
        ToggleReadingCommand = new RelayCommand(() => { Reading = !Reading; });
        IncCommand = new RelayCommand(() => Bump(+1));
        DecCommand = new RelayCommand(() => Bump(-1));
        AddSlotCommand = new RelayCommand(AddSlot);
        RemoveSlotCommand = new RelayCommand(p => { if (p is StratSlot s && _selected != null) { _selected.Slots.Remove(s); _main.RequestSave(); } });
        OpenStuffCommand = new RelayCommand(p =>
        {
            if (p is StratSlot s && s.HasUrl)
                try { Process.Start(new ProcessStartInfo(s.Url) { UseShellExecute = true }); } catch { }
        });
        _main.Characters.CollectionChanged += (_, _) => RefreshChars();
        Reload();
    }

    private AppData Data => _main.Data;
    public ObservableCollection<StratRun> Runs => Data.StratRuns;

    public void Reload()
    {
        RefreshChars();
        Hook(Runs.FirstOrDefault(r => r.Id == Data.StratSelected) ?? Runs.FirstOrDefault());
        Raise(nameof(Runs));
        RaiseSelection();
    }

    private void Hook(StratRun run)
    {
        if (_selected != null) _selected.PropertyChanged -= OnRunChanged;
        _selected = run;
        if (_selected != null) _selected.PropertyChanged += OnRunChanged;
        RebuildChecks();
    }

    private void OnRunChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(StratRun.Text) && !_toggling) RebuildChecks();
        if (e.PropertyName == nameof(StratRun.Count)) RaiseTimes();
    }

    private void RaiseSelection()
    {
        Raise(nameof(Selected)); Raise(nameof(HasSelected)); Raise(nameof(NoSelection));
        Raise(nameof(ActiveRunLine)); RaiseTimes();
    }

    /// <summary>À appeler quand la liste des personnages ou leurs stuffs ont pu changer.</summary>
    public void RefreshChars()
    {
        var names = _main.Characters.Select(c => c.Name).ToList();
        if (!CharNames.SequenceEqual(names))
        {
            CharNames.Clear();
            foreach (var n in names) CharNames.Add(n);
        }
        foreach (var r in Runs) foreach (var s in r.Slots) s.RefreshChoices();
    }

    /// <summary>Renommage d'un personnage : les lignes d'équipe le suivent.</summary>
    public void RenameChar(string oldName, string newName)
    {
        foreach (var r in Runs)
            foreach (var s in r.Slots)
                if (s.Char == oldName) { var keep = s.Stuff; s.Char = newName; s.Stuff = keep; }
        RefreshChars();
    }

    public StratRun Selected
    {
        get => _selected;
        set
        {
            if (value == _selected) return;
            Hook(value);
            Data.StratSelected = value?.Id ?? "";
            _main.RequestSave();
            RaiseSelection();
            RefreshChars();
        }
    }
    public bool HasSelected => _selected != null;
    public bool NoSelection => _selected == null;

    // ───────── Mode lecture, modèles ─────────

    public bool Reading
    {
        get => _reading;
        set { if (Set(ref _reading, value)) { Raise(nameof(IsEditing)); Raise(nameof(ReadLabel)); Raise(nameof(ShowEditChecks)); } }
    }
    public bool IsEditing => !_reading;
    public string ReadLabel => _reading ? "Modifier" : "Mode lecture";

    public bool TemplatesOpen { get => _templatesOpen; set => Set(ref _templatesOpen, value); }

    // ───────── Compteur de runs ─────────

    public string TodayText => "Aujourd'hui : " + TodayCount;
    public string TodayCountText => TodayCount.ToString();
    public string RunMeta => TodayText + " · dernier run " + (_selected?.LastRun == null ? "jamais" : Ago(_selected.LastRun.Value));
    /// <summary>Le run actif, tel qu'il compte pour les drops : « Paradoxe 1 Salle, n° 12 ».</summary>
    public string ActiveRunLine => _selected == null ? "aucun run" : _selected.Name + ", n° " + _selected.Count;

    public int TodayCount
    {
        get { EnsureDay(); return Data.RunsToday; }
    }

    private void EnsureDay()
    {
        var d = DateTime.Today.ToString("yyyy-MM-dd");
        if (Data.RunsDay == d) return;
        Data.RunsDay = d;
        Data.RunsToday = 0;
    }

    public void RaiseTimes()
    {
        Raise(nameof(TodayText)); Raise(nameof(TodayCountText)); Raise(nameof(RunMeta)); Raise(nameof(ActiveRunLine));
    }

    private void Bump(int delta)
    {
        var r = _selected;
        if (r == null) return;
        if (delta < 0 && r.Count == 0) return;
        EnsureDay();
        r.Count += delta;
        Data.RunsToday = Math.Max(0, Data.RunsToday + delta);
        if (delta > 0) r.LastRun = DateTime.Now;
        _main.RequestSave();
        RaiseTimes();
    }

    /// <summary>Durée écoulée, sans « il y a » : « à l'instant », « 21 min », « 2 h 14 », « 3 j ».</summary>
    public static string Elapsed(DateTime t)
    {
        int m = (int)Math.Max(0, (DateTime.Now - t).TotalMinutes);
        if (m < 1) return "à l'instant";
        if (m < 60) return m + " min";
        int h = m / 60;
        if (h < 24) return h + " h " + (m % 60).ToString("00");
        return (h / 24) + " j";
    }

    public static string Ago(DateTime t)
    {
        var e = Elapsed(t);
        return e == "à l'instant" ? e : "il y a " + e;
    }

    // ───────── Cases à cocher dans le texte ─────────

    public bool HasChecks => Checks.Count > 0;
    public bool ShowEditChecks => Checks.Count > 0 && !_reading;

    /// <summary>Texte du run sans les lignes à cocher (affiché en mode lecture).</summary>
    public string ReadText
    {
        get
        {
            if (_selected == null) return "";
            var keep = _selected.Text.Split('\n').Where(l => !CheckRe.IsMatch(l.TrimEnd('\r'))).Select(l => l.TrimEnd('\r'));
            return string.Join(Environment.NewLine, keep).Trim();
        }
    }

    private void RebuildChecks()
    {
        if (_buildingChecks) return;
        _buildingChecks = true;
        Checks.Clear();
        if (_selected != null)
        {
            var lines = _selected.Text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var m = CheckRe.Match(lines[i].TrimEnd('\r'));
                if (m.Success) Checks.Add(new CheckItem(i, m.Groups[2].Value, m.Groups[1].Value != " ", OnCheckToggled));
            }
        }
        _buildingChecks = false;
        Raise(nameof(HasChecks));
        Raise(nameof(ShowEditChecks));
        Raise(nameof(ReadText));
    }

    private void OnCheckToggled(CheckItem it, bool value)
    {
        if (_buildingChecks || _selected == null) return;
        var lines = _selected.Text.Split('\n');
        if (it.Line < 0 || it.Line >= lines.Length) return;
        bool cr = lines[it.Line].EndsWith("\r");
        lines[it.Line] = (value ? "- [x] " : "- [ ] ") + it.Label + (cr ? "\r" : "");
        _toggling = true;
        _selected.Text = string.Join("\n", lines);
        _toggling = false;
    }

    // ───────── Runs ─────────

    private string UniqueName(string baseName)
    {
        if (Runs.All(r => r.Name != baseName)) return baseName;
        int n = 2;
        while (Runs.Any(r => r.Name == baseName + " " + n)) n++;
        return baseName + " " + n;
    }

    private void AddRun(string template)
    {
        string name;
        bool team = true;
        switch (template)
        {
            case "salle": name = UniqueName("Paradoxe 1 Salle"); break;
            case "boss": name = UniqueName("Paradoxe 1 Boss"); break;
            default:
                name = UniqueName("Nouveau run");
                team = false;
                break;
        }
        var run = new StratRun { Name = name };
        if (team && CharNames.Count > 0) run.Slots.Add(new StratSlot { Char = CharNames[0] });
        Runs.Add(run);
        TemplatesOpen = false;
        Selected = run;
        _main.RequestSave();
    }

    private void DeleteRun()
    {
        var run = _selected;
        if (run == null) return;
        int idx = Runs.IndexOf(run);
        Runs.Remove(run);
        Selected = Runs.Count > 0 ? Runs[Math.Min(idx, Runs.Count - 1)] : null;
        _main.RequestSave();
        _main.ShowUndo("Run supprimé : " + run.Name, () =>
        {
            Runs.Insert(Math.Min(idx, Runs.Count), run);
            Selected = run;
            _main.RequestSave();
        });
    }

    private void DuplicateRun()
    {
        var src = _selected;
        if (src == null) return;
        var run = new StratRun { Name = UniqueName(src.Name + " (copie)"), Text = src.Text };
        foreach (var s in src.Slots)
        {
            var c = new StratSlot { Char = s.Char };
            c.Stuff = s.Stuff;
            run.Slots.Add(c);
        }
        Runs.Insert(Runs.IndexOf(src) + 1, run);
        Selected = run;
        _main.RequestSave();
        _main.ShowToast("Run dupliqué");
    }

    private void ExportRun()
    {
        var r = _selected;
        if (r == null) return;
        var file = new RunFile { Name = r.Name, Text = r.Text };
        foreach (var s in r.Slots) file.Slots.Add(new RunFileSlot { Char = s.Char, Stuff = s.Stuff });
        var safe = new string(r.Name.Select(ch => Array.IndexOf(System.IO.Path.GetInvalidFileNameChars(), ch) >= 0 ? '-' : ch).ToArray());
        var dlg = new Microsoft.Win32.SaveFileDialog { FileName = "Run - " + safe + ".json", Filter = "Run Dofus (*.json)|*.json" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            System.IO.File.WriteAllText(dlg.FileName, JsonSerializer.Serialize(file, new JsonSerializerOptions { WriteIndented = true }));
            _main.ShowToast("Run exporté");
        }
        catch (Exception ex) { _main.ShowToast("Export impossible : " + ex.Message); }
    }

    private void ImportRun()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Run Dofus (*.json)|*.json" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var file = JsonSerializer.Deserialize<RunFile>(System.IO.File.ReadAllText(dlg.FileName));
            if (file == null || string.IsNullOrWhiteSpace(file.Name)) { _main.ShowToast("Ce fichier n'est pas un run valide"); return; }
            var run = new StratRun { Name = UniqueName(file.Name), Text = file.Text ?? "" };
            foreach (var s in (file.Slots ?? new()).Take(StratRun.MaxSlots))
            {
                var c = new StratSlot { Char = s.Char ?? "" };
                c.Stuff = s.Stuff ?? "";
                run.Slots.Add(c);
            }
            Runs.Add(run);
            Selected = run;
            _main.RequestSave();
            _main.ShowToast("Run importé");
        }
        catch { _main.ShowToast("Ce fichier n'est pas un run valide"); }
    }

    private void AddSlot()
    {
        if (_selected == null || !_selected.CanAddSlot) return;
        var free = CharNames.FirstOrDefault(n => _selected.Slots.All(s => s.Char != n)) ?? CharNames.FirstOrDefault() ?? "";
        _selected.Slots.Add(new StratSlot { Char = free });
        _main.RequestSave();
    }
}
