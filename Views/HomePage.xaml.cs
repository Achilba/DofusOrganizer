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
    public HomePage()
    {
        InitializeComponent();
        DataContextChanged += (_, e) =>
        {
            if (e.NewValue is MainViewModel vm)
                vm.PropertyChanged += (s2, a) => { if (a.PropertyName != null && (a.PropertyName.StartsWith("Home") || a.PropertyName == "Data")) ApplyLayout(); };
            ApplyLayout();
        };
        Loaded += (_, _) => ApplyLayout();
    }

    /// <summary>Affiche ou masque les cartes de l'accueil et place Raccourcis / Invitations selon les réglages.</summary>
    private void ApplyLayout()
    {
        if (DataContext is not MainViewModel vm) return;
        var d = vm.Data;
        bool chars = !d.HomeHideChars, sc = !d.HomeHideShortcuts, iv = !d.HomeHideInvites, swap = d.HomeSwapBottom;
        CharsList.Visibility = chars ? Visibility.Visible : Visibility.Collapsed;
        bool any = sc || iv;
        BottomGrid.Visibility = any ? Visibility.Visible : Visibility.Collapsed;
        BottomRow.MinHeight = any ? 190 : 0;
        BottomRow.Height = any ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;
        ShortcutsCard.Visibility = sc ? Visibility.Visible : Visibility.Collapsed;
        InvitesCard.Visibility = iv ? Visibility.Visible : Visibility.Collapsed;
        Grid.SetColumn(ShortcutsCard, swap ? 1 : 0);
        Grid.SetColumn(InvitesCard, swap ? 0 : 1);
        // largeurs : Raccourcis 6, Invitations 5 ; une carte seule prend toute la largeur
        double wSc = sc ? (iv ? 6 : 1) : 0, wIv = iv ? (sc ? 5 : 1) : 0;
        var colSc = swap ? BottomCol1 : BottomCol0;
        var colIv = swap ? BottomCol0 : BottomCol1;
        colSc.Width = new GridLength(wSc, GridUnitType.Star);
        colIv.Width = new GridLength(wIv, GridUnitType.Star);
        colSc.MinWidth = sc ? 300 : 0;
        colIv.MinWidth = 0;
        // espace entre les deux cartes : à droite de la carte de gauche
        ShortcutsCard.Margin = new Thickness(0, 0, (sc && iv && !swap) ? 12 : 0, 0);
        InvitesCard.Margin = new Thickness(0, 0, (sc && iv && swap) ? 12 : 0, 0);
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
