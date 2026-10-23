using System.Diagnostics;

namespace DofusTeamOrganizer.Services;

/// <summary>Détection des fenêtres DOFUS (par nom de processus) et activation par personnage.</summary>
public static class DofusWindows
{
    private static bool IsDofusProcess(IntPtr h)
    {
        try
        {
            Native.GetWindowThreadProcessId(h, out uint pid);
            if (pid == (uint)Environment.ProcessId) return false;
            using var p = Process.GetProcessById((int)pid);
            return p.ProcessName.Contains("dofus", StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    public static bool IsForegroundDofus()
    {
        var h = Native.GetForegroundWindow();
        return h != IntPtr.Zero && IsDofusProcess(h);
    }

    /// <summary>Titre de chaque fenêtre visible dont le titre n'est pas vide.</summary>
    private static List<(IntPtr Handle, string Title)> VisibleWindows()
    {
        var list = new List<(IntPtr, string)>();
        Native.EnumWindows((h, _) =>
        {
            if (Native.IsWindowVisible(h))
            {
                var t = Native.GetTitle(h);
                if (t.Length > 0) list.Add((h, t));
            }
            return true;
        }, IntPtr.Zero);
        return list;
    }

    /// <summary>Pour chaque nom, la fenêtre DOFUS correspondante (IntPtr.Zero si absente).</summary>
    public static Dictionary<string, IntPtr> FindAll(IEnumerable<string> names)
    {
        var windows = VisibleWindows();
        var result = new Dictionary<string, IntPtr>();
        foreach (var name in names)
        {
            IntPtr found = IntPtr.Zero;
            foreach (var (h, title) in windows)
            {
                if (title.Contains(name, StringComparison.OrdinalIgnoreCase) && IsDofusProcess(h))
                {
                    found = h;
                    break;
                }
            }
            result[name] = found;
        }
        return result;
    }

    public static IntPtr Find(string name) => FindAll(new[] { name })[name];

    /// <summary>Nom du personnage dont la fenêtre est au premier plan, ou "".</summary>
    public static string ActiveCharacter(IEnumerable<string> names)
    {
        var fg = Native.GetForegroundWindow();
        if (fg == IntPtr.Zero) return "";
        var title = Native.GetTitle(fg);
        foreach (var n in names)
            if (title.Contains(n, StringComparison.OrdinalIgnoreCase)) return n;
        return "";
    }

    public static bool Activate(IntPtr h)
    {
        if (h == IntPtr.Zero) return false;
        if (Native.IsIconic(h)) Native.ShowWindow(h, Native.SW_RESTORE);
        return Native.SetForegroundWindow(h);
    }
}
