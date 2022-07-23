namespace LXGaming.Captain.Triggers;

public abstract class TriggerBuilderBase<TTriggerBuilder, TTrigger>
    where TTriggerBuilder : TriggerBuilderBase<TTriggerBuilder, TTrigger>
    where TTrigger : TriggerBase {

    public int Threshold { get; set; }
    public TimeSpan? ResetAfter { get; set; }
    public TimeSpan? FireInterval { get; set; }

    public abstract TTrigger Build();

    public TTriggerBuilder WithThreshold(int threshold) {
        Threshold = threshold;
        return (TTriggerBuilder) this;
    }

    public TTriggerBuilder WithResetAfter(TimeSpan? resetAfter) {
        ResetAfter = resetAfter;
        return (TTriggerBuilder) this;
    }

    public TTriggerBuilder WithFireInterval(TimeSpan? fireInterval) {
        FireInterval = fireInterval;
        return (TTriggerBuilder) this;
    }
}