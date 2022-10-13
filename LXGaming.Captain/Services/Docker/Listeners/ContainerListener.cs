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
        return message.Action switch {
            "destroy" => OnDestroyAsync(message),
            "die" => OnDieAsync(message),
            _ => Task.CompletedTask
        };
    }

    private Task OnDestroyAsync(Message message) {
        return _dockerService.UnregisterAsync(message.Actor.ID, message.Actor.GetName() ?? message.Actor.GetId(), message.Actor.Attributes);
    }

    private async Task OnDieAsync(Message message) {
        if (!_dockerService.GetLabelValue(message.Actor.Attributes, Labels.Enabled)) {
            return;
        }

        _logger.LogDebug("Container Die: {Name} ({Id})", message.Actor.GetName(), message.Actor.GetId());

        var trigger = _dockerService.GetOrCreateTrigger(message.Actor.ID);
        if (!trigger.Execute()) {
            return;
        }

        _logger.LogWarning("Restart Loop Detected: {Name} ({Id})", message.Actor.GetName(), message.Actor.GetId());

        var restartCategory = _configuration.Config?.DockerCategory.RestartCategory;
        if ((restartCategory?.AutomaticStop ?? false) && !_dockerService.GetLabelValue(message.Actor.Attributes, Labels.MonitorOnly)) {
            await _dockerService.DockerClient.Containers.StopContainerAsync(message.Actor.ID, new ContainerStopParameters());
        }

        await _notificationService.NotifyAsync(provider => provider.SendRestartLoopAsync(message.Actor));
    }
}