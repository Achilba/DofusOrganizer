using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using DofusTeamOrganizer.ViewModels;

namespace DofusTeamOrganizer.Views;

public partial class MiniWindow : Window
{
    private bool _placed;

    public MiniWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplySettings();
        LocationChanged += (_, _) => RememberPosition();
        // Si la barre Mini Songe a été déplacée entre-temps, on reprend la place commune
        IsVisibleChanged += (_, _) => { if (IsVisible && _placed && Vm != null) MiniPlacement.Apply(this, Vm.Data, Width); };
        DataContextChanged += (_, e) =>
        {
            if (e.NewValue is MainViewModel vm) vm.MiniSettingsChanged += ApplySettings;
        };
    }

    private MainViewModel Vm => DataContext as MainViewModel;

    private void ApplySettings()
    {
        var vm = Vm;
        if (vm == null) return;
        Topmost = vm.Data.MiniAlwaysOnTop;
        if (vm.Data.MiniCompact)
        {
            // Barre compacte : la fenêtre s'ajuste exactement à son contenu (plus d'espace vide à droite)
            SizeToContent = SizeToContent.Width;
        }
        else
        {
            SizeToContent = SizeToContent.Manual;
            Width = 800;
        }
        if (!_placed)
        {
            _placed = true;
            MiniPlacement.Apply(this, vm.Data, Width);
        }
    }

    private void RememberPosition()
    {
        if (!_placed || Vm == null || !IsVisible) return;
        MiniPlacement.Remember(this, Vm.Data);
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
        // Alt+F4 ne détruit pas la mini-barre : on la cache seulement.
        e.Cancel = true;
        Hide();
    }
}
