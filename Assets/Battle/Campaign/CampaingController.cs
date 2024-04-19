using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CampaingController : MonoBehaviour {
    public BattleManager battleManager {  get; private set; }
    public float researchModifier;
    public float systemSizeModifier;

    public virtual void SetupBattle(BattleManager battleManager) {
        this.battleManager = battleManager;
    }

    public virtual void UpdateController() {

    }

    public abstract string GetPathToChapterFolder();
}
