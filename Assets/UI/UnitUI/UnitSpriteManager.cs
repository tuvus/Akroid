using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitSpriteManager : MonoBehaviour {
    public BattleManager battleManager { get; private set; }
    public UIManager uIManager { get; private set; }

    public Dictionary<BattleObject, BattleObjectUI> objects { get; private set; }

    public void SetupUnitSpriteManager(BattleManager battleManager, UIManager uIManager) {
        this.battleManager = battleManager;
        this.uIManager = uIManager;
        objects = new Dictionary<BattleObject, BattleObjectUI>();
        battleManager.objectCreatedEvent += OnObjectCreated;
        battleManager.objectRemovedEvent += OnObjectRemoved;
    }

    /// <summary>
    /// Updates the state of the sprites
    /// </summary>
    public void UpdateSpriteManager() {
        foreach (var objPair in objects.ToList()) {
            if (objPair.Value == null) {
                BattleObjectUI objectUI = Instantiate(objPair.Key.GetPrefab(), uIManager.GetStarTransform()).GetComponent<BattleObjectUI>();
                objectUI.Setup(objPair.Key);
                objects[objPair.Key] = objectUI;
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
    }

    private void OnObjectRemoved(BattleObject battleObject) {
        Destroy(objects[battleObject]);
        objects.Remove(battleObject);
    }
}
