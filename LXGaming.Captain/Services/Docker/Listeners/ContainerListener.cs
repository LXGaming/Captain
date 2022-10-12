using Discord;
using Docker.DotNet.Models;
using LXGaming.Captain.Configuration;
using LXGaming.Captain.Models;
using LXGaming.Captain.Services.Discord;
using LXGaming.Captain.Services.Docker.Utilities;
using LXGaming.Captain.Utilities;
using LXGaming.Common.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LXGaming.Captain.Services.Docker.Listeners;

[Service(ServiceLifetime.Singleton, typeof(IListener))]
public class ContainerListener : IListener {

    private readonly IConfiguration _configuration;
    private readonly DiscordService _discordService;
    private readonly DockerService _dockerService;
    private readonly ILogger<ContainerListener> _logger;

    public string Type => "container";

    public ContainerListener(IConfiguration configuration, DiscordService discordService, DockerService dockerService, ILogger<ContainerListener> logger) {
        _configuration = configuration;
        _discordService = discordService;
        _dockerService = dockerService;
        _logger = logger;
    }

    public Task ExecuteAsync(Message message) {
        return message.Action switch {
            "die" => OnDieAsync(message),
            _ => Task.CompletedTask
        };
    }

    public async Task OnDieAsync(Message message) {
        if (!_dockerService.GetLabelValue(message.Actor.Attributes, Labels.Enabled)) {
            return;
        }

        _logger.LogDebug("[{Type} {Action}] {Name} ({Id})",
            message.Type, message.Action,
            message.Actor.GetName(), message.Actor.GetId());

        var trigger = _dockerService.GetOrCreateTrigger(message.Actor.ID);
        if (!trigger.Execute()) {
            return;
        }

        _logger.LogWarning("Restart Loop Detected: {Name} ({Id})",
            message.Actor.GetName(), message.Actor.GetId());

        if ((_configuration.Config?.DockerCategory.AutomaticStop ?? false) && !_dockerService.GetLabelValue(message.Actor.Attributes, Labels.MonitorOnly)) {
            await _dockerService.DockerClient.Containers.StopContainerAsync(message.Actor.ID, new ContainerStopParameters());
        }

        var embedBuilder = new EmbedBuilder();
        embedBuilder.WithColor(Color.Orange);
        embedBuilder.WithTitle("Restart Loop Detected");
        embedBuilder.AddField("Id", $"```{message.Actor.GetId()}```", true);
        embedBuilder.AddField("Name", $"```{message.Actor.GetName()}```", true);
        embedBuilder.AddField("Exit Code", $"```{message.Actor.GetExitCode()}```", true);
        embedBuilder.WithFooter($"{Constants.Application.Name} v{Constants.Application.Version}");
        await _discordService.SendAlertAsync(embed: embedBuilder.Build());
    }
}