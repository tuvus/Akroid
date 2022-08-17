using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FactionAI : MonoBehaviour {
    protected Faction faction;

    public virtual void SetupFactionAI(Faction faction) {
        this.faction = faction;
    }

    public virtual void UpdateFactionAI() {
        faction.UpdateFactionResearch();
    }
}
