using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using DofusTeamOrganizer.Models;

namespace DofusTeamOrganizer.ViewModels;

// ═════════ Donjons : succès par personnage ═════════
// 4 cases par donjon : Challenge 1, Challenge 2 (noms saisis par l'utilisateur), Duo, Spécial.
// Cases cochées stockées par personnage ; noms de challenge communs à tous les personnages.

public class AchRow : ObservableObject
{
    private readonly DungeonsViewModel _vm;

    public AchRow(DungeonEntry e, DungeonsViewModel vm)
    {
        _vm = vm;
        Name = e.Name;
        LevelText = e.Level.Replace("/", " / ");
        var m = Regex.Match(e.Level, @"\d+");
        LevelNum = m.Success ? int.Parse(m.Value) : 0;
        Pos = e.Pos;
    }

    public string Name { get; }
    public string LevelText { get; }
    public int LevelNum { get; }
    public string Pos { get; }

    public bool C1 { get => _vm.GetAch(Name, 0); set { _vm.SetAch(Name, 0, value); Raise(nameof(C1)); } }
    public bool C2 { get => _vm.GetAch(Name, 1); set { _vm.SetAch(Name, 1, value); Raise(nameof(C2)); } }
    public bool Duo { get => _vm.GetAch(Name, 2); set { _vm.SetAch(Name, 2, value); Raise(nameof(Duo)); } }
    public bool Special { get => _vm.GetAch(Name, 3); set { _vm.SetAch(Name, 3, value); Raise(nameof(Special)); } }

    public string N1 { get => _vm.GetChallenge(Name, 0); set { _vm.SetChallenge(Name, 0, value); Raise(nameof(N1)); } }
    public string N2 { get => _vm.GetChallenge(Name, 1); set { _vm.SetChallenge(Name, 1, value); Raise(nameof(N2)); } }

    public string Others => _vm.OthersMissing(Name);
    public bool AnyMissing => _vm.AnyMissing(Name);
    public int Count => (C1 ? 1 : 0) + (C2 ? 1 : 0) + (Duo ? 1 : 0) + (Special ? 1 : 0);

    public void RaiseOthers() => Raise(nameof(Others));

    public void RefreshAll()
    {
        Raise(nameof(C1)); Raise(nameof(C2)); Raise(nameof(Duo)); Raise(nameof(Special));
        Raise(nameof(Others));
    }
}

public partial class DungeonsViewModel
{
    private readonly List<AchRow> _achAll = new();
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

    public string AchCounter => _achAll.Sum(r => r.Count) + " / " + (_achAll.Count * 4) + " succès";
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
        foreach (var e in DungeonData.All) _achAll.Add(new AchRow(e, this));
        EnsureChar();
        Raise(nameof(CharNames));
        Raise(nameof(AchChar));
    }

    private static string Key(string dj, int k) => dj + "|" + k;

    internal bool GetAch(string dj, int k)
        => _achChar != null && _main.Data.DungeonAch.TryGetValue(_achChar, out var l) && l.Contains(Key(dj, k));

    internal void SetAch(string dj, int k, bool v)
    {
        if (_achChar == null) return;
        var d = _main.Data.DungeonAch;
        if (!d.TryGetValue(_achChar, out var l)) { l = new List<string>(); d[_achChar] = l; }
        var key = Key(dj, k);
        if (v) { if (!l.Contains(key)) l.Add(key); }
        else l.Remove(key);
        _main.RequestSave();
        Raise(nameof(AchCounter));
        foreach (var r in _achAll)
            if (r.Name == dj) r.RaiseOthers();
    }

    internal string GetChallenge(string dj, int k)
        => _main.Data.DungeonChallenge.TryGetValue(Key(dj, k), out var s) ? s : "";

    internal void SetChallenge(string dj, int k, string v)
    {
        v = (v ?? "").Trim();
        var d = _main.Data.DungeonChallenge;
        if (v.Length == 0) d.Remove(Key(dj, k)); else d[Key(dj, k)] = v;
        _main.RequestSave();
    }

    private bool CharMissing(string name, string dj)
    {
        _main.Data.DungeonAch.TryGetValue(name, out var l);
        for (int k = 0; k < 4; k++)
            if (l == null || !l.Contains(Key(dj, k))) return true;
        return false;
    }

    internal bool AnyMissing(string dj) => _main.Data.Characters.Any(c => CharMissing(c.Name, dj));

    internal string OthersMissing(string dj)
    {
        var miss = new List<string>();
        int others = 0;
        foreach (var c in _main.Data.Characters)
        {
            if (c.Name == _achChar) continue;
            others++;
            if (CharMissing(c.Name, dj)) miss.Add(c.Name);
        }
        if (miss.Count == 0) return "";
        if (others > 1 && miss.Count == others) return "Tous les autres";
        return string.Join(", ", miss);
    }

    private void RefreshAch()
    {
        var q = _query.Trim().ToLowerInvariant();
        IEnumerable<AchRow> items = _achAll.Where(d =>
            InLevelRange(d.LevelNum) &&
            (!_onlyMissing || d.AnyMissing) &&
            (q.Length == 0 || (d.Name + " " + d.LevelText + " " + d.Pos).ToLowerInvariant().Contains(q)));

        items = _sort switch
        {
            "niveau décroissant" => items.OrderByDescending(d => d.LevelNum).ThenBy(d => d.Name, StringComparer.CurrentCulture),
            "nom (A → Z)" => items.OrderBy(d => d.Name, StringComparer.CurrentCulture),
            _ => items.OrderBy(d => d.LevelNum).ThenBy(d => d.Name, StringComparer.CurrentCulture)
        };

        AchView.Clear();
        foreach (var d in items) AchView.Add(d);
        Raise(nameof(AchShown));
        Raise(nameof(AchCounter));
    }
}
