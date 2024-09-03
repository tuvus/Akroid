using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Shipyard : Station {

    public override void UpdateUnit(float deltaTime) {
        base.UpdateUnit(deltaTime);
        if (built) moduleSystem.Get<ConstructionBay>().ForEach(c => c.UpdateConstructionBay(deltaTime));
    }

    public ConstructionBay GetConstructionBay() {
        return moduleSystem.Get<ConstructionBay>().FirstOrDefault();
    }
}
