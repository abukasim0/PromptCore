using System.Windows;
using PromptCore.Data;
using PromptCore.UI.ViewModels;
namespace PromptCore.UI;

public partial class MainWindow : Window
{
    private bool _dark = true;
    private bool _arabic;
    private readonly SettingsRepository _settings;

    public MainWindow(MainViewModel viewModel, SettingsRepository settings)
    {
        InitializeComponent();
        _settings = settings;

        // Restore the last used theme and language before the window is shown.
        _dark = settings.Get("ui.theme", "dark") != "light";
        _arabic = settings.Get("ui.language", "en") == "ar";
        if (!_dark) ResourceSwitcher.Swap("Themes/", "Themes/Light.xaml");
        if (_arabic)
        {
            ResourceSwitcher.Swap("Strings/", "Strings/Strings.ar.xaml");
            FlowDirection = FlowDirection.RightToLeft;
        }

        DataContext = viewModel;
    }

    private void Theme_Click(object sender, RoutedEventArgs e)
    {
        _dark = !_dark;
        ResourceSwitcher.Swap("Themes/", _dark ? "Themes/Dark.xaml" : "Themes/Light.xaml");
        _settings.Set("ui.theme", _dark ? "dark" : "light");
    }

    private void Lang_Click(object sender, RoutedEventArgs e)
    {
        _arabic = !_arabic;
        ResourceSwitcher.Swap("Strings/", _arabic ? "Strings/Strings.ar.xaml" : "Strings/Strings.en.xaml");
        FlowDirection = _arabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        _settings.Set("ui.language", _arabic ? "ar" : "en");
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var about = new AboutWindow(_arabic) { Owner = this };
        about.ShowDialog();
    }
}
