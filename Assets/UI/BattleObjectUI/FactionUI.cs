using UnityEngine;

public class FactionUI : ObjectUI {
    public Faction faction { get; private set; }

    public void Setup(Faction faction) {
        base.Setup();
        this.faction = faction;
    }

    public override void UpdateObject() {

    }

    public override bool IsSelectable() {
        return true;
    }
}
