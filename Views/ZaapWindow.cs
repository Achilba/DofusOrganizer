using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;
using DofusTeamOrganizer.Models;
using DofusTeamOrganizer.ViewModels;

namespace DofusTeamOrganizer.Views;

/// <summary>Fenêtre Zaaps à part (option « Zaap déporté ») : recherche, liste visuelle, un clic copie le Zaap puis la liste se replie.</summary>
public class ZaapWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly TextBox _box = new();
    private readonly TextBlock _hint = new();
    private readonly StackPanel _list = new();
    private readonly Border _listHost = new();
    private readonly Path _chevron = new();
    private bool _expanded;
    private bool _placed;
    private bool _quiet;
    private ZaapEntry _first;

    private static T Res<T>(T e, DependencyProperty p, string key) where T : DependencyObject
    {
        e.SetResourceReference(p, key);
        return e;
    }

    public ZaapWindow(MainViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        Title = "Zaaps";
        Width = 340;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        Topmost = vm.Data.MiniAlwaysOnTop;
        WindowStartupLocation = WindowStartupLocation.Manual;
        SetResourceReference(ForegroundProperty, "Text");
        SetResourceReference(FontFamilyProperty, "Body");
        SetBinding(OpacityProperty, new Binding("MiniOpacity"));

        var frame = new Border { CornerRadius = new CornerRadius(12), BorderThickness = new Thickness(1) };
        Res(frame, Border.BackgroundProperty, "Side");
        Res(frame, Border.BorderBrushProperty, "Line2");
        var root = new StackPanel();
        frame.Child = root;

        // ── Barre de titre : poignée, « Zaaps », flèche (déplier / replier), fermer
        var head = new Border { Height = 40, Background = Brushes.Transparent, Padding = new Thickness(10, 0, 6, 0) };
        head.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ButtonState == MouseButtonState.Pressed && e.OriginalSource is not Button) { try { DragMove(); } catch { } }
        };
        var hg = new Grid();
        hg.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        hg.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        hg.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        hg.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var grip = new Path { Data = (Geometry)Application.Current.FindResource("IcoGrip"), StrokeThickness = 3, Width = 8, Height = 14, Stretch = Stretch.Uniform,
            Stroke = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80)), StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round,
            Margin = new Thickness(2, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center, ToolTip = "Glisser pour déplacer" };
        hg.Children.Add(grip);
        var title = new TextBlock { Text = "Zaaps", FontSize = 16, VerticalAlignment = VerticalAlignment.Center };
        title.SetResourceReference(TextBlock.FontFamilyProperty, "Display");
        Grid.SetColumn(title, 1);
        hg.Children.Add(title);

        _chevron.Data = (Geometry)Application.Current.FindResource("IcoChevronDown");
        _chevron.StrokeThickness = 2.4; _chevron.Width = 14; _chevron.Height = 14; _chevron.Stretch = Stretch.Uniform;
        _chevron.StrokeStartLineCap = PenLineCap.Round; _chevron.StrokeEndLineCap = PenLineCap.Round; _chevron.StrokeLineJoin = PenLineJoin.Round;
        _chevron.RenderTransformOrigin = new Point(0.5, 0.5);
        _chevron.RenderTransform = new RotateTransform(0);
        Res(_chevron, Shape.StrokeProperty, "Text");
        var toggle = new Button { Content = _chevron, Width = 30, Height = 28, MinHeight = 0, Padding = new Thickness(0), Margin = new Thickness(0, 0, 4, 0), ToolTip = "Afficher / replier la liste" };
        toggle.Click += (_, _) => { if (_expanded) Collapse(); else Expand(); };
        Grid.SetColumn(toggle, 2);
        hg.Children.Add(toggle);

        var close = new Button { Content = "✕", Width = 30, Height = 28, MinHeight = 0, Padding = new Thickness(0), ToolTip = "Remettre le Zaap dans la barre Mini" };
        close.Click += (_, _) => _vm.DetachedZaap = false;
        Grid.SetColumn(close, 3);
        hg.Children.Add(close);
        head.Child = hg;
        root.Children.Add(head);

        // ── Recherche
        var boxGrid = new Grid { Margin = new Thickness(10, 0, 10, 10) };
        _box.Height = 38; _box.MinHeight = 0; _box.VerticalContentAlignment = VerticalAlignment.Center; _box.FontSize = 14;
        _box.TextChanged += (_, _) => { _hint.Visibility = _box.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed; if (_quiet) return; Expand(); };
        _box.GotKeyboardFocus += (_, _) => { if (!_quiet) Expand(); };
        _box.PreviewMouseLeftButtonDown += (_, _) => { if (!_expanded) Expand(); };
        _box.PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) { Collapse(); e.Handled = true; }
            else if (e.Key == Key.Enter && _first != null) { Pick(_first); e.Handled = true; }
        };
        _hint.Text = "Cania, Sufokia, -5,-23…"; _hint.FontSize = 14; _hint.IsHitTestVisible = false;
        _hint.VerticalAlignment = VerticalAlignment.Center; _hint.Margin = new Thickness(12, 0, 0, 0);
        Res(_hint, TextBlock.ForegroundProperty, "Dim");
        boxGrid.Children.Add(_box);
        boxGrid.Children.Add(_hint);
        root.Children.Add(boxGrid);

        // ── Liste (repliée au départ)
        _listHost.Visibility = Visibility.Collapsed;
        _listHost.BorderThickness = new Thickness(0, 1, 0, 0);
        Res(_listHost, Border.BorderBrushProperty, "Line");
        _listHost.Child = new ScrollViewer { Content = _list, MaxHeight = 330, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(10, 8, 10, 10) };
        root.Children.Add(_listHost);

        Content = frame;

        Loaded += (_, _) => Place();
        IsVisibleChanged += (_, _) =>
        {
            if (!IsVisible) return;
            Topmost = _vm.Data.MiniAlwaysOnTop;
            if (_placed) Apply();
        };
        LocationChanged += (_, _) => Remember();
    }

    // ── Position mémorisée (comme les autres barres)
    private void Place()
    {
        if (_placed) return;
        _placed = true;
        Apply();
    }

    private void Apply()
    {
        var d = _vm.Data;
        d.ZaapWinPositions ??= new();
        var wa = SystemParameters.WorkArea;
        double left = wa.Right - Width - 24, top = wa.Top + 80;
        if (d.ZaapWinPositions.TryGetValue(MiniPlacement.ScreenKey(), out var p) && p != null && p.Length == 2) { left = p[0]; top = p[1]; }
        left = Math.Max(SystemParameters.VirtualScreenLeft, Math.Min(left, SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 100));
        top = Math.Max(SystemParameters.VirtualScreenTop, Math.Min(top, SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 60));
        Left = left;
        Top = top;
    }

    private void Remember()
    {
        if (!_placed || !IsVisible) return;
        _vm.Data.ZaapWinPositions ??= new();
        _vm.Data.ZaapWinPositions[MiniPlacement.ScreenKey()] = new[] { Left, Top };
        _vm.RequestSave();
    }

    // ── Déplier / replier
    private void Expand()
    {
        _expanded = true;
        _listHost.Visibility = Visibility.Visible;
        ((RotateTransform)_chevron.RenderTransform).Angle = 180;
        Refresh();
    }

    private void Collapse()
    {
        _expanded = false;
        _listHost.Visibility = Visibility.Collapsed;
        ((RotateTransform)_chevron.RenderTransform).Angle = 0;
    }

    private static string Norm(string s)
    {
        var d = (s ?? "").ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in d)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
        return sb.ToString().Replace(" ", "");
    }

    private void Refresh()
    {
        var q = Norm(_box.Text);
        var found = ZaapData.All.Where(z => q.Length == 0 || Norm(z.Label).Contains(q) || Norm(z.Coord).Contains(q)).ToList();
        var favCoords = new HashSet<string>(_vm.Favorites.Select(f => f.Coord));
        var favs = found.Where(z => favCoords.Contains(z.Coord)).OrderBy(z => z.Label, StringComparer.CurrentCulture).ToList();
        var rest = found.Where(z => !favCoords.Contains(z.Coord)).ToList();

        _list.Children.Clear();
        _first = favs.FirstOrDefault() ?? rest.FirstOrDefault();
        if (found.Count == 0)
        {
            _list.Children.Add(Res(new TextBlock { Text = "Aucun Zaap ne correspond.", Margin = new Thickness(4, 6, 0, 6) }, TextBlock.ForegroundProperty, "Dim"));
            return;
        }
        if (favs.Count > 0) { _list.Children.Add(Header("Favoris")); foreach (var z in favs) _list.Children.Add(Row(z, true)); }
        if (rest.Count > 0) { _list.Children.Add(Header(favs.Count > 0 ? "Autres Zaaps" : "Résultats")); foreach (var z in rest) _list.Children.Add(Row(z, false)); }
    }

    private static UIElement Header(string text)
    {
        var t = new TextBlock { Text = text.ToUpperInvariant(), FontSize = 11, Margin = new Thickness(2, 6, 0, 6) };
        t.SetResourceReference(TextBlock.FontFamilyProperty, "BodyXB");
        t.SetResourceReference(TextBlock.ForegroundProperty, "Dim");
        return t;
    }

    private UIElement Row(ZaapEntry z, bool fav)
    {
        // « Zone · Lieu » : le lieu en gros, la zone dessous
        string name = z.Label, zone = "";
        int i = z.Label.IndexOf(" · ", StringComparison.Ordinal);
        if (i > 0) { zone = z.Label.Substring(0, i); name = z.Label.Substring(i + 3); }

        var card = new Border { CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1), Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 6), Cursor = Cursors.Hand };
        Res(card, Border.BackgroundProperty, "Panel");
        Res(card, Border.BorderBrushProperty, "Line");
        card.MouseEnter += (_, _) => card.SetResourceReference(Border.BorderBrushProperty, "Accent");
        card.MouseLeave += (_, _) => card.SetResourceReference(Border.BorderBrushProperty, "Line");
        card.MouseLeftButtonUp += (_, _) => Pick(z);

        var g = new Grid();
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Symbole neutre (anneau + point) en attendant une vraie icône de Zaap
        var tile = new Border { Width = 34, Height = 34, CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 10, 0) };
        Res(tile, Border.BackgroundProperty, "AccentSoft");
        Res(tile, Border.BorderBrushProperty, "Line2");
        var sym = new Grid { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var ring = new Ellipse { Width = 18, Height = 18, StrokeThickness = 2 };
        Res(ring, Shape.StrokeProperty, "Accent");
        var dot = new Ellipse { Width = 6, Height = 6 };
        Res(dot, Shape.FillProperty, "Accent");
        sym.Children.Add(ring);
        sym.Children.Add(dot);
        tile.Child = sym;
        g.Children.Add(tile);

        var txt = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var n = new TextBlock { Text = name, FontSize = 14, TextTrimming = TextTrimming.CharacterEllipsis };
        n.SetResourceReference(TextBlock.FontFamilyProperty, "BodyXB");
        txt.Children.Add(n);
        if (zone != "") txt.Children.Add(Res(new TextBlock { Text = zone, FontSize = 12 }, TextBlock.ForegroundProperty, "Dim"));
        Grid.SetColumn(txt, 1);
        g.Children.Add(txt);

        var c = new TextBlock { Text = z.Coord, FontSize = 13, Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        c.SetResourceReference(TextBlock.FontFamilyProperty, "BodyXB");
        Res(c, TextBlock.ForegroundProperty, "Muted");
        Grid.SetColumn(c, 2);
        g.Children.Add(c);

        if (fav)
        {
            var star = new Path { Data = (Geometry)Application.Current.FindResource("IcoStar"), Width = 13, Height = 13, Stretch = Stretch.Uniform, StrokeThickness = 0, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, ToolTip = "Favori" };
            Res(star, Shape.FillProperty, "Accent");
            Grid.SetColumn(star, 3);
            g.Children.Add(star);
        }
        card.Child = g;
        return card;
    }

    private void Pick(ZaapEntry z)
    {
        _vm.CopyZaap(z);
        _quiet = true;
        _box.Text = "";
        _quiet = false;
        Collapse();
        Keyboard.ClearFocus();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Alt+F4 ne détruit pas la fenêtre : on la cache seulement (l'option reste cochée)
        e.Cancel = true;
        Hide();
    }
}
