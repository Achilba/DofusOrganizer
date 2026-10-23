using System.Windows;
using System.Windows.Controls;
using DofusTeamOrganizer.ViewModels;

namespace DofusTeamOrganizer.Views;

public partial class SongesPage : UserControl
{
    public SongesPage() { InitializeComponent(); }

    private void Inc_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is LegendItem l) l.Qty++;
    }

    private void Dec_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is LegendItem l) l.Qty--;
    }
}
