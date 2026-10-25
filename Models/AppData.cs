using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using DofusTeamOrganizer.ViewModels;

namespace DofusTeamOrganizer.Models;

/// <summary>Objet de données : toute modification déclenche l'événement Changed (→ sauvegarde).</summary>
public class TrackedObject : ObservableObject
{
    public static event Action Changed;

    protected override void Raise([CallerMemberName] string name = null)
    {
        base.Raise(name);
        if (name != "Detected" && name != "IsMain" && name != "Number" && name != "LevelValue" && name != "FactionColor" && name != "OrderText" && name != "JobChips" && name != "HasJobs") Changed?.Invoke();
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
    public bool InSwap { get => _inSwap; set => Set(ref _inSwap, value); }
    /// <summary>Personnage suivi dans la vue Succès de la page Donjons (et dans la colonne « Manque »).</summary>
    public bool AchTracked { get => _achTracked; set => Set(ref _achTracked, value); }
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
    /// <summary>Niveau affiché dans la carte : « 150 », ou « Oméga 60 » au-delà de 200.</summary>
    [JsonIgnore] public string LevelValue => _charLevel >= 200 ? LevelText : _charLevel.ToString();
    /// <summary>Couleur du nom de la cité : bleu clair pour Bonta, rouge pour Brâkmar.</summary>
    [JsonIgnore] public string FactionColor => _faction == "Bonta" ? "#7CC4FF" : _faction == "Brâkmar" ? "#FF6B5F" : "#F3F0E8";
    /// <summary>Sous-titre de l'alignement : ordre et niveau d'alignement.</summary>
    [JsonIgnore] public string OrderText => _faction == "Neutre" || string.IsNullOrEmpty(_faction) ? "Aucune faction"
        : (string.IsNullOrEmpty(_order) || _order == "Aucun" ? "Sans ordre" : _order) + " · niveau " + _alignLevel;
    /// <summary>Métiers du personnage, du plus haut niveau au plus bas.</summary>
    [JsonIgnore] public JobChip[] JobChips => Jobs.OrderByDescending(j => j.Level).ThenBy(j => j.Name).Select(j => new JobChip(j.Name, j.Level, j.Level >= 200)).ToArray();
    [JsonIgnore] public bool HasJobs => Jobs.Count > 0;
    /// <summary>À appeler quand on affiche l'accueil : le niveau, l'alignement ou les métiers ont pu changer.</summary>
    public void RefreshCardInfo()
    {
        Raise(nameof(LevelValue)); Raise(nameof(FactionColor)); Raise(nameof(OrderText)); Raise(nameof(JobChips)); Raise(nameof(HasJobs));
    }
    [JsonIgnore] public string Initial => string.IsNullOrEmpty(_name) ? "?" : _name.Substring(0, 1).ToUpperInvariant();
}

public class AppData
{
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
    public double? MiniLeft { get; set; }
    public double? MiniTop { get; set; }
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
