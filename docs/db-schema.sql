-- PromptCore library schema — reference DDL (as applied by DbMigrations.MigrationV1)
-- Managed automatically via PRAGMA user_version; do NOT run by hand on a real database.

PRAGMA journal_mode = WAL;
PRAGMA foreign_keys = ON;

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

CREATE TABLE IF NOT EXISTS tags (
    id   INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT    NOT NULL UNIQUE COLLATE NOCASE
);

CREATE TABLE IF NOT EXISTS prompt_tags (
    prompt_id INTEGER NOT NULL REFERENCES prompts(id) ON DELETE CASCADE,
    tag_id    INTEGER NOT NULL REFERENCES tags(id)    ON DELETE CASCADE,
    PRIMARY KEY (prompt_id, tag_id)
);

CREATE TABLE IF NOT EXISTS history (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    prompt_id  INTEGER REFERENCES prompts(id) ON DELETE SET NULL,
    action     TEXT    NOT NULL,
    detail     TEXT    NOT NULL DEFAULT '',
    created_at TEXT    NOT NULL
);

CREATE TABLE IF NOT EXISTS settings (
    key        TEXT PRIMARY KEY,
    value      TEXT NOT NULL DEFAULT '',
    updated_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_prompts_updated  ON prompts (updated_at DESC);
CREATE INDEX IF NOT EXISTS idx_prompts_favorite ON prompts (is_favorite) WHERE is_favorite = 1;
CREATE INDEX IF NOT EXISTS idx_history_created  ON history (created_at DESC);
CREATE INDEX IF NOT EXISTS idx_history_prompt   ON history (prompt_id);
CREATE INDEX IF NOT EXISTS idx_prompt_tags_tag  ON prompt_tags (tag_id);
