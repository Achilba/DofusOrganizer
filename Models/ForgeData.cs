namespace DofusTeamOrganizer.Models;

/// <summary>Une caractéristique forgeable : poids par point, points de la petite rune et nombre de tailles de rune (1 = simple, 2 = + Pa, 3 = + Ra).</summary>
public record ForgePreset(string Name, double Weight, double Base, int Sizes)
{
    public override string ToString() => Name;
}

/// <summary>Ligne enregistrée dans la page Forgemagie.</summary>
public class ForgeLineData
{
    public string Stat { get; set; } = "";
    public double? Value { get; set; }
    public double? Min { get; set; }
    public double? Max { get; set; }
    public double Weight { get; set; } = 1;
}

public static class ForgeData
{
    /// <summary>Poids par point repris du guide DoFocus ; certaines valeurs restent à vérifier (rune Ra, rune Pa de certaines lignes).</summary>
    public static readonly ForgePreset[] Presets =
    {
        new("Vitalité", 0.2, 5, 3),
        new("Force", 1, 1, 3),
        new("Intelligence", 1, 1, 3),
        new("Chance", 1, 1, 3),
        new("Agilité", 1, 1, 3),
        new("Sagesse", 3, 1, 3),
        new("Initiative", 0.1, 10, 3),
        new("Pods", 0.25, 10, 3),
        new("Puissance", 2, 1, 3),
        new("Prospection", 3, 1, 2),
        new("Coups critiques (%)", 10, 1, 1),
        new("Dommages (fixe)", 5, 1, 2),
        new("Dommages Critiques", 5, 1, 2),
        new("Dommages Poussée", 5, 1, 2),
        new("Résistances (%)", 6, 1, 1),
        new("Tacle", 4, 1, 2),
        new("Fuite", 4, 1, 2),
        new("Retrait PA", 7, 1, 2),
        new("Retrait PM", 7, 1, 2),
        new("Résistance PA", 7, 1, 2),
        new("Résistance PM", 7, 1, 2),
        new("Autre (poids à saisir)", 1, 1, 3),
    };

    public static readonly ForgePreset Custom = Presets[Presets.Length - 1];

    public static ForgePreset Find(string name) => Presets.FirstOrDefault(p => p.Name == name) ?? Custom;
}
