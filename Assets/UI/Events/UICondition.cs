using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public abstract class UICondition : EventCondition {
    protected LocalPlayer localPlayer;
    protected UIBattleManager uiBattleManager;

    public UICondition(LocalPlayer localPlayer, UIBattleManager uiBattleManager, ConditionType conditionType,
        bool visualize = false) : base(conditionType, visualize) {
        this.localPlayer = localPlayer;
        this.uiBattleManager = uiBattleManager;
    }

    public override bool CheckCondition(EventManager eventManager, float deltaTime) {
        // Most UIConditions will check their condition during the UI update and not the battle update
        return false;
    }

    /// <summary>
    ///     Checks the UICondition during the UI frame.
    /// </summary>
    /// <returns>True if the condition is fulfilled and the event should be removed, false otherwise.</returns>
    public abstract bool CheckUICondition(EventManager eventManager);

    /// <summary>
    ///     Decides which objects should be visualised by this event.
    ///     Returns the objects in the list given to avoid garbage collection
    /// </summary>
    public abstract void GetVisualizedObjects(List<ObjectUI> objectsToVisualise, List<Button> buttonsToVisualize);

    /// <summary>
    ///     Helper function to help managing selecting ships
    /// </summary>
    protected void AddShipsToSelect(List<ShipUI> shipsToSelect, List<ObjectUI> objectsToVisualize,
        List<Button> buttonsToVisualize,
        bool includeFleet = true) {
        HashSet<UnitUI> selectedUnits =
            localPlayer.GetLocalPlayerGameInput().GetSelectedUnits().GetAllUnits().ToHashSet();
        if (!includeFleet && localPlayer.GetLocalPlayerGameInput().GetSelectedUnits().fleet != null)
            selectedUnits = new HashSet<UnitUI>();
        UnitMenu unitMenu =
            (UnitMenu)localPlayer.playerUI.playerObjectUI.uIMenus[typeof(UnitUI)];

        foreach (ShipUI shipUI in shipsToSelect) {
            if (selectedUnits.Contains(shipUI)) continue;
            Ship ship = shipUI.ship;

            if (ship.dockedStation != null) {
                StationUI dockedStationUI = (StationUI)uiBattleManager.units[ship.dockedStation];
                if (!objectsToVisualize.Contains(dockedStationUI)) objectsToVisualize.Add(dockedStationUI);

                // If the station panel has been opened highlight the ship button
                if (localPlayer.playerUI.playerObjectUI.gameObject.activeSelf && unitMenu.selectedSystem == null &&
                    unitMenu.hangarUI.unit == ship.dockedStation)
                    buttonsToVisualize.Add(unitMenu.hangarUI.GetButtonOfShip(ship));
            } else {
                objectsToVisualize.Add(shipUI);
            }
        }
    }
}
