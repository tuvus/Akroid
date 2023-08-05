using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CampaingController : MonoBehaviour {
    public float researchModifier;
    public float systemSizeModifier;

    public virtual void SetupBattle() {
    }

    public virtual void UpdateController() {

    }

    public abstract string GetPathToChapterFolder();
}
