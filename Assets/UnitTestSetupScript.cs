using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitTestSetupScript : MonoBehaviour {
    Unit unit;
    public UnitScriptableObject unitScriptableObject;
    public void Start() {
        unit = GetComponent<Unit>();
        unit.SetupUnit("TestAria", BattleManager.Instance.factions[0], new BattleManager.PositionGiver(transform.position), 0, 1, unitScriptableObject);
    }
}
