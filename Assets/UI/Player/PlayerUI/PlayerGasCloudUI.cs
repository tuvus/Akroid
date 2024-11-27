using TMPro;
using UnityEngine;

public class PlayerGasCloudUI : PlayerUIMenu<GasCloudUI> {
    [SerializeField] TMP_Text gasCloudName;
    [SerializeField] TMP_Text resourceType;
    [SerializeField] TMP_Text resourceAmount;

    protected override void RefreshMiddlePanel() {
        gasCloudName.text = displayedObject.gasCloud.objectName;
        resourceType.text = "Resource Type: " + displayedObject.gasCloud.gasCloudScriptableObject.type.ToString();
        resourceAmount.text = "Resources: " + NumFormatter.ConvertNumber(displayedObject.gasCloud.resources);
    }
}
