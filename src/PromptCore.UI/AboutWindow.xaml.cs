using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace PromptCore.UI;

public partial class AboutWindow : Window
{
    public AboutWindow(bool arabic)
    {
        InitializeComponent();
        // Match the main window's language direction (strings come from app-level resources).
        FlowDirection = arabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    }

    private void Link_Click(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        if (e.Uri is not null)
        {
            Process.Start(new ProcessStartInfo(e.Uri.ToString()) { UseShellExecute = true });
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
