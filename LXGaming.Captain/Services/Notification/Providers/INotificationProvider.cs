using Docker.DotNet.Models;

namespace LXGaming.Captain.Services.Notification.Providers;

public interface INotificationProvider {

    Task SendRestartLoopAsync(Actor actor);
}