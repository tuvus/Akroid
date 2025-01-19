using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour {
    private BattleManager.BattleSettings battleSettings;
    private List<Faction.FactionData> factions;
    private string campaignControllerPath;

    public static void LoadBattle(BattleManager.BattleSettings battleSettings, List<Faction.FactionData> factions) {
        SceneLoader sceneLoader = new GameObject("Loader").AddComponent<SceneLoader>();
        sceneLoader.battleSettings = battleSettings;
        sceneLoader.factions = factions;
        sceneLoader.StartCoroutine(sceneLoader.LoadBattleScene(false));
    }

    public static void LoadBattle(string campaignControllerPath) {
        SceneLoader sceneLoader = new GameObject("Loader").AddComponent<SceneLoader>();
        sceneLoader.campaignControllerPath = campaignControllerPath;
        sceneLoader.StartCoroutine(sceneLoader.LoadBattleScene(true));
    }

    private IEnumerator LoadBattleScene(bool campaign) {
        yield return null;
        GameObject startCamera = GameObject.Find("Main Camera");
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Loading", LoadSceneMode.Additive);
        asyncLoad.allowSceneActivation = false;
        while (asyncLoad.progress < 0.9f) {
            yield return null;
        }

        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone) {
            yield return null;
        }

        // Unfortunatly unity gives no good way to handle multiple audio listeners when loading a new scene.
        // The current solution I have found is to have a deactivated camera object with an audio listener in the loading scene
        // and deactivating the previous camera right before activating the loading camera.
        SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetSceneByName("Loading"));
        GameObject loadingCamera = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(o => o.name == "Loading Camera").First();
        startCamera.SetActive(false);
        loadingCamera.SetActive(true);
        SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());

        // Load the Battle Scene
        asyncLoad = SceneManager.LoadSceneAsync("Battle", LoadSceneMode.Additive);
        asyncLoad.allowSceneActivation = false;
        while (asyncLoad.progress < 0.9f) {
            yield return null;
        }

        DestroyImmediate(loadingCamera);
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
