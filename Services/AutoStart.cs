using Microsoft.Win32;

namespace DofusTeamOrganizer.Services;

/// <summary>Démarrage automatique avec Windows (clé « Run » de l'utilisateur, aucun droit administrateur nécessaire).</summary>
public static class AutoStart
{
    private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DofusTeamOrganizer";

    private static string Command => "\"" + Environment.ProcessPath + "\"";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
            return key?.GetValue(ValueName) != null;
        }
        catch { return false; }
    }

    /// <summary>Retourne false si Windows a refusé l'écriture.</summary>
    public static bool Set(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(KeyPath);
            if (key == null) return false;
            if (enabled) key.SetValue(ValueName, Command);
            else key.DeleteValue(ValueName, false);
            return true;
        }
        catch { return false; }
    }

    /// <summary>Si le démarrage est activé mais que l'appli a changé de dossier, met le chemin à jour.</summary>
    public static void Refresh()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
            var current = key?.GetValue(ValueName) as string;
            if (current != null && current != Command) Set(true);
        }
        catch { }
    }
}
