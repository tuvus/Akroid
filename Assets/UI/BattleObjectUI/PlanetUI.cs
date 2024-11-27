using UnityEngine;

public class PlanetUI : ObjectUI {
    public Planet planet { get; private set; }

    public void Setup(Planet planet) {
        this.planet = planet;
    }


    public override void UpdateObject() {
    }

    public override bool IsSelectable() {
        return true;
    }
}
