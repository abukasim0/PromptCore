namespace PromptCore.Core;

public enum VariableKind { Text, Dropdown }

public sealed record PromptVariable(
    string Key,
    VariableKind Kind = VariableKind.Text,
    bool Required = true,
    string? Default = null,
    IReadOnlyList<string>? Options = null);

/// <summary>Type: role, context, objective, instructions, constraints, outputFormat, examples, criteria, custom, free.</summary>
public sealed record PromptSection(string Type, string Title, string Text, bool Enabled = true);

/// <summary>One model shared by the structured builder and the free-text editor (free text = a single "free" section).</summary>
public sealed class PromptDocument
{
    public string Name { get; set; } = "";
    public string Language { get; set; } = "en";
    public List<PromptSection> Sections { get; set; } = new();
    public List<PromptVariable> Variables { get; set; } = new();
}
