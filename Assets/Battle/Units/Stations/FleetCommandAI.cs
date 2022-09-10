using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class FleetCommandAI : ShipyardAI {
    public override void UpdateAI(float deltaTime) {
        base.UpdateAI(deltaTime);
        UpdateFleetCommand();
    }

    private void UpdateFleetCommand() {
        Profiler.BeginSample("FleetCommandAI");
        if (waitTime <= 0) {
            Ship scienceShip = station.GetHanger().GetResearchShip();
            if (scienceShip != null && !scienceShip.IsDammaged()) {
                station.faction.AddScience(scienceShip.GetResearchEquiptment().DownloadData());
            }
            waitTime += waitSpeed;
        }
        Profiler.EndSample();
    }
}
