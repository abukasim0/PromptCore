using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using PromptForge.Core;
namespace PromptForge.UI.ViewModels;

public partial class FillField : ObservableObject
{
    public FillField(PromptVariable v)
    {
        Variable = v;
        Label = v.Required ? v.Key + " *" : v.Key;
        IsDropdown = v.Kind == VariableKind.Dropdown;
        Options = v.Options ?? Array.Empty<string>();
        _value = v.Default ?? "";
    }

    public PromptVariable Variable { get; }
    public string Label { get; }
    public bool IsDropdown { get; }
    public bool IsText => !IsDropdown;
    public IReadOnlyList<string> Options { get; }
    [ObservableProperty] private string? _value;
}

public partial class FillViewModel : ObservableObject
{
    private readonly string _template;
    private readonly IReadOnlyList<PromptVariable> _vars;

    public FillViewModel(string template, IReadOnlyList<PromptVariable> vars)
    {
        _template = template;
        _vars = vars;
        var used = VariableEngine.Find(template).ToHashSet();
        Fields = vars.Where(v => used.Contains(v.Key)).GroupBy(v => v.Key).Select(g => new FillField(g.First())).ToList();
        foreach (var f in Fields) f.PropertyChanged += (_, _) => Update();
        Update();
    }

    public IReadOnlyList<FillField> Fields { get; }
    [ObservableProperty] private string _preview = "";
    [ObservableProperty] private string _error = "";
    [ObservableProperty] private bool _canAccept;

    private static string Res(string key, string fallback) => Application.Current.TryFindResource(key) as string ?? fallback;

    private void Update()
    {
        var values = Fields.ToDictionary(f => f.Variable.Key, f => f.Value ?? "");
        var r = VariableEngine.Apply(_template, _vars, values);
        Preview = r.Text;
        var parts = new List<string>();
        if (r.MissingRequired.Count > 0) parts.Add(Res("Fill.ErrMissing", "Required:") + " " + string.Join(", ", r.MissingRequired));
        if (r.InvalidOptions.Count > 0) parts.Add(Res("Fill.ErrInvalid", "Not an allowed option:") + " " + string.Join(", ", r.InvalidOptions));
        Error = string.Join("\n", parts);
        CanAccept = parts.Count == 0;
    }
}
