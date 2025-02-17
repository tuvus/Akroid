using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FleetUI : ObjectUI {
    public Fleet fleet { get; private set; }
    [SerializeField] private FleetAI fleetAI;
    private UIBattleManager uiBattleManager;

    public void Setup(Fleet fleet, UIBattleManager uiBattleManager) {
        base.Setup(fleet);
        this.fleet = fleet;
        this.uiBattleManager = uiBattleManager;
        fleetAI = fleet.fleetAI;
    }

    public IEnumerable<ShipUI> GetShipsUI() {
        return fleet.ships.Select(s => (ShipUI)uiBattleManager.units[s]);
    }

    public override void UpdateObject() { }

    public override void SelectObject(UnitSelection.SelectionStrength selectionStrength = UnitSelection.SelectionStrength.Unselected) {
        base.SelectObject(selectionStrength);
        GetShipsUI().ToList().ForEach(s => s.SelectObject(selectionStrength));
    }

    public override void UnselectObject() {
        base.UnselectObject();
        GetShipsUI().ToList().ForEach(s => s.UnselectObject());
    }

    public override bool IsSelectable() {
        return true;
    }
}
