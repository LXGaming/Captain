namespace LXGaming.Captain.Triggers.Simple;

public class SimpleTrigger : TriggerBase {

    public int Count { get; protected set; }
    public DateTime? LastUpdatedAt { get; protected set; }

    public SimpleTrigger(int threshold, TimeSpan? resetAfter, TimeSpan? fireInterval) : base(threshold, resetAfter, fireInterval) {
    }

    public override bool Execute() {
        var now = DateTime.UtcNow;
        if (ResetAfter != null && LastUpdatedAt != null && now - LastUpdatedAt >= ResetAfter) {
            Reset();
        }

        Count += 1;
        LastUpdatedAt = now;

        if (Count < Threshold || !CanFire(now)) {
            return false;
        }

        LastFiredAt = now;
        return true;
    }

    public override void Reset() {
        Count = 0;
        LastUpdatedAt = null;
        base.Reset();
    }
}