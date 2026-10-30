using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using DofusTeamOrganizer.ViewModels;

namespace DofusTeamOrganizer.Views;

/// <summary>Petite note flottante, toujours au premier plan, à poser à côté du jeu (construite en code).</summary>
public class NoteFloatWindow : Window
{
    private readonly MainViewModel _vm;
    private bool _ready;

    public NoteFloatWindow(MainViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        Title = "Note";
        Topmost = true;
        ShowInTaskbar = false;
        MinWidth = 220;
        MinHeight = 160;
        Width = 340;
        Height = 380;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        SetResourceReference(BackgroundProperty, "Bg");
        SetResourceReference(ForegroundProperty, "Text");
        SetResourceReference(FontFamilyProperty, "Body");
        SourceInitialized += (_, _) => DofusTeamOrganizer.Services.WindowStyler.UseDarkTitleBar(new System.Windows.Interop.WindowInteropHelper(this).Handle);

        var root = new DockPanel { Margin = new Thickness(10) };

        var combo = new ComboBox { Margin = new Thickness(0, 0, 0, 8), ToolTip = "Choisir la note à afficher" };
        combo.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("NoteKeys"));
        combo.SetBinding(Selector.SelectedItemProperty, new Binding("SelectedNote") { Mode = BindingMode.TwoWay });
        DockPanel.SetDock(combo, Dock.Top);
        root.Children.Add(combo);

        var box = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalContentAlignment = VerticalAlignment.Top,
            Padding = new Thickness(10),
            FontSize = 14,
        };
        box.SetBinding(TextBox.TextProperty, new Binding("NoteText") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
        box.SetResourceReference(BackgroundProperty, "Side");
        root.Children.Add(box);
        Content = root;

        RestoreSavedBounds(vm.Data);
        LocationChanged += (_, _) => Remember();
        SizeChanged += (_, _) => Remember();
        _ready = true;
    }

    private void RestoreSavedBounds(DofusTeamOrganizer.Models.AppData d)
    {
        try
        {
            if (d.NoteWinWidth < MinWidth || d.NoteWinHeight < MinHeight) return;
            double vl = SystemParameters.VirtualScreenLeft, vt = SystemParameters.VirtualScreenTop;
            double vr = vl + SystemParameters.VirtualScreenWidth, vb = vt + SystemParameters.VirtualScreenHeight;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Width = Math.Min(d.NoteWinWidth, SystemParameters.VirtualScreenWidth);
            Height = Math.Min(d.NoteWinHeight, SystemParameters.VirtualScreenHeight);
            Left = Math.Max(vl, Math.Min(d.NoteWinLeft, vr - 120));
            Top = Math.Max(vt, Math.Min(d.NoteWinTop, vb - 80));
        }
        catch { /* place par défaut */ }
    }

    private void Remember()
    {
        if (!_ready || !IsLoaded || WindowState != WindowState.Normal) return;
        var d = _vm.Data;
        d.NoteWinLeft = Left; d.NoteWinTop = Top; d.NoteWinWidth = Width; d.NoteWinHeight = Height;
        _vm.RequestSave();
    }
}
