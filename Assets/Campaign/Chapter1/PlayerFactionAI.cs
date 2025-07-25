using System.Collections.Generic;
using System.Linq;

public class PlayerFactionAI : FactionAI {
    private Chapter1 chapter1;
    private FactionCommManager commManager;
    private bool nextState;
    private int nextStationToSendTo;
    private MiningStation playerMiningStation;

    public PlayerFactionAI(BattleManager battleManager, Faction faction) : base(battleManager, faction) {
        nextStationToSendTo = 0;
        commManager = faction.GetFactionCommManager();
        autoResearch = false;
    }

    public void Setup(Chapter1 chapter1, MiningStation playerMiningStation) {
        this.chapter1 = chapter1;
        this.playerMiningStation = playerMiningStation;
    }

    public override void UpdateFactionAI(float deltaTime) {
        ManageIdleShips();
        foreach (Ship ship in playerMiningStation.GetAllDockedShips()) {
            if (ship.IsScienceShip() && !ship.IsDamaged()) {
                ship.moduleSystem.Get<ResearchEquipment>().ForEach(r => faction.AddScience(r.DownloadData()));
            }
        }
    }

    private void ManageIdleShips() {
        foreach (Ship ship in idleShips.Where(s => s.IsIdle() && s.IsTransportShip() && s.fleet == null)) {
            ship.shipAI.AddUnitAICommand(Command.CreateTradeTransportCommand());
        }
    }

    public override void OnShipBuilt(Ship ship) {
        if (ship.IsCombatShip()) {
            battleManager.GetLocalPlayer().AddOwnedUnit(ship);
        }

        ship.shipAI.AddUnitAICommand(Command.CreateDockCommand(playerMiningStation));
    }

    public bool WantMoreTransportShips() {
        if (4 > faction.GetShipCountOfType(Ship.ShipType.Transport) +
            chapter1.shipyardFactionAI.GetOrderCount(Ship.ShipClass.Transport, faction)) {
            return true;
        }
        return false;
    }


    public override Station GetFleetCommand() {
        return playerMiningStation;
    }
}
