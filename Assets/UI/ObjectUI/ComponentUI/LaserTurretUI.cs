public class LaserTurretUI : TurretUI {
    private LaserUI laserUI;
    public LaserTurret laserTurret { get; private set; }

    public override void Setup(BattleObject battleObject, UIManager uIManager, UnitUI unitUI, int componentIndex) {
        base.Setup(battleObject, uIManager, unitUI, componentIndex);
        laserTurret = (LaserTurret)battleObject;
        laserUI = Instantiate(laserTurret.laser.GetPrefab(), transform).GetComponent<LaserUI>();
        laserUI.Setup(laserTurret.laser, uIManager, this);
    }

    public override void RemoveComponent() {
        base.RemoveComponent();
        DestroyImmediate(laserUI.gameObject);
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
