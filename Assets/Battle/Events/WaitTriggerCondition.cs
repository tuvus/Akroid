using System;


/**
 * Waits until the completer action is called somewhere else.
 */
public class WaitTriggerCondition : EventCondition {
    public bool completed { get; private set; }
    public Action completer { get; private set; }

    public WaitTriggerCondition() : base(ConditionType.WaitTrigger, false) {
        completed = false;
        this.completer = () => { completed = true; };
    }

    public override bool CheckCondition(EventManager eventManager, float deltaTime) {
        return completed;
    }
}
