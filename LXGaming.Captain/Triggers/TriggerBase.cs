namespace LXGaming.Captain.Triggers; 

public abstract class TriggerBase {
    
    public int Threshold { get; }
    public TimeSpan? ResetAfter { get; }
    public TimeSpan? FireInterval { get; }
    public DateTime? LastFiredAt { get; protected set; }

    protected TriggerBase(int threshold, TimeSpan? resetAfter, TimeSpan? fireInterval) {
        Threshold = threshold;
        ResetAfter = resetAfter;
        FireInterval = fireInterval;
    }

    public abstract bool Execute();

    public virtual void Reset() {
        LastFiredAt = null;
    }
    
    protected bool CanFire(DateTime now) {
        return LastFiredAt == null || (FireInterval != null && now - LastFiredAt >= FireInterval);
    }
}