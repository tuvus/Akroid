using TMPro;
using UnityEngine;

public class PlayerGasCloudUI : PlayerUIMenu<GasCloud> {
    [SerializeField] TMP_Text gasCloudName;
    [SerializeField] TMP_Text resourceType;
    [SerializeField] TMP_Text resourceAmount;

    protected override void RefreshMiddlePanel() {
        gasCloudName.text = displayedObject.objectName;
        resourceType.text = "Resource Type: " + displayedObject.gasCloudType.ToString();
        resourceAmount.text = "Resources: " + NumFormatter.ConvertNumber(displayedObject.resources);
    }
}
