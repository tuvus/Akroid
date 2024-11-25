using System;

/// <summary>
/// A catch-all event type that allows for a custom condition.
/// The downside is that there is no way to visualize this condition.
/// </summary>
public class PredicateCondition : EventCondition {
    public Predicate<EventManager> predicate { get; private set; }

    public PredicateCondition(Predicate<EventManager> predicate) : base(ConditionType.Predicate) {
        this.predicate = predicate;
    }


    public override bool CheckCondition(EventManager eventManager, float deltaTime) {
        return predicate(eventManager);
    }
}
