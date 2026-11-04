using PromptCore.Data;
using Xunit;

public class StorageTests : IDisposable
{
    private readonly string _path;
    private readonly PromptCoreDb _db;

    public StorageTests()
    {
        _path = Path.Combine(Path.GetTempPath(), $"pf_test_{Guid.NewGuid():N}.db");
        _db = new PromptCoreDb(_path);
    }

    public void Dispose()
    {
        _db.Dispose();
        foreach (var f in new[] { _path, _path + "-shm", _path + "-wal" })
            if (File.Exists(f)) File.Delete(f);
    }

    private static SavedPrompt NewDraft(string name = "My prompt",
        string idea = "فكرة عربية للاختبار", string result = "## Role\nTest") =>
        new(0, name, idea, "general", "chatgpt", "en", result, null, IsFavorite: false, UseCount: 0,
            CreatedAt: default, UpdatedAt: default);

    [Fact]
    public void Migrate_creates_schema_and_is_idempotent()
    {
        _db.Migrate();
        Assert.Equal(PromptCoreDb.CurrentSchemaVersion, _db.GetUserVersion());
        _db.Migrate(); // second run must not throw or change the version
        Assert.Equal(PromptCoreDb.CurrentSchemaVersion, _db.GetUserVersion());
    }

    [Fact]
    public void Save_and_get_roundtrip_preserves_values()
    {
        _db.Migrate();
        var repo = new PromptRepository(_db);
        var id = repo.Save(NewDraft());
        Assert.True(id > 0);

        var loaded = repo.GetById(id)!;
        Assert.Equal("My prompt", loaded.Name);
        Assert.Equal("فكرة عربية للاختبار", loaded.Idea);
        Assert.Equal("general", loaded.TypeId);
        Assert.Equal("chatgpt", loaded.ModelId);
        Assert.Equal("## Role\nTest", loaded.ResultText);
        Assert.False(loaded.IsFavorite);
        Assert.True(loaded.UpdatedAt > DateTime.MinValue);
    }

    [Fact]
    public void Update_keeps_id_and_changes_fields()
    {
        _db.Migrate();
        var repo = new PromptRepository(_db);
        var id = repo.Save(NewDraft());
        repo.Save(repo.GetById(id)! with { Name = "Renamed", ResultText = "new" });

        var loaded = repo.GetById(id)!;
        Assert.Equal("Renamed", loaded.Name);
        Assert.Equal("new", loaded.ResultText);
        Assert.Equal(id, loaded.Id);
    }

    [Fact]
    public void GetRecent_orders_by_recency_and_search_filters()
    {
        _db.Migrate();
        var repo = new PromptRepository(_db);
        repo.Save(NewDraft("First: invoice writer"));
        repo.Save(NewDraft("Second: blog intro"));

        var all = repo.GetRecent();
        Assert.Equal(2, all.Count);
        Assert.Equal("Second: blog intro", all[0].Name); // newest first

        var hit = repo.GetRecent(search: "blog");
        Assert.Single(hit);
        Assert.Equal("Second: blog intro", hit[0].Name);

        Assert.Empty(repo.GetRecent(search: "البريد الإلكتروني"));
    }

    [Fact]
    public void Search_percent_sign_is_escaped()
    {
        _db.Migrate();
        var repo = new PromptRepository(_db);
        repo.Save(NewDraft("100% off"));
        repo.Save(NewDraft("plain"));

        Assert.Single(repo.GetRecent(search: "100%"));
        Assert.Empty(repo.GetRecent(search: "1__ off"));
    }

    [Fact]
    public void ToggleFavorite_flips_state()
    {
        _db.Migrate();
        var repo = new PromptRepository(_db);
        var id = repo.Save(NewDraft());

        Assert.True(repo.ToggleFavorite(id));
        Assert.True(repo.GetById(id)!.IsFavorite);
        Assert.False(repo.ToggleFavorite(id));
        Assert.False(repo.GetById(id)!.IsFavorite);
    }

    [Fact]
    public void Favorites_only_filter()
    {
        _db.Migrate();
        var repo = new PromptRepository(_db);
        var a = repo.Save(NewDraft("A"));
        repo.Save(NewDraft("B"));
        repo.ToggleFavorite(a);

        var favs = repo.GetRecent(favoritesOnly: true);
        Assert.Single(favs);
        Assert.Equal("A", favs[0].Name);
    }

    [Fact]
    public void RecordUse_increments_counter()
    {
        _db.Migrate();
        var repo = new PromptRepository(_db);
        var id = repo.Save(NewDraft());
        repo.RecordUse(id);
        repo.RecordUse(id);
        Assert.Equal(2, repo.GetById(id)!.UseCount);
    }

    [Fact]
    public void Tags_survive_and_cascade_on_delete()
    {
        _db.Migrate();
        var repo = new PromptRepository(_db);
        var id = repo.Save(NewDraft());
        repo.AddTag(id, "marketing");
        repo.AddTag(id, "Marketing"); // case-insensitive duplicate is ignored
        repo.AddTag(id, "b2b");
        Assert.Equal(new[] { "b2b", "marketing" }, repo.GetTags(id));

        Assert.True(repo.Delete(id));
        Assert.Empty(repo.GetTags(id));
        Assert.Null(repo.GetById(id));
    }

    [Fact]
    public void History_add_recent_and_prune()
    {
        _db.Migrate();
        var repo = new PromptRepository(_db);
        var history = new HistoryRepository(_db);
        var id = repo.Save(NewDraft());

        history.Add("save", "My prompt", id);
        history.Add("copy", "My prompt", id);
        var entries = history.Recent();
        Assert.Equal(2, entries.Count);
        Assert.Equal("copy", entries[0].Action); // newest first
        Assert.Equal(id, entries[0].PromptId);

        for (var i = 0; i < 5; i++) history.Add("open");
        Assert.Equal(5, history.Prune(keep: 2)); // 7 rows kept 2 → 5 removed
        Assert.Equal(2, history.Recent().Count);
    }

    [Fact]
    public void Settings_roundtrip_and_overwrite()
    {
        _db.Migrate();
        var settings = new SettingsRepository(_db);
        Assert.Equal("dark", settings.Get("theme", "dark")); // fallback before first write

        settings.Set("theme", "light");
        Assert.Equal("light", settings.Get("theme", "dark"));
        settings.Set("theme", "dark");
        Assert.Equal("dark", settings.Get("theme", "light"));
    }

    [Fact]
    public void Deleting_prompt_sets_history_reference_null()
    {
        _db.Migrate();
        var repo = new PromptRepository(_db);
        var history = new HistoryRepository(_db);
        var id = repo.Save(NewDraft());
        history.Add("save", "x", id);
        repo.Delete(id);

        var entry = history.Recent()[0];
        Assert.Null(entry.PromptId); // FK ON DELETE SET NULL keeps the log intact
    }
}
