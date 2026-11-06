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
                if (TitleMatches(title, name) && IsDofusProcess(h))
                {
                    found = h;
                    break;
                }
            }
            result[name] = found;
        }
        return result;
    }

    private static readonly System.Text.RegularExpressions.Regex TitleRx =
        new(@"^(.+?)\s+-\s+([^-]+?)\s+-\s+\d+(\.\d+)+", System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>Le titre correspond-il à ce pseudo ? Comparaison exacte quand le titre est « Pseudo - Classe - version », sinon le nom doit figurer dans le titre.</summary>
    private static bool TitleMatches(string title, string name)
    {
        var m = TitleRx.Match(title);
        if (m.Success) return string.Equals(m.Groups[1].Value.Trim(), name, StringComparison.OrdinalIgnoreCase);
        return title.Contains(name, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Pseudo et classe lus dans les titres des fenêtres Dofus (« Pseudo - Classe - 3.x.x - Release »). Rien n'est lu dans le jeu lui-même.</summary>
    public static List<(string Name, string Class)> TitleNames()
    {
        var list = new List<(string Name, string Class)>();
        foreach (var (h, title) in VisibleWindows())
        {
            var m = TitleRx.Match(title);
            if (!m.Success || !IsDofusProcess(h)) continue;
            var name = m.Groups[1].Value.Trim();
            if (name.Length < 2 || name.Length > 40 || list.Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))) continue;
            var cls = Models.GameLists.Classes.FirstOrDefault(c => string.Equals(c, m.Groups[2].Value.Trim(), StringComparison.OrdinalIgnoreCase)) ?? "";
            list.Add((name, cls));
        }
        return list;
    }

    public static IntPtr Find(string name) => FindAll(new[] { name })[name];

    /// <summary>Nom du personnage dont la fenêtre est au premier plan, ou "".</summary>
    public static string ActiveCharacter(IEnumerable<string> names)
    {
        var fg = Native.GetForegroundWindow();
        if (fg == IntPtr.Zero) return "";
        var title = Native.GetTitle(fg);
        foreach (var n in names)
            if (TitleMatches(title, n)) return n;
        return "";
    }

    public static bool Activate(IntPtr h)
    {
        if (h == IntPtr.Zero) return false;
        if (Native.IsIconic(h)) Native.ShowWindow(h, Native.SW_RESTORE);
        return Native.SetForegroundWindow(h);
    }
}
