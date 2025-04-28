using System.Globalization;
using Docker.DotNet;
using Docker.DotNet.Models;
using LXGaming.Captain.Configuration.Categories.Docker;
using LXGaming.Captain.Models;
using LXGaming.Captain.Services.Docker.Listeners;
using LXGaming.Captain.Services.Docker.Models;
using LXGaming.Captain.Services.Docker.Utilities;
using LXGaming.Captain.Services.Notification;
using LXGaming.Captain.Triggers;
using LXGaming.Captain.Triggers.Simple;
using LXGaming.Configuration;
using LXGaming.Configuration.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CaptainConfig = LXGaming.Captain.Configuration.Config;

namespace LXGaming.Captain.Services.Docker;

public class DockerService(
    IConfiguration configuration,
    IDockerClient dockerClient,
    ILogger<DockerService> logger,
    NotificationService notificationService,
    IServiceProvider serviceProvider) : IHostedService, IDisposable {

    private readonly IProvider<CaptainConfig> _config = configuration.GetRequiredProvider<IProvider<CaptainConfig>>();
    private readonly CancellationTokenSource _cancelSource = new();
    private readonly Dictionary<string, Container> _containers = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;

    public async Task StartAsync(CancellationToken cancellationToken) {
        _ = dockerClient.System.MonitorEventsAsync(
            new ContainerEventsParameters(),
            new Progress<Message>(OnMessageAsync),
            _cancelSource.Token).ContinueWith(task => {
            logger.LogError(task.Exception, "Encountered an error while monitoring events");
        }, TaskContinuationOptions.OnlyOnFaulted);

        var parameters = new ContainersListParameters {
            All = true
        };
        foreach (var container in await dockerClient.Containers.ListContainersAsync(parameters, cancellationToken)) {
            await RegisterAsync(container.ID);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        try {
            _cancelSource.Cancel();
        } catch (AggregateException ex) {
            logger.LogError(ex, "Encountered an error while performing cancellation");
        }

        return Task.CompletedTask;
    }

    public async Task RegisterAsync(string id) {
        if (_containers.TryGetValue(id, out var existingContainer)) {
            logger.LogWarning("Container {Name} ({Id}) is already registered", existingContainer.Name, existingContainer.GetShortId());
            return;
        }

        var inspect = await dockerClient.Containers.InspectContainerAsync(id);
        if (!GetLabelValue(inspect.Config.Labels, Labels.Enabled)) {
            return;
        }

        var container = new Container {
            Id = id,
            Name = inspect.GetName(),
            Labels = inspect.Config.Labels,
            Tty = inspect.Config.Tty,
            RestartTrigger = CreateRestartTrigger(inspect.Config.Labels)
        };

        _containers.Add(id, container);
        if (inspect.State.Running) {
            await OnStartAsync(container, DateTimeOffset.UtcNow);
        }

        logger.LogInformation("Registered {Name} ({Id})", container.Name, container.GetShortId());
    }

    public Task UnregisterAsync(string id) {
        if (!_containers.Remove(id, out var existingContainer)) {
            return Task.CompletedTask;
        }

        logger.LogInformation("Unregistered {Name} ({Id})", existingContainer.Name, existingContainer.GetShortId());
        return Task.CompletedTask;
    }

    public Task OnStartAsync(Container container, DateTimeOffset startedAt) {
        var logCategories = _config.Value?.DockerCategory.LogCategories
            .Where(category => category.Names?.Contains(container.Name) == true || (!string.IsNullOrEmpty(category.Label) && container.Labels.ContainsKey(category.Label)))
            .ToList();
        if (logCategories == null || logCategories.Count == 0) {
            return Task.CompletedTask;
        }

        _ = dockerClient.Containers.GetLogsAsync(container.Id, container.Tty, new ContainerLogsParameters {
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

    // ReSharper disable once AsyncVoidMethod
    private async void OnMessageAsync(Message message) {
        await _lock.WaitAsync(_cancelSource.Token);

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
            _lock.Release();
        }
    }

    private TriggerBase CreateRestartTrigger(IDictionary<string,string> labels) {
        var category = _config.Value?.DockerCategory;
        if (category == null) {
            throw new InvalidOperationException("DockerCategory is unavailable");
        }

        var threshold = GetLabelValue(labels, Labels.RestartThreshold, category.RestartCategory.Threshold);
        var timeout = GetLabelValue(labels, Labels.RestartTimeout, category.RestartCategory.Timeout);
        return new SimpleTriggerBuilder()
            .WithThreshold(threshold)
            .WithResetAfter(TimeSpan.FromSeconds(timeout))
            .Build();
    }

    public Container? GetContainer(string key) {
        return _containers.GetValueOrDefault(key);
    }

    public T GetLabelValue<T>(IDictionary<string, string> dictionary, Label<T> label, T? defaultValue = null) where T : class, IConvertible {
        return GetLabelValue(dictionary, label.Id, defaultValue ?? label.DefaultValue);
    }

    public T GetLabelValue<T>(IDictionary<string, string> dictionary, Label<T> label, T? defaultValue = null) where T : struct, IConvertible {
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

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (_disposed) {
            return;
        }

        if (disposing) {
            _cancelSource.Dispose();
            _lock.Dispose();
        }

        _disposed = true;
    }
}