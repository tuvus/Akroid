using System.Linq;
using Unity.Mathematics;

public class ShipyardFactionAI : FactionAI {
    private Chapter1 chapter1;
    private PlanetFactionAI planetFactionAI;
    private Shipyard shipyard;
    // private float transportTime;

    public ShipyardFactionAI(BattleManager battleManager, Faction faction) : base(battleManager, faction) { }

    public void Setup(Chapter1 chapter1, PlanetFactionAI planetFactionAI, Shipyard shipyard) {
        this.chapter1 = chapter1;
        this.planetFactionAI = planetFactionAI;
        this.shipyard = shipyard;
        // transportTime = 0;
        // We need to re-add the Idle ships since we are setting up after creating them
        faction.ships.ToList().ForEach(s => idleShips.Add(s));
        EventChainBuilder purchaseTransportsChain = new EventChainBuilder();
        purchaseTransportsChain.AddCondition(new PredicateCondition(_ =>
            faction.credits > battleManager.GetShipBlueprint(Ship.ShipClass.Transport).shipScriptableObject.cost * 5));
        purchaseTransportsChain.AddAction(() =>
            shipyard.GetConstructionBay().AddConstructionToBeginningQueue(
                new Ship.ShipConstructionBlueprint(faction, battleManager.GetShipBlueprint(Ship.ShipClass.Transport))));
        purchaseTransportsChain.AddCondition(new PredicateCondition(_ =>
            faction.credits > battleManager.GetShipBlueprint(Ship.ShipClass.Transport).shipScriptableObject.cost * 10));
        purchaseTransportsChain.AddAction(() =>
            shipyard.GetConstructionBay().AddConstructionToBeginningQueue(
                new Ship.ShipConstructionBlueprint(faction, battleManager.GetShipBlueprint(Ship.ShipClass.Transport))));
        purchaseTransportsChain.AddCondition(new PredicateCondition(_ =>
            faction.credits > battleManager.GetShipBlueprint(Ship.ShipClass.HeavyTransport).shipScriptableObject.cost *
            5));
        purchaseTransportsChain.AddAction(() =>
            shipyard.GetConstructionBay().AddConstructionToBeginningQueue(
                new Ship.ShipConstructionBlueprint(faction,
                    battleManager.GetShipBlueprint(Ship.ShipClass.HeavyTransport))));
        purchaseTransportsChain.AddCondition(new PredicateCondition(_ =>
            faction.credits > battleManager.GetShipBlueprint(Ship.ShipClass.HeavyTransport).shipScriptableObject.cost *
            10));
        purchaseTransportsChain.AddAction(() =>
            shipyard.GetConstructionBay().AddConstructionToBeginningQueue(
                new Ship.ShipConstructionBlueprint(faction,
                    battleManager.GetShipBlueprint(Ship.ShipClass.HeavyTransport))));

        purchaseTransportsChain.Build(chapter1.eventManager)();
    }

    public override void UpdateFactionAI(float deltaTime) {
        base.UpdateFactionAI(deltaTime);
        UpdateFactionCommunication(deltaTime);
        ManageIdleShips();
        ManageTransportShips(deltaTime);
    }

    private void UpdateFactionCommunication(float deltaTime) {
        for (int i = 0; i < faction.GetFactionCommManager().communicationLog.Count; i++) {
            if (faction.GetFactionCommManager().communicationLog[i].isActive &&
                faction.GetFactionCommManager().communicationLog[i].options.Length > 0)
                faction.GetFactionCommManager().communicationLog[i].ChooseOption(0);
        }
    }

    private void ManageTransportShips(float deltaTime) {
        // transportTime -= deltaTime;
        // if (transportTime > 0) return;
        // foreach (Ship ship in faction.ships.Where(s => s.IsTransportShip())) {
        //     if (ship.dockedStation == shipyard) {
        //         shipyard.LoadCargoFromUnit(300, CargoBay.CargoTypes.Metal, ship);
        //         shipyard.LoadCargoFromUnit(300, CargoBay.CargoTypes.Gas, ship);
        //     } else if (ship.dockedStation == chapter1.tradeStation) {
        //         bool loadedCargo = false;
        //         if (shipyard.GetAllCargoOfType(CargoBay.CargoTypes.Metal, true) < 2400 * 20) {
        //             long cargoToLoad = math.min(300, ship.GetAvailableCargoSpace(CargoBay.CargoTypes.Metal));
        //             if (faction.credits >= cargoToLoad * chapter1.resourceCosts[CargoBay.CargoTypes.Metal]) {
        //                 long cargoLoaded = 300 - ship.LoadCargo(cargoToLoad, CargoBay.CargoTypes.Metal);
        //                 if (cargoLoaded > 0) loadedCargo = true;
        //             }
        //         }
        //         if (shipyard.GetAllCargoOfType(CargoBay.CargoTypes.Gas, true) < 2400 * 20) {
        //             long cargoToLoad = math.min(300, ship.GetAvailableCargoSpace(CargoBay.CargoTypes.Gas));
        //             if (faction.credits >= cargoToLoad * chapter1.resourceCosts[CargoBay.CargoTypes.Gas]) {
        //                 long cargoLoaded = 300 - ship.LoadCargo(cargoToLoad, CargoBay.CargoTypes.Gas);
        //                 if (cargoLoaded > 0) loadedCargo = true;
        //             }
        //         }
        //         if (!loadedCargo && ship.GetAllCargoOfType(CargoBay.CargoTypes.All) > 0) {
        //             ship.UndockShip(shipyard.GetPosition());
        //             ship.shipAI.AddUnitAICommand(
        //                 Command.CreateTransportCommand(chapter1.tradeStation, shipyard, CargoBay.CargoTypes.All, true),
        //                 Command.CommandAction.Replace);
        //         }
        //     }
        // }

        // transportTime += 8;
    }

    private void ManageIdleShips() {
        foreach (Ship ship in idleShips) {
            if (ship.IsTransportShip() && ship.IsIdle()) {
                ship.shipAI.AddUnitAICommand(Command.CreateTradeCommand(shipyard), Command.CommandAction.Replace);
            }
        }
    }

    public int GetOrderCount(Ship.ShipClass shipClass, Faction faction) {
        return shipyard.GetConstructionBay().GetNumberOfShipsOfClassFaction(shipClass, faction);
    }
}
