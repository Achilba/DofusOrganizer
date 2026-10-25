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
        if (name != "Detected" && name != "IsMain" && name != "Number") Changed?.Invoke();
    }
}

public class JobEntry : TrackedObject
{
    private int _level = 1;
    public string Name { get; set; } = "";
    public int Level { get => _level; set { if (Set(ref _level, Math.Min(200, Math.Max(1, value)))) Raise(nameof(CanDec)); } }
    [JsonIgnore] public bool CanDec => _level > 1;
}

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

    public string Name { get => _name; set => Set(ref _name, value); }
    public string ClassName { get => _className; set { if (value != null) Set(ref _className, value); } }
    public bool InSwap { get => _inSwap; set => Set(ref _inSwap, value); }
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
    [JsonIgnore] public string Initial => string.IsNullOrEmpty(_name) ? "?" : _name.Substring(0, 1).ToUpperInvariant();
}

public class AppData
{
    public int DataVersion { get; set; } = 1;

    public ObservableCollection<Character> Characters { get; set; } = new()
    {
        new Character { Name = "Achilpoi",  ClassName = "Crâ",        Color = "#F2B544", InSwap = true },
        new Character { Name = "Ablihpa",   ClassName = "Pandawa",    Color = "#7FD1B9", InSwap = true },
        new Character { Name = "Achilenu",  ClassName = "Enutrof",    Color = "#E8A07A", InSwap = true },
        new Character { Name = "Achilba",   ClassName = "Féca",       Color = "#9DB7F5", InSwap = true },
        new Character { Name = "Achicri",   ClassName = "Sacrieur",   Color = "#E57C7C", InSwap = false },
        new Character { Name = "Achilance", ClassName = "Forgelance", Color = "#B5C46B", InSwap = false },
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
