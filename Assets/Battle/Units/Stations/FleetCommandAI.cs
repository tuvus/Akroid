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
            for (int i = 0; i < station.GetHangar().ships.Count; i++) {
                Ship ship = station.GetHangar().ships[i];
                if (ship.IsScienceShip() && !ship.IsDamaged()) {
                    station.faction.AddScience(ship.GetResearchEquiptment().DownloadData());
                }
            }
            waitTime += waitSpeed;
        }
        Profiler.EndSample();
    }
}
