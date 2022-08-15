using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CampaingController : MonoBehaviour {


    public virtual void SetupBattle() {
    }

    public virtual void UpdateControler() {

    }

    public abstract string GetPathToChapterFolder();
}
