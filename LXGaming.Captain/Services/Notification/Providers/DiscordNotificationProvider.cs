using Discord;
using Discord.Webhook;
using LXGaming.Captain.Configuration;
using LXGaming.Captain.Services.Docker.Models;
using LXGaming.Captain.Utilities;
using LXGaming.Common.Hosting;
using LXGaming.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LXGaming.Captain.Services.Notification.Providers;

[Service(ServiceLifetime.Singleton, typeof(INotificationProvider))]
public class DiscordNotificationProvider(
    IConfiguration configuration,
    ILogger<DiscordNotificationProvider> logger) : IHostedService, INotificationProvider {

    private readonly IProvider<Config> _config = configuration.GetRequiredProvider<Config>();
    private DiscordWebhookClient? _discordClient;

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

        _discordClient = new DiscordWebhookClient(url);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        _discordClient?.Dispose();
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
        return SendAlertAsync(new[] { embedBuilder.Build() });
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
        return SendAlertAsync(new[] { embedBuilder.Build() });
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
        return SendAlertAsync(new[] { embedBuilder.Build() });
    }

    private Task<ulong> SendAlertAsync(IEnumerable<Embed>? embeds = null) {
        var mentions = _config.Value?.NotificationCategory.Mentions;
        if (mentions == null || mentions.Count == 0) {
            return SendMessageAsync(null, embeds);
        }

        return SendMessageAsync(string.Join(' ', mentions), embeds);
    }

    private Task<ulong> SendMessageAsync(string? text = null, IEnumerable<Embed>? embeds = null) {
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
            username: category.Username,
            avatarUrl: category.AvatarUrl
        );
    }
}