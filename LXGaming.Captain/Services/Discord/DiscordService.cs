using Discord;
using Discord.Webhook;
using LXGaming.Captain.Configuration;
using LXGaming.Common.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LXGaming.Captain.Services.Discord;

[Service(ServiceLifetime.Singleton)]
public class DiscordService : IHostedService {

    private readonly IConfiguration _configuration;
    private readonly ILogger<DiscordService> _logger;
    private DiscordWebhookClient? _discordClient;

    public DiscordService(IConfiguration configuration, ILogger<DiscordService> logger) {
        _configuration = configuration;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken) {
        var discordCategory = _configuration.Config?.DiscordCategory;
        if (discordCategory == null) {
            throw new InvalidOperationException("DiscordCategory is unavailable");
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
        var mentions = _configuration.Config?.DiscordCategory.Mentions;
        return SendMessageAsync(mentions != null ? string.Join(' ', mentions) : null, embed);
    }

    public Task SendMessageAsync(string? text = null, Embed? embed = null) {
        var discordCategory = _configuration.Config?.DiscordCategory;
        return _discordClient?.SendMessageAsync(
            text: text,
            embeds: new[] { embed },
            username: discordCategory?.Username,
            avatarUrl: discordCategory?.AvatarUrl) ?? Task.CompletedTask;
    }
}