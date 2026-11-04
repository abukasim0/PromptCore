using PromptForge.Core;
using Xunit;

public class CoreTests
{
    [Fact]
    public void Compose_skips_disabled_and_empty_sections_and_keeps_order()
    {
        var d = new PromptDocument();
        d.Sections.Add(new("role", "Role", "Analyst"));
        d.Sections.Add(new("context", "Context", "ignored", Enabled: false));
        d.Sections.Add(new("objective", "Objective", " Summarize "));
        Assert.Equal("## Role\nAnalyst\n\n## Objective\nSummarize", PromptComposer.Compose(d));
    }

    [Fact]
    public void Variables_use_default_and_report_missing_and_undeclared()
    {
        var vars = new[] { new PromptVariable("TONE", Default: "formal"), new PromptVariable("TASK") };
        var r = VariableEngine.Apply("{TONE} {TASK} {OTHER}", vars, new Dictionary<string, string>());
        Assert.Equal("formal {TASK} {OTHER}", r.Text);
        Assert.Equal(new[] { "TASK" }, r.MissingRequired);
        Assert.Equal(new[] { "OTHER" }, r.Undeclared);
    }

    [Fact]
    public void Dropdown_rejects_value_outside_options()
    {
        var vars = new[] { new PromptVariable("FMT", VariableKind.Dropdown, Options: new[] { "md", "txt" }) };
        var r = VariableEngine.Apply("{FMT}", vars, new Dictionary<string, string> { ["FMT"] = "pdf" });
        Assert.Equal(new[] { "FMT" }, r.InvalidOptions);
    }

    [Fact]
    public void Diff_marks_added_and_removed_lines()
    {
        var d = TextDiff.Compare("a\nb\nc", "a\nc\nd");
        Assert.Contains(new DiffLine(DiffKind.Removed, "b"), d);
        Assert.Contains(new DiffLine(DiffKind.Added, "d"), d);
        Assert.Contains(new DiffLine(DiffKind.Same, "a"), d);
    }

    [Fact]
    public void Optimizer_reports_without_mutating_the_document()
    {
        var d = new PromptDocument();
        d.Sections.Add(new("instructions", "Instructions", "Write {X} about some things"));
        var before = PromptComposer.Compose(d);
        var rules = Optimizer.Analyze(d).Select(f => f.Rule).ToList();
        Assert.Contains("missing-objective", rules);
        Assert.Contains("undefined-variable", rules);
        Assert.Contains("vague-wording", rules);
        Assert.Equal(before, PromptComposer.Compose(d));
    }
}
