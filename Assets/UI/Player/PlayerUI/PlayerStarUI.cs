using TMPro;
using UnityEngine;

public class PlayerStarUI : PlayerUIMenu<Star> {
    [SerializeField] TMP_Text starName;

    protected override void RefreshMiddlePanel() {
        starName.text = displayedObject.objectName;
    }
}
