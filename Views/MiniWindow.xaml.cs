using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using DofusTeamOrganizer.ViewModels;

namespace DofusTeamOrganizer.Views;

public partial class MiniWindow : Window
{
    private bool _placed;
    private const double MinBarWidth = 520;
    private const double MaxBarWidth = 1800;

    public MiniWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplySettings();
        LocationChanged += (_, _) => RememberPosition();
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
        if (!vm.ShowMiniZaap)
        {
            // Barre compacte, ou sans le champ Zaap (masqué ou déporté) : la fenêtre s'ajuste exactement à son contenu (plus d'espace vide à droite)
            SizeToContent = SizeToContent.Width;
        }
        else
        {
            SizeToContent = SizeToContent.Manual;
            Width = Math.Max(MinBarWidth, Math.Min(MaxBarWidth, vm.Data.MiniWidth ?? 800));
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

    private void Resize_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
    {
        if (SizeToContent != SizeToContent.Manual) return;
        Width = Math.Max(MinBarWidth, Math.Min(MaxBarWidth, Width + e.HorizontalChange));
    }

    private void Resize_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
    {
        if (Vm == null) return;
        Vm.Data.MiniWidth = Width;
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
