using System.Windows;
using PromptForge.UI.ViewModels;
namespace PromptForge.UI.Views;

public partial class FillVariablesWindow : Window
{
    public FillVariablesWindow(FillViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        if (Application.Current.MainWindow is { } main) FlowDirection = main.FlowDirection;
    }

    private void Ok_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
