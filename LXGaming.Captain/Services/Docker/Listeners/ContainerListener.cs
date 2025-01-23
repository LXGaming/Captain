using Docker.DotNet;
using Docker.DotNet.Models;
using LXGaming.Captain.Models;
using LXGaming.Captain.Services.Docker.Utilities;
using LXGaming.Captain.Services.Notification;
using LXGaming.Configuration;
using LXGaming.Configuration.Generic;
using LXGaming.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CaptainConfig = LXGaming.Captain.Configuration.Config;

namespace LXGaming.Captain.Services.Docker.Listeners;

[Service(ServiceLifetime.Singleton, typeof(IListener))]
public class ContainerListener(
    IConfiguration configuration,
    IDockerClient dockerClient,
    DockerService dockerService,
    ILogger<ContainerListener> logger,
    NotificationService notificationService) : IListener {

    public string Type => "container";

    private readonly IProvider<CaptainConfig> _config = configuration.GetRequiredProvider<IProvider<CaptainConfig>>();

    public Task ExecuteAsync(Message message) {
        var (key, value) = message.ParseAction();
        return key switch {
            "create" => OnCreateAsync(message),
            "destroy" => OnDestroyAsync(message),
            "die" => OnDieAsync(message),
            "health_status" => OnHealthStatusAsync(message, value),
            "start" => OnStartAsync(message),
            _ => Task.CompletedTask
        };
    }

    private Task OnCreateAsync(Message message) {
        return dockerService.RegisterAsync(message.Actor.ID);
    }

    private Task OnDestroyAsync(Message message) {
        return dockerService.UnregisterAsync(message.Actor.ID);
    }

    private async Task OnDieAsync(Message message) {
        var container = dockerService.GetContainer(message.Actor.ID);
        if (container == null) {
            return;
        }

        logger.LogDebug("Container Die: {Name} ({Id})", container.Name, container.ShortId);

        if (!container.RestartTrigger.Execute()) {
            return;
        }

        var restartCategory = _config.Value?.DockerCategory.RestartCategory;
        if (dockerService.GetLabelValue(container.Labels, Labels.RestartAutomaticStop, restartCategory?.AutomaticStop)
            && !dockerService.GetLabelValue(container.Labels, Labels.MonitorOnly)) {
            await dockerClient.Containers.StopContainerAsync(container.Id, new ContainerStopParameters());
        }

        await notificationService.NotifyAsync(provider => provider.SendRestartLoopAsync(container, message.Actor.GetExitCode() ?? "Unknown"));
    }

    private Task OnHealthStatusAsync(Message message, string? status) {
        var container = dockerService.GetContainer(message.Actor.ID);
        if (container == null) {
            return Task.CompletedTask;
        }

        logger.LogDebug("Container Health Status: {Name} ({Id})", container.Name, container.ShortId);

        var healthCategory = _config.Value?.DockerCategory.HealthCategory;
        if (string.Equals(status, "healthy") && dockerService.GetLabelValue(container.Labels, Labels.HealthHealthy, healthCategory?.Healthy)) {
            return notificationService.NotifyAsync(provider => provider.SendHealthStatusAsync(container, true));
        }

        if (string.Equals(status, "unhealthy") && dockerService.GetLabelValue(container.Labels, Labels.HealthUnhealthy, healthCategory?.Unhealthy)) {
            return notificationService.NotifyAsync(provider => provider.SendHealthStatusAsync(container, false));
        }

        return Task.CompletedTask;
    }

    private Task OnStartAsync(Message message) {
        var container = dockerService.GetContainer(message.Actor.ID);
        if (container == null) {
            return Task.CompletedTask;
        }

        logger.LogDebug("Container Start: {Name} ({Id})", container.Name, container.ShortId);

        return dockerService.OnStartAsync(container, DateTimeOffset.FromUnixTimeSeconds(message.Time));
    }
}