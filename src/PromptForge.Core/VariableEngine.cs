using System.Text.RegularExpressions;
namespace PromptForge.Core;

public sealed record SubstitutionResult(
    string Text,
    IReadOnlyList<string> MissingRequired,
    IReadOnlyList<string> Undeclared,
    IReadOnlyList<string> InvalidOptions);

/// <summary>Placeholders look like {NAME}: a letter or underscore, then letters, digits or underscores.</summary>
public static class VariableEngine
{
    private static readonly Regex Rx = new(@"\{([A-Za-z_][A-Za-z0-9_]*)\}", RegexOptions.Compiled);

    public static IReadOnlyList<string> Find(string text) =>
        Rx.Matches(text).Select(m => m.Groups[1].Value).Distinct().ToList();

    public static SubstitutionResult Apply(string text, IReadOnlyList<PromptVariable> vars, IReadOnlyDictionary<string, string> values)
    {
        var declared = vars.GroupBy(v => v.Key).ToDictionary(g => g.Key, g => g.First());
        var missing = new List<string>(); var undeclared = new List<string>(); var invalid = new List<string>();
        var result = Rx.Replace(text, m =>
        {
            var key = m.Groups[1].Value;
            if (!declared.TryGetValue(key, out var v)) { if (!undeclared.Contains(key)) undeclared.Add(key); return m.Value; }
            values.TryGetValue(key, out var val);
            if (string.IsNullOrWhiteSpace(val)) val = v.Default;
            if (string.IsNullOrWhiteSpace(val))
            {
                if (v.Required) { if (!missing.Contains(key)) missing.Add(key); return m.Value; }
                return "";
            }
            if (v.Kind == VariableKind.Dropdown && v.Options is not null && !v.Options.Contains(val))
            { if (!invalid.Contains(key)) invalid.Add(key); return m.Value; }
            return val;
        });
        return new SubstitutionResult(result, missing, undeclared, invalid);
    }
}
