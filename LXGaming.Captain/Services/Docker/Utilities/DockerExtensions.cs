using System.Text;
using Docker.DotNet;
using Docker.DotNet.Models;
using LXGaming.Captain.Services.Docker.Models;
using Microsoft.Extensions.DependencyInjection;
using MultiplexedStream = LXGaming.Captain.Services.Docker.Utilities.IO.MultiplexedStream;

namespace LXGaming.Captain.Services.Docker.Utilities;

public static class DockerExtensions {

    public static IServiceCollection AddDockerService(this IServiceCollection services) {
        return services
            .AddSingleton(new DockerClientConfiguration().CreateClient())
            .AddSingleton<IDockerClient>(provider => provider.GetRequiredService<DockerClient>())
            .AddSingleton<DockerService>()
            .AddHostedService(provider => provider.GetRequiredService<DockerService>());
    }

    public static string? GetExitCode(this Actor actor) {
        return actor.GetAttributeValue("exitCode");
    }

    private static string? GetAttributeValue(this Actor actor, string key) {
        return actor.Attributes.TryGetValue(key, out var value) ? value : null;
    }

    public static string GetShortId(this Container container) {
        return container.Id.Length > 12 ? container.Id[..12] : container.Id;
    }

    public static string GetName(this ContainerInspectResponse response) {
        return response.Name.StartsWith('/') ? response.Name[1..] : response.Name;
    }

    public static async Task<MultiplexedStream> GetLogsAsync(this IContainerOperations containers, string id, bool tty,
        ContainerLogsParameters parameters, CancellationToken cancellationToken = default) {
#pragma warning disable CS0618
        var stream = await containers.GetContainerLogsAsync(id, parameters, cancellationToken);
#pragma warning restore CS0618
        return new MultiplexedStream(stream, !tty);
    }

    public static async Task GetLogsAsync(this IContainerOperations containers, string id, bool tty,
        ContainerLogsParameters parameters, Func<string, Task> func, CancellationToken cancellationToken = default) {
        var taskCompletionSource = new TaskCompletionSource<string?>();
        await using var stream = await containers.GetLogsAsync(id, tty, parameters, cancellationToken);
        using var streamReader = new StreamReader(stream, new UTF8Encoding(false));
        await using (cancellationToken.Register(() => taskCompletionSource.TrySetCanceled(cancellationToken))) {
            string? line;
            while ((line = await await Task.WhenAny(streamReader.ReadLineAsync(), taskCompletionSource.Task)) != null) {
                await func(line);
            }
        }
    }

    public static MessageAction ParseAction(this Message message) {
        var split = message.Action.Split(':', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return split.Length switch {
            1 => new MessageAction(split[0], null),
            2 => new MessageAction(split[0], split[1]),
            _ => new MessageAction(message.Action, null)
        };
    }
}