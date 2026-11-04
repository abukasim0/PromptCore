using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PromptCore.Core;
using PromptCore.UI.Views;
namespace PromptCore.UI.ViewModels;

public partial class BuilderViewModel : ObservableObject
{
    private static readonly (string Type, string Title)[] Defaults =
    {
        ("role", "Role"), ("context", "Context"), ("objective", "Objective"), ("instructions", "Instructions"),
        ("constraints", "Constraints"), ("outputFormat", "Output Format"), ("examples", "Examples"), ("criteria", "Evaluation Criteria")
    };

    private bool _loading;

    public ObservableCollection<SectionItem> Sections { get; } = new();
    public ObservableCollection<VariableItem> Variables { get; } = new();
    public ObservableCollection<string> Findings { get; } = new();

    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _preview = "";

    public BuilderViewModel()
    {
        foreach (var (type, title) in Defaults) Add(new SectionItem(type, title));
        Refresh();
    }

    partial void OnNameChanged(string value) => Refresh();

    private void Add(SectionItem item)
    {
        item.PropertyChanged += (_, _) => Refresh();
        Sections.Add(item);
    }

    private void AddVariableItem(VariableItem item)
    {
        item.PropertyChanged += (_, _) => Refresh();
        Variables.Add(item);
    }

    private PromptDocument BuildDocument()
    {
        var doc = new PromptDocument { Name = Name };
        foreach (var s in Sections) doc.Sections.Add(new PromptSection(s.Type, s.Title, s.Text, s.Enabled));
        foreach (var v in Variables.Where(v => v.IsValidKey)) doc.Variables.Add(v.ToVariable());
        return doc;
    }

    private void Refresh()
    {
        if (_loading) return;
        var doc = BuildDocument();
        Preview = PromptComposer.Compose(doc);
        Findings.Clear();
        foreach (var f in Optimizer.Analyze(doc))
            Findings.Add((f.Severity == Severity.Warning ? "⚠ " : "ℹ ") + f.Message);
        foreach (var v in Variables.Where(v => !v.IsValidKey))
            Findings.Add("⚠ Invalid variable name: \"" + v.Key + "\"");
    }

    [RelayCommand]
    private void MoveUp(SectionItem? item)
    {
        if (item is null) return;
        var i = Sections.IndexOf(item);
        if (i > 0) { Sections.Move(i, i - 1); Refresh(); }
    }

    [RelayCommand]
    private void MoveDown(SectionItem? item)
    {
        if (item is null) return;
        var i = Sections.IndexOf(item);
        if (i >= 0 && i < Sections.Count - 1) { Sections.Move(i, i + 1); Refresh(); }
    }

    [RelayCommand]
    private void AddSection()
    {
        Add(new SectionItem("custom", "Custom"));
        Refresh();
    }

    [RelayCommand]
    private void AddVariable()
    {
        AddVariableItem(new VariableItem());
        Refresh();
    }

    [RelayCommand]
    private void RemoveVariable(VariableItem? item)
    {
        if (item is null) return;
        Variables.Remove(item);
        Refresh();
    }

    [RelayCommand]
    private void DetectVariables()
    {
        var declared = Variables.Select(v => v.Key.Trim()).ToHashSet();
        foreach (var key in VariableEngine.Find(Preview).Where(k => !declared.Contains(k)))
            AddVariableItem(new VariableItem { Key = key });
        Refresh();
    }

    [RelayCommand]
    private void Copy()
    {
        if (string.IsNullOrWhiteSpace(Preview)) return;
        var doc = BuildDocument();
        var used = VariableEngine.Find(Preview).Any(k => doc.Variables.Any(v => v.Key == k));
        if (!used) { Clipboard.SetText(Preview); return; }

        var vm = new FillViewModel(Preview, doc.Variables);
        var win = new FillVariablesWindow(vm) { Owner = Application.Current.MainWindow };
        if (win.ShowDialog() == true) Clipboard.SetText(vm.Preview);
    }

    [RelayCommand]
    private void Export()
    {
        var dlg = new SaveFileDialog { Filter = "JSON (*.json)|*.json|Markdown (*.md)|*.md|Text (*.txt)|*.txt", FileName = string.IsNullOrWhiteSpace(Name) ? "prompt" : Name };
        if (dlg.ShowDialog() != true) return;
        var doc = BuildDocument();
        var content = dlg.FilterIndex switch
        {
            1 => PromptJson.Export(doc),
            2 => PromptJson.ToMarkdown(doc),
            _ => PromptJson.ToText(doc)
        };
        File.WriteAllText(dlg.FileName, content);
    }

    [RelayCommand]
    private void Import()
    {
        var dlg = new OpenFileDialog { Filter = "JSON (*.json)|*.json" };
        if (dlg.ShowDialog() != true) return;
        var result = PromptJson.Import(File.ReadAllText(dlg.FileName));
        if (!result.Ok)
        {
            var title = Application.Current.TryFindResource("Msg.ImportFailed") as string ?? "Import failed";
            MessageBox.Show(string.Join("\n", result.Errors), title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _loading = true;
        Sections.Clear();
        Variables.Clear();
        foreach (var s in result.Document!.Sections)
            Add(new SectionItem(s.Type, s.Title) { Text = s.Text, Enabled = s.Enabled });
        foreach (var v in result.Document.Variables) AddVariableItem(VariableItem.From(v));
        Name = result.Document.Name;
        _loading = false;
        Refresh();
    }
}
