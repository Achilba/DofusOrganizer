using DofusTeamOrganizer.Models;

namespace DofusTeamOrganizer.ViewModels;

/// <summary>Une colonne du tableau « Personnages et fonctions » (Paramètres) : son nom, son compteur et « tout cocher / tout décocher ».</summary>
public class FeatureColumn : ObservableObject
{
    private readonly MainViewModel _main;
    private readonly Func<Character, bool> _get;
    private readonly Action<Character, bool> _set;

    public FeatureColumn(MainViewModel main, string label, Func<Character, bool> get, Action<Character, bool> set)
    {
        _main = main; Label = label; _get = get; _set = set;
        ToggleAllCommand = new RelayCommand(ToggleAll);
    }

    public string Label { get; }
    public RelayCommand ToggleAllCommand { get; }
    public string CountText => _main.Characters.Count(_get) + " / " + _main.Characters.Count;

    public void Refresh() => Raise(nameof(CountText));

    private void ToggleAll()
    {
        // Si tout le monde est coché on décoche tout, sinon on coche tout
        bool target = !_main.Characters.All(_get);
        foreach (var c in _main.Characters.ToList()) _set(c, target);
        _main.RequestSave();
    }
}
