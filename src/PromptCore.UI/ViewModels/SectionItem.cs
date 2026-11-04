using CommunityToolkit.Mvvm.ComponentModel;
namespace PromptCore.UI.ViewModels;

public partial class SectionItem : ObservableObject
{
    public SectionItem(string type, string title) { Type = type; _title = title; }
    public string Type { get; }
    [ObservableProperty] private string _title;
    [ObservableProperty] private string _text = "";
    [ObservableProperty] private bool _enabled = true;
}
