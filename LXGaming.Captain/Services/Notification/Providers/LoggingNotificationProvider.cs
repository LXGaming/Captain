using LXGaming.Captain.Services.Docker.Models;
using LXGaming.Common.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LXGaming.Captain.Services.Notification.Providers;

[Service(ServiceLifetime.Singleton, typeof(INotificationProvider))]
public class LoggingNotificationProvider : INotificationProvider {

    private readonly ILogger<LoggingNotificationProvider> _logger;

    public LoggingNotificationProvider(ILogger<LoggingNotificationProvider> logger) {
        _logger = logger;
    }

    public Task SendHealthStatusAsync(Container container, bool state) {
        _logger.LogWarning("Health Status {Name} ({Id}): {State}", container.Name, container.ShortId, state);
        return Task.CompletedTask;
    }

    public Task SendLogAsync(Container container, string message) {
        _logger.LogWarning("Log {Name} ({Id}): {Message}", container.Name, container.ShortId, message);
        return Task.CompletedTask;
    }

    public Task SendRestartLoopAsync(Container container, string exitCode) {
        _logger.LogWarning("Restart Loop {Name} ({Id}): {ExitCode}", container.Name, container.ShortId, exitCode);
        return Task.CompletedTask;
    }
}