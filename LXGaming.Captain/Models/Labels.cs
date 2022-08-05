namespace LXGaming.Captain.Models;

public static class Labels {

    private const string Prefix = "io.github.lxgaming.captain";

    public static Label<bool> Enabled { get; } = new($"{Prefix}.enable", "Enable", true);
    public static Label<bool> MonitorOnly { get; } = new($"{Prefix}.monitor-only", "Monitor Only", false);
}