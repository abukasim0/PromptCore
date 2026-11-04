namespace PromptForge.UI.ViewModels;

/// <summary>Honest stand-in for modules that are not implemented yet.</summary>
public sealed class PlaceholderViewModel
{
    public PlaceholderViewModel(string key) { Key = key; }
    public string Key { get; }
}
