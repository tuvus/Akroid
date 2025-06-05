using UnityEngine;

public class CampaignSetup : MonoBehaviour {
    private StartMenu startMenu;

    public GameObject chapter1;

    public void SetStartMenu(StartMenu startMenu) {
        this.startMenu = startMenu;
    }

    public void StartCampaignChapter(int chapter) {
        startMenu.PlayButtonClickSound();
        gameObject.SetActive(true);
        SceneLoader.LoadBattle(chapter1);
    }

    public void ShowCampaingChapterPanel(bool show) {
        startMenu.PlayButtonClickSound();
        gameObject.SetActive(show);
    }
}
