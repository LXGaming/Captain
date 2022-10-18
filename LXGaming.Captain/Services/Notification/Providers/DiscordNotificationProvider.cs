using Discord;
using Discord.Webhook;
using LXGaming.Captain.Configuration;
using LXGaming.Captain.Services.Docker.Models;
using LXGaming.Captain.Utilities;
using LXGaming.Common.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LXGaming.Captain.Services.Notification.Providers;

[Service(ServiceLifetime.Singleton, typeof(INotificationProvider))]
public class DiscordNotificationProvider : IHostedService, INotificationProvider {

    private readonly IConfiguration _configuration;
    private readonly ILogger<DiscordNotificationProvider> _logger;
    private DiscordWebhookClient? _discordClient;

    public DiscordNotificationProvider(IConfiguration configuration, ILogger<DiscordNotificationProvider> logger) {
        _configuration = configuration;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken) {
        var discordCategory = _configuration.Config?.NotificationCategory;
        if (discordCategory == null) {
            throw new InvalidOperationException("NotificationCategory is unavailable");
        }

        if (!discordCategory.Enabled) {
            return Task.CompletedTask;
        }

        var url = discordCategory.Url;
        if (string.IsNullOrEmpty(url)) {
            _logger.LogWarning("Url has not been configured for Discord");
            return Task.CompletedTask;
        }

        _discordClient = new DiscordWebhookClient(url);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        _discordClient?.Dispose();
        return Task.CompletedTask;
    }

    public Task SendAlertAsync(Embed embed) {
        var mentions = _configuration.Config?.NotificationCategory.Mentions;
        return SendMessageAsync(mentions != null ? string.Join(' ', mentions) : null, embed);
    }

    public Task SendMessageAsync(string? text = null, Embed? embed = null) {
        var discordCategory = _configuration.Config?.NotificationCategory;
        return _discordClient?.SendMessageAsync(
            text: text,
            embeds: new[] { embed },
            username: discordCategory?.Username,
            avatarUrl: discordCategory?.AvatarUrl) ?? Task.CompletedTask;
    }

    public Task SendHealthStatusAsync(Container container, bool state) {
        var embedBuilder = new EmbedBuilder();
        embedBuilder.WithColor(state ? Color.Green : Color.Red);
        embedBuilder.WithTitle("Health");
        embedBuilder.AddField("Id", $"```{container.ShortId}```", true);
        embedBuilder.AddField("Name", $"```{container.Name}```", true);
        embedBuilder.AddField("Status", $"```{(state ? "Healthy" : "Unhealthy")}```", true);
        embedBuilder.WithFooter($"{Constants.Application.Name} v{Constants.Application.Version}");
        return SendAlertAsync(embedBuilder.Build());
    }

    public Task SendLogAsync(Container container, string message) {
        var embedBuilder = new EmbedBuilder();
        embedBuilder.WithColor(Color.Orange);
        embedBuilder.WithTitle("Log");
        embedBuilder.AddField("Id", $"```{container.ShortId}```", true);
        embedBuilder.AddField("Name", $"```{container.Name}```", true);
        embedBuilder.AddField("Message", $"```{message}```", true);
        embedBuilder.WithFooter($"{Constants.Application.Name} v{Constants.Application.Version}");
        return SendAlertAsync(embedBuilder.Build());
    }

    public Task SendRestartLoopAsync(Container container, string exitCode) {
        var embedBuilder = new EmbedBuilder();
        embedBuilder.WithColor(Color.Orange);
        embedBuilder.WithTitle("Restart Loop Detected");
        embedBuilder.AddField("Id", $"```{container.ShortId}```", true);
        embedBuilder.AddField("Name", $"```{container.Name}```", true);
        embedBuilder.AddField("Exit Code", $"```{exitCode}```", true);
        embedBuilder.WithFooter($"{Constants.Application.Name} v{Constants.Application.Version}");
        return SendAlertAsync(embedBuilder.Build());
    }
}