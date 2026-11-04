using PromptForge.Core;
using Xunit;

public class VariableRobustnessTests
{
    [Fact]
    public void Duplicate_variable_keys_do_not_throw_and_first_definition_wins()
    {
        var vars = new[] { new PromptVariable("A", Default: "one"), new PromptVariable("A", Default: "two") };
        var r = VariableEngine.Apply("{A}", vars, new Dictionary<string, string>());
        Assert.Equal("one", r.Text);
    }

    [Fact]
    public void Optional_variable_without_value_becomes_empty()
    {
        var vars = new[] { new PromptVariable("A", Required: false) };
        var r = VariableEngine.Apply("x{A}y", vars, new Dictionary<string, string>());
        Assert.Equal("xy", r.Text);
        Assert.Empty(r.MissingRequired);
    }
}
