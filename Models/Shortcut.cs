using System.Windows.Input;

namespace DofusTeamOrganizer.Models;

/// <summary>Un raccourci clavier : modificateurs (valeurs de RegisterHotKey : 1 Alt, 2 Ctrl, 4 Maj, 8 Windows) + code de touche Windows.</summary>
public class ShortcutSpec
{
    public int Mods { get; set; }
    public int Vk { get; set; }

    public ShortcutSpec() { }
    public ShortcutSpec(int mods, int vk) { Mods = mods; Vk = vk; }

    public bool Same(ShortcutSpec o) => o != null && o.Mods == Mods && o.Vk == Vk;
    public override string ToString() => ShortcutDefs.Display(this);
}

/// <summary>Raccourcis par défaut, libellés et affichage.</summary>
public static class ShortcutDefs
{
    public const string Toggle = "toggle", Invites = "invites", Next = "next", Prev = "prev", Main = "main";

    /// <summary>Ordre d'affichage dans Paramètres.</summary>
    public static readonly string[] All = { Next, Prev, Main, Invites, Toggle };

    public static ShortcutSpec Default(string action) => action switch
    {
        Toggle => new ShortcutSpec(1 | 2, 0x4F),   // Ctrl+Alt+O (global)
        Invites => new ShortcutSpec(0, 0x7B),      // F12
        Next => new ShortcutSpec(0, 0xDC),         // * (AZERTY ; le * du pavé numérique est ajouté automatiquement)
        Prev => new ShortcutSpec(0, 0x75),         // F6
        Main => new ShortcutSpec(0, 0x70),         // F1
        _ => new ShortcutSpec(0, 0)
    };

    public static string Label(string action) => action switch
    {
        Toggle => "Afficher / masquer",
        Invites => "Copier les invitations",
        Next => "Personnage suivant",
        Prev => "Personnage précédent",
        Main => "Personnage principal",
        _ => action
    };

    public static string Display(ShortcutSpec s)
    {
        if (s == null || s.Vk == 0) return "—";
        var parts = new List<string>();
        if ((s.Mods & 2) != 0) parts.Add("Ctrl");
        if ((s.Mods & 1) != 0) parts.Add("Alt");
        if ((s.Mods & 4) != 0) parts.Add("Maj");
        if ((s.Mods & 8) != 0) parts.Add("Win");
        parts.Add(KeyName(s.Vk));
        return string.Join("+", parts);
    }

    private static string KeyName(int vk)
    {
        if (vk == 0xDC || vk == 0x6A) return "*";
        if (vk >= 0x70 && vk <= 0x87) return "F" + (vk - 0x6F);
        if ((vk >= 0x41 && vk <= 0x5A) || (vk >= 0x30 && vk <= 0x39)) return ((char)vk).ToString();
        try
        {
            var name = KeyInterop.KeyFromVirtualKey(vk).ToString();
            if (name.StartsWith("NumPad")) return "Pavé " + name.Substring(6);
            return name;
        }
        catch { return "Touche " + vk; }
    }

    /// <summary>Une touche seule (sans Ctrl/Alt/Win) n'est autorisée que si elle ne gêne pas la saisie dans DOFUS : touches F1…F24, * et touches d'opération du pavé numérique.</summary>
    public static bool AllowedAlone(int vk) =>
        (vk >= 0x70 && vk <= 0x87) || vk == 0xDC || (vk >= 0x6A && vk <= 0x6F);
}
