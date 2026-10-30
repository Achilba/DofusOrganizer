using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using DofusTeamOrganizer.Models;
using DofusTeamOrganizer.Services;
using DofusTeamOrganizer.ViewModels;

namespace DofusTeamOrganizer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => WindowStyler.UseDarkTitleBar(new WindowInteropHelper(this).Handle);
        DataContextChanged += (_, e) =>
        {
            if (e.NewValue is MainViewModel vm) { vm.UiScaleChanged += ApplyScale; ApplyScale(); RestoreWindowBounds(vm.Data); }
        };
        LocationChanged += (_, _) => RememberBounds();
        SizeChanged += (_, _) => RememberBounds();
        StateChanged += (_, _) => RememberBounds();
        Loaded += (_, _) => ApplyScale();
    }

    /// <summary>Taille du texte : agrandit ou réduit tout le contenu de la fenêtre principale.</summary>
    private void ApplyScale()
    {
        if (Content is not FrameworkElement root) return;
        var s = (DataContext as MainViewModel)?.Data.UiScale ?? 1.0;
        if (s < 0.8 || s > 1.5) s = 1.0;
        root.LayoutTransform = Math.Abs(s - 1.0) < 0.001 ? Transform.Identity : new ScaleTransform(s, s);
    }

    private bool _boundsReady;

    /// <summary>Remet la fenêtre à sa dernière taille et place, si elles sont encore visibles à l'écran.</summary>
    private void RestoreWindowBounds(AppData d)
    {
        try
        {
            if (d.WinWidth >= MinWidth && d.WinHeight >= MinHeight)
            {
                double vl = SystemParameters.VirtualScreenLeft, vt = SystemParameters.VirtualScreenTop;
                double vr = vl + SystemParameters.VirtualScreenWidth, vb = vt + SystemParameters.VirtualScreenHeight;
                double w = Math.Min(d.WinWidth, SystemParameters.VirtualScreenWidth);
                double h = Math.Min(d.WinHeight, SystemParameters.VirtualScreenHeight);
                double l = Math.Max(vl, Math.Min(d.WinLeft, vr - 200));
                double t = Math.Max(vt, Math.Min(d.WinTop, vb - 100));
                WindowStartupLocation = WindowStartupLocation.Manual;
                Width = w; Height = h; Left = l; Top = t;
                if (d.WinMaximized) WindowState = WindowState.Maximized;
            }
        }
        catch { /* en cas de souci on garde la place par défaut */ }
        _boundsReady = true;
    }

    private void RememberBounds()
    {
        if (!_boundsReady || DataContext is not MainViewModel vm || !IsLoaded) return;
        if (WindowState == WindowState.Minimized) return;
        var d = vm.Data;
        d.WinMaximized = WindowState == WindowState.Maximized;
        var r = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
        if (r.Width < MinWidth || r.Height < MinHeight || double.IsNaN(r.Left)) return;
        d.WinLeft = r.Left; d.WinTop = r.Top; d.WinWidth = r.Width; d.WinHeight = r.Height;
        vm.RequestSave();
    }

    private GlobalSearch Search => (DataContext as MainViewModel)?.Search;

    // ───── Recherche globale (Ctrl+K) ─────

    private void Search_KeyDown(object sender, KeyEventArgs e)
    {
        var s = Search;
        if (s == null) return;
        switch (e.Key)
        {
            case Key.Down: s.Move(1); e.Handled = true; break;
            case Key.Up: s.Move(-1); e.Handled = true; break;
            case Key.Enter: s.ActivateSelected(); e.Handled = true; break;
            case Key.Escape: s.Close(); e.Handled = true; break;
        }
    }

    private void Search_Backdrop(object sender, MouseButtonEventArgs e) => Search?.Close();

    private void SearchHit_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is SearchHit hit)
        {
            Search?.Activate(hit);
            e.Handled = true;
        }
    }

    /// <summary>À l'ouverture, le curseur est directement dans le champ de saisie.</summary>
    private void SearchBox_VisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true && sender is TextBox tb)
            Dispatcher.BeginInvoke(new Action(() => { tb.Focus(); Keyboard.Focus(tb); tb.SelectAll(); }), System.Windows.Threading.DispatcherPriority.Input);
    }

    private void SearchList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox lb && lb.SelectedItem != null) lb.ScrollIntoView(lb.SelectedItem);
    }
}
