using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using DofusTeamOrganizer.ViewModels;

namespace DofusTeamOrganizer.Models;

/// <summary>Objet de données : toute modification déclenche l'événement Changed (→ sauvegarde).</summary>
public class TrackedObject : ObservableObject
{
    public static event Action Changed;

    /// <summary>Prévient les vues sans déclencher de sauvegarde (valeurs calculées).</summary>
    protected void RaiseQuiet(string name) => base.Raise(name);

    /// <summary>Demande une sauvegarde (modification d'une liste, qui n'est pas suivie automatiquement).</summary>
    protected static void NotifyChanged() => Changed?.Invoke();

    protected override void Raise([CallerMemberName] string name = null)
    {
        base.Raise(name);
        if (name != "Ini" && name != "CanAddSlot" && name != "CanDec" && name != "ClassName" && name != "Detected" && name != "IsMain" && name != "Number" && name != "LevelValue" && name != "IsOmega" && name != "FactionColor" && name != "OrderText" && name != "JobChips" && name != "HasJobs" && name != "RoutinesText" && name != "HasRoutines") Changed?.Invoke();
    }
}

public class JobEntry : TrackedObject
{
    private int _level = 1;
    public string Name { get; set; } = "";
    public int Level { get => _level; set { if (Set(ref _level, Math.Min(200, Math.Max(1, value)))) Raise(nameof(CanDec)); } }
    [JsonIgnore] public bool CanDec => _level > 1;
}

/// <summary>Un métier affiché sur la carte de l'accueil.</summary>
public record JobChip(string Name, int Level, bool Max);

public class StuffLink
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public string Tag { get; set; } = "PvM";
    public string Version { get; set; } = GameLists.CurrentVersion;
}

public class Character : TrackedObject
{
    private string _name = "";
    private string _className = "";
    private bool _inSwap = true;
    private bool _detected;
    private bool _isMain;
    private int _number;
    private int _charLevel = 1;
    private string _faction = "Neutre";
    private string _order = "Aucun";
    private int _alignLevel = 1;
    private bool _achTracked = true;

    public string Name { get => _name; set => Set(ref _name, value); }
    public string ClassName { get => _className; set { if (value != null) Set(ref _className, value); } }
    public bool InSwap { get => _inSwap; set { if (Set(ref _inSwap, value)) OnFeature(); } }
    /// <summary>Personnage suivi dans la vue Succès de la page Donjons (et dans la colonne « Manque »).</summary>
    public bool AchTracked { get => _achTracked; set { if (Set(ref _achTracked, value)) OnFeature(); } }

    // Fonctions où le personnage apparaît (Paramètres) : décoché = il disparaît de la fonction, ses données sont gardées
    private bool _showHome = true;
    private bool _showTasks = true;
    private bool _showDreams = true;
    private bool _showNotes = true;
    public bool ShowHome { get => _showHome; set { if (Set(ref _showHome, value)) OnFeature(); } }
    public bool ShowTasks { get => _showTasks; set { if (Set(ref _showTasks, value)) OnFeature(); } }
    public bool ShowDreams { get => _showDreams; set { if (Set(ref _showDreams, value)) OnFeature(); } }
    public bool ShowNotes { get => _showNotes; set { if (Set(ref _showNotes, value)) OnFeature(); } }
    /// <summary>Levé quand une de ces cases change (l'appli rafraîchit alors les pages concernées).</summary>
    public static event Action FeatureChanged;
    private void OnFeature() { RaiseQuiet(nameof(AnyOff)); FeatureChanged?.Invoke(); }
    /// <summary>Vrai si au moins une fonction est décochée (la ligne est assombrie dans les Paramètres).</summary>
    [JsonIgnore] public bool AnyOff => !(_showHome && _showTasks && _showDreams && _showNotes && _achTracked && _inSwap);
    public string Color { get; set; } = "#F2B544";

    public int CharLevel
    {
        get => _charLevel;
        set { if (Set(ref _charLevel, Math.Max(1, value))) Raise(nameof(LevelText)); }
    }

    public string Faction
    {
        get => _faction;
        set
        {
            if (value == null || !Set(ref _faction, value)) return;
            Raise(nameof(Orders));
            if (!Orders.Contains(_order)) Order = "Aucun";
            Raise(nameof(Order));
        }
    }

    public string Order { get => _order; set { if (value != null) Set(ref _order, value); } }
    public int AlignLevel { get => _alignLevel; set => Set(ref _alignLevel, Math.Min(100, Math.Max(1, value))); }

    public ObservableCollection<JobEntry> Jobs { get; set; } = new();
    public ObservableCollection<StuffLink> Stuffs { get; set; } = new();

    [JsonIgnore] public string[] Orders => GameLists.OrdersFor(_faction);
    [JsonIgnore] public string LevelText => GameLists.LevelText(_charLevel);
    [JsonIgnore] public bool Detected { get => _detected; set => Set(ref _detected, value); }
    [JsonIgnore] public bool IsMain { get => _isMain; set => Set(ref _isMain, value); }
    [JsonIgnore] public int Number { get => _number; set => Set(ref _number, value); }
    // ───── Infos de la carte de l'accueil (calculées, jamais enregistrées) ─────
    /// <summary>Niveau affiché dans la carte : « 150 » ; au-delà de 200, seulement le chiffre Oméga (260 donne « 60 »), le mot Oméga étant écrit à part en petit.</summary>
    [JsonIgnore] public string LevelValue => _charLevel > 200 ? (_charLevel - 200).ToString() : _charLevel.ToString();
    [JsonIgnore] public bool IsOmega => _charLevel > 200;
    /// <summary>Couleur du nom de la cité : bleu clair pour Bonta, rouge pour Brâkmar.</summary>
    [JsonIgnore] public string FactionColor => _faction == "Bonta" ? "#7CC4FF" : _faction == "Brâkmar" ? "#FF6B5F" : "#F3F0E8";
    /// <summary>Sous-titre de l'alignement : ordre et niveau d'alignement.</summary>
    [JsonIgnore] public string OrderText => _faction == "Neutre" || string.IsNullOrEmpty(_faction) ? "Aucune faction"
        : (string.IsNullOrEmpty(_order) || _order == "Aucun" ? "Sans ordre" : _order) + " · niveau " + _alignLevel;
    /// <summary>Métiers du personnage, du plus haut niveau au plus bas.</summary>
    [JsonIgnore] public JobChip[] JobChips => Jobs.OrderByDescending(j => j.Level).ThenBy(j => j.Name).Select(j => new JobChip(j.Name, j.Level, j.Level >= 200)).ToArray();
    [JsonIgnore] public bool HasJobs => Jobs.Count > 0;
    private int _routinesLeft;
    [JsonIgnore] public bool HasRoutines => _routinesLeft > 0;
    [JsonIgnore] public string RoutinesText => _routinesLeft + (_routinesLeft > 1 ? " tâches à faire" : " tâche à faire");
    public void SetRoutinesLeft(int n) { if (n == _routinesLeft) return; _routinesLeft = n; Raise(nameof(RoutinesText)); Raise(nameof(HasRoutines)); }
    /// <summary>À appeler quand on affiche l'accueil : le niveau, l'alignement ou les métiers ont pu changer.</summary>
    public void RefreshCardInfo()
    {
        Raise(nameof(LevelValue)); Raise(nameof(IsOmega)); Raise(nameof(FactionColor)); Raise(nameof(OrderText)); Raise(nameof(JobChips)); Raise(nameof(HasJobs));
    }
    [JsonIgnore] public string Initial => string.IsNullOrEmpty(_name) ? "?" : _name.Substring(0, 1).ToUpperInvariant();
}

/// <summary>Une ligne d'équipe d'une rubrique de strat : un personnage et l'un de ses stuffs.</summary>
public class StratSlot : TrackedObject
{
    /// <summary>Renvoie les stuffs d'un personnage (branché par StratViewModel).</summary>
    public static Func<string, IEnumerable<StuffLink>> Resolver;
    /// <summary>Renvoie la classe d'un personnage (pour l'icône).</summary>
    public static Func<string, string> ClassResolver;
    private static readonly int[] Choices = { 1, 2, 3, 4 };

    private int _ini = 1;
    private string _char = "";
    private string _stuff = "";

    public string Char
    {
        get => _char;
        set { if (value != null && Set(ref _char, value)) { _lastJoin = null; Stuff = ""; Raise(nameof(StuffChoices)); RaiseQuiet(nameof(ClassName)); } }
    }
    [JsonIgnore] public string ClassName => ClassResolver?.Invoke(_char) ?? "";
    [JsonIgnore] public StratRun Owner { get; set; }
    [JsonIgnore] public int[] IniChoices => Choices;
    /// <summary>Position dans l'ordre d'initiative (1 à 4) : c'est la place dans la liste ; la changer déplace la carte.</summary>
    [JsonIgnore]
    public int Ini
    {
        get => _ini;
        set { if (value != _ini && value >= 1 && Owner != null) Owner.MoveSlot(this, value - 1); }
    }
    public void SetIni(int n) { if (_ini == n) return; _ini = n; RaiseQuiet(nameof(Ini)); }
    public string Stuff
    {
        get => _stuff;
        set { if (value != null && Set(ref _stuff, value)) { Raise(nameof(Url)); Raise(nameof(HasUrl)); } }
    }

    [JsonIgnore] public IEnumerable<string> StuffChoices => (Resolver?.Invoke(_char) ?? Enumerable.Empty<StuffLink>()).Select(s => s.Name).ToList();
    [JsonIgnore] public string Url => Resolver?.Invoke(_char)?.FirstOrDefault(s => s.Name == _stuff)?.Url ?? "";
    [JsonIgnore] public bool HasUrl => !string.IsNullOrWhiteSpace(Url);
    private string _lastJoin;
    /// <summary>Relit les stuffs du personnage ; ne touche à l'affichage que si la liste a changé (sinon la sélection serait perdue).</summary>
    public void RefreshChoices()
    {
        var j = string.Join("\u001f", StuffChoices);
        if (j == _lastJoin) return;
        _lastJoin = j;
        Raise(nameof(StuffChoices)); Raise(nameof(Url)); Raise(nameof(HasUrl)); Raise(nameof(Stuff));
    }
}

/// <summary>Un run type de la strat (par exemple « Paradoxe 1 Salle ») : équipe choisie dans l'ordre d'initiative, texte libre et nombre de runs faits.</summary>
public class StratRun : TrackedObject
{
    public const int MaxSlots = 4;

    private string _name = "";
    private string _text = "";
    private int _count;
    private ObservableCollection<StratSlot> _slots;

    public StratRun() { Slots = new ObservableCollection<StratSlot>(); }

    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get => _name; set => Set(ref _name, value ?? ""); }
    public string Text { get => _text; set => Set(ref _text, value ?? ""); }
    /// <summary>Nombre de runs réalisés.</summary>
    public int Count { get => _count; set { if (Set(ref _count, Math.Max(0, value))) RaiseQuiet(nameof(CanDec)); } }
    /// <summary>Date du dernier run ajouté avec le compteur.</summary>
    public DateTime? LastRun { get; set; }
    [JsonIgnore] public bool CanDec => _count > 0;
    [JsonIgnore] public bool CanAddSlot => _slots.Count < MaxSlots;

    /// <summary>Équipe, dans l'ordre d'initiative (la première carte est l'INI 1).</summary>
    public ObservableCollection<StratSlot> Slots
    {
        get => _slots;
        set
        {
            if (_slots != null) _slots.CollectionChanged -= SlotsChanged;
            _slots = value ?? new ObservableCollection<StratSlot>();
            _slots.CollectionChanged += SlotsChanged;
            Renumber();
        }
    }

    private void SlotsChanged(object sender, NotifyCollectionChangedEventArgs e) => Renumber();

    private void Renumber()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            _slots[i].Owner = this;
            _slots[i].SetIni(i + 1);
        }
        RaiseQuiet(nameof(CanAddSlot));
    }

    /// <summary>Place une carte à un nouvel endroit de l'ordre d'initiative.</summary>
    public void MoveSlot(StratSlot s, int newIndex)
    {
        int i = _slots.IndexOf(s);
        if (i < 0) return;
        newIndex = Math.Max(0, Math.Min(newIndex, _slots.Count - 1));
        if (newIndex != i)
        {
            _slots.Move(i, newIndex);
            NotifyChanged();
        }
        else Renumber();
    }

    /// <summary>Texte copié par « Copier la strat » : équipe dans l'ordre INI, puis la stratégie.</summary>
    public string Summary()
        => _name + "\n" + string.Join("\n", _slots.Select((x, i) => "INI " + (i + 1) + " · " + x.Char + (string.IsNullOrEmpty(x.Stuff) ? "" : " · " + x.Stuff))) + "\n\n" + _text;
}

/// <summary>Dernière légende obtenue : son nom, l'heure et le numéro du run en cours à ce moment.</summary>
public class DropInfo
{
    public string Name { get; set; } = "";
    public DateTime At { get; set; }
    public int Run { get; set; }
    public string RunName { get; set; } = "";
    /// <summary>Nombre total de runs comptés au moment du drop (sert aux statistiques).</summary>
    public int Total { get; set; }
}

/// <summary>Une tâche récurrente (par personnage) : tous les jours à minuit, ou un jour fixe de la semaine à 3 h.</summary>
public class RoutineDef
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public bool Weekly { get; set; }
    /// <summary>Jour de la remise à zéro hebdomadaire (0 = dimanche … 6 = samedi) ; 2 = mardi.</summary>
    public int Weekday { get; set; } = 2;
    /// <summary>Affiche une notification quand la tâche est remise à zéro.</summary>
    public bool Notify { get; set; }
}

public class AppData
{
    public ObservableCollection<StratRun> StratRuns { get; set; } = new()
    {
        new StratRun { Name = "Paradoxe 1 Salle" },
        new StratRun { Name = "Paradoxe 1 Boss" },
    };
    public string StratSelected { get; set; } = "";
    /// <summary>Dernière légende obtenue (page Songes, vue Drops).</summary>
    public DropInfo LastDrop { get; set; }
    /// <summary>Runs ajoutés aujourd'hui avec le compteur (remis à zéro quand la date change).</summary>
    public int RunsToday { get; set; }
    public string RunsDay { get; set; } = "";
    /// <summary>Total de runs comptés depuis l'arrivée des statistiques (0.12.0).</summary>
    public int RunsTotal { get; set; }
    /// <summary>Historique des légendes obtenues (les 300 dernières), pour les statistiques.</summary>
    public List<DropInfo> DropHistory { get; set; } = new();
    /// <summary>Vue de la page Songes : « Strat » ou « Drops ».</summary>
    public string SongesMode { get; set; } = "Strat";

    public List<RoutineDef> Routines { get; set; } = new()
    {
        new RoutineDef { Id = "guildaton", Name = "Guildaton", Weekly = true, Weekday = 2 },
        new RoutineDef { Id = "trousseau", Name = "Trousseau de clés", Weekly = true, Weekday = 2, Notify = true },
    };
    /// <summary>Cases cochées, sous la forme « identifiant de tâche|nom du personnage ».</summary>
    public List<string> RoutineDone { get; set; } = new();
    /// <summary>Dernière période vue de chaque tâche (sert à savoir quand remettre à zéro).</summary>
    public Dictionary<string, string> RoutinePeriod { get; set; } = new();

    /// <summary>1 = avant la case « Suivi dans les succès » ; 2 = actuel. Les anciens fichiers sont migrés au chargement (Storage).</summary>
    public int DataVersion { get; set; } = 2;

    public ObservableCollection<Character> Characters { get; set; } = new()
    {
        new Character { Name = "Personnage 1", ClassName = "Iop", Color = "#F2B544", InSwap = true },
    };

    public int SelectedCharacter { get; set; } = 0;
    public bool SwapEnabled { get; set; } = true;
    public bool MiniAlwaysOnTop { get; set; } = true;
    public double MiniOpacity { get; set; } = 1.0;
    /// <summary>Taille du texte et des éléments de la fenêtre principale (1 = 100 %).</summary>
    public double UiScale { get; set; } = 1.0;
    public double? MiniLeft { get; set; }
    public double? MiniTop { get; set; }
    /// <summary>Position de la barre Mini réservée aux songes.</summary>
    public double? SongeMiniLeft { get; set; }
    public double? SongeMiniTop { get; set; }
    public Dictionary<string, double[]> SongeMiniPositions { get; set; } = new();
    /// <summary>Position de la mini-barre mémorisée par configuration d'écrans (clé = taille et origine de l'écran virtuel).</summary>
    public Dictionary<string, double[]> MiniPositions { get; set; } = new();
    /// <summary>Raccourcis personnalisés (clé = action). Absent : valeur par défaut.</summary>
    public Dictionary<string, ShortcutSpec> Shortcuts { get; set; } = new();
    public bool StartInMini { get; set; } = false;
    public bool AutoBackup { get; set; } = true;
    public bool RememberMiniPosition { get; set; } = true;
    public bool MiniCompact { get; set; } = false;
    public string LastBackup { get; set; } = "";
    /// <summary>Dernier dossier choisi pour « Copier ailleurs ».</summary>
    public string ExternalBackupDir { get; set; } = "";
    public string LastSeenVersion { get; set; } = "";
    /// <summary>Thème de couleurs (clé de ThemeCatalog).</summary>
    public string Theme { get; set; } = "classique";

    public List<string> FavoriteZaaps { get; set; } = new(ZaapData.DefaultFavorites);
    public List<string> DofusDone { get; set; } = new();
    /// <summary>Dofus au statut « Démarré » (ni à faire, ni terminé).</summary>
    public List<string> DofusStarted { get; set; } = new();
    public List<string> DungeonsDone { get; set; } = new();
    /// <summary>Succès de donjon cochés, par nom de personnage : « Boss|0..4 » (0 à 2 = challenges, 3 = Duo/Trio/Quatuor, 4 = Spécial).</summary>
    public Dictionary<string, List<string>> BossAch { get; set; } = new();
    public List<string> AlmanaxDone { get; set; } = new();
    public List<string> AlmanaxPrepared { get; set; } = new();
    public Dictionary<string, int> Legends { get; set; } = new();
    public Dictionary<string, string> Notes { get; set; } = new();
}
