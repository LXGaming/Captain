using Discord;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Events;
using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Services.Extensions;
using Humanizer;
using LXGaming.Captain.Configuration;
using LXGaming.Captain.Configuration.Categories;
using LXGaming.Captain.Models;
using LXGaming.Captain.Services.Discord;
using LXGaming.Captain.Triggers;
using LXGaming.Captain.Triggers.Simple;
using LXGaming.Captain.Utilities;
using LXGaming.Common.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LXGaming.Captain.Services.Docker;

[Service(ServiceLifetime.Singleton)]
public class DockerService : IHostedService {

    public IHostService HostService { get; private set; } = null!;

    private readonly IConfiguration _configuration;
    private readonly DiscordService _discordService;
    private readonly ILogger<DockerService> _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly IList<Task> _tasks;
    private readonly IDictionary<string, TriggerBase> _triggers;

    public DockerService(IConfiguration configuration, DiscordService discordService, ILogger<DockerService> logger) {
        _configuration = configuration;
        _discordService = discordService;
        _logger = logger;
        _cancellationTokenSource = new CancellationTokenSource();
        _tasks = new List<Task>();
        _triggers = new Dictionary<string, TriggerBase>();
    }

    public Task StartAsync(CancellationToken cancellationToken) {
        HostService = new Builder()
            .UseHost()
            .UseNative()
            .Build();

        _tasks.Add(Task.Factory.StartNew(async () => {
            using var events = HostService.Events(_cancellationTokenSource.Token);
            await Toolbox.ProcessConsoleStreamAsync(events, @event => {
                if (!GetLabelValue(@event.EventActor, Labels.Enabled)) {
                    return Task.CompletedTask;
                }

                return @event.Type switch {
                    EventType.Container => @event.Action switch {
                        EventAction.Die => OnContainerDieEventAsync((ContainerDieEvent) @event),
                        _ => Task.CompletedTask
                    },
                    _ => Task.CompletedTask
                };
            });
        }, TaskCreationOptions.LongRunning));

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken) {
        try {
            _cancellationTokenSource.Cancel();

            foreach (var task in _tasks) {
                await task;
            }
        } catch (AggregateException ex) {
            _logger.LogError(ex, "Encountered an error while performing cancellation");
        }

        _cancellationTokenSource.Dispose();
        HostService.Dispose();
    }

    private T GetLabelValue<T>(EventActor eventActor, Label<T> label) where T : IConvertible {
        foreach (var (key, value) in eventActor.Labels) {
            if (!string.Equals(key, label.Id, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(value)) {
                continue;
            }

            try {
                return Label<T>.FromString(value);
            } catch (Exception ex) {
                _logger.LogWarning(ex, "Encountered an error while converting {Key}={Value} to {Type}", key, value, typeof(T));
                return label.DefaultValue;
            }
        }

        return label.DefaultValue;
    }

    private async Task OnContainerDieEventAsync(ContainerDieEvent @event) {
        _logger.LogDebug("[{Type} {Action}] {Name} ({Id})",
            @event.Type, @event.Action,
            @event.EventActor.Name, @event.EventActor.Id.Truncate(12, ""));

        var trigger = GetOrCreateTrigger(@event.EventActor.Id);
        if (!trigger.Execute()) {
            return;
        }

        _logger.LogWarning("Restart Loop Detected: {Name} ({Id})",
            @event.EventActor.Name, @event.EventActor.Id.Truncate(12, ""));

        if ((_configuration.Config?.DockerCategory.AutomaticStop ?? false) && !GetLabelValue(@event.EventActor, Labels.MonitorOnly)) {
            HostService.GetContainers()
                .SingleOrDefault(model => string.Equals(model.Id, @event.EventActor.Id))?
                .Stop();
        }

        var embedBuilder = new EmbedBuilder();
        embedBuilder.WithColor(Color.Orange);
        embedBuilder.WithTitle("Restart Loop Detected");
        embedBuilder.AddField("Id", $"```{@event.EventActor.Id.Truncate(12, "")}```", true);
        embedBuilder.AddField("Name", $"```{@event.EventActor.Name}```", true);
        embedBuilder.AddField("Exit Code", $"```{@event.EventActor.ExitCode}```", true);
        embedBuilder.WithFooter($"{Constants.Application.Name} v{Constants.Application.Version}");
        await _discordService.SendAlertAsync(embed: embedBuilder.Build());
    }

    private TriggerBase GetOrCreateTrigger(string key) {
        if (_triggers.TryGetValue(key, out var existingTrigger)) {
            return existingTrigger;
        }

        var dockerCategory = _configuration.Config?.DockerCategory;
        var trigger = new SimpleTriggerBuilder()
            .WithThreshold(dockerCategory?.RestartThreshold ?? DockerCategory.DefaultRestartThreshold)
            .WithResetAfter(TimeSpan.FromSeconds(dockerCategory?.RestartTimeout ?? DockerCategory.DefaultRestartTimeout))
            .Build();

        _triggers.Add(key, trigger);
        return trigger;
    }
}