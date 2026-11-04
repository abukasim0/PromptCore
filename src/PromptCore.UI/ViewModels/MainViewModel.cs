using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PromptCore.Core;
using PromptCore.Data;
namespace PromptCore.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly PromptRepository _prompts;
    private readonly HistoryRepository _history;
    private int _loadedId;   // id of the saved prompt currently in the editor (0 = new)

    public IReadOnlyList<QuickType> Types => QuickPrompt.Types;
    public IReadOnlyList<ModelProfile> Models => ModelProfiles.BuiltIn;

    public ObservableCollection<SavedPrompt> SavedItems { get; } = new();

    [ObservableProperty] private string _idea = "";
    [ObservableProperty] private QuickType _selectedType;
    [ObservableProperty] private ModelProfile _selectedModel;
    [ObservableProperty] private string _result = "";
    [ObservableProperty] private string _status = "";
    [ObservableProperty] private bool _isLibraryOpen;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _favoritesOnly;

    public MainViewModel(PromptRepository prompts, HistoryRepository history)
    {
        _prompts = prompts;
        _history = history;
        _selectedType = QuickPrompt.Types[0];
        _selectedModel = ModelProfiles.BuiltIn.First(m => m.Id == "chatgpt");
        RefreshSaved();
    }

    partial void OnIdeaChanged(string value) => Update();
    partial void OnSelectedTypeChanged(QuickType value) => Update();
    partial void OnSelectedModelChanged(ModelProfile value) => Update();
    partial void OnSearchTextChanged(string value) => RefreshSaved();
    partial void OnFavoritesOnlyChanged(bool value) => RefreshSaved();

    private void Update()
    {
        Result = QuickPrompt.Generate(Idea, SelectedType, SelectedModel);
        Status = "";
    }

    private string L(string key) => Application.Current.TryFindResource(key) as string ?? "";

    // ----- editor actions ----------------------------------------------------

    [RelayCommand]
    private void Copy()
    {
        if (string.IsNullOrWhiteSpace(Result)) return;
        Clipboard.SetText(Result);
        if (_loadedId != 0) { _prompts.RecordUse(_loadedId); _history.Add("copy", CurrentName(), _loadedId); }
        Status = L("Simple.Copied");
    }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Result) || string.IsNullOrWhiteSpace(Idea)) { Status = L("Msg.NothingToSave"); return; }

        var draft = new SavedPrompt(_loadedId, AutoName(), Idea.Trim(), SelectedType.Id, SelectedModel.Id,
            DetectLanguage(Idea), Result, null, IsFavorite: false, UseCount: 0, CreatedAt: default, UpdatedAt: default);
        var id = _prompts.Save(draft);
        _history.Add("save", draft.Name, id);
        if (_loadedId != 0 && _loadedId != id) _history.Add("overwrite", draft.Name, id);
        _loadedId = id;
        Status = L("Msg.Saved");
        RefreshSaved();
    }

    // ----- library actions ---------------------------------------------------

    [RelayCommand]
    private void ToggleLibrary() => IsLibraryOpen = !IsLibraryOpen;

    [RelayCommand]
    private void FavoritesFilter() => FavoritesOnly = !FavoritesOnly;

    [RelayCommand]
    private void LoadSaved(SavedPrompt? item)
    {
        if (item is null) return;
        Idea = item.Idea;
        SelectedType = Types.FirstOrDefault(t => t.Id == item.TypeId) ?? Types[0];
        SelectedModel = Models.FirstOrDefault(m => m.Id == item.ModelId) ?? Models[0];
        // Update() regenerated the text; show the stored one so edits in templates never surprise the user.
        Result = item.ResultText;
        _loadedId = item.Id;
        Status = L("Msg.Loaded");
    }

    [RelayCommand]
    private void CopySaved(SavedPrompt? item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.ResultText)) return;
        Clipboard.SetText(item.ResultText);
        _prompts.RecordUse(item.Id);
        _history.Add("copy", item.Name, item.Id);
        Status = L("Simple.Copied");
    }

    [RelayCommand]
    private void ToggleFavorite(SavedPrompt? item)
    {
        if (item is null) return;
        _prompts.ToggleFavorite(item.Id);
        RefreshSaved();
    }

    [RelayCommand]
    private void DeleteSaved(SavedPrompt? item)
    {
        if (item is null) return;
        _prompts.Delete(item.Id);
        _history.Add("delete", item.Name, item.Id);
        if (_loadedId == item.Id) _loadedId = 0;
        Status = L("Msg.Deleted");
        RefreshSaved();
    }

    private void RefreshSaved()
    {
        var items = _prompts.GetRecent(50, string.IsNullOrWhiteSpace(SearchText) ? null : SearchText, FavoritesOnly);
        SavedItems.Clear();
        foreach (var p in items) SavedItems.Add(p);
    }

    // ----- helpers -----------------------------------------------------------

    private string CurrentName() => AutoName();

    private string AutoName()
    {
        var line = Idea.Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.Length > 0);
        if (string.IsNullOrEmpty(line)) return SelectedType.Name;
        return line.Length <= 60 ? line : line[..60].TrimEnd() + "…";
    }

    private static string DetectLanguage(string text)
    {
        var arabic = 0; var latin = 0;
        foreach (var c in text)
        {
            if (c is >= '\u0600' and <= '\u06FF' or >= '\u0750' and <= '\u077F' or >= '\u08A0' and <= '\u08FF') arabic++;
            else if (char.IsAsciiLetter(c)) latin++;
        }
        return arabic > latin ? "ar" : "en";
    }
}
