using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DofusTeamOrganizer.Views;

public enum NewAccountChoice { Later, Never, Add }

/// <summary>Fenêtre « Nouveau compte détecté » (construite en code, aux couleurs de l'appli).</summary>
public class NewAccountWindow : Window
{
    private static Brush B(string key) => (Brush)Application.Current.FindResource(key);
    private static FontFamily F(string key) => (FontFamily)Application.Current.FindResource(key);

    public NewAccountChoice Choice { get; private set; } = NewAccountChoice.Later;
    public bool ShowCard => _card.IsChecked == true;
    private readonly CheckBox _card;

    public NewAccountWindow(string name, string className)
    {
        Title = "Nouveau compte";
        Width = 440;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        Topmost = true;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = B("Bg");
        Foreground = B("Text");
        FontFamily = F("Body");
        SourceInitialized += (_, _) => DofusTeamOrganizer.Services.WindowStyler.UseDarkTitleBar(new System.Windows.Interop.WindowInteropHelper(this).Handle);

        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(new TextBlock { Text = "Nouveau compte détecté", FontSize = 22, FontFamily = F("Display"), Margin = new Thickness(0, 0, 0, 6) });
        panel.Children.Add(new TextBlock
        {
            Text = "Une fenêtre Dofus avec un pseudo inconnu vient de s'ouvrir. Veux-tu l'ajouter à ta liste ?",
            TextWrapping = TextWrapping.Wrap,
            Foreground = B("Dim"),
            Margin = new Thickness(0, 0, 0, 18)
        });

        // Carte du compte : initiale, pseudo, classe
        var avatar = new Border
        {
            Width = 44, Height = 44, CornerRadius = new CornerRadius(22), Background = B("Accent"),
            Child = new TextBlock
            {
                Text = name.Substring(0, 1).ToUpperInvariant(), FontFamily = F("Display"), FontSize = 21,
                Foreground = new SolidColorBrush(Color.FromRgb(0x12, 0x15, 0x1A)),
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            }
        };
        var info = new StackPanel { Margin = new Thickness(14, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        info.Children.Add(new TextBlock { Text = name, FontSize = 16, FontFamily = F("BodyXB"), TextTrimming = TextTrimming.CharacterEllipsis });
        if (className != "") info.Children.Add(new TextBlock { Text = className, FontSize = 12, Foreground = B("Dim") });
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(avatar);
        row.Children.Add(info);
        panel.Children.Add(new Border
        {
            Background = B("Panel"), BorderBrush = B("Line"), BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10), Padding = new Thickness(14), Child = row, Margin = new Thickness(0, 0, 0, 18)
        });

        _card = new CheckBox
        {
            Content = "Afficher sa carte sur l'accueil", IsChecked = true,
            Style = (Style)Application.Current.FindResource("Switch"), Margin = new Thickness(0, 0, 0, 20)
        };
        panel.Children.Add(_card);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var never = new Button { Content = "Ne plus proposer", Margin = new Thickness(0, 0, 10, 0) };
        never.Click += (_, _) => Pick(NewAccountChoice.Never);
        var later = new Button { Content = "Plus tard", Margin = new Thickness(0, 0, 10, 0) };
        later.Click += (_, _) => Pick(NewAccountChoice.Later);
        var add = new Button { Content = "Ajouter", Style = (Style)Application.Current.FindResource("BtnPrimary"), IsDefault = true };
        add.Click += (_, _) => Pick(NewAccountChoice.Add);
        buttons.Children.Add(never);
        buttons.Children.Add(later);
        buttons.Children.Add(add);
        panel.Children.Add(buttons);

        Content = panel;
    }

    private void Pick(NewAccountChoice choice)
    {
        Choice = choice;
        DialogResult = choice == NewAccountChoice.Add;
    }
}
