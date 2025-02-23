using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Faction;

public class SimulationSetup : MonoBehaviour {
    [SerializeField] private GameObject editSimulationPanel;
    [SerializeField] private TMP_InputField editSimulationStars;
    [SerializeField] private TMP_InputField editSimulationAsteroids;
    [SerializeField] private TMP_InputField editSimulationAsteroidCount;
    [SerializeField] private TMP_InputField editSimulationGasClouds;
    [SerializeField] private TMP_InputField editSimulationSystemSizeModifier;
    [SerializeField] private TMP_InputField editSimulationResearchModifier;
    [SerializeField] private GameObject factionPrefab;
    [SerializeField] private Transform factionList;
    [SerializeField] private GameObject editFactionPanel;
    [SerializeField] private TMP_InputField editFactionName;
    [SerializeField] private TMP_InputField editFactionAbbreviation;
    [SerializeField] private TMP_InputField editFactionCredits;
    [SerializeField] private TMP_InputField editFactionScience;
    [SerializeField] private TMP_InputField editFactionShips;
    [SerializeField] private TMP_InputField editFactionStations;

    public List<FactionData> factions;
    private int selectedFaction;
    private BattleManager.BattleSettings battleSettings;
    private ColorPicker colorPicker = new ColorPicker();
    private StartMenu startMenu;

    public void SetStartMenu(StartMenu startMenu) {
        this.startMenu = startMenu;
    }

    public void ShowSimulationSetup() {
        gameObject.SetActive(true);
        factions = new List<FactionData>();
        battleSettings = new BattleManager.BattleSettings {
            starCount = 3,
            asteroidFieldCount = 60,
            asteroidCountModifier = 1,
            gasCloudCount = 16,
            systemSizeModifier = 1,
            researchModifier = 1.01f,
        };

        for (int i = 0; i < factionList.childCount; i++) {
            Destroy(factionList.GetChild(i).gameObject);
        }

        editSimulationPanel.SetActive(false);
        SelectFaction(-1);
    }

    public void SetupDefaultSimulation() {
        ShowSimulationSetup();
        startMenu.buttonSound.Play();
        gameObject.SetActive(true);
        battleSettings = new BattleManager.BattleSettings {
            asteroidFieldCount = 80,
            starCount = 3,
            asteroidCountModifier = 1.2f,
            gasCloudCount = 16,
            systemSizeModifier = 1.2f,
            researchModifier = 1.01f,
        };
        ColorPicker colorPicker = new ColorPicker();
        factions.Add(new FactionData("Faction1", "F1", colorPicker.PickColor(), Random.Range(1000000, 1500000), 0, 5, 5));
        factions.Add(new FactionData("Faction2", "F2", colorPicker.PickColor(), Random.Range(1000000, 1500000), 0, 5, 5));
        factions.Add(new FactionData("Faction3", "F3", colorPicker.PickColor(), Random.Range(1000000, 1500000), 0, 5, 5));
        factions.Add(new FactionData("Faction4", "F4", colorPicker.PickColor(), Random.Range(1000000, 1500000), 0, 5, 5));
        StartSimulation();
    }

    public void SetupBattleSimulation() {
        ShowSimulationSetup();
        startMenu.buttonSound.Play();
        gameObject.SetActive(true);
        battleSettings = new BattleManager.BattleSettings() {
            asteroidFieldCount = 0,
            starCount = 0,
            asteroidCountModifier = 1f,
            gasCloudCount = 0,
            systemSizeModifier = 0.1f,
            researchModifier = 1.01f,
        };
        factions.Add(new FactionData("Faction1", "F1", colorPicker.PickColor(), Random.Range(1000000, 1500000), 0, 45, 1));
        factions.Add(new FactionData("Faction2", "F2", colorPicker.PickColor(), Random.Range(1000000, 1500000), 0, 45, 1));
        StartSimulation();
    }

    public void ToggleSimulationSettings() {
        startMenu.buttonSound.Play();
        editSimulationPanel.SetActive(!editSimulationPanel.activeSelf);
        if (editSimulationPanel.activeSelf) {
            editFactionPanel.SetActive(false);
            editSimulationStars.SetTextWithoutNotify(battleSettings.starCount.ToString());
            editSimulationAsteroids.SetTextWithoutNotify(battleSettings.asteroidFieldCount.ToString());
            editSimulationAsteroidCount.SetTextWithoutNotify(battleSettings.asteroidCountModifier.ToString());
            editSimulationGasClouds.SetTextWithoutNotify(battleSettings.gasCloudCount.ToString());
            editSimulationSystemSizeModifier.SetTextWithoutNotify(battleSettings.systemSizeModifier.ToString());
            editSimulationResearchModifier.SetTextWithoutNotify(battleSettings.researchModifier.ToString());
        }
    }

    public void UpdateSimulation() {
        try {
            battleSettings.starCount = int.Parse(editSimulationStars.text);
            battleSettings.asteroidFieldCount = int.Parse(editSimulationAsteroids.text);
            battleSettings.asteroidCountModifier = float.Parse(editSimulationAsteroidCount.text);
            battleSettings.gasCloudCount = int.Parse(editSimulationGasClouds.text);
            battleSettings.systemSizeModifier = float.Parse(editSimulationSystemSizeModifier.text);
            battleSettings.researchModifier = float.Parse(editSimulationResearchModifier.text);
        } catch {
            Debug.LogWarning("Error parsing simulation inputs");
            battleSettings.starCount = 3;
            battleSettings.asteroidFieldCount = 20;
            battleSettings.asteroidCountModifier = 1.2f;
            battleSettings.gasCloudCount = 16;
            battleSettings.systemSizeModifier = 1f;
            battleSettings.researchModifier = 1.1f;
        }
    }

    public void AddFaction() {
        if (factions.Count > 0) {
            string newName = factions[factions.Count - 1].name;
            int pastInt = 0;
            if (int.TryParse(newName.Substring(newName.Length - 1), out pastInt)) {
                newName = AddNumberToTheEnd(newName.Substring(0, newName.Length - 1), pastInt);
            } else {
                newName = newName + 1;
            }

            factions.Add(new FactionData(newName, "F" + factions.Count.ToString(), colorPicker.PickColor(),
                factions[factions.Count - 1].credits, factions[factions.Count - 1].science, factions[factions.Count - 1].ships,
                factions[factions.Count - 1].stations));
        } else {
            factions.Add(new FactionData("New Faction", "F" + factions.Count.ToString(), colorPicker.PickColor(), 200000, 0, 2, 2));
        }

        GameObject newFactionPrefab = Instantiate(factionPrefab, factionList);
        newFactionPrefab.name = factions[factions.Count - 1].name;
        newFactionPrefab.transform.GetChild(0).GetComponent<TMP_Text>().text = newFactionPrefab.name;
        newFactionPrefab.GetComponent<Button>().onClick.AddListener(() => SelectFaction(newFactionPrefab.transform.GetSiblingIndex()));
        SelectFaction(factions.Count - 1);
    }

    private string AddNumberToTheEnd(string original, int pastInt) {
        if (pastInt == 9) {
            int nextPastInt = 0;
            if (int.TryParse(original.Substring(original.Length - 1), out nextPastInt)) {
                return AddNumberToTheEnd(original.Substring(0, original.Length - 1), nextPastInt) + "0";
            } else {
                return original + "10";
            }
        } else {
            return original + (pastInt + 1);
        }
    }

    public void SelectFaction(int factionIndex) {
        startMenu.buttonSound.Play();
        if (factionIndex == -1 || factionIndex == selectedFaction) {
            selectedFaction = -1;
            editFactionPanel.SetActive(false);
        } else {
            editSimulationPanel.SetActive(false);
            selectedFaction = factionIndex;
            editFactionPanel.SetActive(true);
            editFactionName.SetTextWithoutNotify(factions[factionIndex].name);
            editFactionAbbreviation.SetTextWithoutNotify(factions[factionIndex].abbreviatedName);
            editFactionCredits.SetTextWithoutNotify(factions[factionIndex].credits.ToString());
            editFactionScience.SetTextWithoutNotify(factions[factionIndex].science.ToString());
            editFactionShips.SetTextWithoutNotify(factions[factionIndex].ships.ToString());
            editFactionStations.SetTextWithoutNotify(factions[factionIndex].stations.ToString());
        }
    }

    public void UpdateSelectedFaction() {
        try {
            factions[selectedFaction] = new FactionData(editFactionName.text, editFactionAbbreviation.text, colorPicker.PickColor(),
                long.Parse(editFactionCredits.text), long.Parse(editFactionScience.text), int.Parse(editFactionShips.text),
                int.Parse(editFactionStations.text));
        } catch { }

        factionList.GetChild(selectedFaction).gameObject.name = factions[selectedFaction].name;
        factionList.GetChild(selectedFaction).GetChild(0).GetComponent<TMP_Text>().text = factions[selectedFaction].name;
    }

    public void RemoveSelectedFaction() {
        startMenu.buttonSound.Play();
        factions.RemoveAt(selectedFaction);
        Destroy(factionList.GetChild(selectedFaction).gameObject);
        SelectFaction(-1);
    }

    public void StartSimulation() {
        startMenu.buttonSound.Play();
        SceneLoader.LoadBattle(battleSettings, factions);
    }
}
