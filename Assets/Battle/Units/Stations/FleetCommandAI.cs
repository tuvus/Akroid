using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class FleetCommandAI : ShipyardAI {
    public FleetCommandAI(Station station) : base(station) { }

    public override void UpdateAI(float deltaTime) {
        base.UpdateAI(deltaTime);
        UpdateFleetCommand();
    }

    private void UpdateFleetCommand() {
        Profiler.BeginSample("FleetCommandAI");
        if (waitTime <= 0) {
            foreach (var ship in station.GetAllDockedShips()) {
                if (ship.IsScienceShip() && !ship.IsDamaged()) {
                    ship.moduleSystem.Get<ResearchEquipment>().ForEach(r => station.faction.AddScience(r.DownloadData()));
                }
            }
            waitTime += waitSpeed;
        }
        Profiler.EndSample();
    }
}
