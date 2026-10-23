using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DofusTeamOrganizer.Models;
using DofusTeamOrganizer.ViewModels;

namespace DofusTeamOrganizer.Views;

public partial class ZaapPicker : UserControl
{
    public ZaapPicker()
    {
        InitializeComponent();
    }

    private static string Norm(string s)
    {
        var d = (s ?? "").ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in d)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
        return sb.ToString().Replace(" ", "");
    }

    private void Refresh()
    {
        var q = Norm(Box.Text);
        var items = ZaapData.All.Where(z => q.Length == 0 || Norm(z.Label).Contains(q) || Norm(z.Coord).Contains(q)).ToList();
        ZList.ItemsSource = items;
        NoMatch.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        Hint.Visibility = Box.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Box_TextChanged(object sender, TextChangedEventArgs e)
    {
        Refresh();
        if (Box.IsKeyboardFocused) Pop.IsOpen = true;
    }

    private void Box_GotFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        Refresh();
        Pop.IsOpen = true;
    }

    private void Box_LostFocus(object sender, KeyboardFocusChangedEventArgs e) => Pop.IsOpen = false;

    private void Box_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { Pop.IsOpen = false; return; }
        if (e.Key == Key.Enter && ZList.ItemsSource is IEnumerable<ZaapEntry> items)
        {
            var first = items.FirstOrDefault();
            if (first != null) Pick(first);
            e.Handled = true;
        }
    }

    private void Item_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is ZaapEntry z) Pick(z);
    }

    private void Pick(ZaapEntry z)
    {
        if (DataContext is MainViewModel vm) vm.CopyZaap(z);
        Pop.IsOpen = false;
        Box.Text = "";
        Keyboard.ClearFocus();
    }
}
