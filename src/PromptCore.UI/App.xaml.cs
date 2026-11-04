using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PromptCore.Data;
using PromptCore.UI.ViewModels;
namespace PromptCore.UI;

public partial class App : Application
{
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Library database: %AppData%\PromptCore\promptcore.db (roaming, per user).
        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PromptCore");
        var dbPath = Path.Combine(dataDir, "promptcore.db");

        // One-time migration from the pre-rename location (%AppData%\PromptForge\promptforge.db):
        // copy the old database so existing prompts/history survive the rename to PromptCore.
        var legacyDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PromptForge");
        var legacyDb = Path.Combine(legacyDir, "promptforge.db");
        if (!File.Exists(dbPath) && File.Exists(legacyDb))
        {
            Directory.CreateDirectory(dataDir);
            File.Copy(legacyDb, dbPath, overwrite: false);
        }

        var db = new PromptCoreDb(dbPath);
        db.Migrate();

        var sc = new ServiceCollection();
        sc.AddSingleton(db);
        sc.AddSingleton<PromptRepository>();
        sc.AddSingleton<HistoryRepository>();
        sc.AddSingleton<SettingsRepository>();
        sc.AddSingleton<MainViewModel>();
        sc.AddSingleton<MainWindow>();
        _services = sc.BuildServiceProvider();

        _services.GetRequiredService<MainWindow>().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose(); // disposes PromptCoreDb (IDisposable singleton)
        base.OnExit(e);
    }
}
