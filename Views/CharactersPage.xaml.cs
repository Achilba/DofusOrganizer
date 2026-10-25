using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using DofusTeamOrganizer.Models;
using DofusTeamOrganizer.ViewModels;

namespace DofusTeamOrganizer.Views;

public partial class CharactersPage : UserControl
{
    public CharactersPage() { InitializeComponent(); }

    private void Align_Inc(object sender, RoutedEventArgs e)
    {
        if ((DataContext as CharactersViewModel)?.Selected is { } c) c.AlignLevel++;
    }

    private void Align_Dec(object sender, RoutedEventArgs e)
    {
        if ((DataContext as CharactersViewModel)?.Selected is { } c) c.AlignLevel--;
    }

    private void Align_LostFocus(object sender, RoutedEventArgs e) => CommitAlign((TextBox)sender);

    private void Align_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        CommitAlign((TextBox)sender);
        e.Handled = true;
    }

    /// <summary>
    /// Valide la saisie, puis réaffiche la valeur réellement retenue (limitée entre 1 et 100).
    /// Il faut d'abord envoyer le texte tapé à la source : sinon le réaffichage efface la saisie.
    /// </summary>
    private static void CommitAlign(TextBox box)
    {
        var be = box.GetBindingExpression(TextBox.TextProperty);
        be?.UpdateSource();
        be?.UpdateTarget();
    }

    private void Job_Inc(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is JobEntry j) j.Level++;
    }

    private void Job_Dec(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is JobEntry j) j.Level--;
    }

    private void Link_Navigate(object sender, RequestNavigateEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); } catch { }
        e.Handled = true;
    }

    private void Name_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox tb)
        {
            tb.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            e.Handled = true;
        }
    }

    private void Url_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is CharactersViewModel vm)
        {
            vm.AddStuffCommand.Execute(null);
            e.Handled = true;
        }
    }
}
