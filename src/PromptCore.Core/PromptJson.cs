using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
namespace PromptCore.Core;

public sealed class PromptEnvelope
{
    public string SchemaVersion { get; set; } = PromptJson.CurrentSchema;
    public string Name { get; set; } = "";
    public string Language { get; set; } = "en";
    public List<PromptSection> Sections { get; set; } = new();
    public List<PromptVariable> Variables { get; set; } = new();
    public Dictionary<string, int> Metadata { get; set; } = new() { ["version"] = 1 };
}

public sealed record ImportResult(PromptDocument? Document, IReadOnlyList<string> Errors)
{
    public bool Ok => Document is not null;
}

/// <summary>Versioned JSON import/export (schema 1.x). Import never throws: problems come back as errors.</summary>
public static class PromptJson
{
    public const string CurrentSchema = "1.0";
    private static readonly Regex KeyRx = new(@"^[A-Za-z_][A-Za-z0-9_]*$");

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // keeps Arabic readable in the file
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Export(PromptDocument doc) => JsonSerializer.Serialize(new PromptEnvelope
    {
        Name = doc.Name, Language = doc.Language, Sections = doc.Sections, Variables = doc.Variables
    }, Options);

    public static string ToText(PromptDocument doc) => PromptComposer.Compose(doc);

    public static string ToMarkdown(PromptDocument doc) =>
        string.IsNullOrWhiteSpace(doc.Name) ? PromptComposer.Compose(doc) : "# " + doc.Name + "\n\n" + PromptComposer.Compose(doc);

    public static ImportResult Import(string json)
    {
        ImportResult Fail(string m) => new(null, new[] { m });
        try
        {
            using var jd = JsonDocument.Parse(json);
            var root = jd.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return Fail("Root must be an object.");
            if (!root.TryGetProperty("schemaVersion", out var sv) || sv.ValueKind != JsonValueKind.String) return Fail("Missing schemaVersion.");
            var version = sv.GetString()!;
            if (version.Split('.')[0] != "1") return Fail($"Unsupported schemaVersion {version}.");
            if (!root.TryGetProperty("sections", out var s) || s.ValueKind != JsonValueKind.Array) return Fail("sections must be an array.");

            var env = root.Deserialize<PromptEnvelope>(Options)!;
            var errors = new List<string>();
            for (int i = 0; i < env.Sections.Count; i++)
            {
                var sec = env.Sections[i];
                if (string.IsNullOrWhiteSpace(sec.Type)) errors.Add($"Section {i + 1}: missing type.");
                if (sec.Text is null) errors.Add($"Section {i + 1}: missing text.");
            }
            foreach (var v in env.Variables.Where(v => v.Key is null || !KeyRx.IsMatch(v.Key)))
                errors.Add($"Invalid variable key: \"{v.Key}\".");
            if (errors.Count > 0) return new(null, errors);

            return new(new PromptDocument { Name = env.Name, Language = env.Language, Sections = env.Sections, Variables = env.Variables }, Array.Empty<string>());
        }
        catch (JsonException e) { return Fail("Invalid JSON: " + e.Message); }
    }
}
