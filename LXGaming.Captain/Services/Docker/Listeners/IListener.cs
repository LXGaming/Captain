using Docker.DotNet.Models;

namespace LXGaming.Captain.Services.Docker.Listeners;

public interface IListener {

    string Type { get; }

    Task ExecuteAsync(Message message);
}