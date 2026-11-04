namespace PromptForge.Core;

public static class PromptComposer
{
    public static string Compose(PromptDocument doc)
    {
        var blocks = new List<string>();
        foreach (var s in doc.Sections.Where(s => s.Enabled && !string.IsNullOrWhiteSpace(s.Text)))
        {
            var text = s.Text.Trim();
            blocks.Add(s.Type == "free" ? text : "## " + s.Title + "\n" + text);
        }
        return string.Join("\n\n", blocks);
    }
}
