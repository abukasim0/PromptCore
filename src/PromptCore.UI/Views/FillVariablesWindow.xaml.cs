using System.Windows;
using PromptCore.UI.ViewModels;
namespace PromptCore.UI.Views;

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
