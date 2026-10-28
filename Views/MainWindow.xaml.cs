using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
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
            if (e.NewValue is MainViewModel vm) { vm.UiScaleChanged += ApplyScale; ApplyScale(); }
        };
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
