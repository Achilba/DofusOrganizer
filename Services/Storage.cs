using System.IO;
using System.Text.Json;
using DofusTeamOrganizer.Models;

namespace DofusTeamOrganizer.Services;

/// <summary>
/// Sauvegarde JSON dans %AppData%\DofusTeamOrganizer : écriture atomique,
/// sauvegarde quotidienne (7 copies), export / import.
/// </summary>
public class Storage
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public string Dir { get; }
    public string FilePath => Path.Combine(Dir, "data.json");
    public string BackupDir => Path.Combine(Dir, "Backups");
    public string ErrorLogPath => Path.Combine(Dir, "errors.log");

    public Storage()
    {
        Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DofusTeamOrganizer");
        Directory.CreateDirectory(Dir);
        Directory.CreateDirectory(BackupDir);
    }

    /// <summary>Charge les données. Si le fichier est illisible, tente la dernière sauvegarde.</summary>
    public AppData Load(out string warning)
    {
        warning = "";
        if (!File.Exists(FilePath)) return new AppData();

        var data = TryRead(FilePath);
        if (data != null) return data;

        // Fichier corrompu : on le met de côté puis on essaie les sauvegardes, de la plus récente à la plus ancienne.
        try { File.Copy(FilePath, Path.Combine(Dir, "data.corrompu.json"), true); } catch { }
        foreach (var b in Directory.GetFiles(BackupDir, "data-*.json").OrderByDescending(f => f))
        {
            data = TryRead(b);
            if (data != null)
            {
                warning = "Le fichier de données était illisible : la sauvegarde du " + Path.GetFileNameWithoutExtension(b).Replace("data-", "") + " a été restaurée.";
                return data;
            }
        }
        warning = "Le fichier de données était illisible et aucune sauvegarde valide n'a été trouvée : données remises à zéro (l'ancien fichier est conservé : data.corrompu.json).";
        return new AppData();
    }

    private static AppData TryRead(string path)
    {
        try
        {
            var d = JsonSerializer.Deserialize<AppData>(File.ReadAllText(path), Options);
            return d;
        }
        catch { return null; }
    }

    public void Save(AppData data)
    {
        var tmp = FilePath + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(data, Options));
        if (File.Exists(FilePath)) File.Replace(tmp, FilePath, null);
        else File.Move(tmp, FilePath);
    }

    /// <summary>Une copie par jour, on garde les 7 plus récentes.</summary>
    public void DailyBackup(AppData data)
    {
        try
        {
            if (!File.Exists(FilePath)) return;
            var today = Path.Combine(BackupDir, "data-" + DateTime.Now.ToString("yyyyMMdd") + ".json");
            if (!File.Exists(today))
            {
                File.Copy(FilePath, today, false);
                data.LastBackup = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            }
            foreach (var old in Directory.GetFiles(BackupDir, "data-*.json").OrderByDescending(f => f).Skip(7))
                File.Delete(old);
        }
        catch { /* une sauvegarde ratée ne doit jamais empêcher de lancer l'appli */ }
    }

    public void ExportTo(string path, AppData data)
    {
        Save(data);
        File.Copy(FilePath, path, true);
    }

    /// <summary>Copie les données actuelles dans un dossier choisi (autre disque, clé USB, cloud…).</summary>
    public string CopyTo(string folder, AppData data)
    {
        Save(data);
        var dest = Path.Combine(folder, "DofusTeamOrganizer-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".json");
        File.Copy(FilePath, dest, true);
        return dest;
    }

    /// <summary>Importe un fichier exporté. Renvoie null si le fichier n'est pas valide.</summary>
    public AppData Import(string path)
    {
        var d = TryRead(path);
        if (d == null || d.Characters == null) return null;
        try { if (File.Exists(FilePath)) File.Copy(FilePath, Path.Combine(BackupDir, "data-avant-import-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json"), true); } catch { }
        Save(d);
        return d;
    }

    public void LogError(string text)
    {
        try { File.AppendAllText(ErrorLogPath, DateTime.Now.ToString("s") + "  " + text + Environment.NewLine + Environment.NewLine); } catch { }
    }
}
