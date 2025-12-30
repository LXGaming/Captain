namespace LXGaming.Captain.Triggers.Tracking;

public class TrackingTriggerBuilder : TriggerBuilderBase<TrackingTriggerBuilder, TrackingTrigger> {

    public override TrackingTrigger Build() {
        return new TrackingTrigger(Threshold, ResetAfter, FireInterval);
    }
}