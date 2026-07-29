using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Serilog;

namespace Knarr.App;

sealed class Program
{
    private const int _attachParentProcess = -1;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        AttachParentConsole();
        Log.Logger = CreateLogger();
        try
        {
            Log.Information("Knarr starting up");
            BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Knarr terminated unexpectedly");
        }
        finally
        {
            Log.Information("Knarr shutting down");
            Log.CloseAndFlush();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();

    private static Serilog.ILogger CreateLogger()
    {
        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Knarr",
            "logs");

        return new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.File(
                Path.Combine(logDirectory, "knarr-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug)
            .CreateLogger();
    }

    private static void AttachParentConsole()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        if (AttachConsole(_attachParentProcess))
        {
            StreamWriter stdout = new(Console.OpenStandardOutput()) { AutoFlush = true };
            Console.SetOut(stdout);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AttachConsole(int dwProcessId);
}
