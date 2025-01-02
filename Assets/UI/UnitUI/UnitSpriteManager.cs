using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitSpriteManager : MonoBehaviour {
    public BattleManager battleManager { get; private set; }
    public UIManager uIManager { get; private set; }


    public Dictionary<IObject, ObjectUI> objects { get; private set; }
    public Dictionary<BattleObject, BattleObjectUI> battleObjects { get; private set; }
    public Dictionary<Unit, UnitUI> units { get; private set; }
    public Dictionary<Fleet, FleetUI> fleetUIs { get; private set; }
    public Dictionary<Faction, FactionUI> factionUIs { get; private set; }
    public HashSet<ObjectUI> objectsToUpdate { get; private set; }
    private HashSet<BattleObject> objectsToCreate;

    public void SetupUnitSpriteManager(BattleManager battleManager, UIManager uIManager) {
        this.battleManager = battleManager;
        this.uIManager = uIManager;
        objects = new Dictionary<IObject, ObjectUI>();
        battleObjects = new Dictionary<BattleObject, BattleObjectUI>();
        units = new Dictionary<Unit, UnitUI>();
        fleetUIs = new Dictionary<Fleet, FleetUI>();
        factionUIs = new Dictionary<Faction, FactionUI>();
        objectsToUpdate = new HashSet<ObjectUI>();
        objectsToCreate = new HashSet<BattleObject>();
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
        CreateNewObjects();
        foreach (var objectUI in objectsToUpdate) {
            objectUI.UpdateObject();
        }
    }

    private void CreateNewObjects() {
        foreach (var iObject in objectsToCreate) {
            BattleObjectUI battleObjectUI = Instantiate(iObject.GetPrefab()).GetComponent<BattleObjectUI>();
            battleObjectUI.Setup(iObject, uIManager);
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

            battleObjects.Add(iObject, battleObjectUI);
            objects.Add(iObject, battleObjectUI);
            if (iObject.IsUnit()) units.Add((Unit)iObject, (UnitUI)battleObjectUI);
        }
        objectsToCreate.Clear();
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
        if (iObject is AsteroidField asteroidField) {
            AsteroidFieldUI asteroidFieldUI =
                Instantiate((GameObject)Resources.Load("Prefabs/AsteroidField"), uIManager.GetAsteroidFieldTransform())
                    .GetComponent<AsteroidFieldUI>();
            asteroidFieldUI.Setup(asteroidField);
            objects.Add(asteroidField, asteroidFieldUI);
            return;
        } else if (iObject is BattleObject battleObject) {
            objectsToCreate.Add(battleObject);
        }

        throw new Exception("Creating an object without the UI!");
    }

    private void OnObjectRemoved(IObject iObject) {
        Destroy(objects[iObject].gameObject);
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

        objectsToCreate.Add(battleObject);
    }

    private void OnOnBattleObjectRemoved(BattleObject battleObject) {
        // If the object is destroyed before the battleObjectUI has been set up lets skip destroying it
        // Many simulation frames might have occured since the object was registered to be created
        if (!battleObjects.ContainsKey(battleObject)) {
            if (!objectsToCreate.Contains(battleObject)) throw new Exception("Trying to remove an object UI that doesn't exist!");
            objectsToCreate.Remove(battleObject);
            return;
        }
        BattleObjectUI battleObjectUI = battleObjects[battleObject];
        battleObjectUI.OnBattleObjectRemoved();
        Destroy(battleObjectUI.gameObject);

        battleObjects.Remove(battleObject);
        objects.Remove(battleObject);
        if (battleObject.IsUnit()) units.Remove((Unit)battleObject);
        if (objectsToUpdate.Contains(battleObjectUI)) objectsToUpdate.Remove(battleObjectUI);
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
        Destroy(fleetUIs[fleet].gameObject);
        fleetUIs.Remove(fleet);
        objects.Remove(fleet);
    }
}
