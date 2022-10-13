using LXGaming.Captain.Services.Notification.Providers;
using LXGaming.Common.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LXGaming.Captain.Services.Notification;

[Service(ServiceLifetime.Singleton)]
public class NotificationService {

    private readonly ILogger<NotificationService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public NotificationService(ILogger<NotificationService> logger, IServiceProvider serviceProvider) {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task NotifyAsync(Func<INotificationProvider, Task> func) {
        foreach (var notification in _serviceProvider.GetServices<INotificationProvider>()) {
            try {
                await func(notification);
            } catch (Exception ex) {
                _logger.LogError(ex, "Encountered an error while sending notification");
            }
        }
    }
}