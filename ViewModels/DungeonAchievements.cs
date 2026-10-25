using System.Collections.ObjectModel;
using DofusTeamOrganizer.Models;

namespace DofusTeamOrganizer.ViewModels;

// ═════════ Donjons : succès par boss et par personnage ═════════
// Pour chaque boss (relevé DofusDB) : jusqu'à 3 challenges (cases 0 à 2), le succès de groupe
// Duo / Trio / Quatuor (case 3) et le succès Spécial (case 4). Cases cochées stockées par personnage.

public class AchRow : ObservableObject
{
    private readonly DungeonsViewModel _vm;
    private readonly BossAch _b;

    public AchRow(BossAch b, int order, DungeonsViewModel vm)
    {
        _b = b;
        _vm = vm;
        Order = order;
    }

    public int Order { get; }
    public string Key => _b.Boss;
    public string Name => _b.Boss;
    public string Bracket => _b.Bracket.Contains(" à ") ? "Niv. " + _b.Bracket : _b.Bracket;
    public int LevelNum => _b.LevelNum;

    // Disponibilité et libellés des 5 cases
    public bool H1 => _b.Challenges.Length > 0;
    public bool H2 => _b.Challenges.Length > 1;
    public bool H3 => _b.Challenges.Length > 2;
    public bool HG => _b.GroupLabel != null;
    public bool HS => _b.Special;
    public string T1 => H1 ? _b.Challenges[0] : "";
    public string T2 => H2 ? _b.Challenges[1] : "";
    public string T3 => H3 ? _b.Challenges[2] : "";
    public string TG => _b.GroupLabel ?? "";
    public string TS => "Spécial";

    public bool[] Available => new[] { H1, H2, H3, HG, HS };
    public int Total => Available.Count(a => a);

    public bool C1 { get => _vm.GetAch(Key, 0); set { _vm.SetAch(Key, 0, value); Raise(nameof(C1)); } }
    public bool C2 { get => _vm.GetAch(Key, 1); set { _vm.SetAch(Key, 1, value); Raise(nameof(C2)); } }
    public bool C3 { get => _vm.GetAch(Key, 2); set { _vm.SetAch(Key, 2, value); Raise(nameof(C3)); } }
    public bool G { get => _vm.GetAch(Key, 3); set { _vm.SetAch(Key, 3, value); Raise(nameof(G)); } }
    public bool S { get => _vm.GetAch(Key, 4); set { _vm.SetAch(Key, 4, value); Raise(nameof(S)); } }

    public string Others => _vm.OthersMissing(Key);
    public bool AnyMissing => _vm.AnyMissing(Key);

    public int Count
    {
        get
        {
            int n = 0;
            var a = Available;
            for (int k = 0; k < 5; k++)
                if (a[k] && _vm.GetAch(Key, k)) n++;
            return n;
        }
    }

    public void RaiseOthers() => Raise(nameof(Others));

    public void RefreshAll()
    {
        Raise(nameof(C1)); Raise(nameof(C2)); Raise(nameof(C3)); Raise(nameof(G)); Raise(nameof(S));
        Raise(nameof(Others));
    }
}

public partial class DungeonsViewModel
{
    private readonly List<AchRow> _achAll = new();
    private readonly Dictionary<string, AchRow> _achByKey = new();
    private bool _achMode;
    private bool _onlyMissing;
    private string _achChar;

    public ObservableCollection<AchRow> AchView { get; } = new();

    public bool AchMode
    {
        get => _achMode;
        set
        {
            if (!Set(ref _achMode, value)) return;
            if (value)
            {
                EnsureChar();
                Raise(nameof(CharNames));
                Raise(nameof(AchChar));
            }
            Raise(nameof(ListMode));
            Refresh();
        }
    }

    public bool ListMode => !_achMode;

    public bool OnlyMissing { get => _onlyMissing; set { if (Set(ref _onlyMissing, value)) Refresh(); } }

    public List<string> CharNames => _main.Data.Characters.Select(c => c.Name).ToList();

    public string AchChar
    {
        get => _achChar;
        set
        {
            if (value == null || !Set(ref _achChar, value)) return;
            foreach (var r in _achAll) r.RefreshAll();
            Raise(nameof(AchCounter));
        }
    }

    public string AchCounter => _achAll.Sum(r => r.Count) + " / " + _achAll.Sum(r => r.Total) + " succès";
    public string AchShown => AchView.Count + " affichés";

    private void EnsureChar()
    {
        var names = _main.Data.Characters.Select(c => c.Name).ToList();
        if (_achChar != null && names.Contains(_achChar)) return;
        var sel = _main.Data.SelectedCharacter;
        _achChar = sel >= 0 && sel < names.Count ? names[sel] : names.FirstOrDefault();
    }

    private void ReloadAch()
    {
        _achAll.Clear();
        _achByKey.Clear();
        int i = 0;
        foreach (var b in DungeonAchData.All)
        {
            var r = new AchRow(b, i++, this);
            _achAll.Add(r);
            _achByKey[r.Key] = r;
        }
        EnsureChar();
        Raise(nameof(CharNames));
        Raise(nameof(AchChar));
    }

    private static string Key(string boss, int k) => boss + "|" + k;

    internal bool GetAch(string boss, int k)
        => _achChar != null && _main.Data.BossAch.TryGetValue(_achChar, out var l) && l.Contains(Key(boss, k));

    internal void SetAch(string boss, int k, bool v)
    {
        if (_achChar == null) return;
        var d = _main.Data.BossAch;
        if (!d.TryGetValue(_achChar, out var l)) { l = new List<string>(); d[_achChar] = l; }
        var key = Key(boss, k);
        if (v) { if (!l.Contains(key)) l.Add(key); }
        else l.Remove(key);
        _main.RequestSave();
        Raise(nameof(AchCounter));
        if (_achByKey.TryGetValue(boss, out var row)) row.RaiseOthers();
    }

    private bool CharMissing(string name, string boss)
    {
        if (!_achByKey.TryGetValue(boss, out var row)) return false;
        _main.Data.BossAch.TryGetValue(name, out var l);
        var a = row.Available;
        for (int k = 0; k < 5; k++)
            if (a[k] && (l == null || !l.Contains(Key(boss, k)))) return true;
        return false;
    }

    internal bool AnyMissing(string boss) => _main.Data.Characters.Any(c => CharMissing(c.Name, boss));

    internal string OthersMissing(string boss)
    {
        var miss = new List<string>();
        int others = 0;
        foreach (var c in _main.Data.Characters)
        {
            if (c.Name == _achChar) continue;
            others++;
            if (CharMissing(c.Name, boss)) miss.Add(c.Name);
        }
        if (miss.Count == 0) return "";
        if (others > 1 && miss.Count == others) return "Tous les autres";
        return string.Join(", ", miss);
    }

    private void RefreshAch()
    {
        var q = _query.Trim().ToLowerInvariant();
        IEnumerable<AchRow> items = _achAll.Where(d =>
            (d.LevelNum > 0 ? InLevelRange(d.LevelNum) : _levelFilter == "tous") &&
            (!_onlyMissing || d.AnyMissing) &&
            (q.Length == 0 || (d.Name + " " + d.Bracket + " " + d.T1 + " " + d.T2 + " " + d.T3).ToLowerInvariant().Contains(q)));

        items = _sort switch
        {
            "niveau décroissant" => items.OrderByDescending(d => d.LevelNum).ThenBy(d => d.Order),
            "nom (A → Z)" => items.OrderBy(d => d.Name, StringComparer.CurrentCulture),
            _ => items.OrderBy(d => d.LevelNum == 0 ? 9999 : d.LevelNum).ThenBy(d => d.Order)
        };

        AchView.Clear();
        foreach (var d in items) AchView.Add(d);
        Raise(nameof(AchShown));
        Raise(nameof(AchCounter));
    }
}
