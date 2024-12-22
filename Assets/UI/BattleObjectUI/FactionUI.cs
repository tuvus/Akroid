using UnityEngine;

public class FactionUI : ObjectUI {
    public Faction faction { get; private set; }

    public void Setup(Faction faction) {
        base.Setup(faction);
        this.faction = faction;
    }

    public override void UpdateObject() { }

    public override bool IsSelectable() {
        return true;
    }

    public Transform GetShipTransform() {
        return transform.GetChild(0);
    }
    public Transform GetStationsTransform() {
        return transform.GetChild(1);
    }
    public Transform GetFleetTransform() {
        return transform.GetChild(2);
    }
}
