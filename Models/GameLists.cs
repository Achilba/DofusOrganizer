namespace DofusTeamOrganizer.Models;

/// <summary>Listes de référence intégrées à l'appli.</summary>
public static class GameLists
{
    public const string CurrentVersion = "3.7";

    public static readonly string[] Classes =
    {
        "Crâ", "Ecaflip", "Eliotrope", "Eniripsa", "Enutrof", "Féca", "Forgelance", "Huppermage", "Iop", "Osamodas",
        "Ouginak", "Pandawa", "Roublard", "Sacrieur", "Sadida", "Sram", "Steamer", "Xélor", "Zobal"
    };

    public static readonly string[] Jobs =
    {
        // Récolte
        "Alchimiste", "Bûcheron", "Chasseur", "Mineur", "Paysan", "Pêcheur",
        // Artisanat
        "Bijoutier", "Bricoleur", "Cordonnier", "Façonneur", "Forgeron", "Sculpteur", "Tailleur",
        // Forgemagie
        "Cordomage", "Costumage", "Façomage", "Forgemage", "Joaillomage", "Sculptemage"
    };

    public static readonly string[] Factions = { "Bonta", "Brâkmar", "Neutre" };

    public static string[] OrdersFor(string faction) => faction switch
    {
        "Bonta" => new[] { "Aucun", "Œil attentif", "Cœur vaillant", "Esprit salvateur" },
        "Brâkmar" => new[] { "Aucun", "Œil putride", "Cœur saignant", "Esprit malsain" },
        _ => new[] { "Aucun" }
    };

    public static readonly string[] Versions = { "3.0", "3.1", "3.2", "3.3", "3.4", "3.5", "3.6", "3.7" };
    public static readonly string[] Tags = { "PvM", "PvP", "Songes" };

    public static readonly string[] AlignLevels = Enumerable.Range(1, 100).Select(i => i.ToString()).ToArray();

    public static readonly string[] LegendsTop = { "Bouclier", "Coffret acheté" };

    public static readonly string[] LegendsAnimal =
    {
        "Légende animale de Bakushana", "Légende animale de Kwakarticho", "Légende animale de Magicrabe", "Légende animale de Poukachi"
    };

    public static readonly string[] LegendsClassic =
    {
        "Légende d'Amayiro", "Légende d'Helséphine", "Légende d'Henual", "Légende d'Oto Mustam", "Légende de Brumaire",
        "Légende de Brâm Barbe-Monde", "Légende de Buhorado", "Légende de Corruption", "Légende de Crocobur",
        "Légende de Dame Jhessica", "Légende de Dodge", "Légende de Fallanster", "Légende de Ganymède", "Légende de Guerre",
        "Légende de Jahash Jurgen", "Légende de Menalt", "Légende de Mille Lieues", "Légende de Misère", "Légende de Mériana",
        "Légende de Rykke Errel", "Légende de Servitude", "Légende de Thanatena", "Légende du Cul Botté", "Légende du Destin",
        "Légende du Miroir", "Légende du Trompe-la-Mort"
    };

    public static string LevelText(int level) => level >= 200 ? "Oméga " + (level - 200) : "Niv. " + level;
}
