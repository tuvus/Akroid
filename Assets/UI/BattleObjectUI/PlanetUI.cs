public class PlanetUI : BattleObjectUI {
    public Planet planet { get; private set; }

    public void Setup(Planet planet) {
        this.planet = planet;
    }
}
