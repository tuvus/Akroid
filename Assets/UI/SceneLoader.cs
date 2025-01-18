using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour {
    private BattleManager.BattleSettings battleSettings;
    private List<Faction.FactionData> factions;
    private string campaignControllerPath;

    public void LoadBattle(BattleManager.BattleSettings battleSettings, List<Faction.FactionData> factions) {
        this.battleSettings = battleSettings;
        this.factions = factions;
        StartCoroutine(LoadBattleScene(false));
    }

    public void LoadBattle(string campaignControllerPath) {
        this.campaignControllerPath = campaignControllerPath;
        StartCoroutine(LoadBattleScene(true));
    }

    private IEnumerator LoadBattleScene(bool campaign) {
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
        BattleManager battleManager = GameObject.Find("Battle").GetComponent<BattleManager>();
        UIManager uIManager = GameObject.Find("Battle").GetComponent<UIManager>();
        uIManager.PreBattleManagerSetup(battleManager);
        if (campaign) {
            CampaingController campaingController = Instantiate(Resources.Load<GameObject>(campaignControllerPath),
                GameObject.Find("Game").transform).GetComponent<CampaingController>();
            battleManager.SetupBattle(campaingController);
        } else battleManager.SetupBattle(battleSettings, factions);

        uIManager.SetupUIManager();
        Destroy(gameObject);
    }
}
