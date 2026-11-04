using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PromptForge.Data;
using PromptForge.UI.ViewModels;
namespace PromptForge.UI;

public partial class App : Application
{
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Library database: %AppData%\PromptForge\promptforge.db (roaming, per user).
        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PromptForge");
        var db = new PromptForgeDb(Path.Combine(dataDir, "promptforge.db"));
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
        _services?.Dispose(); // disposes PromptForgeDb (IDisposable singleton)
        base.OnExit(e);
    }
}
