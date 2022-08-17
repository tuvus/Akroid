using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationFactionAI : FactionAI {

    public override void SetupFactionAI(Faction faction) {
        base.SetupFactionAI(faction);
    }

    public override void UpdateFactionAI() {
        base.UpdateFactionAI();
        if (faction.GetFleetCommand() != null) { 
            
        }
    }
}
