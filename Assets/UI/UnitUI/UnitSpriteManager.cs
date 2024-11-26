using System.Collections.Generic;
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

    public void UpdateSpriteManager() {
        foreach (var objPair in objects) {
            objPair.Value.UpdateObject();
        }
    }

    private void OnObjectCreated(BattleObject battleObject) {
        if (battleObject.GetPrefab() == null) return;
        GameObject obj = Instantiate(battleObject.GetPrefab(), uIManager.GetStarTransform());
        objects.Add(battleObject, obj.GetComponent<BattleObjectUI>());
    }

    private void OnObjectRemoved(BattleObject battleObject) {
        Destroy(objects[battleObject]);
        objects.Remove(battleObject);
    }
}
