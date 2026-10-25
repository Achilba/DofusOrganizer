using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using DofusTeamOrganizer.Models;
using DofusTeamOrganizer.ViewModels;

namespace DofusTeamOrganizer.Views;

public partial class HomePage : UserControl
{
    private Point _start;
    private bool _armed;

    public HomePage() { InitializeComponent(); }

    private static bool InsideScrollBar(DependencyObject d)
    {
        while (d != null)
        {
            if (d is ScrollBar) return true;
            d = d is Visual || d is System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d);
        }
        return false;
    }

    private void Card_Down(object sender, MouseButtonEventArgs e)
    {
        _start = e.GetPosition(null);
        _armed = !InsideScrollBar(e.OriginalSource as DependencyObject);
    }

    private void Card_Move(object sender, MouseEventArgs e)
    {
        if (!_armed || e.LeftButton != MouseButtonState.Pressed) { _armed = false; return; }
        var p = e.GetPosition(null);
        if (Math.Abs(p.X - _start.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(p.Y - _start.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        _armed = false;
        if ((sender as FrameworkElement)?.DataContext is Character c)
            DragDrop.DoDragDrop((DependencyObject)sender, new DataObject("DtoCharacter", c), DragDropEffects.Move);
    }

    private void Card_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent("DtoCharacter") ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void Card_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData("DtoCharacter") is Character from && (sender as FrameworkElement)?.DataContext is Character to
            && DataContext is MainViewModel vm)
            vm.MoveCharacterTo(from, to);
        e.Handled = true;
    }

    /// <summary>Clic sur le niveau, l'alignement ou un métier : ouvre la fiche du personnage.</summary>
    private void Open_Click(object sender, MouseButtonEventArgs e)
    {
        DependencyObject d = sender as DependencyObject;
        while (d != null)
        {
            if (d is FrameworkElement fe && fe.DataContext is Character c)
            {
                if (DataContext is MainViewModel vm) vm.OpenCharacter(c);
                e.Handled = true;
                return;
            }
            d = VisualTreeHelper.GetParent(d);
        }
    }
}
