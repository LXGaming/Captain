using Docker.DotNet.Models;
using LXGaming.Captain.Configuration;
using LXGaming.Captain.Models;
using LXGaming.Captain.Services.Docker.Utilities;
using LXGaming.Captain.Services.Notification;
using LXGaming.Common.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LXGaming.Captain.Services.Docker.Listeners;

[Service(ServiceLifetime.Singleton, typeof(IListener))]
public class ContainerListener : IListener {

    private readonly IConfiguration _configuration;
    private readonly DockerService _dockerService;
    private readonly ILogger<ContainerListener> _logger;
    private readonly NotificationService _notificationService;

    public string Type => "container";

    public ContainerListener(IConfiguration configuration, DockerService dockerService, ILogger<ContainerListener> logger, NotificationService notificationService) {
        _configuration = configuration;
        _dockerService = dockerService;
        _logger = logger;
        _notificationService = notificationService;
    }

    public Task ExecuteAsync(Message message) {
        var (key, value) = message.ParseAction();
        return key switch {
            "create" => OnCreateAsync(message),
            "destroy" => OnDestroyAsync(message),
            "die" => OnDieAsync(message),
            "health_status" => OnHealthStatusAsync(message, value),
            _ => Task.CompletedTask
        };
    }

    private Task OnCreateAsync(Message message) {
        return _dockerService.RegisterAsync(message.Actor.ID, message.Actor.GetName() ?? message.Actor.GetId(), message.Actor.GetLabels());
    }

    private Task OnDestroyAsync(Message message) {
        return _dockerService.UnregisterAsync(message.Actor.ID, message.Actor.GetName() ?? message.Actor.GetId(), message.Actor.GetLabels());
    }

    private async Task OnDieAsync(Message message) {
        var container = _dockerService.GetContainer(message.Actor.ID);
        if (container == null) {
            return;
        }

        _logger.LogDebug("Container Die: {Name} ({Id})", container.Name, container.ShortId);

        if (!container.RestartTrigger.Execute()) {
            return;
        }

        var restartCategory = _configuration.Config?.DockerCategory.RestartCategory;
        if (_dockerService.GetLabelValue(container.Labels, Labels.RestartAutomaticStop, restartCategory?.AutomaticStop)
            && !_dockerService.GetLabelValue(container.Labels, Labels.MonitorOnly)) {
            await _dockerService.DockerClient.Containers.StopContainerAsync(container.Id, new ContainerStopParameters());
        }

        await _notificationService.NotifyAsync(provider => provider.SendRestartLoopAsync(container, message.Actor.GetExitCode() ?? "Unknown"));
    }

    private Task OnHealthStatusAsync(Message message, string? status) {
        var container = _dockerService.GetContainer(message.Actor.ID);
        if (container == null) {
            return Task.CompletedTask;
        }

        if (!_dockerService.GetLabelValue(container.Labels, Labels.Enabled)) {
            return Task.CompletedTask;
        }

        _logger.LogDebug("Container Health Status: {Name} ({Id})", container.Name, container.ShortId);

        var healthCategory = _configuration.Config?.DockerCategory.HealthCategory;
        if (string.Equals(status, "healthy") && _dockerService.GetLabelValue(container.Labels, Labels.HealthHealthy, healthCategory?.Healthy)) {
            return _notificationService.NotifyAsync(provider => provider.SendHealthStatusAsync(container, true));
        }

        if (string.Equals(status, "unhealthy") && _dockerService.GetLabelValue(container.Labels, Labels.HealthUnhealthy, healthCategory?.Unhealthy)) {
            return _notificationService.NotifyAsync(provider => provider.SendHealthStatusAsync(container, false));
        }

        return Task.CompletedTask;
    }
}