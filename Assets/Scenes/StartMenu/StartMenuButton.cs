using UnityEngine;

public class StartMenuButton : MonoBehaviour {

    public void OnClick() {
        StartMenu.Instance.PlayButtonClickSound();
    }

    public void OnHover() {
        StartMenu.Instance.PlayButtonHoverSound();
    }
}
