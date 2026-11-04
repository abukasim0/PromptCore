# PromptCore — Development Status

Last update: this file documents the state after the UI redesign + storage layer (branch `feature/ui-redesign-db`).

## Direction (unchanged)
ONE window: idea -> type -> AI model -> copy. The old Builder/variables screens remain superseded.

## Current state (verified)
- Core: prompt model, composer, variable engine, line diff, rule-based optimizer, PromptJson, ModelProfiles — unchanged, still dependency-free
- QuickPrompt: 8 task-specific prompt types, Arabic/English templates, model-specific layout
- **Data (new)**: `PromptCore.Data` — SQLite (Microsoft.Data.Sqlite) with 5 tables (prompts, tags, prompt_tags, history, settings), versioned migrations (PRAGMA user_version), WAL mode, parameterized queries, cascade deletes
- **UI (redesigned)**: Emerald Night / Emerald Day theme tokens (24 brushes), implicit styles for Button/TextBox/ComboBox/ScrollBar, collapsible library panel (search + favorites filter + copy/load/favorite/delete), persisted theme & language prefs in the settings table
- Tests: **37/37 passed** (25 core + 12 storage); solution builds clean from scratch with zero warnings
- Library location: `%AppData%\PromptCore\promptcore.db` (created and migrated automatically at startup; an old `%AppData%\PromptForge\promptforge.db` is auto-copied once)

## Not started
- History viewer UI (history table is written and queryable)
- Templates beyond the 8 types; installer; FTS5 search index (LIKE search is sufficient at desktop scale)

## Next
- Run the updated UI on Windows and tune spacing
- Optional: show recent history in the library panel footer

## v1.1.0 — PromptCore rename + branding (2026-11-05)

- Renamed app PromptForge -> **PromptCore** (projects, namespaces, solution, window title).
- Added app icon `Assets/app.ico` (7 sizes, embedded via ApplicationIcon + window Icon).
- Added About window with developer info and links (GitHub / Telegram / blog / linktree).
- Data dir moved to `%AppData%\PromptCore\promptcore.db`; legacy `%AppData%\PromptForge`
  database is auto-copied once on first run.
- Exe export verified: single-file publish (win-x64) produces `PromptCore.exe` with
  embedded icon + version metadata; 37/37 tests passing.
