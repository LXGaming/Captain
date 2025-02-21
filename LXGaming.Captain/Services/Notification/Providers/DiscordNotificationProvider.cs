using Discord;
using Discord.Webhook;
using LXGaming.Captain.Configuration;
using LXGaming.Captain.Services.Docker.Models;
using LXGaming.Captain.Utilities;
using LXGaming.Configuration;
using LXGaming.Configuration.Generic;
using LXGaming.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LXGaming.Captain.Services.Notification.Providers;

[Service(ServiceLifetime.Singleton, typeof(INotificationProvider))]
public class DiscordNotificationProvider(
    IConfiguration configuration,
    ILogger<DiscordNotificationProvider> logger) : IHostedService, INotificationProvider, IDisposable {

    private readonly IProvider<Config> _config = configuration.GetRequiredProvider<IProvider<Config>>();
    private DiscordWebhookClient? _discordClient;
    private bool _disposed;

    public Task StartAsync(CancellationToken cancellationToken) {
        var category = _config.Value?.NotificationCategory;
        if (category == null) {
            throw new InvalidOperationException("NotificationCategory is unavailable");
        }

        if (!category.Enabled) {
            return Task.CompletedTask;
        }

        var url = category.Url;
        if (string.IsNullOrEmpty(url)) {
            logger.LogWarning("Url has not been configured for Discord");
            return Task.CompletedTask;
        }

        try {
            _discordClient = new DiscordWebhookClient(url);
        } catch (Exception ex) {
            logger.LogError(ex, "Encountered an error while initialising DiscordWebhookClient");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    public Task SendHealthStatusAsync(Container container, bool state) {
        if (_discordClient == null) {
            return Task.CompletedTask;
        }

        var embedBuilder = new EmbedBuilder();
        embedBuilder.WithColor(state ? Color.Green : Color.Red);
        embedBuilder.WithTitle("Health");
        embedBuilder.AddField("Id", $"```\n{container.ShortId}\n```", true);
        embedBuilder.AddField("Name", $"```\n{container.Name}\n```", true);
        embedBuilder.AddField("Status", $"```\n{(state ? "Healthy" : "Unhealthy")}\n```", true);
        embedBuilder.WithFooter($"{Constants.Application.Name} v{Constants.Application.Version}");
        return SendAlertAsync([embedBuilder.Build()]);
    }

    public Task SendLogAsync(Container container, string message) {
        if (_discordClient == null) {
            return Task.CompletedTask;
        }

        var embedBuilder = new EmbedBuilder();
        embedBuilder.WithColor(Color.Orange);
        embedBuilder.WithTitle("Log");
        embedBuilder.AddField("Id", $"```\n{container.ShortId}\n```", true);
        embedBuilder.AddField("Name", $"```\n{container.Name}\n```", true);
        embedBuilder.AddField("Message", $"```\n{message}\n```", true);
        embedBuilder.WithFooter($"{Constants.Application.Name} v{Constants.Application.Version}");
        return SendAlertAsync([embedBuilder.Build()]);
    }

    public Task SendRestartLoopAsync(Container container, string exitCode) {
        if (_discordClient == null) {
            return Task.CompletedTask;
        }

        var embedBuilder = new EmbedBuilder();
        embedBuilder.WithColor(Color.Orange);
        embedBuilder.WithTitle("Restart Loop Detected");
        embedBuilder.AddField("Id", $"```\n{container.ShortId}\n```", true);
        embedBuilder.AddField("Name", $"```\n{container.Name}\n```", true);
        embedBuilder.AddField("Exit Code", $"```\n{exitCode}\n```", true);
        embedBuilder.WithFooter($"{Constants.Application.Name} v{Constants.Application.Version}");
        return SendAlertAsync([embedBuilder.Build()]);
    }

    private Task<ulong> SendAlertAsync(IEnumerable<Embed>? embeds = null) {
        var mentions = _config.Value?.NotificationCategory.Mentions;
        if (mentions == null || mentions.Count == 0) {
            return SendMessageAsync(null, embeds);
        }

        var text = string.Join(' ', mentions);
        if (string.IsNullOrWhiteSpace(text)) {
            return SendMessageAsync(null, embeds);
        }

        return SendMessageAsync(text, embeds);
    }

    private Task<ulong> SendMessageAsync(string? text = null, IEnumerable<Embed>? embeds = null) {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var category = _config.Value?.NotificationCategory;
        if (category == null) {
            throw new InvalidOperationException("NotificationCategory is unavailable");
        }

        if (_discordClient == null) {
            throw new InvalidOperationException("DiscordClient is unavailable");
        }

        return _discordClient.SendMessageAsync(
            text: text,
            embeds: embeds,
            username: !string.IsNullOrEmpty(category.Username) ? category.Username : null,
            avatarUrl: !string.IsNullOrEmpty(category.AvatarUrl) ? category.AvatarUrl : null
        );
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
            _discordClient?.Dispose();
        }

        _disposed = true;
    }
}