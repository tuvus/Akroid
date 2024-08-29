using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerGasCloudUI : MonoBehaviour {
    PlayerUI playerUI;

    [SerializeField] GameObject gasCloudStatsUI;

    public GasCloud displayedGasCloud { get; private set; }
    [SerializeField] TMP_Text gasCloudName;
    [SerializeField] TMP_Text resourceType;
    [SerializeField] TMP_Text resourceAmount;

    public void SetupPlayerGasCloudUI(PlayerUI playerUI) {
        this.playerUI = playerUI;
    }

    public void DisplayGasCloud(GasCloud gasCloud) {
        displayedGasCloud = gasCloud;
        gasCloudStatsUI.SetActive(gasCloud != null);
        UpdateGasCloudDisplay();
    }

    public void UpdateGasCloudUI() {
        UpdateGasCloudDisplay();
    }

    public void UpdateGasCloudDisplay() {
        if (gasCloudStatsUI.activeSelf) {
            gasCloudName.text = displayedGasCloud.objectName;
            resourceType.text = "Resource Type: " + displayedGasCloud.gasCloudType.ToString();
            resourceAmount.text = "Resources: " + NumFormatter.ConvertNumber(displayedGasCloud.resources);
        }
    }
}
