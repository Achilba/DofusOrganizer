using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DofusTeamOrganizer.ViewModels;

public class DofusItem : ObservableObject
{
    private bool _done;
    private readonly Action<DofusItem> _changed;

    public DofusItem(string name, string color, bool special, string future, bool done, Action<DofusItem> changed)
    {
        Name = name;
        Color = color;
        Special = special;
        Future = future;
        _done = done;
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
    public string Subtitle => Done ? "Terminé" : (Future ?? "À faire");

    public bool Done
    {
        get => _done;
        set
        {
            if (!Set(ref _done, value)) return;
            Raise(nameof(Subtitle));
            _changed?.Invoke(this);
        }
    }
}
