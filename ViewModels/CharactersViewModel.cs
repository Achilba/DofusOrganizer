using System.Collections.ObjectModel;
using DofusTeamOrganizer.Models;

namespace DofusTeamOrganizer.ViewModels;

/// <summary>Une ligne de la comparaison de deux personnages ; le meilleur côté est mis en avant.</summary>
public class CompareRow
{
    public string Label { get; init; } = "";
    public string A { get; init; } = "";
    public string B { get; init; } = "";
    public bool WinA { get; init; }
    public bool WinB { get; init; }
}

public class CharactersViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private Character _selected;
    private string _tagFilter = "Tous";
    private string _verFilter = "Toutes";
    private string _newJob;
    private string _newName = "", _newUrl = "", _error = "";
    private string _newTag = "PvM";
    private string _newVersion = GameLists.CurrentVersion;

    public ObservableCollection<StuffLink> VisibleStuffs { get; } = new();
    public ObservableCollection<string> AvailableJobs { get; } = new();

    public string[] ClassChoices => GameLists.Classes;
    public string[] FactionChoices => GameLists.Factions;
    public int[] AlignLevelChoices { get; } = Enumerable.Range(1, 100).ToArray();
    public string[] TagFilters { get; } = new[] { "Tous" }.Concat(GameLists.Tags).ToArray();
    public string[] VersionFilters { get; } = new[] { "Toutes" }.Concat(GameLists.Versions).ToArray();
    public string[] TagChoices => GameLists.Tags;
    public string[] VersionChoices => GameLists.Versions;

    // ───── Comparaison de deux personnages ─────
    public const string NoCompare = "Aucun";
    public ObservableCollection<string> CompareChoices { get; } = new();
    public ObservableCollection<CompareRow> CompareRows { get; } = new();
    private string _compareWith = NoCompare;
    public string CompareWith
    {
        get => _compareWith;
        set { if (value != null && Set(ref _compareWith, value)) BuildCompare(); }
    }
    public bool HasCompare { get; private set; }
    public string CompareNameA => _selected?.Name ?? "";
    public string CompareNameB => _compareWith;

    private static int AchTotal() => DungeonAchData.All.Sum(b => b.Challenges.Length + (b.GroupLabel != null ? 1 : 0) + (b.Special ? 1 : 0));

    private int AchDone(string name) => _main.Data.BossAch.TryGetValue(name, out var l) ? l.Count : 0;

    private static CompareRow Num(string label, int a, int b, string textA, string textB, bool higherWins = true) => new()
    {
        Label = label, A = textA, B = textB,
        WinA = a != b && (higherWins ? a > b : a < b),
        WinB = a != b && (higherWins ? b > a : b < a),
    };

    /// <summary>Met à jour la liste des personnages comparables et le tableau.</summary>
    public void RefreshCompare()
    {
        CompareChoices.Clear();
        CompareChoices.Add(NoCompare);
        foreach (var c in Characters.Where(c => c != _selected)) CompareChoices.Add(c.Name);
        if (!CompareChoices.Contains(_compareWith)) _compareWith = NoCompare;
        Raise(nameof(CompareWith));
        BuildCompare();
    }

    private void BuildCompare()
    {
        CompareRows.Clear();
        var a = _selected;
        var b = Characters.FirstOrDefault(c => c != a && c.Name == _compareWith);
        HasCompare = a != null && b != null;
        if (HasCompare)
        {
            int total = AchTotal();
            int da = AchDone(a.Name), db = AchDone(b.Name);
            int ja = a.Jobs.Count, jb = b.Jobs.Count;
            int ma = a.Jobs.Count(j => j.Level >= 200), mb = b.Jobs.Count(j => j.Level >= 200);
            static string Best(Character c)
            {
                var top = c.Jobs.OrderByDescending(x => x.Level).FirstOrDefault();
                return top == null ? "—" : top.Name + " " + top.Level;
            }
            CompareRows.Add(new CompareRow { Label = "Classe", A = a.ClassName, B = b.ClassName });
            CompareRows.Add(Num("Niveau", a.CharLevel, b.CharLevel, a.LevelText, b.LevelText));
            CompareRows.Add(new CompareRow { Label = "Alignement", A = a.OrderText, B = b.OrderText });
            CompareRows.Add(Num("Niveau d'alignement", a.AlignLevel, b.AlignLevel, a.AlignLevel.ToString(), b.AlignLevel.ToString()));
            CompareRows.Add(Num("Métiers", ja, jb, ja.ToString(), jb.ToString()));
            CompareRows.Add(Num("Métiers niveau 200", ma, mb, ma.ToString(), mb.ToString()));
            CompareRows.Add(new CompareRow { Label = "Meilleur métier", A = Best(a), B = Best(b) });
            CompareRows.Add(Num("Succès de donjon", da, db, da + " / " + total, db + " / " + total));
            CompareRows.Add(Num("Stuffs enregistrés", a.Stuffs.Count, b.Stuffs.Count, a.Stuffs.Count.ToString(), b.Stuffs.Count.ToString()));
        }
        Raise(nameof(HasCompare)); Raise(nameof(CompareNameA)); Raise(nameof(CompareNameB));
    }

    public RelayCommand AddJobCommand { get; }
    public RelayCommand RemoveJobCommand { get; }
    public RelayCommand AddStuffCommand { get; }
    public RelayCommand RemoveStuffCommand { get; }
    public RelayCommand CopyStuffCommand { get; }
    public RelayCommand CopySummaryCommand { get; }
    public RelayCommand AddCharacterCommand { get; }
    public RelayCommand RemoveCharacterCommand { get; }

    public CharactersViewModel(MainViewModel main)
    {
        _main = main;
        AddJobCommand = new RelayCommand(AddJob);
        RemoveJobCommand = new RelayCommand(p => RemoveJob(p as JobEntry));
        AddStuffCommand = new RelayCommand(AddStuff);
        RemoveStuffCommand = new RelayCommand(p => RemoveStuff(p as StuffLink));
        CopyStuffCommand = new RelayCommand(p => { if (p is StuffLink s) _main.CopyWithToast(s.Url, "Lien copié"); });
        CopySummaryCommand = new RelayCommand(CopySummary);
        AddCharacterCommand = new RelayCommand(() =>
        {
            Selected = _main.AddCharacter();
            NameError = "";
            RaiseCount();
        });
        RemoveCharacterCommand = new RelayCommand(() =>
        {
            if (_selected == null || Characters.Count <= 1) return;
            int i = Characters.IndexOf(_selected);
            if (_main.RemoveCharacter(_selected))
            {
                Selected = Characters[Math.Min(i, Characters.Count - 1)];
                NameError = "";
                RaiseCount();
            }
        });
        Reload();
    }

    public ObservableCollection<Character> Characters => _main.Characters;

    private string _nameError = "";
    public string NameError { get => _nameError; set => Set(ref _nameError, value); }
    public bool CanRemove => Characters.Count > 1;
    private void RaiseCount() => Raise(nameof(CanRemove));

    /// <summary>Nom du personnage choisi, modifiable : le changement est appliqué en quittant le champ ou avec Entrée.</summary>
    public string EditName
    {
        get => _selected?.Name ?? "";
        set
        {
            NameError = _main.RenameCharacter(_selected, value);
            Raise(nameof(EditName));
            RefreshCompare();
        }
    }

    public Character Selected
    {
        get => _selected;
        set
        {
            if (value == null || !Set(ref _selected, value)) return;
            _main.Data.SelectedCharacter = Math.Max(0, Characters.IndexOf(value));
            _main.RequestSave();
            Error = "";
            NameError = "";
            Raise(nameof(EditName));
            RefreshJobs();
            RefreshStuffs();
            RefreshCompare();
        }
    }

    /// <summary>Appelé au démarrage et après un import de données.</summary>
    public void Reload()
    {
        var i = Math.Min(Math.Max(0, _main.Data.SelectedCharacter), Math.Max(0, Characters.Count - 1));
        _selected = Characters.Count > 0 ? Characters[i] : null;
        Raise(nameof(Selected));
        Raise(nameof(EditName));
        Raise(nameof(CanRemove));
        Raise(nameof(Characters));
        RefreshJobs();
        RefreshStuffs();
        RefreshCompare();
    }

    // ───── Métiers ─────

    public string NewJob { get => _newJob; set => Set(ref _newJob, value); }

    private void RefreshJobs()
    {
        AvailableJobs.Clear();
        if (_selected == null) return;
        foreach (var j in GameLists.Jobs.Where(n => _selected.Jobs.All(x => x.Name != n))) AvailableJobs.Add(j);
        _newJob = AvailableJobs.FirstOrDefault();
        Raise(nameof(NewJob));
        Raise(nameof(NoJobs));
    }

    public bool NoJobs => _selected == null || _selected.Jobs.Count == 0;

    private void AddJob()
    {
        if (_selected == null || string.IsNullOrEmpty(_newJob)) return;
        _selected.Jobs.Add(new JobEntry { Name = _newJob, Level = 1 });
        _main.RequestSave();
        RefreshJobs();
    }

    private void RemoveJob(JobEntry job)
    {
        if (_selected == null || job == null) return;
        var ch = _selected;
        int idx = ch.Jobs.IndexOf(job);
        ch.Jobs.Remove(job);
        _main.RequestSave();
        RefreshJobs();
        _main.ShowUndo("Métier supprimé : " + job.Name, () =>
        {
            ch.Jobs.Insert(Math.Min(idx, ch.Jobs.Count), job);
            _main.RequestSave();
            if (ch == _selected) RefreshJobs();
        });
    }

    // ───── Stuffs ─────

    public string TagFilter { get => _tagFilter; set { if (value != null && Set(ref _tagFilter, value)) RefreshStuffs(); } }
    public string VerFilter { get => _verFilter; set { if (value != null && Set(ref _verFilter, value)) RefreshStuffs(); } }
    public string NewName { get => _newName; set => Set(ref _newName, value); }
    public string NewUrl { get => _newUrl; set => Set(ref _newUrl, value); }
    public string NewTag { get => _newTag; set { if (value != null) Set(ref _newTag, value); } }
    public string NewVersion { get => _newVersion; set { if (value != null) Set(ref _newVersion, value); } }
    public string Error { get => _error; set => Set(ref _error, value); }
    public bool NoStuff => VisibleStuffs.Count == 0;

    private void RefreshStuffs()
    {
        VisibleStuffs.Clear();
        if (_selected != null)
        {
            foreach (var s in _selected.Stuffs)
            {
                if (_tagFilter != "Tous" && s.Tag != _tagFilter) continue;
                if (_verFilter != "Toutes" && s.Version != _verFilter) continue;
                VisibleStuffs.Add(s);
            }
        }
        Raise(nameof(NoStuff));
    }

    private void AddStuff()
    {
        var url = (_newUrl ?? "").Trim();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            Error = "Colle un lien complet qui commence par http:// ou https://";
            return;
        }
        if (_selected == null) return;
        var name = (_newName ?? "").Trim();
        if (name.Length == 0) name = uri.Host;
        _selected.Stuffs.Add(new StuffLink { Name = name, Url = url, Tag = _newTag, Version = _newVersion });
        NewName = "";
        NewUrl = "";
        Error = "";
        _main.RequestSave();
        RefreshStuffs();
    }

    private void RemoveStuff(StuffLink s)
    {
        if (_selected == null || s == null) return;
        var ch = _selected;
        int idx = ch.Stuffs.IndexOf(s);
        ch.Stuffs.Remove(s);
        _main.RequestSave();
        RefreshStuffs();
        _main.ShowUndo("Lien supprimé : " + s.Name, () =>
        {
            ch.Stuffs.Insert(Math.Min(idx, ch.Stuffs.Count), s);
            _main.RequestSave();
            if (ch == _selected) RefreshStuffs();
        });
    }

    // ───── Résumé ─────

    private void CopySummary()
    {
        var c = _selected;
        if (c == null) return;
        var text = c.Name + " — " + c.ClassName + " " + c.LevelText
                   + " — Alignement : " + c.Faction + (c.Order != "Aucun" ? ", " + c.Order : "") + ", niv. " + c.AlignLevel
                   + (c.Jobs.Count > 0 ? " — Métiers : " + string.Join(", ", c.Jobs.Select(j => j.Name + " " + j.Level)) : "");
        _main.CopyWithToast(text, "Résumé copié");
    }
}
