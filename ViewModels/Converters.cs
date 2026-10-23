using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace DofusTeamOrganizer.ViewModels;

/// <summary>Visible si la valeur (texte) est égale au paramètre.</summary>
public class EqualsVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object parameter, CultureInfo c)
        => string.Equals(value as string, parameter as string) ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type t, object parameter, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>Visible si le texte n'est pas vide.</summary>
public class NotEmptyVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object parameter, CultureInfo c)
        => string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object value, Type t, object parameter, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>Style des boutons de navigation : paramètre = "page|bg", "page|bar" ou "page|fg".</summary>
public class NavConverter : IValueConverter
{
    private static readonly Brush Active = new SolidColorBrush(Color.FromRgb(0x23, 0x29, 0x36));
    private static readonly Brush Accent = new SolidColorBrush(Color.FromRgb(0xF2, 0xB5, 0x44));
    private static readonly Brush Light = new SolidColorBrush(Color.FromRgb(0xF3, 0xF0, 0xE8));
    private static readonly Brush Muted = new SolidColorBrush(Color.FromRgb(0xB4, 0xBA, 0xC6));

    public object Convert(object value, Type t, object parameter, CultureInfo c)
    {
        var parts = ((string)parameter).Split('|');
        bool on = (value as string) == parts[0];
        return parts[1] switch
        {
            "bg" => on ? Active : Brushes.Transparent,
            "bar" => on ? Accent : Brushes.Transparent,
            _ => on ? Light : Muted
        };
    }
    public object ConvertBack(object value, Type t, object parameter, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>Convertit "#RRGGBB" en pinceau.</summary>
public class HexBrushConverter : IValueConverter
{
    public object Convert(object value, Type t, object parameter, CultureInfo c)
    {
        try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(value as string ?? "#888888")); }
        catch { return Brushes.Gray; }
    }
    public object ConvertBack(object value, Type t, object parameter, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>Pastille d'état d'un personnage : paramètre = dot | text | bg | border | label.</summary>
public class StatusConverter : IValueConverter
{
    private static Brush Hex(string h) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(h));
    private static readonly Brush DotOn = Hex("#3DD68C"), DotOff = Hex("#6B7280");
    private static readonly Brush TextOn = Hex("#7BE8B0"), TextOff = Hex("#B4BAC6");
    private static readonly Brush BgOn = Hex("#1F3DD68C"), BgOff = Hex("#1E232C");
    private static readonly Brush BorderOn = Hex("#3A4150"), BorderOff = Hex("#2B313C");

    public object Convert(object value, Type t, object parameter, CultureInfo c)
    {
        bool on = value is true;
        return (parameter as string) switch
        {
            "dot" => on ? DotOn : DotOff,
            "text" => on ? TextOn : TextOff,
            "bg" => on ? BgOn : BgOff,
            "border" => on ? BorderOn : BorderOff,
            _ => on ? "Connecté" : "Déconnecté"
        };
    }
    public object ConvertBack(object value, Type t, object parameter, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>Visible si le booléen est faux.</summary>
public class InverseBoolVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object parameter, CultureInfo c) => value is true ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object value, Type t, object parameter, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>Rotation du chevron d'une section repliable (90° si ouverte).</summary>
public class OpenAngleConverter : IValueConverter
{
    public object Convert(object value, Type t, object parameter, CultureInfo c) => value is true ? 90.0 : 0.0;
    public object ConvertBack(object value, Type t, object parameter, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>Barre cliquable du texte barré pour une tâche terminée.</summary>
public class StrikeConverter : IValueConverter
{
    public object Convert(object value, Type t, object parameter, CultureInfo c)
        => value is true ? TextDecorations.Strikethrough : null;
    public object ConvertBack(object value, Type t, object parameter, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>Nom de classe → icône (Assets\&lt;classe&gt;.png). Renvoie null si l'icône n'existe pas.</summary>
public class ClassIconConverter : IValueConverter
{
    private static readonly System.Collections.Generic.Dictionary<string, System.Windows.Media.ImageSource> Cache = new();

    public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        var name = value as string;
        if (string.IsNullOrWhiteSpace(name)) return null;
        if (Cache.TryGetValue(name, out var cached)) return cached;
        System.Windows.Media.ImageSource img = null;
        try
        {
            var sb = new System.Text.StringBuilder();
            foreach (var ch in name.Normalize(System.Text.NormalizationForm.FormD))
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
                sb.Append(char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '-');
            }
            var uri = new System.Uri("pack://application:,,,/Assets/" + sb.ToString().Trim('-') + ".png", System.UriKind.Absolute);
            if (System.Windows.Application.GetResourceStream(uri) != null)
            {
                var bmp = new System.Windows.Media.Imaging.BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = uri;
                bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                img = bmp;
            }
        }
        catch { img = null; }
        Cache[name] = img;
        return img;
    }

    public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new System.NotSupportedException();
}
