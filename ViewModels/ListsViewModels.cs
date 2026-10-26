using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using DofusTeamOrganizer.Models;

namespace DofusTeamOrganizer.ViewModels;

// ═════════ Donjons ═════════

public class DungeonItem : ObservableObject
{
    private bool _done;
    private bool _silent;
    private readonly Action<DungeonItem> _changed;

    public DungeonItem(DungeonEntry e, bool done, Action<DungeonItem> changed)
    {
        Name = e.Name;
        LevelText = e.Level.Replace("/", " / ");
        var m = Regex.Match(e.Level, @"\d+");
        LevelNum = m.Success ? int.Parse(m.Value) : 0;
        Pos = Regex.IsMatch(e.Pos, @"^-?\d") ? "[" + e.Pos + "]" : e.Pos;
        _done = done;
        _changed = changed;
    }

    public string Name { get; }
    public string LevelText { get; }
    public int LevelNum { get; }
    public string Pos { get; }

    public bool Done
    {
        get => _done;
        set { if (Set(ref _done, value) && !_silent) _changed?.Invoke(this); }
    }

    public void SetSilently(bool v) { _silent = true; Done = v; _silent = false; }
}

public partial class DungeonsViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private readonly List<DungeonItem> _all = new();
    private string _query = "";
    private string _sort = "Niveau croissant";
    private bool _hideDone;
    private string _levelFilter = "Tous";

    public ObservableCollection<DungeonItem> View { get; } = new();
    /// <summary>Tous les donjons (sans filtre), pour la recherche globale.</summary>
    public IReadOnlyList<DungeonItem> All => _all;
    public string[] LevelFilterChoices { get; } = { "Tous", "1 à 50", "51 à 100", "101 à 150", "151 à 200" };
    public string LevelFilter { get => _levelFilter; set { if (value != null && Set(ref _levelFilter, value)) Refresh(); } }
    public string[] SortChoices { get; } = { "Niveau croissant", "Niveau décroissant", "Nom (A → Z)" };

    public DungeonsViewModel(MainViewModel main)
    {
        _main = main;
        Reload();
    }

    public void Reload()
    {
        _all.Clear();
        foreach (var e in DungeonData.All)
            _all.Add(new DungeonItem(e, _main.Data.DungeonsDone.Contains(e.Name), OnDone));
        ReloadAch();
        Refresh();
    }

    public string Query { get => _query; set { if (Set(ref _query, value ?? "")) Refresh(); } }
    public RelayCommand ClearQueryCommand => new(() => Query = "");
    public string Sort { get => _sort; set { if (value != null && Set(ref _sort, value)) Refresh(); } }
    public bool HideDone { get => _hideDone; set { if (Set(ref _hideDone, value)) Refresh(); } }
    public string Counter => _all.Count(d => d.Done) + " / " + _all.Count;
    public string Shown => View.Count + " affichés";

    private void OnDone(DungeonItem item)
    {
        var list = _main.Data.DungeonsDone;
        if (item.Done) { if (!list.Contains(item.Name)) list.Add(item.Name); }
        else list.Remove(item.Name);
        _main.RequestSave();
        Raise(nameof(Counter));

        if (_hideDone) Refresh();
    }

    private bool InLevelRange(int n) => _levelFilter switch
    {
        "1 à 50" => n <= 50,
        "51 à 100" => n >= 51 && n <= 100,
        "101 à 150" => n >= 101 && n <= 150,
        "151 à 200" => n >= 151,
        _ => true
    };

    private void Refresh()
    {
        var q = _query.Trim().ToLowerInvariant();
        IEnumerable<DungeonItem> items = _all.Where(d =>
            (!_hideDone || !d.Done) && InLevelRange(d.LevelNum) &&
            (q.Length == 0 || (d.Name + " " + d.LevelText + " " + d.Pos).ToLowerInvariant().Contains(q)));

        items = _sort switch
        {
            "Niveau décroissant" => items.OrderByDescending(d => d.LevelNum).ThenBy(d => d.Name, StringComparer.CurrentCulture),
            "Nom (A → Z)" => items.OrderBy(d => d.Name, StringComparer.CurrentCulture),
            _ => items.OrderBy(d => d.LevelNum).ThenBy(d => d.Name, StringComparer.CurrentCulture)
        };

        View.Clear();
        foreach (var d in items) View.Add(d);
        Raise(nameof(Shown));
        Raise(nameof(Counter));
        RefreshAch();
    }
}

// ═════════ Songes ═════════

public class LegendItem : ObservableObject
{
    private int _qty;
    private readonly Action<LegendItem> _changed;

    public LegendItem(string key, string name, int qty, Action<LegendItem> changed)
    {
        Key = key; Name = name; _qty = qty; _changed = changed;
    }

    public string Key { get; }
    public string Name { get; }
    public int Qty
    {
        get => _qty;
        set
        {
            int old = _qty;
            if (Set(ref _qty, Math.Max(0, value))) { LastDelta = _qty - old; Raise(nameof(CanDec)); _changed?.Invoke(this); }
        }
    }
    /// <summary>Dernière variation du compteur (positive : une légende de plus).</summary>
    public int LastDelta { get; private set; }
    public bool CanDec => _qty > 0;
}

public class SongesViewModel : ObservableObject
{
    private readonly MainViewModel _main;

    public ObservableCollection<LegendItem> Top { get; } = new();
    public ObservableCollection<LegendItem> Animal { get; } = new();
    public ObservableCollection<LegendItem> Classic { get; } = new();

    public SongesViewModel(MainViewModel main)
    {
        _main = main;
        Strat = new StratViewModel(main);
        Reload();
        // Les durées affichées (« depuis 2 h 14 », « dernier run il y a 21 min ») se mettent à jour toutes seules
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(20) };
        timer.Tick += (_, _) => { Raise(nameof(DropSince)); Strat.RaiseTimes(); };
        timer.Start();
    }

    public StratViewModel Strat { get; }
    public string[] Modes { get; } = { "Strat", "Drops" };
    public string Mode
    {
        get => _main.Data.SongesMode == "Drops" ? "Drops" : "Strat";
        set
        {
            if (value == null || value == Mode) return;
            _main.Data.SongesMode = value;
            _main.RequestSave();
            Raise(); Raise(nameof(IsStrat)); Raise(nameof(IsDrops));
        }
    }
    public bool IsStrat => Mode == "Strat";
    public bool IsDrops => Mode == "Drops";

    public void Reload()
    {
        Top.Clear(); Animal.Clear(); Classic.Clear();
        foreach (var n in GameLists.LegendsTop) Top.Add(Make("t|", n));
        foreach (var n in GameLists.LegendsAnimal) Animal.Add(Make("a|", n));
        foreach (var n in GameLists.LegendsClassic) Classic.Add(Make("c|", n));
        Raise(nameof(Total));
        Raise(nameof(TotalText));
        Strat?.Reload();
        RaiseDrop();
        Raise(nameof(Mode)); Raise(nameof(IsStrat)); Raise(nameof(IsDrops));
    }

    private LegendItem Make(string prefix, string name)
    {
        var key = prefix + name;
        _main.Data.Legends.TryGetValue(key, out var qty);
        return new LegendItem(key, name, qty, OnChanged);
    }

    private void OnChanged(LegendItem item)
    {
        _main.Data.Legends[item.Key] = item.Qty;
        if (item.LastDelta > 0 && (item.Key.StartsWith("a|") || item.Key.StartsWith("c|")))
        {
            // Dernier drop : le numéro de run est le compteur du run actif à cet instant
            var run = Strat.Selected;
            _main.Data.LastDrop = new DropInfo { Name = item.Name, At = DateTime.Now, Run = run?.Count ?? 0, RunName = run?.Name ?? "" };
            RaiseDrop();
        }
        _main.RequestSave();
        Raise(nameof(Total));
        Raise(nameof(TotalText));
    }

    private void RaiseDrop()
    {
        Raise(nameof(HasDrop)); Raise(nameof(DropName)); Raise(nameof(DropSince)); Raise(nameof(DropRun)); Raise(nameof(DropRunLine));
    }

    public bool HasDrop => _main.Data.LastDrop != null;
    public string DropName => _main.Data.LastDrop?.Name ?? "Aucun drop enregistré";
    public string DropSince => _main.Data.LastDrop == null ? "—" : StratViewModel.Elapsed(_main.Data.LastDrop.At);
    public string DropRun => _main.Data.LastDrop == null ? "—" : _main.Data.LastDrop.Run.ToString();
    public string DropRunLine => _main.Data.LastDrop == null ? "" : "Run : " + _main.Data.LastDrop.RunName;

    public int Total => Animal.Sum(a => a.Qty) + Classic.Sum(c => c.Qty);
    public string TotalText => "Légendes droppées : " + Total;
}
