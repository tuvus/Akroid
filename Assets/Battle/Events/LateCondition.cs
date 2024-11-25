using System;

/// <summary>
/// Use this method when the object doesn't exist by the time of building the event chain
/// but will be by the time the condition is called.
/// This function will create an EventCondition type that calls the eventConditionFunction to
/// create the actual function once the object exits.
///
/// Sometimes the nessesary objects haven't been created by the time of building the event chain.
/// So we need some way to pass in an object only once it has been created.
/// The solution this function provides is to create a wrapper EventCondition with a function that creates
/// the actual EventCondition the first time it checks for it's condition.
/// Then it will act like the condition it is wrapping until the condition is true.
/// </summary>
public class LateCondition : EventCondition {

    public Func<EventCondition> eventConditionFunction { get; private set; }
    public EventCondition eventCondition { get; private set; }

    public LateCondition(Func<EventCondition> eventConditionFunction) : base(ConditionType.LateCondition, true) {
        this.eventConditionFunction = eventConditionFunction;
    }

    public override bool CheckCondition(EventManager eventManager, float deltaTime) {
        if (eventCondition == null) eventCondition = eventConditionFunction();
        return eventCondition.CheckCondition(eventManager, deltaTime);
    }
}
