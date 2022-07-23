using System.Reflection;
using Ductus.FluentDocker.Executors;

namespace LXGaming.Captain.Utilities;

public static class Toolbox {

    public static async Task ProcessConsoleStreamAsync<T>(ConsoleStream<T> stream, Func<T, Task> func) where T : class {
        T value;
        while (!stream.IsFinished && (value = stream.Read()) != null) {
            await func(value);
        }
    }

    public static string GetAssemblyVersion(Assembly assembly) {
        return (assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? assembly.GetCustomAttribute<AssemblyVersionAttribute>()?.Version
                ?? "null").Split('+', '-')[0];
    }
}