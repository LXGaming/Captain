using Docker.DotNet.Models;

namespace LXGaming.Captain.Services.Notification.Providers;

public interface INotificationProvider {

    Task SendHealthStatusAsync(Actor actor, bool state);

    Task SendRestartLoopAsync(Actor actor);
}