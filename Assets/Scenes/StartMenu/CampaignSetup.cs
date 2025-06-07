using UnityEngine;

public class CampaignSetup : MonoBehaviour {
    public GameObject chapter1;
    private StartMenu startMenu;

    public void SetStartMenu(StartMenu startMenu) {
        this.startMenu = startMenu;
    }

    public void StartCampaignChapter(int chapter) {
        startMenu.PlayButtonClickSound();
        gameObject.SetActive(true);
        SceneLoader.LoadBattle(chapter1);
    }

    public void ShowCampaignChapterPanel(bool show) {
        startMenu.PlayButtonClickSound();
        gameObject.SetActive(show);
    }
}
