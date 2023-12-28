namespace LXGaming.Captain.Triggers;

public abstract class TriggerBase(int threshold, TimeSpan? resetAfter, TimeSpan? fireInterval) {

    public int Threshold { get; } = threshold;
    public TimeSpan? ResetAfter { get; } = resetAfter;
    public TimeSpan? FireInterval { get; } = fireInterval;
    public DateTime? LastFiredAt { get; protected set; }

    public abstract bool Execute();

    public virtual void Reset() {
        LastFiredAt = null;
    }

    protected bool CanFire(DateTime now) {
        return LastFiredAt == null || (FireInterval != null && now - LastFiredAt >= FireInterval);
    }
}