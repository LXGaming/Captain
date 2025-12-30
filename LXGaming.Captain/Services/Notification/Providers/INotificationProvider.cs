using LXGaming.Captain.Services.Docker.Models;

namespace LXGaming.Captain.Services.Notification.Providers;

public interface INotificationProvider {

    Task SendHealthStatusAsync(Container container, bool state);

    Task SendLogAsync(Container container, string message);

    Task SendRestartLoopAsync(Container container, string exitCode);
}