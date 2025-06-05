public class PlaceholderCondition : EventCondition {
    public PlaceholderCondition(object[] args) : base(ConditionType.Placeholder) {
        this.args = args;
    }
    public object[] args { get; private set; }

    public override bool CheckCondition(EventManager eventManager, float deltaTime) {
        return true;
    }
}
