using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    /// <summary>
    /// Loads the battle scene asyncronusly with either the campaing or the randomized setup.
    /// Since setting up the battle can take a while we load the loading scene first and display a progress bar.
    /// We do most of the expensive setup on a seperate thread so that it doesn't block the UI and make the program unresponsive.
    /// Calls to unity's api, for example Resources.Load can only be done on the main thread so we can't put the campaing loading
    /// on a different thread without restructuring it.
    /// </summary>
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
            .First(o => o.name == "LoadingCamera");
        startCamera.SetActive(false);
        loadingCamera.SetActive(true);
        SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());

        TMP_Text statusText = FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .First(t => t.gameObject.name == "StatusText");
        Slider loadingBar = FindObjectsByType<Slider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).First();
        float totalProgress = 20 + 5;
        if (!campaign)
            totalProgress += factions.Sum(f => f.stations + f.ships) +
                battleSettings.asteroidFieldCount + battleSettings.starCount + battleSettings.gasCloudCount;
        else totalProgress += 5;
        statusText.SetText("Loading Scene...");
        // Load the Battle Scene
        asyncLoad = SceneManager.LoadSceneAsync("Battle", LoadSceneMode.Additive);
        asyncLoad.allowSceneActivation = false;
        while (asyncLoad.progress < 0.9f) {
            loadingBar.value = asyncLoad.progress * 20 / totalProgress;
            yield return null;
        }

        statusText.SetText("Activating Scene...");

        loadingBar.value = 20 / totalProgress;
        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone) {
            yield return null;
        }

        SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetSceneByName("Battle"));
        Transform gameTransform = GameObject.Find("Game").transform;
        BattleManager battleManager = gameTransform.GetChild(0).GetComponent<BattleManager>();
        UIManager uIManager = gameTransform.GetChild(0).GetComponent<UIManager>();
        uIManager.PreBattleManagerSetup(battleManager);
        battleManager.InitializeBattle();
        if (campaign) {
            CampaingController campaingController = Instantiate(Resources.Load<GameObject>(campaignControllerPath), gameTransform)
                .GetComponent<CampaingController>();
            loadingBar.value = 25 / totalProgress;
            statusText.SetText("Loading Campaing...");
            yield return null;
            // Campaing loading must be done syncronously
            // This can change in the future if we force it to load resources first
            battleManager.SetupBattle(campaingController);
        } else {
            Task.Run(() => battleManager.SetupBattle(battleSettings, factions));
        }

        statusText.SetText("Setting Up...");

        while (battleManager.battleState != BattleManager.BattleState.Setup) {
            // We must not be loading the campaign here
            loadingBar.value = (25 + battleManager.ships.Count + battleManager.stations.Count + battleManager.asteroidFields.Count
                + battleManager.gasClouds.Count + battleManager.stars.Count) / totalProgress;
            if (battleManager.stars.Count != battleSettings.starCount) {
                statusText.SetText("Creating Stars...");
            } else if (battleManager.asteroidFields.Count != battleSettings.asteroidFieldCount) {
                statusText.SetText("Creating Asteroid Fields...");
            } else if (battleManager.factions.Count != factions.Count) {
                statusText.SetText("Creating Factions...");
            } else if (battleManager.gasClouds.Count != battleSettings.gasCloudCount) {
                statusText.SetText("Creating Gas Clouds...");
            } else {
                statusText.SetText("Finishing Setup...");
            }

            yield return null;
        }

        SpriteRenderer shield = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(o => o.name == "Shield");
        shield.gameObject.SetActive(true);
        statusText.SetText("Done!");
        loadingBar.value = 1;
        // We need to wait one more frame to show the shield on the ship
        yield return null;

        loadingCamera.SetActive(false);
        // Activate the battle camera and the canvas for the UI
        gameTransform.GetChild(1).GetChild(0).gameObject.SetActive(true);
        gameTransform.GetChild(1).GetChild(1).gameObject.SetActive(true);
        uIManager.SetupUIManager();
        battleManager.StartBattle();
        SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
        Destroy(gameObject);
    }
}
