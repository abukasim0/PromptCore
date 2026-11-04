using PromptForge.Core;
using Xunit;

public class QuickPromptTests
{
    private static ModelProfile Profile(string id) => ModelProfiles.BuiltIn.Single(p => p.Id == id);

    [Fact]
    public void Empty_idea_gives_empty_result()
    {
        Assert.Equal("", QuickPrompt.Generate("   ", QuickPrompt.Types[0], Profile("generic")));
    }

    [Fact]
    public void Result_contains_idea_role_and_language_rule()
    {
        var t = QuickPrompt.Types.Single(x => x.Id == "programming");
        var text = QuickPrompt.Generate("Write a function to sort a list", t, Profile("chatgpt"));
        Assert.Contains("Write a function to sort a list", text);
        Assert.Contains("You are a senior software engineer.", text);
        Assert.Contains("1. First identify the requested behavior", text);
        Assert.Contains("Reply in the same language as the task.", text);
    }

    [Fact]
    public void Model_profile_changes_layout_but_not_the_idea()
    {
        var t = QuickPrompt.Types[0];
        var md = QuickPrompt.Generate("plan a trip", t, Profile("chatgpt"));
        var xml = QuickPrompt.Generate("plan a trip", t, Profile("claude"));
        Assert.Contains("## Objective", md);
        Assert.Contains("<objective>", xml);
        Assert.Contains("plan a trip", md);
        Assert.Contains("plan a trip", xml);
    }

    [Fact]
    public void Image_type_asks_for_english_prompts()
    {
        var t = QuickPrompt.Types.Single(x => x.Id == "image");
        var result = QuickPrompt.Generate("قطة على القمر", t, Profile("generic"));
        Assert.Contains("اكتب برومبتات الصور باللغة الإنجليزية.", result);
        Assert.Contains("اكتب برومبتين بالإنجليزية", result);
    }

    [Fact]
    public void Arabic_idea_gets_a_detailed_arabic_prompt_with_localized_sections()
    {
        var type = QuickPrompt.Types.Single(x => x.Id == "programming");
        var document = QuickPrompt.Build("اكتب دالة لفرز قائمة", type);
        var result = QuickPrompt.Generate("اكتب دالة لفرز قائمة", type, Profile("chatgpt"));

        Assert.Equal("ar", document.Language);
        Assert.Equal(new[] { "الدور", "المهمة", "طريقة التنفيذ", "القيود", "معايير الجودة", "صيغة المخرجات" },
            document.Sections.Select(section => section.Title));
        Assert.Contains("اكتب دالة لفرز قائمة", result);
        Assert.Contains("الحالات الطرفية", result);
        Assert.Contains("اختبارات", result);
        Assert.Contains("اكتب الشرح بلغة الطلب", result);
        Assert.Contains("## معايير الجودة", result);
    }

    [Fact]
    public void Type_specific_prompts_include_relevant_quality_guidance()
    {
        var research = QuickPrompt.Build("Compare two treatments", QuickPrompt.Types.Single(t => t.Id == "research"));
        var marketing = QuickPrompt.Build("Write a product announcement", QuickPrompt.Types.Single(t => t.Id == "marketing"));
        var image = QuickPrompt.Build("A fox in a snowy forest", QuickPrompt.Types.Single(t => t.Id == "image"));

        Assert.Contains("Never fabricate sources", string.Join("\n", research.Sections.Select(s => s.Text)));
        Assert.Contains("Do not invent product features", string.Join("\n", marketing.Sections.Select(s => s.Text)));
        Assert.Contains("composition", string.Join("\n", image.Sections.Select(s => s.Text)));
        Assert.Equal(6, research.Sections.Count);
    }

    [Fact]
    public void All_types_are_complete_and_unique()
    {
        Assert.Equal(QuickPrompt.Types.Count, QuickPrompt.Types.Select(t => t.Id).Distinct().Count());
        Assert.All(QuickPrompt.Types, t =>
        {
            Assert.False(string.IsNullOrWhiteSpace(t.Role));
            Assert.NotEmpty(t.Instructions);
            Assert.False(string.IsNullOrWhiteSpace(t.OutputFormat));
        });
    }
}
