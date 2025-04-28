using LXGaming.Captain.Services.Docker.Models;
using LXGaming.Captain.Services.Docker.Utilities;
using LXGaming.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LXGaming.Captain.Services.Notification.Providers;

[Service(ServiceLifetime.Singleton, typeof(INotificationProvider))]
public class LoggingNotificationProvider(ILogger<LoggingNotificationProvider> logger) : INotificationProvider {

    public Task SendHealthStatusAsync(Container container, bool state) {
        logger.LogWarning("Health Status {Name} ({Id}): {State}", container.Name, container.GetShortId(), state);
        return Task.CompletedTask;
    }

    public Task SendLogAsync(Container container, string message) {
        logger.LogWarning("Log {Name} ({Id}): {Message}", container.Name, container.GetShortId(), message);
        return Task.CompletedTask;
    }

    public Task SendRestartLoopAsync(Container container, string exitCode) {
        logger.LogWarning("Restart Loop {Name} ({Id}): {ExitCode}", container.Name, container.GetShortId(), exitCode);
        return Task.CompletedTask;
    }
}