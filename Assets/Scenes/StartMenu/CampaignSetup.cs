using UnityEngine;

public class CampaignSetup : MonoBehaviour {
    private StartMenu startMenu;

    public void SetStartMenu(StartMenu startMenu) {
        this.startMenu = startMenu;
    }

    public void StartCampaignChapter(int chapter) {
        startMenu.buttonSound.Play();
        gameObject.SetActive(true);
        SceneLoader.LoadBattle("Chapter" + chapter);
    }

    public void ShowCampaingChapterPanel(bool show) {
        startMenu.buttonSound.Play();
        gameObject.SetActive(show);
    }
}
