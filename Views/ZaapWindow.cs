using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
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
    private readonly WrapPanel _list = new();
    private readonly Border _listHost = new();
    private readonly ScrollViewer _listScroll = new();
    private readonly Path _chevron = new();
    private StackPanel _root;
    private Border _head;
    private Grid _boxGrid;
    private Thumb _resizeTop;
    private Thumb _resizeBottom;
    private ComboBox _colsBox;
    private Popup _toast;
    private TextBlock _toastText;
    private System.Windows.Threading.DispatcherTimer _toastTimer;
    private bool _expanded;
    private bool _placed;
    private bool _quiet;
    private ZaapEntry _first;

    private static T Res<T>(T e, DependencyProperty p, string key) where T : FrameworkElement
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
        _root = new StackPanel { VerticalAlignment = VerticalAlignment.Top };
        frame.Child = _root;

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

        // Liste déroulante : nombre de Zaaps par ligne (même réglage que la page Zaaps)
        _colsBox = new ComboBox { Width = 112, Height = 28, MinHeight = 0, Margin = new Thickness(0, 0, 6, 0), ToolTip = "Zaaps par ligne" };
        _colsBox.Items.Add("1 par ligne");
        _colsBox.Items.Add("2 par ligne");
        _colsBox.Items.Add("3 par ligne");
        _colsBox.SelectedIndex = Cols - 1;
        _colsBox.SelectionChanged += (_, _) =>
        {
            if (_colsBox.SelectedIndex >= 0 && _colsBox.SelectedIndex + 1 != _vm.ZaapCols) _vm.ZaapCols = _colsBox.SelectedIndex + 1;
        };
        var tools = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        tools.Children.Add(_colsBox);
        tools.Children.Add(toggle);
        Grid.SetColumn(tools, 2);
        hg.Children.Add(tools);

        var close = new Button { Content = "✕", Width = 30, Height = 28, MinHeight = 0, Padding = new Thickness(0), ToolTip = "Remettre le Zaap dans la barre Mini" };
        close.Click += (_, _) => _vm.DetachedZaap = false;
        Grid.SetColumn(close, 3);
        hg.Children.Add(close);
        head.Child = hg;
        _head = head;

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
        _boxGrid = boxGrid;

        // ── Liste (repliée au départ)
        _listHost.Visibility = Visibility.Collapsed;
        Res(_listHost, Border.BorderBrushProperty, "Line");
        _listScroll.Content = _list;
        _listScroll.MaxHeight = ListHeight();
        _listScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        _listScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        _list.Width = Inner;
        _listScroll.Padding = new Thickness(10, 8, 10, 10);
        _listHost.Child = _listScroll;

        // Poignées de hauteur : une en haut et une en bas de la fenêtre. Tirer l'une garde l'autre bord en place.
        _resizeTop = MakeGrip(true);
        _resizeBottom = MakeGrip(false);

        // Bulle « COPIÉ » propre à cette fenêtre (elle n'apparaît plus sous Invitations dans la barre Mini)
        _toastText = new TextBlock { FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
        _toastText.SetResourceReference(TextBlock.FontFamilyProperty, "BodyXB");
        _toastText.SetResourceReference(TextBlock.ForegroundProperty, "Accent");
        var toastBox = new Border { Height = 32, CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1), Padding = new Thickness(10, 0, 10, 0), Child = _toastText };
        toastBox.SetResourceReference(Border.BackgroundProperty, "Side");
        toastBox.SetResourceReference(Border.BorderBrushProperty, "Accent");
        _toast = new Popup { PlacementTarget = _head, StaysOpen = true, AllowsTransparency = true, Focusable = false, IsHitTestVisible = false, Child = toastBox };
        _toastTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1800) };
        _toastTimer.Tick += (_, _) => { _toastTimer.Stop(); _toast.IsOpen = false; };
        IsVisibleChanged += (_, _) => { if (!IsVisible) _toast.IsOpen = false; };

        Layout();
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.ZaapCols))
            {
                if (_colsBox.SelectedIndex != Cols - 1) _colsBox.SelectedIndex = Cols - 1;
                if (_expanded) Reanchor(Refresh);
                return;
            }
            if (e.PropertyName != nameof(MainViewModel.ZaapUp)) return;
            Dispatcher.BeginInvoke(new Action(() => { Collapse(); Layout(); }));
        };

        // Les poignées de hauteur flottent sur les bords haut et bas (elles ne décalent rien)
        var outer = new Grid();
        outer.Children.Add(frame);
        outer.Children.Add(_resizeTop);
        outer.Children.Add(_resizeBottom);
        Content = outer;

        Loaded += (_, _) => Place();
        IsVisibleChanged += (_, _) =>
        {
            if (!IsVisible) return;
            Topmost = _vm.Data.MiniAlwaysOnTop;
            if (_placed) Apply();
        };
        LocationChanged += (_, _) => Remember();
    }

    /// <summary>Largeur utile de la liste (fenêtre moins cadre, marges et barre de défilement).</summary>
    private double Inner => Width - 2 - 20 - 16;

    private int Cols => Math.Max(1, Math.Min(3, _vm.Data.ZaapCols ?? 2));

    /// <summary>Poignée de hauteur de la liste. Haut : tirer vers le haut agrandit, le bas reste en place. Bas : tirer vers le bas agrandit, le haut reste en place.</summary>
    private Thumb MakeGrip(bool top)
    {
        var t = new Thumb { Height = 8, VerticalAlignment = top ? VerticalAlignment.Top : VerticalAlignment.Bottom, Cursor = Cursors.SizeNS, Focusable = false, FocusVisualStyle = null, Visibility = Visibility.Collapsed,
            ToolTip = "Tirer pour ajuster la hauteur de la liste" };
        var grab = new FrameworkElementFactory(typeof(Grid));
        grab.SetValue(Panel.BackgroundProperty, Brushes.Transparent);
        var mark = new FrameworkElementFactory(typeof(Border));
        mark.Name = "mark";
        mark.SetValue(WidthProperty, 40.0);
        mark.SetValue(HeightProperty, 3.0);
        mark.SetValue(Border.CornerRadiusProperty, new CornerRadius(2));
        mark.SetResourceReference(Border.BackgroundProperty, "Accent");
        mark.SetValue(OpacityProperty, 0.0);
        mark.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
        grab.AppendChild(mark);
        var tpl = new ControlTemplate(typeof(Thumb)) { VisualTree = grab };
        var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(OpacityProperty, 0.9, "mark"));
        var drag = new Trigger { Property = Thumb.IsDraggingProperty, Value = true };
        drag.Setters.Add(new Setter(OpacityProperty, 0.9, "mark"));
        tpl.Triggers.Add(hover);
        tpl.Triggers.Add(drag);
        t.Template = tpl;
        t.DragDelta += (_, e) =>
        {
            double delta = top ? -e.VerticalChange : e.VerticalChange;
            double h = Math.Max(120, Math.Min(720, _listScroll.MaxHeight + delta));
            Reanchor(() => _listScroll.MaxHeight = h, top);
        };
        t.DragCompleted += (_, _) => { _vm.Data.ZaapListHeight = _listScroll.MaxHeight; _vm.RequestSave(); };
        return t;
    }

    private double ListHeight() => Math.Max(120, Math.Min(720, _vm.Data.ZaapListHeight ?? 330));

    /// <summary>Ordre des blocs : liste sous la recherche (défaut) ou au-dessus (option « vers le haut »).</summary>
    private void Layout()
    {
        bool up = _vm.Data.ZaapUp;
        _root.Children.Clear();
        _listHost.BorderThickness = up ? new Thickness(0, 0, 0, 1) : new Thickness(0, 1, 0, 0);
        // Liste vers le haut : liste, recherche, puis l'entête (titre, flèche, fermer) tout en bas
        _boxGrid.Margin = up ? new Thickness(10, 8, 10, 8) : new Thickness(10, 0, 10, 10);
        _head.SetResourceReference(Border.BorderBrushProperty, "Line");
        _head.BorderThickness = up ? new Thickness(0, 1, 0, 0) : new Thickness(0);
        if (up) { _root.Children.Add(_listHost); _root.Children.Add(_boxGrid); _root.Children.Add(_head); }
        else { _root.Children.Add(_head); _root.Children.Add(_boxGrid); _root.Children.Add(_listHost); }
        _toast.Placement = up ? PlacementMode.Top : PlacementMode.Bottom;
        SetChevron();
    }

    private void ShowCopied(string text)
    {
        _toastText.Text = text == null ? "COPIE IMPOSSIBLE" : "COPIÉ  " + text;
        _toast.IsOpen = false;
        _toast.IsOpen = true;
        _toastTimer.Stop();
        _toastTimer.Start();
    }

    private double Measured()
    {
        _root.Measure(new Size(Width, double.PositiveInfinity));
        return _root.DesiredSize.Height;
    }

    /// <summary>Change la taille du contenu ; en mode « vers le haut », la fenêtre grandit vers le haut pour que la recherche ne bouge pas.</summary>
    private void Reanchor(Action change, bool? anchorBottom = null)
    {
        double before = Measured();
        double top0 = Top;
        change();
        UpdateLayout(); // la fenêtre prend sa nouvelle hauteur tout de suite, avant qu'on touche à sa position
        double after = Measured();
        if (!((anchorBottom ?? _vm.Data.ZaapUp) && _placed)) return;
        double target = Math.Max(SystemParameters.VirtualScreenTop, top0 - (after - before));
        Top = target;
        // Sécurité : si Windows a remis l'ancienne position pendant le redimensionnement, on la corrige (sauf si un réglage plus récent est passé)
        int token = ++_anchorToken;
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (token == _anchorToken && Math.Abs(Top - target) > 0.5) Top = target;
        }), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private int _anchorToken;

    private void SetChevron()
    {
        // Flèche vers le bas = « déplier vers le bas » ; en mode haut, la flèche est inversée
        bool pointsDown = _vm.Data.ZaapUp ? _expanded : !_expanded;
        ((RotateTransform)_chevron.RenderTransform).Angle = pointsDown ? 0 : 180;
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
        // Liste vers le haut et dépliée : on retient la place « repliée » (celle où la fenêtre se rouvrira)
        double top = Top;
        if (_vm.Data.ZaapUp && _expanded)
        {
            double collapsed = _head.DesiredSize.Height + _boxGrid.DesiredSize.Height + 2;
            top = Top + Math.Max(0, Measured() - collapsed);
        }
        _vm.Data.ZaapWinPositions[MiniPlacement.ScreenKey()] = new[] { Left, top };
        _vm.RequestSave();
    }

    // ── Déplier / replier
    private void Expand()
    {
        if (!_expanded)
        {
            Reanchor(() =>
            {
                _expanded = true;
                _listHost.Visibility = Visibility.Visible;
                _resizeTop.Visibility = Visibility.Visible;
                _resizeBottom.Visibility = Visibility.Visible;
                Refresh();
            });
            SetChevron();
        }
        else Refresh();
    }

    private void Collapse()
    {
        if (!_expanded) return;
        Reanchor(() =>
        {
            _expanded = false;
            _listHost.Visibility = Visibility.Collapsed;
            _resizeTop.Visibility = Visibility.Collapsed;
            _resizeBottom.Visibility = Visibility.Collapsed;
        });
        SetChevron();
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
            _list.Children.Add(Res(new TextBlock { Text = "Aucun Zaap ne correspond.", Margin = new Thickness(4, 6, 0, 6), Width = Inner }, TextBlock.ForegroundProperty, "Dim"));
            return;
        }
        if (favs.Count > 0) { _list.Children.Add(Header("Favoris")); foreach (var z in favs) _list.Children.Add(Row(z, true)); }
        if (rest.Count > 0) { _list.Children.Add(Header(favs.Count > 0 ? "Autres Zaaps" : "Résultats")); foreach (var z in rest) _list.Children.Add(Row(z, false)); }
    }

    private UIElement Header(string text)
    {
        var t = new TextBlock { Text = text.ToUpperInvariant(), FontSize = 11, Margin = new Thickness(2, 6, 0, 6), Width = Inner };
        t.SetResourceReference(TextBlock.FontFamilyProperty, "BodyXB");
        t.SetResourceReference(TextBlock.ForegroundProperty, "Dim");
        return t;
    }

    private UIElement Row(ZaapEntry z, bool fav)
    {
        if (Cols > 1) return RowCompact(z, fav);
        // « Zone · Lieu » : le lieu en gros, la zone dessous
        string name = z.Label, zone = "";
        int i = z.Label.IndexOf(" · ", StringComparison.Ordinal);
        if (i > 0) { zone = z.Label.Substring(0, i); name = z.Label.Substring(i + 3); }

        var card = new Border { CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1), Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 6), Cursor = Cursors.Hand, Width = Inner };
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
        var n = new TextBlock { Text = name, FontSize = 14, TextWrapping = TextWrapping.Wrap };
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

    /// <summary>Cellule compacte (2 ou 3 Zaaps par ligne) : symbole et étoile en haut, nom (2 lignes au plus), zone, coordonnées.</summary>
    private UIElement RowCompact(ZaapEntry z, bool fav)
    {
        int cols = Cols;
        string name = z.Label, zone = "";
        int i = z.Label.IndexOf(" · ", StringComparison.Ordinal);
        if (i > 0) { zone = z.Label.Substring(0, i); name = z.Label.Substring(i + 3); }
        double cellW = Math.Floor(Inner / cols) - 6;
        double nameSize = cols == 3 ? 12 : 13;

        var card = new Border { CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1), Padding = new Thickness(8), Margin = new Thickness(0, 0, 6, 6), Cursor = Cursors.Hand, Width = cellW };
        Res(card, Border.BackgroundProperty, "Panel");
        Res(card, Border.BorderBrushProperty, "Line");
        card.MouseEnter += (_, _) => card.SetResourceReference(Border.BorderBrushProperty, "Accent");
        card.MouseLeave += (_, _) => card.SetResourceReference(Border.BorderBrushProperty, "Line");
        card.MouseLeftButtonUp += (_, _) => Pick(z);

        var st = new StackPanel();
        var top = new Grid();
        var tile = new Border { Width = 26, Height = 26, CornerRadius = new CornerRadius(7), BorderThickness = new Thickness(1), HorizontalAlignment = HorizontalAlignment.Left };
        Res(tile, Border.BackgroundProperty, "AccentSoft");
        Res(tile, Border.BorderBrushProperty, "Line2");
        var sym = new Grid { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var ring = new Ellipse { Width = 14, Height = 14, StrokeThickness = 2 };
        Res(ring, Shape.StrokeProperty, "Accent");
        var dot = new Ellipse { Width = 5, Height = 5 };
        Res(dot, Shape.FillProperty, "Accent");
        sym.Children.Add(ring);
        sym.Children.Add(dot);
        tile.Child = sym;
        top.Children.Add(tile);
        if (fav)
        {
            var star = new Path { Data = (Geometry)Application.Current.FindResource("IcoStar"), Width = 12, Height = 12, Stretch = Stretch.Uniform, StrokeThickness = 0, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, ToolTip = "Favori" };
            Res(star, Shape.FillProperty, "Accent");
            top.Children.Add(star);
        }
        st.Children.Add(top);

        var n = new TextBlock { Text = name, FontSize = nameSize, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0) };
        n.SetResourceReference(TextBlock.FontFamilyProperty, "BodyXB");
        st.Children.Add(n);
        if (zone != "") st.Children.Add(Res(new TextBlock { Text = zone, FontSize = 11, TextTrimming = TextTrimming.CharacterEllipsis }, TextBlock.ForegroundProperty, "Dim"));
        var c = new TextBlock { Text = z.Coord, FontSize = 12, Margin = new Thickness(0, 2, 0, 0) };
        c.SetResourceReference(TextBlock.FontFamilyProperty, "BodyXB");
        Res(c, TextBlock.ForegroundProperty, "Muted");
        st.Children.Add(c);
        card.Child = st;
        return card;
    }

    private void Pick(ZaapEntry z)
    {
        ShowCopied(_vm.CopyText(z.Command) ? z.Command : null);
        _quiet = true;
        _box.Text = "";
        _quiet = false;
        if (_vm.Data.ZaapKeepOpen) Refresh(); else Collapse();
        Keyboard.ClearFocus();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Alt+F4 ne détruit pas la fenêtre : on la cache seulement (l'option reste cochée)
        e.Cancel = true;
        Hide();
    }
}
