public class PlanetUI : BattleObjectUI {
    public Planet planet { get; private set; }
    public override void Setup(BattleObject battleObject, UIManager uIManager) {
        base.Setup(battleObject, uIManager);
        planet = (Planet)battleObject;
    }
}
