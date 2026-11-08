using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using DofusTeamOrganizer.Models;

namespace DofusTeamOrganizer.ViewModels;

/// <summary>Une ligne de la page Zaaps : la case cochée met le Zaap en favori.</summary>
public class ZaapFavRow : ObservableObject
{
    private readonly MainViewModel _main;
    private readonly Action _changed;

    public ZaapFavRow(ZaapEntry entry, MainViewModel main, Action changed)
    {
        Entry = entry;
        _main = main;
        _changed = changed;
        CopyCommand = new RelayCommand(() => _main.CopyZaap(Entry));
    }

    public ZaapEntry Entry { get; }
    public string Label => Entry.Label;
    public string Coord => Entry.Coord;
    public RelayCommand CopyCommand { get; }

    public bool IsFavorite
    {
        get => _main.IsZaapFavorite(Entry.Coord);
        set
        {
            if (value == IsFavorite) return;
            _main.SetZaapFavorite(Entry.Coord, value);
            Raise();
            _changed?.Invoke();
        }
    }

    public void RefreshFavorite() => Raise(nameof(IsFavorite));
}

/// <summary>Page Zaaps : tous les Zaaps, une recherche et une case « favori » par Zaap.</summary>
public class ZaapsViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private readonly List<ZaapFavRow> _rows = new();
    private string _query = "";

    public ObservableCollection<ZaapFavRow> View { get; } = new();

    /// <summary>Option « Zaap déporté » (la fenêtre Zaaps à part), réglée depuis cette page.</summary>
    public bool DetachedZaap { get => _main.DetachedZaap; set => _main.DetachedZaap = value; }
    /// <summary>Garder la liste dépliée après le choix d'un Zaap dans la fenêtre à part.</summary>
    public bool ZaapKeepOpen { get => _main.ZaapKeepOpen; set => _main.ZaapKeepOpen = value; }

    public ZaapsViewModel(MainViewModel main)
    {
        _main = main;
        _main.DetachedZaapChanged += () => Raise(nameof(DetachedZaap));
        ClearQueryCommand = new RelayCommand(() => Query = "");
        Reload();
    }

    public RelayCommand ClearQueryCommand { get; }

    public string Query { get => _query; set { if (Set(ref _query, value ?? "")) Refresh(); } }

    public string Counter
    {
        get
        {
            int fav = _rows.Count(r => r.IsFavorite);
            return fav + " favori" + (fav > 1 ? "s" : "") + " · " + _rows.Count + " Zaaps";
        }
    }
    public string Shown => View.Count + " affiché" + (View.Count > 1 ? "s" : "");
    public bool HasNoMatch => View.Count == 0;

    /// <summary>À appeler au démarrage et après un import ou une réinitialisation.</summary>
    public void Reload()
    {
        _rows.Clear();
        foreach (var z in ZaapData.All) _rows.Add(new ZaapFavRow(z, _main, Refresh));
        Refresh();
    }

    private static string Norm(string s)
    {
        var d = (s ?? "").ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in d)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
        return sb.ToString().Replace(" ", "");
    }

    /// <summary>Favoris en premier, par ordre alphabétique ; les autres suivent dans l'ordre de la liste.</summary>
    public void Refresh()
    {
        foreach (var r in _rows) r.RefreshFavorite();
        var q = Norm(_query);
        var found = _rows.Where(r => q.Length == 0 || Norm(r.Label).Contains(q) || Norm(r.Coord).Contains(q)).ToList();
        var ordered = found.Where(r => r.IsFavorite).OrderBy(r => r.Label, StringComparer.CurrentCulture)
            .Concat(found.Where(r => !r.IsFavorite));
        View.Clear();
        foreach (var r in ordered) View.Add(r);
        Raise(nameof(Counter)); Raise(nameof(Shown)); Raise(nameof(HasNoMatch));
    }
}
