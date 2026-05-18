using Avalonia;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Novalist.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Avalonia.Controls.WebView on Linux ships only a GtkX11 adapter, so
        // GTK must initialise against X11 (XWayland on Wayland sessions).
        // Without this, the WebView throws "Unable to initialize GTK" on every
        // Wayland-default distro. Avalonia's own backend ignores GDK_BACKEND.
        if (OperatingSystem.IsLinux()
            && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")))
        {
            Environment.SetEnvironmentVariable("GDK_BACKEND", "x11");
        }

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            LogCrash("AppDomain.UnhandledException", e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            LogCrash("TaskScheduler.UnobservedTaskException", e.Exception);
            e.SetObserved();
        };

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            LogCrash("Main", ex);
            throw;
        }
    }

    internal static void LogCrash(string source, Exception? ex)
    {
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Novalist");
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, "crash.log");
            File.AppendAllText(logPath, $"{DateTime.UtcNow:O} [{source}]\n{ex}\n\n");
        }
        catch { /* best effort */ }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .WithDeveloperTools(options =>
            {
//#if DEBUG
                //options.ConnectOnStartup = true;
//#endif
            });
}
