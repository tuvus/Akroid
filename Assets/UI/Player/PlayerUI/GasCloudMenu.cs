using TMPro;
using UnityEngine;

public class GasCloudMenu : PlayerUIMenu<GasCloudUI> {
    [SerializeField] private TMP_Text resourceType;
    [SerializeField] private TMP_Text resourceAmount;

    protected override void RefreshStatusPanel() {
        resourceType.text = "Resource Type: " + displayedObject.gasCloud.gasCloudScriptableObject.type;
        resourceAmount.text = "Resources: " + NumFormatter.ConvertNumber(displayedObject.gasCloud.resources);
    }
}
