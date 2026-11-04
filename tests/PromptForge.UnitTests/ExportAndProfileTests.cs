using PromptForge.Core;
using Xunit;

public class ExportAndProfileTests
{
    private static PromptDocument Sample()
    {
        var d = new PromptDocument { Name = "مساعد بحث", Language = "ar" };
        d.Sections.Add(new("role", "الدور", "أنت باحث خبير في {SPECIALTY}"));
        d.Sections.Add(new("objective", "الهدف", "لخّص النص"));
        d.Variables.Add(new PromptVariable("SPECIALTY", VariableKind.Dropdown, true, null, new[] { "طب", "قانون" }));
        return d;
    }

    [Fact]
    public void Json_round_trip_preserves_document_and_keeps_arabic_readable()
    {
        var json = PromptJson.Export(Sample());
        Assert.Contains("مساعد بحث", json);
        var r = PromptJson.Import(json);
        Assert.True(r.Ok, string.Join("; ", r.Errors));
        Assert.Equal("مساعد بحث", r.Document!.Name);
        Assert.Equal(2, r.Document.Sections.Count);
        Assert.Equal(VariableKind.Dropdown, r.Document.Variables[0].Kind);
        Assert.Equal(new[] { "طب", "قانون" }, r.Document.Variables[0].Options);
        Assert.Equal(PromptComposer.Compose(Sample()), PromptComposer.Compose(r.Document));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{\"sections\":[]}")]
    [InlineData("{\"schemaVersion\":\"2.0\",\"sections\":[]}")]
    [InlineData("{\"schemaVersion\":\"1.0\",\"sections\":{}}")]
    [InlineData("{\"schemaVersion\":\"1.0\",\"sections\":[{\"title\":\"x\"}]}")]
    public void Import_rejects_bad_input_without_throwing(string json)
    {
        var r = PromptJson.Import(json);
        Assert.False(r.Ok);
        Assert.NotEmpty(r.Errors);
    }

    [Fact]
    public void Import_rejects_invalid_variable_key()
    {
        var r = PromptJson.Import("{\"schemaVersion\":\"1.0\",\"sections\":[],\"variables\":[{\"key\":\"bad key\"}]}");
        Assert.False(r.Ok);
    }

    [Fact]
    public void Markdown_export_adds_title()
    {
        Assert.StartsWith("# مساعد بحث\n\n## الدور", PromptJson.ToMarkdown(Sample()));
    }

    [Fact]
    public void Xml_profile_wraps_sections_keeps_text_and_lists_changes()
    {
        var claude = ModelProfiles.BuiltIn.Single(p => p.Id == "claude");
        var a = ModelProfiles.Adapt(Sample(), claude);
        Assert.Contains("<role>\nأنت باحث خبير في {SPECIALTY}\n</role>", a.Text);
        Assert.Single(a.Changes);
    }

    [Fact]
    public void Custom_instructions_are_appended_and_reported()
    {
        var p = new ModelProfile("x", "X", SectionStyle.Plain, "Answer in Arabic.");
        var a = ModelProfiles.Adapt(Sample(), p);
        Assert.EndsWith("Answer in Arabic.", a.Text);
        Assert.Equal(2, a.Changes.Count);
    }
}
