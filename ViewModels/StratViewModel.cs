using System.Collections.ObjectModel;
using System.Diagnostics;
using DofusTeamOrganizer.Models;

namespace DofusTeamOrganizer.ViewModels;

/// <summary>Vue « Strat » de la page Songes : des rubriques par type de run, avec équipe, stuffs et texte libre.</summary>
public class StratViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private StratRun _selected;

    public ObservableCollection<string> CharNames { get; } = new();
    public RelayCommand AddRunCommand { get; }
    public RelayCommand DeleteRunCommand { get; }
    public RelayCommand AddSlotCommand { get; }
    public RelayCommand RemoveSlotCommand { get; }
    public RelayCommand OpenStuffCommand { get; }

    public StratViewModel(MainViewModel main)
    {
        _main = main;
        StratSlot.Resolver = name => _main.Characters.FirstOrDefault(c => c.Name == name)?.Stuffs;
        AddRunCommand = new RelayCommand(AddRun);
        DeleteRunCommand = new RelayCommand(DeleteRun);
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
        _selected = Runs.FirstOrDefault(r => r.Id == Data.StratSelected) ?? Runs.FirstOrDefault();
        Raise(nameof(Runs)); Raise(nameof(Selected)); Raise(nameof(HasSelected)); Raise(nameof(NoSelection));
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
            if (!Set(ref _selected, value)) return;
            Data.StratSelected = value?.Id ?? "";
            _main.RequestSave();
            Raise(nameof(HasSelected)); Raise(nameof(NoSelection));
            RefreshChars();
        }
    }
    public bool HasSelected => _selected != null;
    public bool NoSelection => _selected == null;

    private void AddRun()
    {
        int n = Runs.Count + 1;
        string name;
        do { name = "Nouvelle rubrique " + n++; } while (Runs.Any(r => r.Name == name));
        var run = new StratRun { Name = name };
        Runs.Add(run);
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
        _main.ShowUndo("Rubrique supprimée : " + run.Name, () =>
        {
            Runs.Insert(Math.Min(idx, Runs.Count), run);
            Selected = run;
            _main.RequestSave();
        });
    }

    private void AddSlot()
    {
        if (_selected == null) return;
        var free = CharNames.FirstOrDefault(n => _selected.Slots.All(s => s.Char != n)) ?? CharNames.FirstOrDefault() ?? "";
        _selected.Slots.Add(new StratSlot { Char = free });
        _main.RequestSave();
    }
}
