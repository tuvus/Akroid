using System.Linq;

public class VictoryCondition : EventCondition {
    private readonly BattleManager battleManager;

    public VictoryCondition(BattleManager battleManager) : base(ConditionType.Victory) {
        this.battleManager = battleManager;
    }

    public override bool CheckCondition(EventManager eventManager, float deltaTime) {
        return battleManager.factions.ToList().Any(f => f.units.Count > 0 && !f.HasEnemy());
    }
}
