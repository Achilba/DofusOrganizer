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
        Width = vm.Data.MiniCompact ? 520 : 940;
        if (!_placed)
        {
            _placed = true;
            var wa = SystemParameters.WorkArea;
            Left = vm.Data.MiniLeft ?? (wa.Left + (wa.Width - Width) / 2);
            Top = vm.Data.MiniTop ?? (wa.Top + 8);
            // garde la barre à l'écran si l'écran a changé
            Left = Math.Max(SystemParameters.VirtualScreenLeft, Math.Min(Left, SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 100));
            Top = Math.Max(SystemParameters.VirtualScreenTop, Math.Min(Top, SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 60));
        }
    }

    private void RememberPosition()
    {
        if (!_placed || Vm == null || !IsVisible) return;
        Vm.Data.MiniLeft = Left;
        Vm.Data.MiniTop = Top;
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
