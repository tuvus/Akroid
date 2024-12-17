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
        battleManager.OnFactionCreated += OnFactionCreated;
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
                objectUI.Setup(objPair.Key, uIManager);
                if (objectUI is StarUI) objectUI.transform.SetParent(uIManager.GetStarTransform());
                else if (objectUI is PlanetUI) objectUI.transform.SetParent(uIManager.GetPlanetsTransform());
                else if (objectUI is GasCloudUI) objectUI.transform.SetParent(uIManager.GetGasCloudsTransform());
                else if (objectUI is AsteroidUI) objectUI.transform.SetParent(uIManager.GetAsteroidFieldTransform());
                else if (objectUI is ProjectileUI) objectUI.transform.SetParent(uIManager.GetProjectileTransform());
                else if (objectUI is MissileUI) objectUI.transform.SetParent(uIManager.GetMissileTransform());
                else if (objectUI.battleObject.faction != null) {
                    FactionUI factionUI = factionUIs[objectUI.battleObject.faction];
                    if (objectUI is ShipUI) objectUI.transform.SetParent(factionUI.GetShipTransform());
                    else if (objectUI is StationUI) objectUI.transform.SetParent(factionUI.GetStationsTransform());
                }

                objects[objPair.Key] = objectUI;
                if (objPair.Key.IsUnit()) units[(Unit)objPair.Key] = (UnitUI)objectUI;
            } else {
                objPair.Value.UpdateObject();
            }
        }
    }

    private void OnFactionCreated(Faction faction) {
        FactionUI factionUI = Instantiate((GameObject)Resources.Load("Prefabs/Faction"),
            uIManager.GetFactionsTransform()).GetComponent<FactionUI>();
        factionUIs.Add(faction, factionUI);
        factionUI.Setup(faction);
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
        BattleObjectUI battleObjectUI = objects[battleObject];
        // If the object is destroyed before the battleObjectUI has been set up lets skip destroying it
        // Many simulation frames might have occured since the object was registered to be created
        if (battleObjectUI != null) {
            battleObjectUI.OnBattleObjectRemoved();
            Destroy(battleObjectUI.gameObject);

        }
        objects.Remove(battleObject);
        if (battleObject.IsUnit()) units.Remove((Unit)battleObject);
    }

    private void OnFleetCreated(Fleet fleet) {
        FactionUI factionUI = factionUIs[fleet.faction];
        FleetUI fleetUI = Instantiate((GameObject)Resources.Load("Prefabs/Fleet"),
            factionUI.GetFleetTransform()).GetComponent<FleetUI>();
        fleetUI.Setup(fleet, this);
        fleetUIs.Add(fleet, fleetUI);
    }

    private void OnFleetRemove(Fleet fleet) {
        Destroy(fleetUIs[fleet]);
        fleetUIs.Remove(fleet);
    }
}
