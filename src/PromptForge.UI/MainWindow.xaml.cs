using System.Windows;
using PromptForge.UI.ViewModels;
namespace PromptForge.UI;

public partial class MainWindow : Window
{
    private bool _dark = true;
    private bool _arabic;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Theme_Click(object sender, RoutedEventArgs e)
    {
        _dark = !_dark;
        ResourceSwitcher.Swap("Themes/", _dark ? "Themes/Dark.xaml" : "Themes/Light.xaml");
    }

    private void Lang_Click(object sender, RoutedEventArgs e)
    {
        _arabic = !_arabic;
        ResourceSwitcher.Swap("Strings/", _arabic ? "Strings/Strings.ar.xaml" : "Strings/Strings.en.xaml");
        FlowDirection = _arabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    }
}
