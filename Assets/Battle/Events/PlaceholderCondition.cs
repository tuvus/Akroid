using System;

public class PlaceholderCondition : EventCondition {
    public object[] args { get; private set; }

    public PlaceholderCondition(object[] args) : base(ConditionType.Placeholder, false) {
        this.args = args;
    }

    public override bool CheckCondition(EventManager eventManager, float deltaTime) {
        return true;
    }
}
