using System.Collections.ObjectModel;
using System.Windows.Threading;
using DofusTeamOrganizer.Models;

namespace DofusTeamOrganizer.ViewModels;

/// <summary>Une case à cocher : une tâche pour un personnage.</summary>
public class RoutineCell : ObservableObject
{
    private readonly RoutineViewModel _vm;
    private readonly string _taskId;
    public string CharName { get; }
    public RoutineCell(RoutineViewModel vm, string taskId, string charName) { _vm = vm; _taskId = taskId; CharName = charName; }

    public bool Done
    {
        get => _vm.IsDone(_taskId, CharName);
        set { _vm.SetDone(_taskId, CharName, value); Raise(); }
    }
}

/// <summary>Une ligne du tableau : une tâche et ses cases (une par personnage).</summary>
public class RoutineRow : ObservableObject
{
    private readonly RoutineViewModel _vm;
    public RoutineDef Def { get; }
    public ObservableCollection<RoutineCell> Cells { get; } = new();
    public RelayCommand DeleteCommand { get; }

    public RoutineRow(RoutineViewModel vm, RoutineDef def, IEnumerable<string> chars)
    {
        _vm = vm; Def = def;
        foreach (var c in chars) Cells.Add(new RoutineCell(vm, def.Id, c));
        DeleteCommand = new RelayCommand(() => vm.Delete(def));
    }

    public string Name
    {
        get => Def.Name;
        set
        {
            var v = (value ?? "").Trim();
            if (v.Length == 0 || v == Def.Name) { Raise(); return; }
            Def.Name = v.Length > 40 ? v.Substring(0, 40) : v;
            _vm.Changed();
            Raise();
        }
    }

    public string ResetText => RoutineViewModel.DescribeReset(Def);
}

public class RoutineViewModel : ObservableObject
{
    public const int WeeklyResetHour = 3;
    private static readonly string[] DaysPlural = { "dimanches", "lundis", "mardis", "mercredis", "jeudis", "vendredis", "samedis" };
    public static readonly string[] DayChoices = { "Lundi", "Mardi", "Mercredi", "Jeudi", "Vendredi", "Samedi", "Dimanche" };
    public static readonly string[] FreqChoices = { "Tous les jours", "Toutes les semaines" };

    private readonly MainViewModel _main;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(30) };
    private string _newName = "";
    private int _newFreq;
    private int _newDay = 1;
    private string _error = "";

    public ObservableCollection<string> CharNames { get; } = new();
    public ObservableCollection<RoutineRow> DailyRows { get; } = new();
    public ObservableCollection<RoutineRow> WeeklyRows { get; } = new();
    public RelayCommand AddCommand { get; }

    public RoutineViewModel(MainViewModel main)
    {
        _main = main;
        AddCommand = new RelayCommand(Add);
        _main.Characters.CollectionChanged += (_, _) => Refresh();
        _timer.Tick += (_, _) => { CheckResets(); Raise(nameof(NextText)); };
        _timer.Start();
        Reload();
    }

    private AppData Data => _main.Data;

    /// <summary>Au démarrage et après un import : applique les remises à zéro puis reconstruit le tableau.</summary>
    public void Reload()
    {
        CheckResets();
        Refresh();
    }

    // ───────── Périodes et remises à zéro ─────────

    /// <summary>Identifiant de la période en cours : la date du jour, ou la date de la dernière remise à zéro hebdomadaire.</summary>
    public static string PeriodKey(RoutineDef d, DateTime now)
    {
        if (!d.Weekly) return now.ToString("yyyy-MM-dd");
        return LastWeeklyReset(d.Weekday, now).ToString("yyyy-MM-dd");
    }

    private static DateTime LastWeeklyReset(int weekday, DateTime now)
    {
        int back = ((int)now.DayOfWeek - weekday + 7) % 7;
        var r = now.Date.AddDays(-back).AddHours(WeeklyResetHour);
        if (r > now) r = r.AddDays(-7);
        return r;
    }

    private static DateTime NextWeeklyReset(int weekday, DateTime now) => LastWeeklyReset(weekday, now).AddDays(7);

    public static string DescribeReset(RoutineDef d)
        => d.Weekly ? "Remise à zéro le " + DaysPlural[Math.Abs(d.Weekday) % 7].TrimEnd('s') + " à " + WeeklyResetHour + " h"
                    : "Remise à zéro tous les jours à minuit";

    public void CheckResets()
    {
        var now = DateTime.Now;
        bool changed = false;
        foreach (var d in Data.Routines.ToList())
        {
            var key = PeriodKey(d, now);
            if (!Data.RoutinePeriod.TryGetValue(d.Id, out var old)) { Data.RoutinePeriod[d.Id] = key; changed = true; continue; }
            if (old == key) continue;
            Data.RoutinePeriod[d.Id] = key;
            Data.RoutineDone.RemoveAll(s => s.StartsWith(d.Id + "|", StringComparison.Ordinal));
            changed = true;
            if (d.Notify) _main.ShowToast(d.Name + " : remise à zéro");
        }
        if (changed) { Refresh(); _main.RequestSave(); }
    }

    public string NextText
    {
        get
        {
            var now = DateTime.Now;
            var parts = new List<string> { "Quotidiennes : " + Span(now.Date.AddDays(1) - now) };
            var weekly = Data.Routines.Where(r => r.Weekly).Select(r => NextWeeklyReset(r.Weekday, now)).ToList();
            if (weekly.Count > 0) parts.Add("Hebdomadaires : " + Span(weekly.Min() - now));
            return "Prochaine remise à zéro · " + string.Join(" · ", parts);
        }
    }

    private static string Span(TimeSpan t)
        => t.TotalDays >= 1 ? (int)t.TotalDays + " j " + t.Hours + " h" : t.Hours + " h " + t.Minutes.ToString("00");

    // ───────── Cases cochées ─────────

    public bool IsDone(string taskId, string charName) => Data.RoutineDone.Contains(taskId + "|" + charName);

    public void SetDone(string taskId, string charName, bool value)
    {
        var k = taskId + "|" + charName;
        if (value) { if (!Data.RoutineDone.Contains(k)) Data.RoutineDone.Add(k); }
        else Data.RoutineDone.Remove(k);
        UpdateCounts();
        _main.RequestSave();
    }

    public void Changed() => _main.RequestSave();

    /// <summary>Renommage d'un personnage : ses cases suivent.</summary>
    public void RenameChar(string oldName, string newName)
    {
        for (int i = 0; i < Data.RoutineDone.Count; i++)
        {
            var s = Data.RoutineDone[i];
            if (s.EndsWith("|" + oldName, StringComparison.Ordinal))
                Data.RoutineDone[i] = s.Substring(0, s.Length - oldName.Length) + newName;
        }
    }

    // ───────── Tableau ─────────

    public void Refresh()
    {
        CharNames.Clear();
        foreach (var c in _main.Characters.Where(x => x.ShowTasks)) CharNames.Add(c.Name);
        DailyRows.Clear();
        WeeklyRows.Clear();
        foreach (var d in Data.Routines)
            (d.Weekly ? WeeklyRows : DailyRows).Add(new RoutineRow(this, d, CharNames));
        Raise(nameof(HasDaily)); Raise(nameof(HasWeekly)); Raise(nameof(NextText));
        UpdateCounts();
    }

    public bool HasDaily => DailyRows.Count > 0;
    public bool HasWeekly => WeeklyRows.Count > 0;

    /// <summary>Met à jour la pastille « n tâches à faire » des cartes de l'accueil.</summary>
    public void UpdateCounts()
    {
        foreach (var c in _main.Characters)
            c.SetRoutinesLeft(c.ShowTasks ? Data.Routines.Count(r => !Data.RoutineDone.Contains(r.Id + "|" + c.Name)) : 0);
    }

    // ───────── Ajout et suppression ─────────

    public string NewName { get => _newName; set { if (Set(ref _newName, value ?? "")) Error = ""; } }
    public int NewFreq { get => _newFreq; set { if (Set(ref _newFreq, value)) Raise(nameof(NewWeekly)); } }
    public int NewDay { get => _newDay; set => Set(ref _newDay, value); }
    public bool NewWeekly => _newFreq == 1;
    public string Error { get => _error; private set => Set(ref _error, value); }

    private void Add()
    {
        var name = _newName.Trim();
        if (name.Length == 0) { Error = "Écris le nom de la tâche."; return; }
        if (name.Length > 40) { Error = "Le nom est trop long (40 caractères au maximum)."; return; }
        var d = new RoutineDef { Name = name, Weekly = NewWeekly, Weekday = (_newDay + 1) % 7 };
        Data.Routines.Add(d);
        Data.RoutinePeriod[d.Id] = PeriodKey(d, DateTime.Now);
        NewName = "";
        Refresh();
        _main.RequestSave();
    }

    public void Delete(RoutineDef d)
    {
        int idx = Data.Routines.IndexOf(d);
        if (idx < 0) return;
        var done = Data.RoutineDone.Where(s => s.StartsWith(d.Id + "|", StringComparison.Ordinal)).ToList();
        Data.RoutineDone.RemoveAll(s => s.StartsWith(d.Id + "|", StringComparison.Ordinal));
        Data.Routines.RemoveAt(idx);
        Refresh();
        _main.RequestSave();
        _main.ShowUndo("Tâche supprimée : " + d.Name, () =>
        {
            Data.Routines.Insert(Math.Min(idx, Data.Routines.Count), d);
            Data.RoutineDone.AddRange(done);
            Refresh();
            _main.RequestSave();
        });
    }
}
