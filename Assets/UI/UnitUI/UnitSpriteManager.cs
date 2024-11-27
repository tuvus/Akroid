using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitSpriteManager : MonoBehaviour {
    public BattleManager battleManager { get; private set; }
    public UIManager uIManager { get; private set; }

    public Dictionary<BattleObject, BattleObjectUI> objects { get; private set; }
    public Dictionary<Unit, UnitUI> units { get; private set; }
    public Dictionary<Fleet, FleetUI> fleetUIs { get; private set; }
    public Dictionary<Faction, FactionUI> factionUIs { get; private set; }

    public void SetupUnitSpriteManager(BattleManager battleManager, UIManager uIManager) {
        this.battleManager = battleManager;
        this.uIManager = uIManager;
        objects = new Dictionary<BattleObject, BattleObjectUI>();
        units = new Dictionary<Unit, UnitUI>();
        fleetUIs = new Dictionary<Fleet, FleetUI>();
        factionUIs = new Dictionary<Faction, FactionUI>();
        battleManager.OnObjectCreated += OnObjectCreated;
        battleManager.OnObjectRemoved += OnObjectRemoved;
        battleManager.OnFleetCreated += OnFleetCreated;
        battleManager.OnFleetRemoved += OnFleetRemove;
    }

    /// <summary>
    /// Updates the state of the sprites
    /// </summary>
    public void UpdateSpriteManager() {
        foreach (var objPair in objects.ToList()) {
            if (objPair.Value == null) {
                BattleObjectUI objectUI = Instantiate(objPair.Key.GetPrefab()).GetComponent<BattleObjectUI>();
                objectUI.Setup(objPair.Key);
                if (objectUI is StarUI) objectUI.transform.SetParent(uIManager.GetStarTransform());
                else if (objectUI is GasCloudUI) objectUI.transform.SetParent(uIManager.GetGasCloudsTransform());
                else if (objectUI is AsteroidUI) objectUI.transform.SetParent(uIManager.GetAsteroidFieldTransform());
                objects[objPair.Key] = objectUI;
                if (objPair.Key.IsUnit()) units[(Unit)objPair.Key] = (UnitUI)objectUI;
            } else {
                objPair.Value.UpdateObject();
            }
        }
    }

    /// <summary>
    /// Registers a sprite for creation, doesn't actually create the object here.
    /// </summary>
    private void OnObjectCreated(BattleObject battleObject) {
        if (battleObject.GetPrefab() == null) return;
        if (battleObject.GetPrefab().GetComponent<BattleObjectUI>() == null) {
            Debug.LogWarning(battleObject.objectName + " had a prefab path but did not have a UI component");
            return;
        }

        objects.Add(battleObject, null);
        if (battleObject.IsUnit()) {
            units.Add((Unit)battleObject, null);
        }
    }

    private void OnObjectRemoved(BattleObject battleObject) {
        Destroy(objects[battleObject]);
        objects.Remove(battleObject);
        if (battleObject.IsUnit()) units.Remove((Unit)battleObject);
    }

    private void OnFleetCreated(Fleet fleet) {
        FleetUI fleetUI = Instantiate((GameObject)Resources.Load("Prefabs/Fleet"),
            uIManager.GetFleetTransform(fleet.faction)).GetComponent<FleetUI>();
        fleetUI.Setup(fleet, this);
        fleetUIs.Add(fleet, fleetUI);
    }

    private void OnFleetRemove(Fleet fleet) {
        Destroy(fleetUIs[fleet]);
        fleetUIs.Remove(fleet);
    }
}
