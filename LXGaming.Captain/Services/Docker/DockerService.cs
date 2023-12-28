using System.Globalization;
using Docker.DotNet;
using Docker.DotNet.Models;
using LXGaming.Captain.Configuration.Categories.Docker;
using LXGaming.Captain.Models;
using LXGaming.Captain.Services.Docker.Listeners;
using LXGaming.Captain.Services.Docker.Models;
using LXGaming.Captain.Services.Docker.Utilities;
using LXGaming.Captain.Services.Notification;
using LXGaming.Captain.Triggers.Simple;
using LXGaming.Common.Hosting;
using LXGaming.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CaptainConfig = LXGaming.Captain.Configuration.Config;

namespace LXGaming.Captain.Services.Docker;

[Service(ServiceLifetime.Singleton)]
public class DockerService(
    IConfiguration configuration,
    ILogger<DockerService> logger,
    NotificationService notificationService,
    IServiceProvider serviceProvider) : IHostedService {

    public DockerClient DockerClient { get; private set; } = null!;

    private readonly IProvider<CaptainConfig> _config = configuration.GetRequiredProvider<CaptainConfig>();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Dictionary<string, Container> _containers = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task StartAsync(CancellationToken cancellationToken) {
        DockerClient = new DockerClientConfiguration().CreateClient();

        _ = DockerClient.System.MonitorEventsAsync(
            new ContainerEventsParameters(),
            new Progress<Message>(OnMessageAsync),
            _cancellationTokenSource.Token).ContinueWith(task => {
            logger.LogError(task.Exception, "Encountered an error while monitoring events");
        }, TaskContinuationOptions.OnlyOnFaulted);

        var parameters = new ContainersListParameters {
            All = true
        };
        foreach (var container in await DockerClient.Containers.ListContainersAsync(parameters, cancellationToken)) {
            await RegisterAsync(container.ID);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        try {
            _cancellationTokenSource.Cancel();
        } catch (AggregateException ex) {
            logger.LogError(ex, "Encountered an error while performing cancellation");
        }

        _cancellationTokenSource.Dispose();
        _semaphore.Dispose();
        DockerClient.Dispose();
        return Task.CompletedTask;
    }

    public async Task RegisterAsync(string id) {
        if (_containers.TryGetValue(id, out var existingContainer)) {
            logger.LogWarning("Container {Name} ({Id}) is already registered", existingContainer.Name, existingContainer.ShortId);
            return;
        }

        var dockerCategory = _config.Value?.DockerCategory;
        if (dockerCategory == null) {
            logger.LogWarning("DockerCategory is unavailable");
            return;
        }

        var inspect = await DockerClient.Containers.InspectContainerAsync(id);
        if (!GetLabelValue(inspect.Config.Labels, Labels.Enabled)) {
            return;
        }

        var container = new ContainerBuilder()
            .WithId(id)
            .WithName(inspect.GetName())
            .WithLabels(inspect.Config.Labels)
            .WithTty(inspect.Config.Tty)
            .WithRestartTrigger(new SimpleTriggerBuilder()
                .WithThreshold(GetLabelValue(inspect.Config.Labels, Labels.RestartThreshold, dockerCategory.RestartCategory.Threshold))
                .WithResetAfter(TimeSpan.FromSeconds(GetLabelValue(inspect.Config.Labels, Labels.RestartTimeout, dockerCategory.RestartCategory.Timeout)))
                .Build())
            .Build();

        _containers.Add(id, container);
        if (inspect.State.Running) {
            await OnStartAsync(container, DateTimeOffset.UtcNow);
        }

        logger.LogInformation("Registered {Name} ({Id})", container.Name, container.ShortId);
    }

    public Task UnregisterAsync(string id) {
        if (!_containers.Remove(id, out var existingContainer)) {
            return Task.CompletedTask;
        }

        logger.LogInformation("Unregistered {Name} ({Id})", existingContainer.Name, existingContainer.ShortId);
        return Task.CompletedTask;
    }

    public Task OnStartAsync(Container container, DateTimeOffset startedAt) {
        var logCategories = _config.Value?.DockerCategory.LogCategories
            .Where(category => (category.Names?.Contains(container.Name) ?? false) || (!string.IsNullOrEmpty(category.Label) && container.Labels.ContainsKey(category.Label)))
            .ToList();
        if (logCategories == null || logCategories.Count == 0) {
            return Task.CompletedTask;
        }

        _ = DockerClient.Containers.GetLogsAsync(container.Id, container.Tty, new ContainerLogsParameters {
            ShowStdout = true,
            ShowStderr = true,
            Since = $"{startedAt.ToUnixTimeSeconds()}",
            Follow = true
        }, message => OnLogAsync(container, logCategories, message)).ContinueWith(task => {
            logger.LogError(task.Exception, "Encountered an error while monitoring logs");
        }, TaskContinuationOptions.OnlyOnFaulted);

        return Task.CompletedTask;
    }

    private async Task OnLogAsync(Container container, List<LogCategory> logCategories, string message) {
        foreach (var logCategory in logCategories) {
            var match = logCategory.Regex?.Match(message);
            if (match is not { Success: true }) {
                continue;
            }

            var result = !string.IsNullOrEmpty(logCategory.Replacement)
                ? match.Result(logCategory.Replacement)
                : message;

            await notificationService.NotifyAsync(provider => provider.SendLogAsync(container, result));
        }
    }

    private async void OnMessageAsync(Message message) {
        await _semaphore.WaitAsync(_cancellationTokenSource.Token);

        try {
            foreach (var listener in serviceProvider.GetServices<IListener>()) {
                try {
                    if (string.Equals(message.Type, listener.Type)) {
                        await listener.ExecuteAsync(message);
                    }
                } catch (Exception ex) {
                    logger.LogError(ex, "Encountered an error while executing {Type}", listener.GetType());
                }
            }
        } finally {
            _semaphore.Release();
        }
    }

    public Container? GetContainer(string key) {
        return _containers.TryGetValue(key, out var value) ? value : null;
    }

    public T GetLabelValue<T>(IDictionary<string, string> dictionary, Label<T> label, T? defaultValue = default) where T : class, IConvertible {
        return GetLabelValue(dictionary, label.Id, defaultValue ?? label.DefaultValue);
    }

    public T GetLabelValue<T>(IDictionary<string, string> dictionary, Label<T> label, T? defaultValue = default) where T : struct, IConvertible {
        return GetLabelValue(dictionary, label.Id, defaultValue ?? label.DefaultValue);
    }

    private T GetLabelValue<T>(IDictionary<string, string> dictionary, string key, T defaultValue) where T : IConvertible {
        foreach (var pair in dictionary) {
            if (!string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            try {
                return (T) Convert.ChangeType(pair.Value, typeof(T), CultureInfo.InvariantCulture);
            } catch (Exception ex) {
                logger.LogWarning(ex, "Encountered an error while converting {Key}={Value} to {Type}", pair.Key, pair.Value, typeof(T));
                return defaultValue;
            }
        }

        return defaultValue;
    }
}