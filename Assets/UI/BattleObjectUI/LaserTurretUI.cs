public class LaserTurretUI : TurretUI {
    private LaserTurret laserTurret;
    private LaserUI laserUI;

    public override void Setup(BattleObject battleObject, UIManager uIManager, UnitUI unitUI) {
        base.Setup(battleObject, uIManager, unitUI);
        laserTurret = (LaserTurret)battleObject;
        laserUI = Instantiate(laserTurret.laser.GetPrefab(), transform).GetComponent<LaserUI>();
        laserUI.Setup(laserTurret.laser, uIManager);
    }

    public override void UpdateObject() {
        base.UpdateObject();
        laserUI.UpdateObject();
    }
}
