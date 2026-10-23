using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DofusTeamOrganizer.Models;

namespace DofusTeamOrganizer.Views;

/// <summary>Fenêtre « Notes de version » construite en code (pas de XAML).</summary>
public class PatchNotesWindow : Window
{
    private static Brush B(string hex) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));

    public PatchNotesWindow()
    {
        Title = "Notes de version";
        Width = 640;
        Height = 560;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = B("#0E1014");
        Foreground = B("#F3F0E8");
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
                Foreground = B("#F2B544"),
                Margin = new Thickness(0, 10, 0, 6)
            });
            foreach (var line in note.Lines)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = "•  " + line,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 5),
                    Foreground = B("#D8D5CC")
                });
            }
        }

        root.Children.Add(new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        Content = root;
    }
}
