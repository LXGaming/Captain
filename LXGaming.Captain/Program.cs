using System.IO.Compression;
using System.Reflection;
using LXGaming.Captain.Configuration;
using LXGaming.Common.Hosting;
using LXGaming.Common.Serilog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.File.Archive;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.ControlledBy(new EnvironmentLoggingLevelSwitch(LogEventLevel.Verbose, LogEventLevel.Debug))
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine("logs", "app-.log"),
        buffered: true,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 1,
        hooks: new ArchiveHooks(CompressionLevel.Optimal))
    .CreateBootstrapLogger();

Log.Information("Initializing...");

try {
    var configuration = new JsonConfiguration(Directory.GetCurrentDirectory());
    await configuration.LoadConfigurationAsync();

    var builder = Host.CreateDefaultBuilder(args);
    builder.UseSerilog();

    builder.ConfigureServices(services => {
        services.AddSingleton<IConfiguration>(configuration);
        services.AddAllServices(Assembly.GetExecutingAssembly());
    });

    var host = builder.Build();

    await host.RunAsync();
    return 0;
} catch (Exception ex) {
    Log.Fatal(ex, "Application failed to initialize");
    return 1;
} finally {
    Log.CloseAndFlush();
}