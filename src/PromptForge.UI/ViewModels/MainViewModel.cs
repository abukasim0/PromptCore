using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PromptForge.Core;
namespace PromptForge.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public IReadOnlyList<QuickType> Types => QuickPrompt.Types;
    public IReadOnlyList<ModelProfile> Models => ModelProfiles.BuiltIn;

    [ObservableProperty] private string _idea = "";
    [ObservableProperty] private QuickType _selectedType;
    [ObservableProperty] private ModelProfile _selectedModel;
    [ObservableProperty] private string _result = "";
    [ObservableProperty] private string _status = "";

    public MainViewModel()
    {
        _selectedType = QuickPrompt.Types[0];
        _selectedModel = ModelProfiles.BuiltIn.First(m => m.Id == "chatgpt");
    }

    partial void OnIdeaChanged(string value) => Update();
    partial void OnSelectedTypeChanged(QuickType value) => Update();
    partial void OnSelectedModelChanged(ModelProfile value) => Update();

    private void Update()
    {
        Result = QuickPrompt.Generate(Idea, SelectedType, SelectedModel);
        Status = "";
    }

    [RelayCommand]
    private void Copy()
    {
        if (string.IsNullOrWhiteSpace(Result)) return;
        Clipboard.SetText(Result);
        Status = Application.Current.TryFindResource("Simple.Copied") as string ?? "Copied";
    }
}
