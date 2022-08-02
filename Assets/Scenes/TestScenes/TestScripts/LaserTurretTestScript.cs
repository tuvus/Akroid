using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserTurretTestScript : LaserTurret {
    public Unit target;
    private void Start() {
        //target.SetupUnit("Target", null, Vector2.zero, 0);
        //transform.parent.GetComponent<Unit>().SetupUnit("ship", null, Vector2.zero, 0);
        SetupTurret(transform.parent.GetComponent<Unit>());
        //targetUnit = target;
    }

    void Update() {
        //targetUnit = target;
        UpdateTurret();
    }
}
