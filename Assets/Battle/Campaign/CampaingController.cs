using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CampaingController : MonoBehaviour {

    public SetupController setupController;

    public void SetupBattle() {
        setupController.Setup(this);
    }

    public void UpdateControler() {

    }

    public string GetPathToChapterFolder() {
        return "Campaign/Chapter1";
    }
}
