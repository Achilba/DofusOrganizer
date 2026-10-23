namespace DofusTeamOrganizer.ViewModels;

public class DofusItem : ObservableObject
{
    private bool _done;
    private readonly Action<DofusItem> _changed;

    public DofusItem(string name, string color, bool special, string future, bool done, Action<DofusItem> changed)
    {
        Name = name;
        Color = color;
        Special = special;
        Future = future;
        _done = done;
        _changed = changed;
    }

    public string Name { get; }
    public string Color { get; }
    public bool Special { get; }
    /// <summary>Version où le Dofus arrive (ex. "3.8"), null s'il est déjà disponible.</summary>
    public string Future { get; }
    public bool Available => Future == null;
    public string Subtitle => Done ? "Terminé" : (Future ?? "À faire");

    public bool Done
    {
        get => _done;
        set
        {
            if (!Set(ref _done, value)) return;
            Raise(nameof(Subtitle));
            _changed?.Invoke(this);
        }
    }
}
