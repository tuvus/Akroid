using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipTestSetupScript : MonoBehaviour {
    Ship ship;
    public ShipScriptableObject shipScriptableObject;
    public void Start() {
        ship = GetComponent<Ship>();
        ship.SetupUnit("TestAria", BattleManager.Instance.factions[0], new BattleManager.PositionGiver(Vector2.zero), 0, 1, shipScriptableObject);
    }
}
