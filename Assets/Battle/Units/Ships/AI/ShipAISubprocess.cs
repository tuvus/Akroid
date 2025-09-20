using System.Linq;
using System.Numerics;
using JetBrains.Annotations;
using Unity.Mathematics;
using Vector2 = UnityEngine.Vector2;

public abstract class ShipAISubprocess {
    protected Ship ship;
    protected ShipAI shipAI;

    public ShipAISubprocess(Ship ship, ShipAI shipAI) {
        this.ship = ship;
        this.shipAI = shipAI;
    }

    public abstract void Update();

    public virtual bool OverrideSetDockTarget(Station targetStation) {
        return false;
    }

    public virtual bool OverrideSetMovePosition(Vector2 targetPosition) {
        return false;
    }

    public virtual bool OverrideSetMaxSpeed(float maxSpeed) {
        return false;
    }

    public virtual Vector2? OverrideGetTargetPosition() {
        return null;
    }
}

public class CrewSubprocess : ShipAISubprocess {
    private bool hasEnoughPersonnel = false;
    private Vector2? storedMovePosition;
    private float? storedMaxSpeed;
    [CanBeNull] private Station storedTargetStation;

    public CrewSubprocess(Ship ship, ShipAI shipAI) : base(ship, shipAI) { }

    public override void Update() {
        if (hasEnoughPersonnel) return;

        Population crewNeeded = ship.GetCrewNeeded();
        if (crewNeeded.TotalPopulation() != 0) {
            // Hire crew
            if (ship.dockedStation == null) {
                // Fly to the closest station with a habitat
                if (ship.shipAction == Ship.ShipAction.Dock || ship.shipAction == Ship.ShipAction.DockRotate ||
                    ship.shipAction == Ship.ShipAction.DockMove)
                    return;
                Station newTargetStation = ship.faction.stations.Where(s =>
                    s.moduleSystem.Get<HabitationArea>().Any(h => h.IsTransferHabitat())).Aggregate((a, b) =>
                    math.distancesq(ship.position, a.position) <= math.distancesq(ship.position, b.position) ? a : b);
                if (newTargetStation != null) ship.SetDockTarget(newTargetStation);
                return;
            }

            ship.dockedStation.personnelRequests.Where(pr => pr.Key.unit == ship).ToList()
                .ForEach(pr => crewNeeded.SubtractPopulation(pr.Value));

            if (crewNeeded.TotalPopulation() == 0) return;

            // Request population
            ship.moduleSystem.Get<Bridge>().ForEach(b => {
                long freeSpace = b.GetCapacity() - b.population.TotalPopulation();
                if (ship.dockedStation.personnelRequests.ContainsKey(b))
                    freeSpace -= ship.dockedStation.personnelRequests[b].TotalPopulation();
                Population popToRequest = new Population();
                HabitationArea.allOccupations.ForEach(p => {
                    long toRequest = math.min(freeSpace, crewNeeded.Get(p));
                    popToRequest.Add(p, toRequest);
                    freeSpace -= toRequest;
                });
                if (popToRequest.TotalPopulation() == 0) return;
                ship.dockedStation.RequestPersonnel(b, popToRequest);
            });

            return;
        }

        hasEnoughPersonnel = true;

        if (storedTargetStation != null) ship.SetDockTarget(storedTargetStation);
        if (storedMovePosition != null) ship.SetMovePosition(storedMovePosition.Value);
        if (storedMaxSpeed != null) ship.SetMaxSpeed(storedMaxSpeed.Value);

        storedTargetStation = null;
        storedMovePosition = null;
        storedMaxSpeed = null;
    }

    public override bool OverrideSetDockTarget(Station targetStation) {
        if (hasEnoughPersonnel) return false;
        storedTargetStation = targetStation;
        storedMovePosition = null;
        return true;
    }

    public override bool OverrideSetMovePosition(Vector2 targetPosition) {
        if (hasEnoughPersonnel) return false;
        storedTargetStation = null;
        storedMovePosition = targetPosition;
        return true;
    }

    public override bool OverrideSetMaxSpeed(float maxSpeed) {
        if (hasEnoughPersonnel) return false;
        storedMaxSpeed = maxSpeed;
        return true;
    }

    public override Vector2? OverrideGetTargetPosition() {
        if (hasEnoughPersonnel) return null;
        if (storedTargetStation != null) return storedTargetStation.GetPosition();
        return storedMovePosition;
    }
}
