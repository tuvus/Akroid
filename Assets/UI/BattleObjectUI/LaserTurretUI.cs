public class LaserTurretUI : TurretUI {
    public LaserTurret laserTurret { get; private set; }
    private LaserUI laserUI;

    public override void Setup(BattleObject battleObject, UIManager uIManager, UnitUI unitUI) {
        base.Setup(battleObject, uIManager, unitUI);
        laserTurret = (LaserTurret)battleObject;
        laserUI = Instantiate(laserTurret.laser.GetPrefab(), transform).GetComponent<LaserUI>();
        laserUI.Setup(laserTurret.laser, uIManager, this);
    }

    public override void UpdateObject() {
        base.UpdateObject();
        laserUI.UpdateObject();
    }

    public override void OnUnitDestroyed() {
        base.OnUnitDestroyed();
        laserUI.OnUnitDestroyed();
    }
}
