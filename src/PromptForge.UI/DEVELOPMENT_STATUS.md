# DEVELOPMENT_STATUS

## Verified (built and run by the project owner on Windows)
- Core: prompt model, composer, variable engine, line diff, rule-based optimizer
- PromptJson (versioned JSON import/export, TXT/MD export), ModelProfiles (9 editable profiles, Adapt with change list)
- Tests: 16/16 passed (.NET 10.0.14, xUnit 2.9.2)
- UI shell: builds and runs; dark/light switch, EN/AR switch with RTL, 9-module navigation all confirmed working

## Written, not yet built or run
- Dependency injection (Microsoft.Extensions.DependencyInjection) and MVVM (CommunityToolkit.Mvvm)
- Prompt Builder screen: 8 default sections (enable/disable, reorder, edit), add custom section, live preview, optimizer findings, copy, export (JSON/MD/TXT), import JSON with validation errors

## Not started
Free-text editor mode, variable editor and fill dialog, undo/redo, autosave, SQLite/EF Core storage, template library, version history, model profile screen, AI directory, installers

## Known gaps
- Optimizer has no contradiction rule yet
- Section titles and optimizer messages are English only; UI labels are localized
- Custom sections cannot be renamed yet
- Theme/language not persisted; export/import use dialogs directly in the view model (to be moved behind an interface)
- Default WPF control styling

## Next
Build and test the Builder screen, then variables (editor + fill dialog), then storage.
