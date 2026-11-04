using System.Windows.Controls;
using PromptForge.UI.ViewModels;
namespace PromptForge.UI.Views;

public partial class PlaceholderView : UserControl
{
    public PlaceholderView()
    {
        InitializeComponent();
        DataContextChanged += (_, e) =>
        {
            if (e.NewValue is PlaceholderViewModel p) PageTitle.SetResourceReference(TextBlock.TextProperty, p.Key);
        };
    }
}
