using System.Windows;
using System.Windows.Media;

namespace DofusTeamOrganizer.Services;

/// <summary>Un thème de couleurs : les jeux de couleurs remplacent les brosses de App.xaml (toute l'appli les suit en direct).</summary>
public class ThemeDef
{
    public string Key { get; init; }
    public string Name { get; init; }
    public Dictionary<string, Color> Colors { get; init; }
    public Color Accent => Colors["Accent"];
    public Color Surface => Colors["Panel"];
    public Color Background => Colors["Bg"];
}

public static class ThemeCatalog
{
    public const string DefaultKey = "classique";

    public static readonly ThemeDef[] All =
    {
        // Classique : les couleurs d'origine de l'appli
        new()
        {
            Key = DefaultKey, Name = "Classique", Colors = new()
            {
                ["Bg"] = C("#0E1014"), ["Header"] = C("#0A0C0F"), ["Side"] = C("#12151B"), ["Panel"] = C("#161A21"),
                ["Field"] = C("#1E232C"), ["Line"] = C("#2B313C"), ["Line2"] = C("#3A4150"), ["KeyBg"] = C("#2A2F3A"),
                ["Accent"] = C("#F2B544"), ["AccentText"] = C("#1B1406"), ["Text"] = C("#F3F0E8"),
                ["Muted"] = C("#CBD0DA"), ["Dim"] = C("#A6AEBD"),
                ["Hover"] = C("#1A1F28"), ["Active"] = C("#232936"), ["AccentSoft"] = C("#2A2416"), ["Track"] = C("#3A4150"),
                ["Hot"] = C("#FF7A2F"), ["HotText"] = C("#1A0E04"),
            }
        },
        Derived("bonta", "Bonta", 215, 0.34, "#5FA8FF", "#FF9F3D"),
        Derived("brakmar", "Brâkmar", 355, 0.30, "#F0544F", "#FFB347"),
        Derived("cawotte", "Cawotte", 24, 0.30, "#E8793A", "#2FD0B8"),
        Derived("emeraude", "Émeraude", 150, 0.28, "#5FD97A", "#FF8A5C"),
        Derived("pourpre", "Pourpre", 275, 0.30, "#B57BFF", "#FFC857"),
        Derived("turquoise", "Turquoise", 188, 0.32, "#35D0E0", "#FF7A66"),
        Derived("ebene", "Ébène", 40, 0.04, "#E6E1D5", "#F2A33A"),
    };

    public static ThemeDef Get(string key) => All.FirstOrDefault(t => t.Key == key) ?? All[0];

    /// <summary>Applique le thème : remplace les brosses des ressources de l'application (les vues utilisent DynamicResource).</summary>
    public static void Apply(string key)
    {
        var app = Application.Current;
        if (app == null) return;
        var theme = Get(key);
        foreach (var kv in theme.Colors)
        {
            var brush = new SolidColorBrush(kv.Value);
            brush.Freeze();
            app.Resources[kv.Key] = brush;
        }
    }

    private static Color C(string hex) => (Color)ColorConverter.ConvertFromString(hex);

    /// <summary>Thème construit à partir d'une teinte, d'une couleur d'accent et d'une couleur « vedette » (boutons de l'accueil) : les fonds sont des niveaux de gris teintés.</summary>
    private static ThemeDef Derived(string key, string name, double hue, double sat, string accentHex, string hotHex)
    {
        var accent = C(accentHex);
        var panel = Hsl(hue, sat, 0.10);
        return new ThemeDef
        {
            Key = key, Name = name, Colors = new()
            {
                ["Header"] = Hsl(hue, sat, 0.045),
                ["Bg"] = Hsl(hue, sat, 0.065),
                ["Side"] = Hsl(hue, sat, 0.085),
                ["Panel"] = panel,
                ["Field"] = Hsl(hue, sat, 0.135),
                ["Line"] = Hsl(hue, sat, 0.175),
                ["KeyBg"] = Hsl(hue, sat, 0.19),
                ["Line2"] = Hsl(hue, sat, 0.24),
                ["Hover"] = Hsl(hue, sat, 0.115),
                ["Active"] = Hsl(hue, sat, 0.165),
                ["Track"] = Hsl(hue, sat, 0.24),
                ["Accent"] = accent,
                ["AccentText"] = C("#14110A"),
                ["Hot"] = C(hotHex), ["HotText"] = C("#1A0E04"),
                ["AccentSoft"] = Mix(panel, accent, 0.14),
                ["Text"] = C("#F3F0E8"),
                ["Muted"] = Hsl(hue, Math.Min(sat, 0.14), 0.80),
                ["Dim"] = Hsl(hue, Math.Min(sat, 0.10), 0.69),
            }
        };
    }

    private static Color Mix(Color a, Color b, double t) => Color.FromRgb(
        (byte)Math.Round(a.R + (b.R - a.R) * t),
        (byte)Math.Round(a.G + (b.G - a.G) * t),
        (byte)Math.Round(a.B + (b.B - a.B) * t));

    /// <summary>Teinte en degrés (0 à 360), saturation et luminosité de 0 à 1.</summary>
    private static Color Hsl(double h, double s, double l)
    {
        double c = (1 - Math.Abs(2 * l - 1)) * s;
        double hp = (h % 360) / 60.0;
        double x = c * (1 - Math.Abs(hp % 2 - 1));
        double r = 0, g = 0, b = 0;
        if (hp < 1) { r = c; g = x; }
        else if (hp < 2) { r = x; g = c; }
        else if (hp < 3) { g = c; b = x; }
        else if (hp < 4) { g = x; b = c; }
        else if (hp < 5) { r = x; b = c; }
        else { r = c; b = x; }
        double m = l - c / 2;
        return Color.FromRgb((byte)Math.Round((r + m) * 255), (byte)Math.Round((g + m) * 255), (byte)Math.Round((b + m) * 255));
    }
}
