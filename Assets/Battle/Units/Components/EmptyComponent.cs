using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmptyComponent : ModuleComponent {
    public EmptyComponent(BattleManager battleManager, IModule module, Unit unit,
        ComponentScriptableObject componentScriptableObject) :
        base(battleManager, module, unit, componentScriptableObject) { }

}
