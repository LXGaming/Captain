using LXGaming.Captain.Services.Notification.Providers;
using LXGaming.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LXGaming.Captain.Services.Notification;

[Service(ServiceLifetime.Singleton)]
public class NotificationService(ILogger<NotificationService> logger, IServiceProvider serviceProvider) {

    public async Task NotifyAsync(Func<INotificationProvider, Task> func) {
        foreach (var notificationProvider in serviceProvider.GetServices<INotificationProvider>()) {
            try {
                await func(notificationProvider);
            } catch (Exception ex) {
                logger.LogError(ex, "Encountered an error while notifying {Name}", notificationProvider.GetType().Name);
            }
        }
    }
}