using System.Globalization;
using Docker.DotNet;
using Docker.DotNet.Models;
using Humanizer;
using LXGaming.Captain.Configuration;
using LXGaming.Captain.Configuration.Categories.Docker;
using LXGaming.Captain.Models;
using LXGaming.Captain.Services.Docker.Listeners;
using LXGaming.Captain.Triggers;
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
    private readonly SemaphoreSlim _semaphore;
    private readonly IDictionary<string, TriggerBase> _triggers;

    public DockerService(IConfiguration configuration, ILogger<DockerService> logger, IServiceProvider serviceProvider) {
        _configuration = configuration;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _cancellationTokenSource = new CancellationTokenSource();
        _semaphore = new SemaphoreSlim(1, 1);
        _triggers = new Dictionary<string, TriggerBase>();
    }

    public Task StartAsync(CancellationToken cancellationToken) {
        DockerClient = new DockerClientConfiguration().CreateClient();

        DockerClient.System.MonitorEventsAsync(
            new ContainerEventsParameters(),
            new Progress<Message>(OnMessageAsync),
            _cancellationTokenSource.Token);

        return Task.CompletedTask;
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

    public Task UnregisterAsync(string id, string name, IDictionary<string, string> labels) {
        _triggers.Remove(id);

        _logger.LogInformation("Unregistered {Name} ({Id})", name, id.Truncate(12, ""));
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

    public TriggerBase GetOrCreateTrigger(string key) {
        if (_triggers.TryGetValue(key, out var existingTrigger)) {
            return existingTrigger;
        }

        var restartCategory = _configuration.Config?.DockerCategory.RestartCategory;
        var trigger = new SimpleTriggerBuilder()
            .WithThreshold(restartCategory?.Threshold ?? RestartCategory.DefaultThreshold)
            .WithResetAfter(TimeSpan.FromSeconds(restartCategory?.Timeout ?? RestartCategory.DefaultTimeout))
            .Build();

        _triggers.Add(key, trigger);
        return trigger;
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