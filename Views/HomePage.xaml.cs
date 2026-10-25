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
    public HomePage() { InitializeComponent(); }

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
