using LXGaming.Captain.Configuration.Categories.Docker;

namespace LXGaming.Captain.Models;

public static class Labels {

    private const string Prefix = "io.github.lxgaming.captain";

    #region Generic
    public static Label<bool> Enabled { get; } = new($"{Prefix}.enable", "Enable", true);
    public static Label<bool> MonitorOnly { get; } = new($"{Prefix}.monitor-only", "Monitor Only", false);
    #endregion

    #region Health
    public static Label<bool> HealthHealthy { get; } = new($"{Prefix}.health.healthy", "Health Healthy", true);
    public static Label<bool> HealthUnhealthy { get; } = new($"{Prefix}.health.unhealthy", "Health Unhealthy", true);
    #endregion

    #region Restart
    public static Label<bool> RestartAutomaticStop { get; } = new($"{Prefix}.restart.automatic-stop", "Restart Automatic Stop", RestartCategory.DefaultAutomaticStop);
    public static Label<int> RestartThreshold { get; } = new($"{Prefix}.restart.threshold", "Restart Threshold", RestartCategory.DefaultThreshold);
    public static Label<int> RestartTimeout { get; } = new($"{Prefix}.restart.timeout", "Restart Timeout", RestartCategory.DefaultTimeout);
    #endregion
}