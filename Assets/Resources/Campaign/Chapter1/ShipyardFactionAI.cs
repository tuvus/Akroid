using System.Collections;
using System.Linq;
using Unity.Mathematics;

public class ShipyardFactionAI : FactionAI {
    Chapter1 chapter1;
    PlanetFactionAI planetFactionAI;
    Shipyard shipyard;
    float transportTime;

    public void SetupShipyardFactionAI(BattleManager battleManager, Faction faction, Chapter1 chapter1, PlanetFactionAI planetFactionAI, Shipyard shipyard) {
        base.SetupFactionAI(battleManager, faction);
        this.chapter1 = chapter1;
        this.planetFactionAI = planetFactionAI;
        this.shipyard = shipyard;
        transportTime = 0;
        // We need to re-add the Idle ships since we are seting up after creating them
        faction.ships.ToList().ForEach((s) => idleShips.Add(s));
    }

    public override void UpdateFactionAI(float deltaTime) {
        base.UpdateFactionAI(deltaTime);
        UpdateFactionCommunication(deltaTime);
        ManageIdleShips();
        ManageTransportShips(deltaTime);
    }

    void UpdateFactionCommunication(float deltaTime) {
        for (int i = 0; i < faction.GetFactionCommManager().communicationLog.Count; i++) {
            if (faction.GetFactionCommManager().communicationLog[i].isActive && faction.GetFactionCommManager().communicationLog[i].options.Length > 0)
                faction.GetFactionCommManager().communicationLog[i].ChooseOption(0);
        }
    }

    void ManageTransportShips(float deltaTime) {
        transportTime -= deltaTime;
        if (transportTime > 0) return;
        foreach (var ship in faction.ships.Where(s => s.IsTransportShip())) {
            if (ship.dockedStation == shipyard) {
                shipyard.LoadCargoFromUnit(100, CargoBay.CargoTypes.Metal, ship);
            } else if (ship.dockedStation == chapter1.tradeStation) {
                long cargoToLoad = math.min(100, ship.GetAvailableCargoSpace(CargoBay.CargoTypes.Metal));
                if (faction.credits >= cargoToLoad * chapter1.resourceCosts[CargoBay.CargoTypes.Metal]) {
                    ship.LoadCargo(cargoToLoad, CargoBay.CargoTypes.Metal);
                } else if (ship.GetAllCargoOfType(CargoBay.CargoTypes.Metal) > 0) {
                    ship.UndockShip(shipyard.GetPosition());
                    ship.shipAI.AddUnitAICommand(Command.CreateTransportCommand(chapter1.tradeStation, shipyard, CargoBay.CargoTypes.Metal), Command.CommandAction.Replace);
                }
            }
        }
        transportTime += 8;
    }

    void ManageIdleShips() {
        foreach (var ship in idleShips) {
            if (ship.IsTransportShip()) {
                ship.shipAI.AddUnitAICommand(Command.CreateTransportCommand(chapter1.tradeStation, shipyard, CargoBay.CargoTypes.Metal), Command.CommandAction.Replace);
            }
        }
    }

    public override double GetSellCostOfMetal() {
        return chapter1.resourceCosts[CargoBay.CargoTypes.Metal] * 1.2;
    }

    public int GetOrderCount(Ship.ShipClass shipClass, Faction faction) {
        return shipyard.GetConstructionBay().GetNumberOfShipsOfClassFaction(shipClass, faction);
    }
}