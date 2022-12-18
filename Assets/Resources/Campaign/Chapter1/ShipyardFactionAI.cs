using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class ShipyardFactionAI : FactionAI {
    Chapter1 chapter1;
    PlanetFactionAI planetFactionAI;
    Shipyard shipyard;

    public void SetupShipyardFactionAI(Chapter1 chapter1, PlanetFactionAI planetFactionAI, Shipyard shipyard) {
        this.chapter1 = chapter1;
        this.planetFactionAI = planetFactionAI;
        this.shipyard = shipyard;
    }

    public override void UpdateFactionAI(float deltaTime) {
        if (shipyard.GetHanger().GetCombatShip(0) != null) {
            if (shipyard.GetHanger().GetCombatShip(0).faction.stations.Count > 0) {
                shipyard.GetHanger().GetCombatShip(0).shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Dock, shipyard.GetHanger().GetCombatShip(0).faction.stations[0]), ShipAI.CommandAction.AddToEnd);
            }
        }
    }

    public void PlaceCombatOrder(Faction faction) {
        if (faction.UseCredits((long)(9800 * chapter1.GetMetalCost()) + 10000)) {
            this.faction.AddCredits((long)(9800 * chapter1.GetMetalCost()) + 10000);
            if (planetFactionAI.AddMetalOrder(this.faction, 9800)) {
                shipyard.GetConstructionBay().AddConstructionToQueue(new Ship.ShipBlueprint(faction.factionIndex, Ship.ShipClass.Lancer, "Lancer", 6000,
                    new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<float>() { 9800 }));
            }
        }
    }

    public void PlaceTransportOrder(Faction faction) {
        if (faction.UseCredits((long)(4800 * chapter1.GetMetalCost()) + 5000)) {
            this.faction.AddCredits((long)(4800 * chapter1.GetMetalCost()) + 5000);
            if (planetFactionAI.AddMetalOrder(this.faction, 4800)) {
                shipyard.GetConstructionBay().AddConstructionToQueue(new Ship.ShipBlueprint(faction.factionIndex, Ship.ShipClass.Transport, "Transport", 2000,
                    new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<float>() { 4800 }));
            }
        }
    }

    public int GetOrderCount(Ship.ShipClass shipClass, int factionIndex) {
        return shipyard.GetConstructionBay().GetNumberOfShipsOfClassFaction(shipClass, factionIndex);
    }
}
