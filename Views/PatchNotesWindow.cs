using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DofusTeamOrganizer.Models;

namespace DofusTeamOrganizer.Views;

/// <summary>Fenêtre « Notes de version » construite en code (pas de XAML).</summary>
public class PatchNotesWindow : Window
{
    /// <summary>Brosse du thème courant (la fenêtre est recréée à chaque ouverture, elle suit donc le thème choisi).</summary>
    private static Brush B(string key) => (Brush)Application.Current.FindResource(key);

    public PatchNotesWindow()
    {
        Title = "Notes de version";
        Width = 640;
        Height = 560;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = B("Bg");
        Foreground = B("Text");
        FontFamily = (FontFamily)Application.Current.FindResource("Body");
        SourceInitialized += (_, _) => DofusTeamOrganizer.Services.WindowStyler.UseDarkTitleBar(new System.Windows.Interop.WindowInteropHelper(this).Handle);

        var root = new DockPanel { Margin = new Thickness(24) };

        var close = new Button { Content = "Fermer", HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        close.Click += (_, _) => Close();
        DockPanel.SetDock(close, Dock.Bottom);
        root.Children.Add(close);

        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = "Nouveautés", FontSize = 28, FontFamily = (FontFamily)Application.Current.FindResource("Display"), Margin = new Thickness(0, 0, 0, 14) });

        foreach (var note in PatchNotes.All)
        {
            stack.Children.Add(new TextBlock
            {
                Text = "Version " + note.Version + "  ·  " + note.Date,
                FontSize = 16,
                FontFamily = (FontFamily)Application.Current.FindResource("BodyXB"),
                Foreground = B("Accent"),
                Margin = new Thickness(0, 10, 0, 6)
            });
            foreach (var line in note.Lines)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = "•  " + line,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 5),
                    Foreground = B("Muted")
                });
            }
        }

        root.Children.Add(new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        Content = root;
    }
}
