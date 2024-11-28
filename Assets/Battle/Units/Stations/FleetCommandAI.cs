
public class FleetCommandAI : ShipyardAI {
    public FleetCommandAI(Station station) : base(station) { }

    public override void UpdateAI(float deltaTime) {
        base.UpdateAI(deltaTime);
        UpdateFleetCommand();
    }

    private void UpdateFleetCommand() { }
}
