public class StationUI : UnitUI {
    public Station station { get; private set; }

    public override void Setup(BattleObject battleObject) {
        base.Setup(battleObject);
        this.station = (Station)battleObject;
    }
}
