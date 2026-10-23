namespace DofusTeamOrganizer.Models;

public record ZaapEntry(string Label, string Coord)
{
    public string Command => "/zaap " + Coord;

    /// <summary>Nom court affiché sur les boutons favoris (ex. « Bonta »).</summary>
    public string ShortName => Coord switch
    {
        "-31,-56" => "Bonta",
        "-26,37" => "Brâkmar",
        "-5,-23" => "Almanax",
        "-20,-20" => "Tour des Voyageurs",
        "-27,-36" => "Malle aux Trésors",
        _ => Label.Contains(" · ") ? Label.Substring(Label.IndexOf(" · ") + 3) : Label
    };
}

public static class ZaapData
{
    public static readonly ZaapEntry[] All =
    {
        new("Amakna · Bord de la forêt maléfique", "-1,13"),
        new("Amakna · Château d'Amakna", "3,-5"),
        new("Amakna · Coin des Bouftous", "5,7"),
        new("Amakna · Montagne des Craqueleurs", "-5,-8"),
        new("Amakna · Plaine des Scarafeuilles", "-1,24"),
        new("Amakna · Port de Madrestam", "7,-4"),
        new("Amakna · Village d'Amakna", "-2,0"),
        new("Astrub · Arche de Vili", "15,-20"),
        new("Astrub · Cité d'Astrub", "5,-18"),
        new("Bonta · Cœur immaculé", "-31,-56"),
        new("Brâkmar · La Cuirasse", "-26,37"),
        new("Cania · Champs de Cania", "-27,-36"),
        new("Cania · Lac de Cania", "-3,-42"),
        new("Cania · Massif de Cania", "-13,-28"),
        new("Cania · Plaine des Porkass (Almanax)", "-5,-23"),
        new("Cania · Plaines Rocheuses", "-17,-47"),
        new("Cania · Routes Rocailleuses", "-20,-20"),
        new("Cania · Village des Kanigs", "0,-56"),
        new("Dopeuls · Village des Dopeuls", "-34,-8"),
        new("Écailles · Crocuzko", "-83,-15"),
        new("Foire du Trool", "-11,-36"),
        new("Frigost · Entrée du château de Harebourg", "-67,-77"),
        new("Frigost · La Bourgade", "-78,-41"),
        new("Frigost · Village enseveli", "-77,-73"),
        new("Grobe · Mont des Tombeaux", "40,-44"),
        new("Incarnam · Cimetière", "3,0"),
        new("Incarnam · Pâturages", "2,-5"),
        new("Incarnam · Route des âmes", "-1,-3"),
        new("Koalaks · Cimetière primitif", "-12,19"),
        new("Koalaks · Village des Éleveurs", "-16,1"),
        new("Moon · Plage de la Tortue", "35,12"),
        new("Nimotopia", "-67,29"),
        new("Otomaï · Village côtier", "-46,18"),
        new("Otomaï · Village de la Canopée", "-54,16"),
        new("Pandala · Village de Pandala", "20,-29"),
        new("Saharach · Dunes des ossements", "15,-58"),
        new("Sidimote · Route des Roulottes", "-25,12"),
        new("Sidimote · Terres Désacrées", "-15,25"),
        new("Sufokia · Rivage sufokien", "10,22"),
        new("Sufokia · Sufokia", "13,26"),
        new("Sufokia · Temple des alliances", "13,35"),
        new("Tainéla", "1,-32"),
        new("Valonia · Futaie enneigée", "39,-82"),
        new("Wabbits · Île de la Cawotte", "25,-4"),
        new("Wabbits · Laboratoires abandonnés", "27,-14"),
    };

    // Favoris par défaut (coordonnées)
    public static readonly string[] DefaultFavorites = { "-31,-56", "-26,37", "-5,-23", "-20,-20", "-27,-36" };
}
