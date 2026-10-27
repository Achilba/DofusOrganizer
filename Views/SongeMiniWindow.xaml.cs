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
        IsVisibleChanged += (_, _) =>
        {
            if (!IsVisible || Vm == null) return;
            Topmost = Vm.Data.MiniAlwaysOnTop;
            if (_placed) MiniPlacement.Apply(this, Vm.Data, 560, true);
        };
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
        MiniPlacement.Apply(this, vm.Data, 560, true);
    }

    private void RememberPosition()
    {
        if (!_placed || Vm == null || !IsVisible) return;
        MiniPlacement.Remember(this, Vm.Data, true);
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
