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

    public virtual void OnStationAdded(Station station) {

    }

    public virtual void OnStationRemoved(Station station) { 
    
    }

    public virtual void OnShipAdded(Ship ship) { 
    }

    public virtual void OnShipRemoved(Ship ship) { 
    }
}
