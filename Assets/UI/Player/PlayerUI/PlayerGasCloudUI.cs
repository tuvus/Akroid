using TMPro;
using UnityEngine;

public class PlayerGasCloudUI : PlayerUIMenu<GasCloudUI> {
    [SerializeField] private TMP_Text gasCloudName;
    [SerializeField] private TMP_Text resourceType;
    [SerializeField] private TMP_Text resourceAmount;

    protected override void RefreshMiddlePanel() {
        gasCloudName.text = displayedObject.gasCloud.objectName;
        resourceType.text = "Resource Type: " + displayedObject.gasCloud.gasCloudScriptableObject.type;
        resourceAmount.text = "Resources: " + NumFormatter.ConvertNumber(displayedObject.gasCloud.resources);
    }
}
