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

        EventChainBuilder upgradeStationChain = new EventChainBuilder();
        upgradeStationChain.AddCondition(
            chapter1.eventManager.CreatePredicateCondition(_ => chapter1.playerMiningStation.IsBuilt()));
        upgradeStationChain.AddCondition(new WaitCondition(1000));
        upgradeStationChain.AddCondition(new PredicateCondition(_ => shipyard.moduleSystem.CanUpgradeSystem(
            shipyard.moduleSystem.moduleToSystem[shipyard.moduleSystem.Get<ConstructionBay>().First()], shipyard)));
        upgradeStationChain.AddAction(() => shipyard.moduleSystem.UpgradeSystem(
            shipyard.moduleSystem.moduleToSystem[shipyard.moduleSystem.Get<ConstructionBay>().First()], shipyard)
        );
        upgradeStationChain.AddCondition(new WaitCondition(2000));
        upgradeStationChain.AddCondition(new PredicateCondition(_ => shipyard.moduleSystem.CanUpgradeSystem(
            shipyard.moduleSystem.moduleToSystem[shipyard.moduleSystem.Get<ConstructionBay>().First()], shipyard)));
        upgradeStationChain.AddAction(() => shipyard.moduleSystem.UpgradeSystem(
            shipyard.moduleSystem.moduleToSystem[shipyard.moduleSystem.Get<ConstructionBay>().First()], shipyard)
        );
        upgradeStationChain.Build(chapter1.eventManager)();
    }

    public override void UpdateFactionAI(float deltaTime) {
        base.UpdateFactionAI(deltaTime);
        UpdateFactionCommunication(deltaTime);
        ManageIdleShips();
    }

    private void UpdateFactionCommunication(float deltaTime) {
        for (int i = 0; i < faction.GetFactionCommManager().communicationLog.Count; i++) {
            if (faction.GetFactionCommManager().communicationLog[i].isActive &&
                faction.GetFactionCommManager().communicationLog[i].options.Length > 0)
                faction.GetFactionCommManager().communicationLog[i].ChooseOption(0);
        }
    }

    private void ManageIdleShips() {
        foreach (Ship ship in idleShips) {
            if (ship.IsTransportShip() && ship.IsIdle()) {
                ship.shipAI.AddUnitAICommand(Command.CreateTradeTransportCommand(shipyard),
                    Command.CommandAction.Replace);
            }
        }
    }

    public int GetOrderCount(Ship.ShipClass shipClass, Faction faction) {
        return shipyard.GetConstructionBay().GetNumberOfShipsOfClassFaction(shipClass, faction);
    }
}
