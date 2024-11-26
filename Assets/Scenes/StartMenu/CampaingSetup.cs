using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms;

public class CampaingSetup : MonoBehaviour {
    [SerializeField] GameObject campaignChapterPanel;

    private void Awake() {
        ShowCampaingChapterPanel(false);
    }

    public void StartCampaingChapter(int chapter) {
        StartCoroutine(ChangeScenes(chapter));
    }

    public void ShowCampaingChapterPanel(bool show) {
        campaignChapterPanel.SetActive(show);
    }

    public IEnumerator ChangeScenes(int chapter) {
        yield return null;
        BattleManager.quickStart = false;
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Loading", LoadSceneMode.Additive);
        asyncLoad.allowSceneActivation = false;
        while (asyncLoad.progress < 0.9f) {
            yield return null;
        }
        DestroyImmediate(Camera.main.gameObject);
        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone) {
            yield return null;
        }
        transform.DetachChildren();
        transform.SetParent(null);
        SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetSceneByName("Loading"));
        SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
        DestroyImmediate(GameObject.Find("Main Camera").GetComponent<AudioListener>());
        asyncLoad = SceneManager.LoadSceneAsync("Battle", LoadSceneMode.Additive);
        asyncLoad.allowSceneActivation = false;
        while (asyncLoad.progress < 0.9f) {
            yield return null;
        }
        DestroyImmediate(Camera.main.gameObject);
        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone) {
            yield return null;
        }
        SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetSceneByName("Battle"));
        SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
        CampaingController campaingController = Instantiate(Resources.Load<GameObject>("Campaign/Chapter" + chapter + "/Chapter" + chapter),
            GameObject.Find("Game").transform).GetComponent<CampaingController>();
        BattleManager battleManager = GameObject.Find("Battle").GetComponent<BattleManager>();
        UIManager uIManager = GameObject.Find("Battle").GetComponent<UIManager>();
        uIManager.PreBattleManagerSetup(battleManager);
        battleManager.SetupBattle(campaingController);
        uIManager.SetupUIManager();
        Destroy(gameObject);
    }
}
