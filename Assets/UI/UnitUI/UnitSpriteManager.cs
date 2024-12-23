using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitSpriteManager : MonoBehaviour {
    public BattleManager battleManager { get; private set; }
    public UIManager uIManager { get; private set; }


    public Dictionary<IObject, ObjectUI> objects { get; private set; }
    public Dictionary<BattleObject, BattleObjectUI> battleObjects { get; private set; }
    public Dictionary<Unit, UnitUI> units { get; private set; }
    public Dictionary<Fleet, FleetUI> fleetUIs { get; private set; }
    public Dictionary<Faction, FactionUI> factionUIs { get; private set; }

    public void SetupUnitSpriteManager(BattleManager battleManager, UIManager uIManager) {
        this.battleManager = battleManager;
        this.uIManager = uIManager;
        objects = new Dictionary<IObject, ObjectUI>();
        battleObjects = new Dictionary<BattleObject, BattleObjectUI>();
        units = new Dictionary<Unit, UnitUI>();
        fleetUIs = new Dictionary<Fleet, FleetUI>();
        factionUIs = new Dictionary<Faction, FactionUI>();
        battleManager.OnFactionCreated += OnFactionCreated;
        battleManager.OnObjectCreated += OnObjectCreated;
        battleManager.OnBattleObjectCreated += OnBattleObjectCreated;
        battleManager.OnBattleObjectRemoved += OnOnBattleObjectRemoved;
        battleManager.OnFleetCreated += OnFleetCreated;
        battleManager.OnFleetRemoved += OnFleetRemove;
    }

    /// <summary>
    /// Updates the state of the sprites
    /// </summary>
    public void UpdateSpriteManager() {
        foreach (var objPair in battleObjects.ToList()) {
            if (objPair.Value == null) {
                BattleObjectUI battleObjectUI = Instantiate(objPair.Key.GetPrefab()).GetComponent<BattleObjectUI>();
                battleObjectUI.Setup(objPair.Key, uIManager);
                if (battleObjectUI is StarUI) battleObjectUI.transform.SetParent(uIManager.GetStarTransform());
                else if (battleObjectUI is PlanetUI) battleObjectUI.transform.SetParent(uIManager.GetPlanetsTransform());
                else if (battleObjectUI is GasCloudUI) battleObjectUI.transform.SetParent(uIManager.GetGasCloudsTransform());
                else if (battleObjectUI is AsteroidUI) battleObjectUI.transform.SetParent(uIManager.GetAsteroidFieldTransform());
                else if (battleObjectUI is ProjectileUI) battleObjectUI.transform.SetParent(uIManager.GetProjectileTransform());
                else if (battleObjectUI is MissileUI) battleObjectUI.transform.SetParent(uIManager.GetMissileTransform());
                else if (battleObjectUI.battleObject.faction != null) {
                    FactionUI factionUI = factionUIs[battleObjectUI.battleObject.faction];
                    if (battleObjectUI is ShipUI) battleObjectUI.transform.SetParent(factionUI.GetShipTransform());
                    else if (battleObjectUI is StationUI) battleObjectUI.transform.SetParent(factionUI.GetStationsTransform());
                }

                battleObjects[objPair.Key] = battleObjectUI;
                objects[objPair.Key] = battleObjectUI;
                if (objPair.Key.IsUnit()) units[(Unit)objPair.Key] = (UnitUI)battleObjectUI;
            } else {
                objPair.Value.UpdateObject();
            }
        }
    }

    private void OnFactionCreated(Faction faction) {
        FactionUI factionUI = Instantiate((GameObject)Resources.Load("Prefabs/Faction"),
            uIManager.GetFactionsTransform()).GetComponent<FactionUI>();
        factionUI.Setup(faction);
        factionUIs.Add(faction, factionUI);
        objects.Add(faction, factionUI);
    }

    /// <summary>
    /// Handles creating all other objects.
    /// </summary>
    private void OnObjectCreated(IObject iObject) {
        ObjectUI objectUI = null;
        if (iObject is AsteroidField asteroidField) {
            AsteroidFieldUI asteroidFieldUI =
                Instantiate((GameObject)Resources.Load("Prefabs/AsteroidField"), uIManager.GetAsteroidFieldTransform())
                    .GetComponent<AsteroidFieldUI>();
            asteroidFieldUI.Setup(asteroidField);
            objects.Add(asteroidField, asteroidFieldUI);
            return;
        }

        throw new Exception("Creating an object without the UI!");
    }

    private void OnObjectRemoved(IObject iObject) {
        Destroy(objects[iObject]);
        objects.Remove(iObject);
    }

    /// <summary>
    /// Registers a sprite for creation, doesn't actually create the object here.
    /// </summary>
    private void OnBattleObjectCreated(BattleObject battleObject) {
        if (battleObject.GetPrefab() == null) return;
        if (battleObject.GetPrefab().GetComponent<BattleObjectUI>() == null) {
            Debug.LogWarning(battleObject.objectName + " had a prefab path but did not have a UI component");
            return;
        }

        battleObjects.Add(battleObject, null);
        objects.Add(battleObject, null);
        if (battleObject.IsUnit()) units.Add((Unit)battleObject, null);
    }

    private void OnOnBattleObjectRemoved(BattleObject battleObject) {
        BattleObjectUI battleObjectUI = battleObjects[battleObject];
        // If the object is destroyed before the battleObjectUI has been set up lets skip destroying it
        // Many simulation frames might have occured since the object was registered to be created
        if (battleObjectUI != null) {
            battleObjectUI.OnBattleObjectRemoved();
            Destroy(battleObjectUI.gameObject);
        }

        battleObjects.Remove(battleObject);
        objects.Remove(battleObject);
        if (battleObject.IsUnit()) units.Remove((Unit)battleObject);
    }

    private void OnFleetCreated(Fleet fleet) {
        FactionUI factionUI = factionUIs[fleet.faction];
        FleetUI fleetUI = Instantiate((GameObject)Resources.Load("Prefabs/Fleet"),
            factionUI.GetFleetTransform()).GetComponent<FleetUI>();
        fleetUI.Setup(fleet, this);
        fleetUIs.Add(fleet, fleetUI);
        objects.Add(fleet, fleetUI);
    }

    private void OnFleetRemove(Fleet fleet) {
        Destroy(fleetUIs[fleet]);
        fleetUIs.Remove(fleet);
        objects.Remove(fleet);
    }
}
