using TMPro;
using UnityEngine;

public class PlayerStarUI : PlayerUIMenu<StarUI> {
    [SerializeField] TMP_Text starName;

    protected override void RefreshMiddlePanel() {
        starName.text = displayedObject.star.objectName;
    }
}
