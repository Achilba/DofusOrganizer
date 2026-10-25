using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using DofusTeamOrganizer.Models;

namespace DofusTeamOrganizer.ViewModels;

/// <summary>Un résultat de la recherche globale.</summary>
public class SearchHit
{
    public string Category { get; init; }
    public string Title { get; init; }
    public string Detail { get; init; }
    public Action Run { get; init; }
    internal int Rank { get; init; }
}

/// <summary>Recherche globale (Ctrl+K) : pages, personnages, Dofus, donjons, légendes, Zaaps et notes.</summary>
public class GlobalSearch : ObservableObject
{
    private readonly MainViewModel _main;
    private bool _open;
    private string _query = "";
    private int _selectedIndex = -1;

    public ObservableCollection<SearchHit> Hits { get; } = new();
    public RelayCommand ShowCommand { get; }
    public RelayCommand CloseCommand { get; }

    public GlobalSearch(MainViewModel main)
    {
        _main = main;
        ShowCommand = new RelayCommand(Show);
        CloseCommand = new RelayCommand(Close);
    }

    public bool Open { get => _open; private set => Set(ref _open, value); }
    public string Query { get => _query; set { if (Set(ref _query, value ?? "")) Rebuild(); } }
    public int SelectedIndex { get => _selectedIndex; set => Set(ref _selectedIndex, value); }
    public bool HasQuery => _query.Trim().Length > 0;
    public bool NoResult => HasQuery && Hits.Count == 0;

    public void Show()
    {
        Query = "";
        Open = true;
    }

    public void Close() => Open = false;

    /// <summary>Déplace la sélection (flèches haut / bas), sans sortir de la liste.</summary>
    public void Move(int delta)
    {
        if (Hits.Count == 0) return;
        SelectedIndex = Math.Min(Hits.Count - 1, Math.Max(0, _selectedIndex + delta));
    }

    /// <summary>Exécute le résultat choisi puis ferme la recherche.</summary>
    public void Activate(SearchHit hit)
    {
        if (hit == null) return;
        Close();
        try { hit.Run?.Invoke(); }
        catch (Exception ex) { _main.Storage.LogError("Recherche : " + ex); }
    }

    public void ActivateSelected()
    {
        if (_selectedIndex >= 0 && _selectedIndex < Hits.Count) Activate(Hits[_selectedIndex]);
        else if (Hits.Count > 0) Activate(Hits[0]);
    }

    // ───────── Recherche ─────────

    /// <summary>Minuscules sans accents : « ebene » trouve « Ébène ».</summary>
    private static string Norm(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new StringBuilder();
        foreach (var ch in s.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(char.ToLowerInvariant(ch));
        return sb.ToString();
    }

    /// <summary>0 = commence par la saisie, 1 = un mot commence par la saisie, 2 = contient, -1 = pas trouvé.</summary>
    private static int Rank(string normalized, string q)
    {
        int i = normalized.IndexOf(q, StringComparison.Ordinal);
        if (i < 0) return -1;
        if (i == 0) return 0;
        return char.IsLetterOrDigit(normalized[i - 1]) ? 2 : 1;
    }

    private void Rebuild()
    {
        Hits.Clear();
        var q = Norm(_query.Trim());
        if (q.Length > 0)
        {
            var found = new List<SearchHit>();
            AddPages(found, q);
            AddCharacters(found, q);
            AddDofus(found, q);
            AddDungeons(found, q);
            AddLegends(found, q);
            AddZaaps(found, q);
            AddNotes(found, q);
            foreach (var h in found.Take(40)) Hits.Add(h);
        }
        SelectedIndex = Hits.Count > 0 ? 0 : -1;
        Raise(nameof(HasQuery));
        Raise(nameof(NoResult));
    }

    /// <summary>Ajoute au plus 6 résultats de la catégorie, les meilleurs d'abord.</summary>
    private static void AddRanked(List<SearchHit> found, IEnumerable<SearchHit> candidates)
        => found.AddRange(candidates.OrderBy(h => h.Rank).ThenBy(h => h.Title, StringComparer.CurrentCulture).Take(6));

    private void AddPages(List<SearchHit> found, string q)
    {
        var list = new List<SearchHit>();
        foreach (var n in _main.NavItems)
        {
            int r = Rank(Norm(n.Label), q);
            if (r < 0) continue;
            var key = n.Key;
            list.Add(new SearchHit { Category = "Page", Title = n.Label, Detail = "Ouvrir la page", Rank = r, Run = () => _main.Page = key });
        }
        AddRanked(found, list);
    }

    private void AddCharacters(List<SearchHit> found, string q)
    {
        var list = new List<SearchHit>();
        foreach (var c in _main.Characters)
        {
            int r = Rank(Norm(c.Name + " " + c.ClassName), q);
            if (r < 0) continue;
            var ch = c;
            list.Add(new SearchHit
            {
                Category = "Personnage", Title = c.Name, Detail = c.ClassName + " · " + c.LevelText, Rank = r,
                Run = () => { _main.CharactersPage.Selected = ch; _main.Page = "chars"; }
            });
        }
        AddRanked(found, list);
    }

    private void AddDofus(List<SearchHit> found, string q)
    {
        var list = new List<SearchHit>();
        foreach (var d in _main.MainDofus.Concat(_main.SpecialDofus))
        {
            int r = Rank(Norm("dofus " + d.Name), q);
            if (r < 0) continue;
            var item = d;
            list.Add(new SearchHit
            {
                Category = "Dofus", Title = d.Name,
                Detail = (d.Subtitle.Length > 0 ? d.Subtitle + " · " : "") + d.StatusText, Rank = r,
                Run = () =>
                {
                    if (item.Special) _main.SpecOpen = true;
                    _main.Page = "dofus";
                }
            });
        }
        AddRanked(found, list);
    }

    private void AddDungeons(List<SearchHit> found, string q)
    {
        var list = new List<SearchHit>();
        foreach (var b in DungeonAchData.All)
        {
            // Boss d'abord ; les noms de challenges et du Spécial passent après (rang décalé)
            int r = Rank(Norm(b.Boss), q);
            if (r < 0)
            {
                var names = b.Challenges.Concat(new[] { b.SpecialName ?? "" });
                foreach (var n in names)
                {
                    int rn = Rank(Norm(n), q);
                    if (rn >= 0) { r = rn + 10; break; }
                }
            }
            if (r < 0) continue;
            var boss = b.Boss;
            list.Add(new SearchHit
            {
                Category = "Donjon", Title = b.Boss,
                Detail = "Niv. " + b.Level + " · " + string.Join(", ", b.Challenges.Take(2)) + (b.SpecialName != null ? " · Spécial : " + b.SpecialName : ""),
                Rank = r,
                Run = () => { _main.Dungeons.Query = boss; _main.Page = "dungeons"; }
            });
        }
        AddRanked(found, list);
    }

    private void AddLegends(List<SearchHit> found, string q)
    {
        var list = new List<SearchHit>();
        foreach (var l in _main.Songes.Top.Concat(_main.Songes.Animal).Concat(_main.Songes.Classic))
        {
            int r = Rank(Norm(l.Name), q);
            if (r < 0) continue;
            list.Add(new SearchHit
            {
                Category = "Songe", Title = l.Name, Detail = l.Qty + " obtenu(s)", Rank = r,
                Run = () => _main.Page = "dreams"
            });
        }
        AddRanked(found, list);
    }

    private void AddZaaps(List<SearchHit> found, string q)
    {
        var list = new List<SearchHit>();
        foreach (var z in ZaapData.All)
        {
            int r = Rank(Norm(z.Label + " " + z.Coord), q);
            if (r < 0) continue;
            var zaap = z;
            list.Add(new SearchHit
            {
                Category = "Zaap", Title = z.Label, Detail = "Copier " + z.Command, Rank = r,
                Run = () => _main.CopyZaapCommand.Execute(zaap)
            });
        }
        AddRanked(found, list);
    }

    private void AddNotes(List<SearchHit> found, string q)
    {
        var list = new List<SearchHit>();
        foreach (var key in _main.NoteKeys)
        {
            if (!_main.Data.Notes.TryGetValue(key, out var text) || string.IsNullOrWhiteSpace(text)) continue;
            var norm = Norm(text);
            int idx = norm.IndexOf(q, StringComparison.Ordinal);
            if (idx < 0) continue;
            // Norm() garde la même longueur que le texte d'origine tant qu'il n'y a pas de caractère composé : on borne par sécurité.
            int start = Math.Max(0, Math.Min(text.Length - 1, idx) - 24);
            var snip = text.Substring(start, Math.Min(text.Length - start, q.Length + 60)).Replace("\r", " ").Replace("\n", " ");
            var k = key;
            list.Add(new SearchHit
            {
                Category = "Note", Title = key, Detail = (start > 0 ? "…" : "") + snip, Rank = 3,
                Run = () => { _main.SelectedNote = k; _main.Page = "notes"; }
            });
        }
        AddRanked(found, list);
    }
}
