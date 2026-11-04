using Microsoft.Data.Sqlite;

namespace PromptForge.Data;

/// <summary>
/// A saved prompt as stored in the library. Matches one row of the "prompts" table.
/// document_json optionally keeps a full PromptDocument (PromptJson export) for lossless reload.
/// </summary>
public sealed record SavedPrompt(
    int Id,
    string Name,
    string Idea,
    string TypeId,
    string ModelId,
    string Language,
    string ResultText,
    string? DocumentJson,
    bool IsFavorite,
    int UseCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>One library action log row (save, copy, delete, restore…).</summary>
public sealed record HistoryEntry(int Id, int? PromptId, string Action, string Detail, DateTime CreatedAt);

/// <summary>
/// Owns the SQLite connection and the schema. Opened with WAL + foreign keys;
/// <see cref="Migrate"/> applies pending schema versions (PRAGMA user_version) inside transactions.
/// </summary>
public sealed class PromptForgeDb : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly object _gate = new();

    public const int CurrentSchemaVersion = 1;

    public PromptForgeDb(string databasePath)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(databasePath));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var csb = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        };
        _conn = new SqliteConnection(csb.ToString());
        _conn.Open();

        Exec("PRAGMA journal_mode=WAL;");
        Exec("PRAGMA foreign_keys=ON;");
        Exec("PRAGMA synchronous=NORMAL;");
    }

    /// <summary>Applies all pending schema migrations. Safe to call repeatedly (idempotent).</summary>
    public void Migrate()
    {
        lock (_gate)
        {
            var version = GetUserVersion();
            foreach (var (targetVersion, apply) in DbMigrations.All)
            {
                if (version >= targetVersion) continue;
                using var tx = _conn.BeginTransaction();
                apply(_conn, tx);
                Exec($"PRAGMA user_version={targetVersion};", tx);
                tx.Commit();
                version = targetVersion;
            }
        }
    }

    public int GetUserVersion()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "PRAGMA user_version;";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>Runs one non-query statement. Callers pass parameters via the factory to avoid injection.</summary>
    public int Exec(string sql, SqliteTransaction? tx = null)
    {
        using var cmd = _conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        return cmd.ExecuteNonQuery();
    }

    /// <summary>Internal helper for repositories; always parameterize values there.</summary>
    internal SqliteCommand CreateCommand(SqliteTransaction? tx = null)
    {
        var cmd = _conn.CreateCommand();
        cmd.Transaction = tx;
        return cmd;
    }

    public void Dispose() => _conn.Dispose();
}

/// <summary>Ordered migration list. Each entry runs once, inside a transaction, guarded by user_version.</summary>
public static class DbMigrations
{
    public static readonly IReadOnlyList<(int Version, Action<SqliteConnection, SqliteTransaction> Apply)> All =
    [
        (1, MigrationV1.Apply)
        // Future schema changes go here as (2, MigrationV2.Apply), (3, ...), ...
    ];
}

/// <summary>Schema v1 — initial library: prompts, tags, prompt_tags, history, settings.</summary>
internal static class MigrationV1
{
    public static void Apply(SqliteConnection conn, SqliteTransaction tx)
    {
        Exec(conn, tx, """
            CREATE TABLE IF NOT EXISTS prompts (
                id            INTEGER PRIMARY KEY AUTOINCREMENT,
                name          TEXT    NOT NULL DEFAULT '',
                idea          TEXT    NOT NULL DEFAULT '',
                type_id       TEXT    NOT NULL DEFAULT 'general',
                model_id      TEXT    NOT NULL DEFAULT 'chatgpt',
                language      TEXT    NOT NULL DEFAULT 'en',
                result_text   TEXT    NOT NULL DEFAULT '',
                document_json TEXT,
                is_favorite   INTEGER NOT NULL DEFAULT 0,
                use_count     INTEGER NOT NULL DEFAULT 0,
                created_at    TEXT    NOT NULL,
                updated_at    TEXT    NOT NULL
            );
            """);

        Exec(conn, tx, """
            CREATE TABLE IF NOT EXISTS tags (
                id   INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT    NOT NULL UNIQUE COLLATE NOCASE
            );
            """);

        Exec(conn, tx, """
            CREATE TABLE IF NOT EXISTS prompt_tags (
                prompt_id INTEGER NOT NULL REFERENCES prompts(id) ON DELETE CASCADE,
                tag_id    INTEGER NOT NULL REFERENCES tags(id)    ON DELETE CASCADE,
                PRIMARY KEY (prompt_id, tag_id)
            );
            """);

        Exec(conn, tx, """
            CREATE TABLE IF NOT EXISTS history (
                id         INTEGER PRIMARY KEY AUTOINCREMENT,
                prompt_id  INTEGER REFERENCES prompts(id) ON DELETE SET NULL,
                action     TEXT    NOT NULL,
                detail     TEXT    NOT NULL DEFAULT '',
                created_at TEXT    NOT NULL
            );
            """);

        Exec(conn, tx, """
            CREATE TABLE IF NOT EXISTS settings (
                key        TEXT PRIMARY KEY,
                value      TEXT NOT NULL DEFAULT '',
                updated_at TEXT NOT NULL
            );
            """);

        // Performance: library list sorts by recency; favorites filter is a partial index.
        Exec(conn, tx, "CREATE INDEX IF NOT EXISTS idx_prompts_updated  ON prompts (updated_at DESC);");
        Exec(conn, tx, "CREATE INDEX IF NOT EXISTS idx_prompts_favorite ON prompts (is_favorite) WHERE is_favorite = 1;");
        Exec(conn, tx, "CREATE INDEX IF NOT EXISTS idx_history_created  ON history (created_at DESC);");
        Exec(conn, tx, "CREATE INDEX IF NOT EXISTS idx_history_prompt   ON history (prompt_id);");
        Exec(conn, tx, "CREATE INDEX IF NOT EXISTS idx_prompt_tags_tag  ON prompt_tags (tag_id);");
    }

    private static void Exec(SqliteConnection conn, SqliteTransaction tx, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}
