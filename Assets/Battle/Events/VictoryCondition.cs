using System.Linq;

public class VictoryCondition : EventCondition {
    private BattleManager battleManager;

    public VictoryCondition(BattleManager battleManager) : base(ConditionType.Victory, false) {
        this.battleManager = battleManager;
    }

    public override bool CheckCondition(EventManager eventManager, float deltaTime) {
        return battleManager.factions.ToList().Any(f => f.units.Count > 0 && !f.HasEnemy());
    }
}
