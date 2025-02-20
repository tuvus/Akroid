using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The UIBattleManager to UIBattleObjects as the BattleManager is to BattleObjects.
/// It manages any world space UI objects (So everything but the player GUI).
/// Only some objects need to update their state every frame,
/// those objects will subscribe to the objectstoupdate HashSet.
///
/// The UIBattleManager subscribes to the BattleManager's OnObjectCreated and OnObjectRemoved events
/// in order to figure out what UI objects need to be created and removed.
/// Note that the UIBattleManager does not do any major work during these calls.
/// These calls might be made on a different thread, preventing us from accessing the Unity API.
/// Instead it stores the creation and removal events in objectsToCreate and objectsToRemove.
/// An object can be in only one of these sets at a time.
/// The UIBattleManager then goes through these sets during the UIUpdate and creates or removes them.
///
/// Most objects are destroyed when they are removed,
/// however projectiles and missiles are simpily hidden
/// since they will likely be needed again soon.
/// </summary>
public class UIBattleManager : MonoBehaviour {
    public BattleManager battleManager { get; private set; }
    public UIManager uIManager { get; private set; }

    /// <summary> Saves the last state of the BattleManager's time scale so we know when it has changed. </summary>
    private float previousSimulationTime;


    public Dictionary<IObject, ObjectUI> objects { get; private set; }
    public Dictionary<BattleObject, BattleObjectUI> battleObjects { get; private set; }
    public Dictionary<Unit, UnitUI> units { get; private set; }
    public Dictionary<Fleet, FleetUI> fleetUIs { get; private set; }
    public Dictionary<Faction, FactionUI> factionUIs { get; private set; }
    public HashSet<ObjectUI> objectsToUpdate { get; private set; }
    public HashSet<IParticleHolder> particleHolders { get; private set; }
    private HashSet<IObject> objectsToCreate;
    private HashSet<IObject> objectsToRemove;

    public void SetupUnitSpriteManager(BattleManager battleManager, UIManager uIManager) {
        this.battleManager = battleManager;
        this.uIManager = uIManager;
        objects = new Dictionary<IObject, ObjectUI>();
        battleObjects = new Dictionary<BattleObject, BattleObjectUI>();
        units = new Dictionary<Unit, UnitUI>();
        fleetUIs = new Dictionary<Fleet, FleetUI>();
        factionUIs = new Dictionary<Faction, FactionUI>();
        objectsToUpdate = new HashSet<ObjectUI>();
        particleHolders = new HashSet<IParticleHolder>();
        objectsToCreate = new HashSet<IObject>();
        objectsToRemove = new HashSet<IObject>();
        battleManager.OnObjectCreated += OnObjectCreated;
        battleManager.OnObjectRemoved += OnObjectRemoved;
    }

    /// <summary>
    /// Updates the state of the sprites
    /// </summary>
    public void UIUpdate() {
        CreateNewObjects();
        RemoveObjects();
        foreach (var objectUI in objectsToUpdate.ToList()) {
            objectUI.UpdateObject();
        }

        // Check if we need to update the particle speeds
        if (Math.Abs(battleManager.timeScale - previousSimulationTime) > 0.01f) {
            previousSimulationTime = uIManager.GetParticleSpeed();
            foreach (var particleHolder in particleHolders) {
                particleHolder.SetParticleSpeed(previousSimulationTime);
            }
        }
    }

    private void CreateNewObjects() {
        foreach (var iObject in objectsToCreate) {
            if (iObject is AsteroidField asteroidField) {
                AsteroidFieldUI asteroidFieldUI =
                    Instantiate((GameObject)Resources.Load("Prefabs/AsteroidField"), uIManager.GetAsteroidFieldTransform())
                        .GetComponent<AsteroidFieldUI>();
                asteroidFieldUI.Setup(asteroidField);
                objects.Add(asteroidField, asteroidFieldUI);
                continue;
            } else if (iObject is Faction faction) {
                FactionUI factionUI = Instantiate((GameObject)Resources.Load("Prefabs/Faction"),
                    uIManager.GetFactionsTransform()).GetComponent<FactionUI>();
                factionUI.Setup(faction);
                factionUIs.Add(faction, factionUI);
                objects.Add(faction, factionUI);
                continue;
            } else if (iObject is Fleet fleet) {
                FactionUI factionUI = factionUIs[fleet.faction];
                FleetUI fleetUI = Instantiate((GameObject)Resources.Load("Prefabs/Fleet"),
                    factionUI.GetFleetTransform()).GetComponent<FleetUI>();
                fleetUI.Setup(fleet, this);
                fleetUIs.Add(fleet, fleetUI);
                objects.Add(fleet, fleetUI);
                continue;
            }

            if (iObject is not BattleObject)
                throw new Exception("Object does not have a UI representation");

            BattleObject battleObject = (BattleObject)iObject;
            if (battleObject.GetPrefab() == null)
                throw new Exception(battleObject.objectName + " did not have a prefab path");
            if (battleObject.GetPrefab().GetComponent<BattleObjectUI>() == null)
                throw new Exception(battleObject.objectName + " had a prefab path but did not have a UI component");

            // Check if the projectile or missile has already been created in a past life
            // In this case we don't need to instantiate it again
            if ((iObject is Projectile || iObject is Missile) && objects.ContainsKey(iObject)) {
                BattleObjectUI objectUI = battleObjects[(BattleObject)iObject];
                // While the object might have been created in a past life,
                // it could have been re-activated before being removed
                // if this is the case clean up and call remove now before setting it up again
                if (objectsToUpdate.Contains(objectUI)) {
                    objectsToUpdate.Remove(objectUI);
                    objectUI.OnBattleObjectRemoved();
                }
                objectUI.Setup(battleObject, uIManager);
                continue;
            }

            BattleObjectUI battleObjectUI = Instantiate(battleObject.GetPrefab()).GetComponent<BattleObjectUI>();
            battleObjectUI.Setup(battleObject, uIManager);
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

            battleObjects.Add(battleObject, battleObjectUI);
            objects.Add(iObject, battleObjectUI);
            if (battleObject.IsUnit()) units.Add((Unit)iObject, (UnitUI)battleObjectUI);
        }

        objectsToCreate.Clear();
    }

    private void RemoveObjects() {
        foreach (var iObject in objectsToRemove) {
            ObjectUI objectUI = objects[iObject];
            if (objectsToUpdate.Contains(objectUI)) objectsToUpdate.Remove(objectUI);

            if (iObject is BattleObject battleObject) {
                BattleObjectUI battleObjectUI = (BattleObjectUI)objectUI;

                battleObjectUI.OnBattleObjectRemoved();
                if (battleObjectUI is ProjectileUI || battleObjectUI is MissileUI) {
                    // Projectiles and missiles don't get destroyed
                    continue;
                }

                battleObjects.Remove(battleObject);
                if (battleObject.IsUnit()) units.Remove((Unit)battleObject);
            } else if (iObject is Fleet fleet) {
                fleetUIs.Remove(fleet);
            }

            Destroy(objectUI.gameObject);
            objects.Remove(iObject);
        }

        objectsToRemove.Clear();
    }

    /// <summary>
    /// Handles creating all other objects.
    /// We don't want to actually create the objects here since we might be on a different thread.
    /// In general it is best to do UI work after all of the simulation updates together.
    /// </summary>
    private void OnObjectCreated(IObject iObject) {
        objectsToCreate.Add(iObject);
        if (objectsToRemove.Contains(iObject)) objectsToRemove.Remove(iObject);
    }

    private void OnObjectRemoved(IObject iObject) {
        // If the object is destroyed before the objectUI has been set up lets skip destroying it
        // Many simulation frames might have occured since the object was registered to be created
        if (!objects.ContainsKey(iObject)) {
            if (!objectsToCreate.Contains(iObject)) throw new Exception("Trying to remove an object UI that doesn't exist!");
            objectsToCreate.Remove(iObject);
            return;
        }

        objectsToRemove.Add(iObject);
    }
}
