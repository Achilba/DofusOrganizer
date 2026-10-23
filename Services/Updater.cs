using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace DofusTeamOrganizer.Services;

public class UpdateInfo
{
    public string Version { get; set; }
    public string AssetApiUrl { get; set; }
    public long Size { get; set; }
}

/// <summary>
/// Mise à jour depuis les « Releases » du dépôt GitHub. Dépôt public : aucun jeton nécessaire.
/// Dépôt privé : mettre un jeton (lecture seule) dans le fichier update-token.txt du dossier de données.
/// </summary>
public class Updater
{
    public const string Owner = "Achilba";
    public const string Repo = "DofusOrganizer";
    public const string AssetName = "DofusTeamOrganizer.exe";

    private readonly string _dataDir;
    public Updater(string dataDir) { _dataDir = dataDir; }

    private string Token
    {
        get
        {
            try
            {
                var p = Path.Combine(_dataDir, "update-token.txt");
                return File.Exists(p) ? File.ReadAllText(p).Trim() : "";
            }
            catch { return ""; }
        }
    }

    private HttpClient NewClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd("DofusTeamOrganizer");
        c.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        var t = Token;
        if (t.Length > 0) c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", t);
        return c;
    }

    public static bool IsNewer(string remote, string current)
    {
        return Version.TryParse((remote ?? "").TrimStart('v', 'V'), out var r)
            && Version.TryParse((current ?? "").TrimStart('v', 'V'), out var c)
            && r > c;
    }

    /// <summary>Renvoie la version plus récente, ou null si à jour. Lève une exception si GitHub est injoignable.</summary>
    public async Task<UpdateInfo> CheckAsync(string currentVersion)
    {
        using var http = NewClient();
        http.Timeout = TimeSpan.FromSeconds(10);
        var json = await http.GetStringAsync($"https://api.github.com/repos/{Owner}/{Repo}/releases/latest");
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var tag = root.GetProperty("tag_name").GetString();
        if (!IsNewer(tag, currentVersion)) return null;

        foreach (var a in root.GetProperty("assets").EnumerateArray())
        {
            if (a.GetProperty("name").GetString() == AssetName)
                return new UpdateInfo
                {
                    Version = tag.TrimStart('v', 'V'),
                    AssetApiUrl = a.GetProperty("url").GetString(),
                    Size = a.GetProperty("size").GetInt64()
                };
        }
        throw new InvalidOperationException("La version " + tag + " ne contient pas " + AssetName + ".");
    }

    /// <summary>
    /// Télécharge le nouvel exe, remplace l'ancien (renommé en .old) et prépare le redémarrage.
    /// Après un succès, l'appelant doit fermer l'appli.
    /// </summary>
    public async Task DownloadAndReplaceAsync(UpdateInfo info)
    {
        var exe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exe) || !exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chemin de l'application introuvable.");

        var tmp = exe + ".new";
        var old = exe + ".old";

        using (var http = NewClient())
        {
            http.Timeout = TimeSpan.FromMinutes(5);
            http.DefaultRequestHeaders.Accept.Clear();
            http.DefaultRequestHeaders.Accept.ParseAdd("application/octet-stream");
            using var resp = await http.GetAsync(info.AssetApiUrl, HttpCompletionOption.ResponseHeadersRead);
            resp.EnsureSuccessStatusCode();
            using var src = await resp.Content.ReadAsStreamAsync();
            using var dst = File.Create(tmp);
            await src.CopyToAsync(dst);
        }

        var len = new FileInfo(tmp).Length;
        if (len < 1_000_000 || (info.Size > 0 && len != info.Size))
        {
            try { File.Delete(tmp); } catch { }
            throw new InvalidOperationException("Téléchargement incomplet, rien n'a été modifié.");
        }

        try
        {
            if (File.Exists(old)) File.Delete(old);
            File.Move(exe, old);      // renommer un exe en cours d'exécution est autorisé
            File.Move(tmp, exe);
        }
        catch
        {
            // retour arrière : on remet l'ancien exe en place
            try { if (!File.Exists(exe) && File.Exists(old)) File.Move(old, exe); } catch { }
            try { File.Delete(tmp); } catch { }
            throw;
        }

        // Relance après la fermeture de l'appli actuelle (libère le verrou d'instance unique).
        Process.Start(new ProcessStartInfo("cmd.exe", "/c ping -n 3 127.0.0.1 >nul & start \"\" \"" + exe + "\"")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden
        });
    }

    /// <summary>Supprime l'ancien exe laissé par une mise à jour précédente.</summary>
    public static void CleanupOld()
    {
        try
        {
            var exe = Environment.ProcessPath;
            if (exe != null && File.Exists(exe + ".old")) File.Delete(exe + ".old");
        }
        catch { }
    }
}
