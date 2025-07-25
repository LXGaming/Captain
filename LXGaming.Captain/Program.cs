using System.IO.Compression;
using System.Text.Json;
using LXGaming.Captain.Configuration;
using LXGaming.Captain.Services.Docker.Utilities;
using LXGaming.Common.Serilog;
using LXGaming.Configuration.File.Json;
using LXGaming.Configuration.Hosting;
using LXGaming.Hosting.Generated;
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

Log.Information("Initialising...");

try {
    var configuration = await JsonFileConfiguration<Config>.LoadAsync(
        options: new JsonSerializerOptions {
            WriteIndented = true
        }
    );

    var builder = Host.CreateDefaultBuilder(args);
    builder.UseConfiguration(configuration);
    builder.UseSerilog();

    builder.ConfigureServices(services => {
        services.AddDockerService();
        services.AddAllServices();
    });

    var host = builder.Build();

    await host.RunAsync();
    return 0;
} catch (Exception ex) {
    Log.Fatal(ex, "Application failed to initialise");
    return 1;
} finally {
    await Log.CloseAndFlushAsync();
}