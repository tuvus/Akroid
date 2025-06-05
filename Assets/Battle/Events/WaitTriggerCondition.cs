using System;

/**
 * Waits until the completer action is called somewhere else.
 */
public class WaitTriggerCondition : EventCondition {
    public WaitTriggerCondition() : base(ConditionType.WaitTrigger) {
        completed = false;
        completer = () => { completed = true; };
    }
    public bool completed { get; private set; }
    public Action completer { get; private set; }

    public override bool CheckCondition(EventManager eventManager, float deltaTime) {
        return completed;
    }
}
