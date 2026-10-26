using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DofusTeamOrganizer.Models;
using DofusTeamOrganizer.ViewModels;

namespace DofusTeamOrganizer.Views;

public partial class SettingsPage : UserControl
{
    private Point _dragStart;

    public SettingsPage() { InitializeComponent(); }

    // ───────── Glisser-déposer des personnages (ordre) ─────────

    private void Grip_Down(object sender, MouseButtonEventArgs e) => _dragStart = e.GetPosition(null);

    private void Grip_Move(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        var d = e.GetPosition(null) - _dragStart;
        if (Math.Abs(d.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(d.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        if (sender is FrameworkElement fe && fe.DataContext is Character c)
            DragDrop.DoDragDrop(fe, new DataObject(c), DragDropEffects.Move);
    }

    private void Row_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(Character)) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void Row_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is MainViewModel vm && e.Data.GetData(typeof(Character)) is Character src
            && sender is FrameworkElement fe && fe.DataContext is Character target && src != target)
        {
            int idx = vm.Characters.IndexOf(target);
            if (idx >= 0) vm.MoveCharacterTo(src, idx);
        }
        e.Handled = true;
    }

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
