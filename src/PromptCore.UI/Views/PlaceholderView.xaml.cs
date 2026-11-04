using System.Windows.Controls;
using PromptCore.UI.ViewModels;
namespace PromptCore.UI.Views;

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
