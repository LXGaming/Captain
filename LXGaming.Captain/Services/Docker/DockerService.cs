using System.Globalization;
using Docker.DotNet;
using Docker.DotNet.Models;
using LXGaming.Captain.Configuration;
using LXGaming.Captain.Models;
using LXGaming.Captain.Services.Docker.Listeners;
using LXGaming.Captain.Services.Docker.Models;
using LXGaming.Captain.Services.Docker.Utilities;
using LXGaming.Captain.Triggers.Simple;
using LXGaming.Common.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LXGaming.Captain.Services.Docker;

[Service(ServiceLifetime.Singleton)]
public class DockerService : IHostedService {

    public DockerClient DockerClient { get; private set; } = null!;

    private readonly IConfiguration _configuration;
    private readonly ILogger<DockerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly IDictionary<string, Container> _containers;
    private readonly SemaphoreSlim _semaphore;

    public DockerService(IConfiguration configuration, ILogger<DockerService> logger, IServiceProvider serviceProvider) {
        _configuration = configuration;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _cancellationTokenSource = new CancellationTokenSource();
        _containers = new Dictionary<string, Container>();
        _semaphore = new SemaphoreSlim(1, 1);
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
        DockerClient = new DockerClientConfiguration().CreateClient();

        _ = DockerClient.System.MonitorEventsAsync(
            new ContainerEventsParameters(),
            new Progress<Message>(OnMessageAsync),
            _cancellationTokenSource.Token);

        var parameters = new ContainersListParameters {
            All = true
        };
        foreach (var container in await DockerClient.Containers.ListContainersAsync(parameters, cancellationToken)) {
            await RegisterAsync(container.ID, container.GetName() ?? container.GetId(), container.Labels);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        try {
            _cancellationTokenSource.Cancel();
        } catch (AggregateException ex) {
            _logger.LogError(ex, "Encountered an error while performing cancellation");
        }

        _cancellationTokenSource.Dispose();
        DockerClient.Dispose();
        return Task.CompletedTask;
    }

    public Task RegisterAsync(string id, string name, IDictionary<string, string> labels) {
        if (_containers.TryGetValue(id, out var existingContainer)) {
            _logger.LogWarning("Container {Name} ({Id}) is already registered", existingContainer.Name, existingContainer.ShortId);
            return Task.CompletedTask;
        }

        if (!GetLabelValue(labels, Labels.Enabled)) {
            return Task.CompletedTask;
        }

        var restartCategory = _configuration.Config?.DockerCategory.RestartCategory;
        var restartTrigger = new SimpleTriggerBuilder()
            .WithThreshold(GetLabelValue(labels, Labels.RestartThreshold, restartCategory?.Threshold))
            .WithResetAfter(TimeSpan.FromSeconds(GetLabelValue(labels, Labels.RestartTimeout, restartCategory?.Timeout)))
            .Build();

        var container = new Container(id, name, labels, restartTrigger);
        _containers.Add(id, container);

        _logger.LogInformation("Registered {Name} ({Id})", container.Name, container.ShortId);
        return Task.CompletedTask;
    }

    public Task UnregisterAsync(string id, string name, IDictionary<string, string> labels) {
        if (!_containers.Remove(id, out var existingContainer)) {
            return Task.CompletedTask;
        }

        _logger.LogInformation("Unregistered {Name} ({Id})", existingContainer.Name, existingContainer.ShortId);
        return Task.CompletedTask;
    }

    private async void OnMessageAsync(Message message) {
        await _semaphore.WaitAsync(_cancellationTokenSource.Token);

        try {
            foreach (var listener in _serviceProvider.GetServices<IListener>()) {
                try {
                    if (string.Equals(message.Type, listener.Type)) {
                        await listener.ExecuteAsync(message);
                    }
                } catch (Exception ex) {
                    _logger.LogError(ex, "Encountered an error while executing {Type}", listener.GetType());
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
                _logger.LogWarning(ex, "Encountered an error while converting {Key}={Value} to {Type}", pair.Key, pair.Value, typeof(T));
                return defaultValue;
            }
        }

        return defaultValue;
    }
}