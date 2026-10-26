using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using DofusTeamOrganizer.ViewModels;

namespace DofusTeamOrganizer.Views;

/// <summary>Barre Mini réservée aux songes : type de run, compteur, total du jour et retour à l'appli.</summary>
public partial class SongeMiniWindow : Window
{
    private bool _placed;

    public SongeMiniWindow()
    {
        InitializeComponent();
        // Même méthode que la barre Mini : taille fixe au départ, puis la fenêtre s'ajuste à son contenu une fois affichée
        Loaded += (_, _) => { SizeToContent = SizeToContent.Width; Place(); };
        IsVisibleChanged += (_, _) => { if (IsVisible && Vm != null) Topmost = Vm.Data.MiniAlwaysOnTop; };
        LocationChanged += (_, _) => RememberPosition();
    }

    private MainViewModel Vm => DataContext as MainViewModel;

    private void Place()
    {
        var vm = Vm;
        if (vm == null) return;
        Topmost = vm.Data.MiniAlwaysOnTop;
        if (_placed) return;
        _placed = true;
        var wa = SystemParameters.WorkArea;
        Left = vm.Data.SongeMiniLeft ?? (wa.Left + (wa.Width - 560) / 2);
        Top = vm.Data.SongeMiniTop ?? (wa.Top + 8);
        // garde la barre à l'écran si l'écran a changé
        Left = Math.Max(SystemParameters.VirtualScreenLeft, Math.Min(Left, SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 100));
        Top = Math.Max(SystemParameters.VirtualScreenTop, Math.Min(Top, SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 60));
    }

    private void RememberPosition()
    {
        if (!_placed || Vm == null || !IsVisible) return;
        Vm.Data.SongeMiniLeft = Left;
        Vm.Data.SongeMiniTop = Top;
        Vm.RequestSave();
    }

    private void Drag_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed && e.OriginalSource is not System.Windows.Controls.TextBox)
        {
            try { DragMove(); } catch { }
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Alt+F4 ne détruit pas la barre : on la cache seulement.
        e.Cancel = true;
        Hide();
    }
}
