﻿using Docker.DotNet.Models;
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
            "destroy" => OnDestroyAsync(message),
            "die" => OnDieAsync(message),
            "health_status" => OnHealthStatusAsync(message, value),
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

    private Task OnHealthStatusAsync(Message message, string? status) {
        if (!_dockerService.GetLabelValue(message.Actor.Attributes, Labels.Enabled)) {
            return Task.CompletedTask;
        }

        _logger.LogDebug("Container Health Status: {Name} ({Id})", message.Actor.GetName(), message.Actor.GetId());

        var healthCategory = _configuration.Config?.DockerCategory.HealthCategory;
        if (string.Equals(status, "healthy") && (healthCategory?.Healthy ?? false)) {
            return _notificationService.NotifyAsync(provider => provider.SendHealthStatusAsync(message.Actor, true));
        }

        if (string.Equals(status, "unhealthy") && (healthCategory?.Unhealthy ?? false)) {
            return _notificationService.NotifyAsync(provider => provider.SendHealthStatusAsync(message.Actor, false));
        }

        return Task.CompletedTask;
    }
}