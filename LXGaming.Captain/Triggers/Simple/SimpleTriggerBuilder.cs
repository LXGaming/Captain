namespace LXGaming.Captain.Triggers.Simple; 

public class SimpleTriggerBuilder : TriggerBuilderBase<SimpleTriggerBuilder, SimpleTrigger> {

    public override SimpleTrigger Build() {
        return new SimpleTrigger(Threshold, ResetAfter, FireInterval);
    }
}