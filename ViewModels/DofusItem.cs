using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DofusTeamOrganizer.ViewModels;

public class DofusItem : ObservableObject
{
    private bool _done;
    private bool _started;
    private readonly Action<DofusItem> _changed;

    public DofusItem(string name, string color, bool special, string future, bool done, bool started, Action<DofusItem> changed)
    {
        Name = name;
        Color = color;
        Special = special;
        Future = future;
        _done = done;
        _started = started && !done;
        _changed = changed;
        Icon = LoadIcon(name);
    }

    /// <summary>Icône officielle (Assets\&lt;nom&gt;.png), null si le fichier n'existe pas : l'œuf coloré est alors affiché.</summary>
    public ImageSource Icon { get; }

    private static ImageSource LoadIcon(string name)
    {
        try
        {
            var sb = new StringBuilder();
            foreach (var ch in name.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
                sb.Append(char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '-');
            }
            var uri = new Uri("pack://application:,,,/Assets/" + sb.ToString().Trim('-') + ".png", UriKind.Absolute);
            if (Application.GetResourceStream(uri) == null) return null;
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = uri;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch { return null; }
    }

    public string Name { get; }
    public string Color { get; }
    public bool Special { get; }
    /// <summary>Version où le Dofus arrive (ex. "3.8"), null s'il est déjà disponible.</summary>
    public string Future { get; }
    public bool Available => Future == null;
    /// <summary>Niveau requis pour équiper le Dofus (source : DofusDB), 0 si inconnu.</summary>
    public int Level => Levels.TryGetValue(Name, out var l) ? l : 0;
    /// <summary>Sous le nom : version d'arrivée pour un Dofus à venir, sinon niveau d'équipement.</summary>
    public string Subtitle => Future != null ? Future : (Level > 0 ? "Niv. " + Level : "");

    private static readonly System.Collections.Generic.Dictionary<string, int> Levels = new()
    {
        ["Argenté"] = 20,
        ["Cawotte"] = 60,
        ["Dokoko"] = 80,
        ["Veilleurs"] = 100,
        ["Émeraude"] = 100,
        ["Pourpre"] = 110,
        ["Domakuro"] = 120,
        ["Dorigami"] = 150,
        ["Turquoise"] = 160,
        ["DDG"] = 180,
        ["Ocre"] = 160,
        ["Nébuleux"] = 180,
        ["Abyssal"] = 180,
        ["Tacheté"] = 180,
        ["Ivoire"] = 180,
        ["Ébène"] = 180,
        ["Vulbis"] = 180,
        ["Forgelave"] = 180,
        ["Argenté Scintillant"] = 180,
        ["Cauchemar"] = 180,
        ["Dom de Pin"] = 180,
        ["Sylvestre"] = 180,
        ["Dolmanax"] = 100,
        ["Cacao"] = 50,
        ["Dokille"] = 80,
        ["Dofawa"] = 6,
        ["Dofoozbz"] = 170,
        ["Kaliptus"] = 100,
        ["Dotruche"] = 110
    };

    public bool Done => _done;
    public bool Started => _started;

    /// <summary>Texte du statut, écrit sous le niveau.</summary>
    public string StatusText => _done ? "Terminé" : (_started ? "En cours" : "À faire");

    /// <summary>
    /// État de la case, qui passe à chaque clic de « À faire » (false) à « En cours » (true) puis « Terminé » (null),
    /// puis revient à « À faire » (c'est l'ordre des états d'une case à trois états).
    /// </summary>
    public bool? CheckState
    {
        get => _done ? null : (_started ? true : false);
        set
        {
            var done = value == null;
            var started = value == true;
            if (done == _done && started == _started) return;
            _done = done;
            _started = started;
            Raise(nameof(CheckState));
            Raise(nameof(Done));
            Raise(nameof(Started));
            Raise(nameof(StatusText));
            _changed?.Invoke(this);
        }
    }
}
