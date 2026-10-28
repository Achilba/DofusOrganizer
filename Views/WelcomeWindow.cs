using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DofusTeamOrganizer.Models;

namespace DofusTeamOrganizer.Views;

/// <summary>Écran de bienvenue du premier lancement (construit en code) : on y saisit ses personnages.</summary>
public class WelcomeWindow : Window
{
    private const int MaxRows = 8;
    private static Brush B(string key) => (Brush)Application.Current.FindResource(key);
    private static FontFamily F(string key) => (FontFamily)Application.Current.FindResource(key);

    private readonly StackPanel _rows = new();
    private readonly Button _add;

    /// <summary>Personnages saisis (nom, classe). Vide si l'utilisateur passe l'étape.</summary>
    public List<(string Name, string ClassName)> Result { get; } = new();

    public WelcomeWindow()
    {
        Title = "Bienvenue";
        Width = 560;
        Height = 560;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = B("Bg");
        Foreground = B("Text");
        FontFamily = F("Body");
        SourceInitialized += (_, _) => DofusTeamOrganizer.Services.WindowStyler.UseDarkTitleBar(new System.Windows.Interop.WindowInteropHelper(this).Handle);

        var root = new DockPanel { Margin = new Thickness(24) };

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        var skip = new Button { Content = "Plus tard", Margin = new Thickness(0, 0, 10, 0) };
        skip.Click += (_, _) => { Result.Clear(); DialogResult = false; };
        var ok = new Button { Content = "Commencer" };
        ok.Click += (_, _) => Finish();
        buttons.Children.Add(skip);
        buttons.Children.Add(ok);
        DockPanel.SetDock(buttons, Dock.Bottom);
        root.Children.Add(buttons);

        var head = new StackPanel();
        head.Children.Add(new TextBlock { Text = "Bienvenue", FontSize = 28, FontFamily = F("Display"), Margin = new Thickness(0, 0, 0, 6) });
        head.Children.Add(new TextBlock
        {
            Text = "Ajoute tes personnages pour commencer : un nom et une classe suffisent. Tu pourras tout modifier ensuite dans Personnages, et choisir les fonctions de chacun (Accueil, Tâches, Songes, Notes) dans Paramètres.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = B("Muted"),
            Margin = new Thickness(0, 0, 0, 14)
        });
        DockPanel.SetDock(head, Dock.Top);
        root.Children.Add(head);

        _add = new Button { Content = "+ Ajouter un personnage", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 0) };
        _add.Click += (_, _) => AddRow("", "Iop");
        var body = new StackPanel();
        body.Children.Add(_rows);
        body.Children.Add(_add);
        root.Children.Add(new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });

        AddRow("", "Iop");
        Content = root;
    }

    private void AddRow(string name, string cls)
    {
        if (_rows.Children.Count >= MaxRows) return;
        var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
        var tb = new TextBox { Text = name, Margin = new Thickness(0, 0, 10, 0), Tag = "name" };
        var cb = new ComboBox { ItemsSource = GameLists.Classes, SelectedItem = cls, Tag = "class" };
        Grid.SetColumn(cb, 1);
        row.Children.Add(tb);
        row.Children.Add(cb);
        _rows.Children.Add(row);
        _add.IsEnabled = _rows.Children.Count < MaxRows;
        tb.Focus();
    }

    private void Finish()
    {
        Result.Clear();
        foreach (Grid row in _rows.Children)
        {
            var tb = row.Children.OfType<TextBox>().First();
            var cb = row.Children.OfType<ComboBox>().First();
            var n = tb.Text.Trim();
            if (n.Length == 0) continue;
            Result.Add((n, cb.SelectedItem as string ?? "Iop"));
        }
        DialogResult = Result.Count > 0;
    }
}
