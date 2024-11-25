
public class WaitCondition : EventCondition{
    public float timeToWait { get; private set; }

    public WaitCondition(float timeToWait) : base(ConditionType.Wait) {
        this.timeToWait = timeToWait;
    }

    public override bool CheckCondition(EventManager eventManager, float deltaTime) {
        timeToWait -= deltaTime;
        if (timeToWait <= 0)
            return true;
        return false;
    }
}
