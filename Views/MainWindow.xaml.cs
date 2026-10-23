using System.Windows;
using System.Windows.Interop;
using DofusTeamOrganizer.Services;

namespace DofusTeamOrganizer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => WindowStyler.UseDarkTitleBar(new WindowInteropHelper(this).Handle);
    }
}
