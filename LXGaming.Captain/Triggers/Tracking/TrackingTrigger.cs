using System.Collections;

namespace LXGaming.Captain.Triggers.Tracking;

public class TrackingTrigger : TriggerBase, IEnumerable<DateTime> {

    private readonly Queue<DateTime> _queue;

    public TrackingTrigger(int threshold, TimeSpan? resetAfter, TimeSpan? fireInterval) : base(threshold, resetAfter, fireInterval) {
        _queue = new Queue<DateTime>();
    }

    public override bool Execute() {
        var now = DateTime.UtcNow;
        if (ResetAfter != null) {
            while (_queue.TryPeek(out var lastUpdatedAt)) {
                if (now - lastUpdatedAt >= ResetAfter) {
                    _queue.Dequeue();
                }
            }
        }

        _queue.Enqueue(now);
        if (_queue.Count < Threshold || !CanFire(now)) {
            return false;
        }

        LastFiredAt = now;
        return true;
    }

    public override void Reset() {
        _queue.Clear();
        base.Reset();
    }

    public IEnumerator<DateTime> GetEnumerator() {
        return _queue.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return _queue.GetEnumerator();
    }
}