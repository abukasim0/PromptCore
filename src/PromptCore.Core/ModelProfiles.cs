namespace PromptCore.Core;

public enum SectionStyle { Markdown, Xml, Plain }

/// <summary>Editable formatting preferences. These are starting points, not platform requirements.</summary>
public sealed record ModelProfile(string Id, string Name, SectionStyle Style, string? CustomInstructions = null);

public sealed record AdaptedPrompt(string Text, IReadOnlyList<string> Changes);

public static class ModelProfiles
{
    public static readonly IReadOnlyList<ModelProfile> BuiltIn = new[]
    {
        new ModelProfile("generic", "Generic", SectionStyle.Plain),
        new ModelProfile("chatgpt", "ChatGPT", SectionStyle.Markdown),
        new ModelProfile("claude", "Claude", SectionStyle.Xml),
        new ModelProfile("gemini", "Gemini", SectionStyle.Markdown),
        new ModelProfile("grok", "Grok", SectionStyle.Plain),
        new ModelProfile("deepseek", "DeepSeek", SectionStyle.Markdown),
        new ModelProfile("perplexity", "Perplexity", SectionStyle.Plain),
        new ModelProfile("qwen", "Qwen", SectionStyle.Markdown),
        new ModelProfile("mistral", "Mistral", SectionStyle.Markdown),
    };

    /// <summary>Only changes how sections are laid out; wording is never altered. Changes are listed so the UI can show them before applying.</summary>
    public static AdaptedPrompt Adapt(PromptDocument doc, ModelProfile profile)
    {
        var secs = doc.Sections.Where(s => s.Enabled && !string.IsNullOrWhiteSpace(s.Text)).ToList();
        var changes = new List<string>();
        var blocks = new List<string>();

        foreach (var s in secs)
        {
            var text = s.Text.Trim();
            if (s.Type == "free") { blocks.Add(text); continue; }
            blocks.Add(profile.Style switch
            {
                SectionStyle.Xml => $"<{s.Type}>\n{text}\n</{s.Type}>",
                SectionStyle.Plain => $"{s.Title}:\n{text}",
                _ => $"## {s.Title}\n{text}"
            });
        }
        if (secs.Any(s => s.Type != "free"))
            changes.Add(profile.Style switch
            {
                SectionStyle.Xml => "Sections wrapped in XML-style tags.",
                SectionStyle.Plain => "Sections written as plain \"Title:\" blocks without Markdown.",
                _ => "Sections written as Markdown headings."
            });

        if (!string.IsNullOrWhiteSpace(profile.CustomInstructions))
        {
            blocks.Add(profile.CustomInstructions.Trim());
            changes.Add("Profile custom instructions appended at the end.");
        }
        return new AdaptedPrompt(string.Join("\n\n", blocks), changes);
    }
}
