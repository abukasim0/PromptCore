namespace PromptForge.Core;

public enum Severity { Info, Warning }
public sealed record Finding(string Rule, Severity Severity, string Message);

/// <summary>Deterministic rule-based analysis. It never modifies the prompt; it only reports findings.</summary>
public static class Optimizer
{
    private static readonly string[] Vague = { "good", "nice", "some", "stuff", "etc", "things", "جيد", "بعض", "أشياء", "إلخ" };

    public static IReadOnlyList<Finding> Analyze(PromptDocument doc)
    {
        var f = new List<Finding>();
        bool Has(string type) => doc.Sections.Any(s => s.Enabled && s.Type == type && !string.IsNullOrWhiteSpace(s.Text));
        var hasFree = doc.Sections.Any(s => s.Enabled && s.Type == "free");

        if (!hasFree && !Has("objective")) f.Add(new("missing-objective", Severity.Warning, "No objective section: state clearly what the model must do."));
        if (!hasFree && !Has("outputFormat")) f.Add(new("missing-output-format", Severity.Info, "No output format defined."));

        var text = PromptComposer.Compose(doc);
        var declared = doc.Variables.Select(v => v.Key).ToHashSet();
        foreach (var k in VariableEngine.Find(text).Where(k => !declared.Contains(k)))
            f.Add(new("undefined-variable", Severity.Warning, $"Variable {{{k}}} is used but not declared."));

        var dups = text.Split('\n').Select(l => l.Trim().ToLowerInvariant()).Where(l => l.Length >= 15 && !l.StartsWith("##"))
            .GroupBy(l => l).Where(g => g.Count() > 1);
        foreach (var g in dups) f.Add(new("repeated-instruction", Severity.Info, $"Line repeated {g.Count()} times: \"{g.Key}\""));

        var words = text.Split(new[] { ' ', '\n', '\t', ',', '.', '،', ':' }, StringSplitOptions.RemoveEmptyEntries).Select(w => w.ToLowerInvariant());
        foreach (var w in words.Where(w => Vague.Contains(w)).Distinct())
            f.Add(new("vague-wording", Severity.Info, $"Vague word \"{w}\": replace it with something measurable."));
        return f;
    }
}
