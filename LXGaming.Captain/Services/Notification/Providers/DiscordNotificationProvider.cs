using Discord;
using Discord.Webhook;
using Docker.DotNet.Models;
using LXGaming.Captain.Configuration;
using LXGaming.Captain.Services.Docker.Utilities;
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

    public Task SendRestartLoopAsync(Actor actor) {
        var embedBuilder = new EmbedBuilder();
        embedBuilder.WithColor(Color.Orange);
        embedBuilder.WithTitle("Restart Loop Detected");
        embedBuilder.AddField("Id", $"```{actor.GetId()}```", true);
        embedBuilder.AddField("Name", $"```{actor.GetName()}```", true);
        embedBuilder.AddField("Exit Code", $"```{actor.GetExitCode()}```", true);
        embedBuilder.WithFooter($"{Constants.Application.Name} v{Constants.Application.Version}");
        return SendAlertAsync(embedBuilder.Build());
    }
}