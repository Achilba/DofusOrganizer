using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DofusTeamOrganizer.Models;
using DofusTeamOrganizer.ViewModels;

namespace DofusTeamOrganizer.Views;

public partial class SongesPage : UserControl
{
    private Point _dragStart;

    public SongesPage() { InitializeComponent(); }

    private void Inc_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is LegendItem l) l.Qty++;
    }

    private void Dec_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is LegendItem l) l.Qty--;
    }

    // ───────── Glisser-déposer des cartes d'équipe (ordre d'initiative) ─────────

    private void Grip_Down(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(null);
    }

    private void Grip_Move(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        var d = e.GetPosition(null) - _dragStart;
        if (Math.Abs(d.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(d.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        if (sender is FrameworkElement fe && fe.DataContext is StratSlot slot)
            DragDrop.DoDragDrop(fe, new DataObject(slot), DragDropEffects.Move);
    }

    private void Slot_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(StratSlot)) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void Slot_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(StratSlot)) is StratSlot src && sender is FrameworkElement fe && fe.DataContext is StratSlot target
            && src != target && src.Owner != null)
        {
            int idx = src.Owner.Slots.IndexOf(target);
            if (idx >= 0) src.Owner.MoveSlot(src, idx);
        }
        e.Handled = true;
    }
}
