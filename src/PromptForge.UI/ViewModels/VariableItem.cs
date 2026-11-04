using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using PromptForge.Core;
namespace PromptForge.UI.ViewModels;

public partial class VariableItem : ObservableObject
{
    private static readonly Regex KeyRx = new(@"^[A-Za-z_][A-Za-z0-9_]*$");

    [ObservableProperty] private string _key = "";
    [ObservableProperty] private bool _required = true;
    [ObservableProperty] private bool _isDropdown;
    [ObservableProperty] private string _default = "";
    [ObservableProperty] private string _optionsText = "";

    public bool IsValidKey => KeyRx.IsMatch(Key.Trim());

    public PromptVariable ToVariable() => new(
        Key.Trim(),
        IsDropdown ? VariableKind.Dropdown : VariableKind.Text,
        Required,
        string.IsNullOrWhiteSpace(Default) ? null : Default.Trim(),
        IsDropdown ? OptionsText.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) : null);

    public static VariableItem From(PromptVariable v) => new()
    {
        Key = v.Key,
        Required = v.Required,
        IsDropdown = v.Kind == VariableKind.Dropdown,
        Default = v.Default ?? "",
        OptionsText = v.Options is null ? "" : string.Join(", ", v.Options)
    };
}
