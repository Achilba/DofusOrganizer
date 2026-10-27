using System.Windows;
using DofusTeamOrganizer.Models;

namespace DofusTeamOrganizer.Views;

/// <summary>Place mémorisée de la barre Mini, partagée avec la barre Mini Songe : on retrouve la barre là où elle était, quelle que soit celle qu'on ouvre.</summary>
internal static class MiniPlacement
{
    /// <summary>Identifie la configuration d'écrans actuelle (nombre, taille et position des écrans).</summary>
    public static string ScreenKey() =>
        (int)SystemParameters.VirtualScreenLeft + "," + (int)SystemParameters.VirtualScreenTop + "," +
        (int)SystemParameters.VirtualScreenWidth + "x" + (int)SystemParameters.VirtualScreenHeight;

    /// <summary>Place la fenêtre à sa place mémorisée. La barre Mini Songe a sa propre place (au premier lancement, elle reprend celle de la barre Mini).</summary>
    public static void Apply(Window w, AppData d, double fallbackWidth, bool songe = false)
    {
        var wa = SystemParameters.WorkArea;
        d.MiniPositions ??= new();
        d.SongeMiniPositions ??= new();
        double left, top;
        var key = ScreenKey();
        if (songe && d.SongeMiniPositions.TryGetValue(key, out var sp) && sp != null && sp.Length == 2)
        {
            left = sp[0];
            top = sp[1];
        }
        else if (!songe && d.MiniPositions.TryGetValue(key, out var p) && p != null && p.Length == 2)
        {
            left = p[0];
            top = p[1];
        }
        else if (songe && d.MiniPositions.TryGetValue(key, out var mp) && mp != null && mp.Length == 2)
        {
            left = mp[0];
            top = mp[1];
        }
        else
        {
            left = d.MiniLeft ?? (wa.Left + (wa.Width - fallbackWidth) / 2);
            top = d.MiniTop ?? (wa.Top + 8);
        }
        // garde la barre à l'écran si l'écran a changé
        left = Math.Max(SystemParameters.VirtualScreenLeft, Math.Min(left, SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 100));
        top = Math.Max(SystemParameters.VirtualScreenTop, Math.Min(top, SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 60));
        // Les deux valeurs sont calculées avant de déplacer la fenêtre (le déplacement enregistre la place)
        w.Left = left;
        w.Top = top;
    }

    public static void Remember(Window w, AppData d, bool songe = false)
    {
        if (songe)
        {
            d.SongeMiniLeft = w.Left;
            d.SongeMiniTop = w.Top;
            d.SongeMiniPositions ??= new();
            d.SongeMiniPositions[ScreenKey()] = new[] { w.Left, w.Top };
            return;
        }
        d.MiniLeft = w.Left;
        d.MiniTop = w.Top;
        d.MiniPositions ??= new();
        d.MiniPositions[ScreenKey()] = new[] { w.Left, w.Top };
    }
}
