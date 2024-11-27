using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FleetUI : MonoBehaviour {
    public Fleet fleet { get; private set; }
    private UnitSpriteManager unitSpriteManager;

    public void Setup(Fleet fleet, UnitSpriteManager unitSpriteManager) {
        this.fleet = fleet;
        this.unitSpriteManager = unitSpriteManager;
    }

    public IEnumerable<ShipUI> GetShipsUI() {
        return fleet.ships.Select(s => (ShipUI)unitSpriteManager.units[s]);
    }
}
