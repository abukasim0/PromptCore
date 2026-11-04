using System.Globalization;
using Microsoft.Data.Sqlite;

namespace PromptForge.Data;

/// <summary>CRUD for the prompt library. All values go through parameters; dates are ISO 8601 UTC.</summary>
public sealed class PromptRepository
{
    private readonly PromptForgeDb _db;
    public PromptRepository(PromptForgeDb db) => _db = db;

    /// <summary>Inserts a new prompt (draft.Id == 0) or updates an existing one. Returns the row id.</summary>
    public int Save(SavedPrompt draft)
    {
        var now = UtcNow();
        lock (_db)
        {
            using var cmd = _db.CreateCommand();
            if (draft.Id == 0)
            {
                cmd.CommandText = """
                    INSERT INTO prompts (name, idea, type_id, model_id, language, result_text, document_json,
                                         is_favorite, use_count, created_at, updated_at)
                    VALUES ($name, $idea, $type, $model, $lang, $result, $doc, $fav, $use, $now, $now);
                    SELECT last_insert_rowid();
                    """;
                Bind(cmd, draft, now);
                return Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
            }

            cmd.CommandText = """
                UPDATE prompts SET name = $name, idea = $idea, type_id = $type, model_id = $model,
                                   language = $lang, result_text = $result, document_json = $doc,
                                   updated_at = $now
                WHERE id = $id;
                """;
            Bind(cmd, draft, now);
            cmd.Parameters.AddWithValue("$id", draft.Id);
            cmd.ExecuteNonQuery();
            return draft.Id;
        }
    }

    public bool Delete(int id)
    {
        lock (_db)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "DELETE FROM prompts WHERE id = $id;";
            cmd.Parameters.AddWithValue("$id", id);
            return cmd.ExecuteNonQuery() > 0;
        }
    }

    public SavedPrompt? GetById(int id)
    {
        lock (_db)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "SELECT * FROM prompts WHERE id = $id LIMIT 1;";
            cmd.Parameters.AddWithValue("$id", id);
            using var r = cmd.ExecuteReader();
            return r.Read() ? Map(r) : null;
        }
    }

    /// <summary>Most recently updated prompts; an optional non-empty search matches name, idea or result.</summary>
    public IReadOnlyList<SavedPrompt> GetRecent(int limit = 50, string? search = null, bool favoritesOnly = false)
    {
        var sql = "SELECT * FROM prompts WHERE 1 = 1";
        if (!string.IsNullOrWhiteSpace(search))
            sql += """
                     AND (name LIKE $q ESCAPE '\'
                       OR idea LIKE $q ESCAPE '\'
                       OR result_text LIKE $q ESCAPE '\')
                    """;
        if (favoritesOnly) sql += " AND is_favorite = 1";
        sql += " ORDER BY updated_at DESC LIMIT $limit;";

        var items = new List<SavedPrompt>();
        lock (_db)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("$limit", limit);
            if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("$q", Like(search));
            using var r = cmd.ExecuteReader();
            while (r.Read()) items.Add(Map(r));
        }
        return items;
    }

    /// <summary>Adds or removes a favorite flag; returns the new state.</summary>
    public bool ToggleFavorite(int id)
    {
        lock (_db)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = """
                UPDATE prompts SET is_favorite = 1 - is_favorite WHERE id = $id;
                SELECT is_favorite FROM prompts WHERE id = $id;
                """;
            cmd.Parameters.AddWithValue("$id", id);
            var result = cmd.ExecuteScalar();
            return result is not null && Convert.ToInt64(result, CultureInfo.InvariantCulture) == 1;
        }
    }

    /// <summary>Counts one more use (copy/export) and stamps the row, keeping recency order honest.</summary>
    public void RecordUse(int id)
    {
        lock (_db)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = """
                UPDATE prompts SET use_count = use_count + 1, updated_at = $now WHERE id = $id;
                """;
            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$now", UtcNow());
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>Attaches a tag (creating it when needed); duplicates are ignored.</summary>
    public void AddTag(int promptId, string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return;
        lock (_db)
        {
            using var ins = _db.CreateCommand();
            ins.Transaction = null;
            ins.CommandText = """
                INSERT INTO tags (name) VALUES ($name) ON CONFLICT(name) DO NOTHING;
                INSERT INTO prompt_tags (prompt_id, tag_id)
                    SELECT $pid, id FROM tags WHERE name = $name
                    ON CONFLICT DO NOTHING;
                """;
            ins.Parameters.AddWithValue("$name", tag.Trim());
            ins.Parameters.AddWithValue("$pid", promptId);
            ins.ExecuteNonQuery();
        }
    }

    public IReadOnlyList<string> GetTags(int promptId)
    {
        var tags = new List<string>();
        lock (_db)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = """
                SELECT t.name FROM tags t
                JOIN prompt_tags pt ON pt.tag_id = t.id
                WHERE pt.prompt_id = $pid ORDER BY t.name;
                """;
            cmd.Parameters.AddWithValue("$pid", promptId);
            using var r = cmd.ExecuteReader();
            while (r.Read()) tags.Add(r.GetString(0));
        }
        return tags;
    }

    // ----- helpers -------------------------------------------------------

    private static void Bind(SqliteCommand cmd, SavedPrompt p, string now)
    {
        cmd.Parameters.AddWithValue("$name", p.Name);
        cmd.Parameters.AddWithValue("$idea", p.Idea);
        cmd.Parameters.AddWithValue("$type", p.TypeId);
        cmd.Parameters.AddWithValue("$model", p.ModelId);
        cmd.Parameters.AddWithValue("$lang", p.Language);
        cmd.Parameters.AddWithValue("$result", p.ResultText);
        cmd.Parameters.AddWithValue("$doc", (object?)p.DocumentJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$fav", p.IsFavorite ? 1 : 0);
        cmd.Parameters.AddWithValue("$use", p.UseCount);
        cmd.Parameters.AddWithValue("$now", now);
    }

    internal static string Like(string input) =>
        "%" + input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";

    internal static string UtcNow() => DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);

    internal static DateTime ParseDate(string value) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d)
            ? d : DateTime.MinValue;

    private static SavedPrompt Map(SqliteDataReader r) => new(
        r.GetInt32(r.GetOrdinal("id")),
        r.GetString(r.GetOrdinal("name")),
        r.GetString(r.GetOrdinal("idea")),
        r.GetString(r.GetOrdinal("type_id")),
        r.GetString(r.GetOrdinal("model_id")),
        r.GetString(r.GetOrdinal("language")),
        r.GetString(r.GetOrdinal("result_text")),
        r.IsDBNull(r.GetOrdinal("document_json")) ? null : r.GetString(r.GetOrdinal("document_json")),
        r.GetInt64(r.GetOrdinal("is_favorite")) == 1,
        (int)r.GetInt64(r.GetOrdinal("use_count")),
        ParseDate(r.GetString(r.GetOrdinal("created_at"))),
        ParseDate(r.GetString(r.GetOrdinal("updated_at"))));
}

/// <summary>Append-only action log with a simple size cap helper.</summary>
public sealed class HistoryRepository
{
    private readonly PromptForgeDb _db;
    public HistoryRepository(PromptForgeDb db) => _db = db;

    public void Add(string action, string detail = "", int? promptId = null)
    {
        lock (_db)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = """
                INSERT INTO history (prompt_id, action, detail, created_at) VALUES ($pid, $action, $detail, $now);
                """;
            cmd.Parameters.AddWithValue("$pid", (object?)promptId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$action", action);
            cmd.Parameters.AddWithValue("$detail", detail);
            cmd.Parameters.AddWithValue("$now", PromptRepository.UtcNow());
            cmd.ExecuteNonQuery();
        }
    }

    public IReadOnlyList<HistoryEntry> Recent(int limit = 100)
    {
        var items = new List<HistoryEntry>();
        lock (_db)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = """
                SELECT id, prompt_id, action, detail, created_at FROM history ORDER BY created_at DESC, id DESC LIMIT $limit;
                """;
            cmd.Parameters.AddWithValue("$limit", limit);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                items.Add(new HistoryEntry(
                    r.GetInt32(0),
                    r.IsDBNull(1) ? null : r.GetInt32(1),
                    r.GetString(2),
                    r.GetString(3),
                    PromptRepository.ParseDate(r.GetString(4))));
        }
        return items;
    }

    /// <summary>Keeps only the newest keep entries; the rest are pruned. Returns the number of removed rows.</summary>
    public int Prune(int keep = 1000)
    {
        lock (_db)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = """
                DELETE FROM history WHERE id NOT IN (
                    SELECT id FROM history ORDER BY created_at DESC, id DESC LIMIT $keep);
                """;
            cmd.Parameters.AddWithValue("$keep", keep);
            return cmd.ExecuteNonQuery();
        }
    }

    public void Clear()
    {
        lock (_db)
        {
            _db.Exec("DELETE FROM history;");
        }
    }
}

/// <summary>Key/value app settings persisted across runs (theme, language, window state…).</summary>
public sealed class SettingsRepository
{
    private readonly PromptForgeDb _db;
    public SettingsRepository(PromptForgeDb db) => _db = db;

    public string Get(string key, string fallback = "")
    {
        lock (_db)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "SELECT value FROM settings WHERE key = $key LIMIT 1;";
            cmd.Parameters.AddWithValue("$key", key);
            var v = cmd.ExecuteScalar() as string;
            return v ?? fallback;
        }
    }

    public void Set(string key, string value)
    {
        lock (_db)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = """
                INSERT INTO settings (key, value, updated_at) VALUES ($key, $value, $now)
                ON CONFLICT(key) DO UPDATE SET value = $value, updated_at = $now;
                """;
            cmd.Parameters.AddWithValue("$key", key);
            cmd.Parameters.AddWithValue("$value", value);
            cmd.Parameters.AddWithValue("$now", PromptRepository.UtcNow());
            cmd.ExecuteNonQuery();
        }
    }
}
