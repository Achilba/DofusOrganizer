using System.Collections.ObjectModel;
using System.Diagnostics;
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
        OpenCommand = new RelayCommand(() =>
        {
            try { Process.Start(new ProcessStartInfo(_b.Url) { UseShellExecute = true }); } catch { }
        });
    }

    public int Order { get; }
    public RelayCommand OpenCommand { get; }
    public string Key => _b.Boss;
    public string Name => _b.Boss;
    public string Bracket => _b.Level.ToString();
    public int LevelNum => _b.Level;

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
    public string SpecialTip => _b.SpecialName == null ? "" : _b.SpecialName + "\n" + _b.SpecialDesc;

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

/// <summary>Une suggestion « à finir en priorité » : un clic filtre la liste sur ce boss.</summary>
public class AchSuggestion
{
    public string Text { get; init; } = "";
    public RelayCommand Command { get; init; }
}

public partial class DungeonsViewModel
{
    private readonly List<AchRow> _achAll = new();
    private readonly Dictionary<string, AchRow> _achByKey = new();
    private bool _achMode = true;
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

    /// <summary>Personnages suivis dans la vue Succès (case « Suivi dans les succès » de la page Personnages).</summary>
    private IEnumerable<Character> AchChars()
        => _main.Data.Characters.Where(c => c.AchTracked);

    /// <summary>Choix « Tous » du sélecteur : une coche s'applique à tous les personnages suivis.</summary>
    public const string AllChars = "Tous";

    public List<string> CharNames => new[] { AllChars }.Concat(AchChars().Select(c => c.Name)).ToList();

    public string AchChar
    {
        get => _achChar;
        set
        {
            if (value == null || !Set(ref _achChar, value)) return;
            foreach (var r in _achAll) r.RefreshAll();
            Raise(nameof(AchCounter));
            Raise(nameof(AchSuggestions)); Raise(nameof(HasAchSuggestions));
        }
    }

    /// <summary>Les 3 boss les plus proches d'être terminés (déjà commencés), puis les plus bas en niveau si besoin.</summary>
    public List<AchSuggestion> AchSuggestions
    {
        get
        {
            var list = new List<AchSuggestion>();
            if (_achChar == null) return list;
            var open = _achAll.Where(r => r.Total - r.Count > 0).ToList();
            var picks = open.Where(r => r.Count > 0).OrderBy(r => r.Total - r.Count).ThenBy(r => r.LevelNum).Take(3).ToList();
            if (picks.Count < 3)
                picks.AddRange(open.Where(r => r.Count == 0).OrderBy(r => r.LevelNum).Take(3 - picks.Count));
            foreach (var r in picks)
            {
                var row = r;
                int miss = r.Total - r.Count;
                list.Add(new AchSuggestion
                {
                    Text = r.Name + " · niv. " + r.Bracket + " · " + (miss > 1 ? "reste " + miss : "reste 1"),
                    Command = new RelayCommand(() => Query = row.Name)
                });
            }
            return list;
        }
    }
    public bool HasAchSuggestions => AchSuggestions.Count > 0;

    public string AchCounter => _achAll.Sum(r => r.Count) + " / " + _achAll.Sum(r => r.Total) + " succès";
    public string AchShown => AchView.Count + " affichés";

    private void EnsureChar()
    {
        var names = AchChars().Select(c => c.Name).ToList();
        if (_achChar != null && (_achChar == AllChars || names.Contains(_achChar))) return;
        var sel = _main.Data.SelectedCharacter;
        var selName = sel >= 0 && sel < _main.Data.Characters.Count ? _main.Data.Characters[sel].Name : null;
        _achChar = selName != null && names.Contains(selName) ? selName : names.FirstOrDefault();
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

    private bool Has(string name, string boss, int k)
        => _main.Data.BossAch.TryGetValue(name, out var l) && l.Contains(Key(boss, k));

    internal bool GetAch(string boss, int k)
    {
        if (_achChar == null) return false;
        if (_achChar == AllChars)
        {
            var all = AchChars().ToList();
            return all.Count > 0 && all.All(c => Has(c.Name, boss, k));
        }
        return Has(_achChar, boss, k);
    }

    internal void SetAch(string boss, int k, bool v)
    {
        if (_achChar == null) return;
        var d = _main.Data.BossAch;
        var key = Key(boss, k);
        var targets = _achChar == AllChars ? AchChars().Select(c => c.Name).ToList() : new List<string> { _achChar };
        foreach (var name in targets)
        {
            if (!d.TryGetValue(name, out var l)) { l = new List<string>(); d[name] = l; }
            if (v) { if (!l.Contains(key)) l.Add(key); }
            else l.Remove(key);
        }
        _main.RequestSave();
        Raise(nameof(AchCounter));
        Raise(nameof(AchSuggestions)); Raise(nameof(HasAchSuggestions));
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

    internal bool AnyMissing(string boss) => AchChars().Any(c => CharMissing(c.Name, boss));

    /// <summary>Nombre de succès encore à faire sur ce boss pour un personnage.</summary>
    private int MissingCount(string name, string boss)
    {
        if (!_achByKey.TryGetValue(boss, out var row)) return 0;
        _main.Data.BossAch.TryGetValue(name, out var l);
        var a = row.Available;
        int n = 0;
        for (int k = 0; k < 5; k++)
            if (a[k] && (l == null || !l.Contains(Key(boss, k)))) n++;
        return n;
    }

    /// <summary>
    /// Colonne « Manque » : « Tous » si aucun personnage n'a rien fait sur ce boss ; sinon une ligne par
    /// personnage à qui il reste des succès, avec le nombre restant (seuls les personnages suivis comptent).
    /// </summary>
    internal string OthersMissing(string boss)
    {
        if (!_achByKey.TryGetValue(boss, out var row)) return "";
        var all = AchChars().ToList();
        var lines = new List<string>();
        int untouched = 0;
        foreach (var c in all)
        {
            int n = MissingCount(c.Name, boss);
            if (n == 0) continue;
            if (n == row.Total) untouched++;
            lines.Add(c.Name + " (" + n + ")");
        }
        if (lines.Count == 0) return "";
        if (all.Count > 1 && untouched == all.Count) return "Tous";
        var rows = new List<string>();
        for (int i = 0; i < lines.Count; i += 2)
            rows.Add(i + 1 < lines.Count ? lines[i] + " · " + lines[i + 1] : lines[i]);
        return string.Join("\n", rows);
    }

    private void RefreshAch()
    {
        var q = _query.Trim().ToLowerInvariant();
        IEnumerable<AchRow> items = _achAll.Where(d =>
            InLevelRange(d.LevelNum) &&
            (!_onlyMissing || d.AnyMissing) &&
            (q.Length == 0 || (d.Name + " " + d.Bracket + " " + d.T1 + " " + d.T2 + " " + d.T3).ToLowerInvariant().Contains(q)));

        items = _sort switch
        {
            "Niveau décroissant" => items.OrderByDescending(d => d.LevelNum).ThenBy(d => d.Order),
            "Nom (A → Z)" => items.OrderBy(d => d.Name, StringComparer.CurrentCulture),
            _ => items.OrderBy(d => d.LevelNum).ThenBy(d => d.Order)
        };

        AchView.Clear();
        foreach (var d in items) AchView.Add(d);
        Raise(nameof(AchShown));
        Raise(nameof(AchCounter));
        Raise(nameof(AchSuggestions)); Raise(nameof(HasAchSuggestions));
    }
}
