using System.Reflection;
using Ductus.FluentDocker.Executors;

namespace LXGaming.Captain.Utilities; 

public static class Toolbox {

    public static async Task ProcessConsoleStream<T>(ConsoleStream<T> stream, Func<T, Task> func) where T : class {
        T value;
        while (!stream.IsFinished && (value = stream.Read()) != null) {
            await func(value);
        }
    }
    
    public static string GetAssembly(string assemblyString, string? packageName = null) {
        return GetAssembly(Assembly.Load(assemblyString), packageName ?? assemblyString);
    }

    public static string GetAssembly(Assembly assembly, string? packageName = null) {
        return $"{packageName ?? GetAssemblyName(assembly) ?? "null"} v{GetAssemblyVersion(assembly)}";
    }

    public static string? GetAssemblyName(Assembly assembly) {
        return assembly.GetName().Name;
    }

    public static string GetAssemblyVersion(Assembly assembly) {
        return (assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? assembly.GetCustomAttribute<AssemblyVersionAttribute>()?.Version
                ?? "null").Split('+', '-')[0];
    }
}