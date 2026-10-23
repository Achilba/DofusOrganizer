using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DofusTeamOrganizer.ViewModels;

namespace DofusTeamOrganizer.Views;

public partial class SettingsPage : UserControl
{
    public SettingsPage() { InitializeComponent(); }

    /// <summary>Saisie d'un nouveau raccourci : la ligne en attente reçoit la touche pressée.</summary>
    private void Page_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        var row = vm.ShortcutRows.FirstOrDefault(r => r.Capturing);
        if (row == null) return;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.ImeProcessed) key = e.ImeProcessedKey;
        e.Handled = true;

        if (key == Key.Escape) { row.Capturing = false; return; }
        // Une touche de modification seule (Ctrl, Alt, Maj, Windows) n'est pas un raccourci : on attend la suite
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin) return;

        int mods = 0;
        var m = Keyboard.Modifiers;
        if ((m & ModifierKeys.Alt) != 0) mods |= 1;
        if ((m & ModifierKeys.Control) != 0) mods |= 2;
        if ((m & ModifierKeys.Shift) != 0) mods |= 4;
        if ((m & ModifierKeys.Windows) != 0) mods |= 8;

        int vk = KeyInterop.VirtualKeyFromKey(key);
        if (vk == 0) return;
        vm.AssignShortcut(row, mods, vk);
    }
}
