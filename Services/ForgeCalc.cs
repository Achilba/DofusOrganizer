namespace DofusTeamOrganizer.Services;

/// <summary>Chances de la forgemagie : succès critique (SC), succès neutre (SN) et échec critique (EC), en %.</summary>
public readonly record struct ForgeResult(double Sc, double Sn, double Ec);

/// <summary>
/// Formule reconstituée à partir de la vidéo de DoFocus et de son simulateur.
/// D = 5 + 45·[palier 80 %] + (65 + 4·poids au-dessus du max)·[over] + 12·max(0, ligne/rune − 14)
///     − 20·max(0, 1,1 − qualité)·[niveau &lt; 170] + 60·qualité^12 ; SC = max(2, (100 − D)/2).
/// SN : 50 si SC reste positif ; quand (100 − D)/2 est négatif, SN descend jusqu'à 15 %. Un over retire 9 points de SN.
/// </summary>
public static class ForgeCalc
{
    /// <param name="lineWeight">Poids actuel de la ligne (valeur × poids par point).</param>
    /// <param name="runeWeight">Poids de la rune (points apportés × poids par point).</param>
    /// <param name="after">Valeur de la ligne après la rune.</param>
    /// <param name="quality">Poids total de l'objet ÷ poids total à ses jets maximum.</param>
    public static ForgeResult Chances(double lineWeight, double runeWeight, double after, double min, double max,
                                      double weightPerPoint, double quality, int level)
    {
        bool over = after > max;
        double pos = (after - min) / Math.Max(1.0, max - min);
        double tier = (!over && pos >= 0.8) ? 45 : 0;
        double overTerm = over ? 65 + 4 * Math.Max(0, (after - max) * weightPerPoint) : 0;
        double ratio = 12 * Math.Max(0, lineWeight / runeWeight - 14);
        double lvl = level < 170 ? 20 * Math.Max(0, 1.1 - quality) : 0;
        double d = 5 + tier + overTerm + ratio - lvl + 60 * Math.Pow(quality, 12);

        double x = (100 - d) / 2;
        double sc = Math.Max(2, x);
        double sn = x >= 0 ? Math.Min(50, 100 - sc) : Math.Max(15.1167, 50 - 0.7495 * Math.Abs(x));
        if (over) sn -= 9.163;
        sn = Math.Max(0, sn);
        return new ForgeResult(sc, sn, Math.Max(0, 100 - sc - sn));
    }
}
