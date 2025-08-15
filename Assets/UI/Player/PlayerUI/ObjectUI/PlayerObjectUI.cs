using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class PlayerObjectUIMenu : MonoBehaviour {
    [SerializeField] private float updateSpeed;
    protected LocalPlayer localPlayer;
    protected PlayerUI playerUI;
    protected UIBattleManager uiBattleManager;
    protected UIManager uiManager;
    private float updateTime;

    public virtual void SetupPlayerObjectUIMenu(PlayerUI playerUI, LocalPlayer localPlayer, UIManager uiManager) {
        this.playerUI = playerUI;
        this.localPlayer = localPlayer;
        this.uiManager = uiManager;
        uiBattleManager = uiManager.uiBattleManager;
    }

    public virtual void SetDisplayedObject(ObjectUI objectUI) {
        updateTime = 0;
    }

    public void UpdateUI() {
        updateTime -= Time.deltaTime;
        if (updateTime <= 0) {
            updateTime += updateSpeed;
            UpdateMenu();
        }
    }

    public abstract bool ShouldShowMenu();

    public abstract void UpdateMenu();
}

public class PlayerObjectUI : PlayerUIMenu<ObjectUI> {
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Transform objectViewCameraTransform;
    [SerializeField] private Camera objectViewCamera;
    [SerializeField] private Transform displayedImageTransform;
    private List<GameObject> moduleUIs;
    [SerializeField] private GameObject moduleUIPrefab;
    private ModuleSystem.System selectedSystem;
    [SerializeField] private ObjectSystemUI objectSystemUI;

    [SerializeField] private ObjectConstructionUI constructionUI;
    [SerializeField] private ObjectHangarUI hangarUI;
    [SerializeField] private UnitUIMenu unitUI;

    public override void SetupPlayerUIMenu(PlayerUI playerUI, LocalPlayer localPlayer, UIManager uiManager) {
        base.SetupPlayerUIMenu(playerUI, localPlayer, uiManager);
        moduleUIs = new List<GameObject>();
        constructionUI.SetupPlayerObjectUIMenu(playerUI, localPlayer, uiManager);
        hangarUI.SetupPlayerObjectUIMenu(playerUI, localPlayer, uiManager);
        objectSystemUI.SetupPlayerObjectUIMenu(playerUI, localPlayer, uiManager);
        objectSystemUI.gameObject.SetActive(false);
        unitUI.SetupUnitUIMenu(playerUI, localPlayer, uiManager);
    }

    public override void SetDisplayedObject(ObjectUI objectToDisplay) {
        base.SetDisplayedObject(objectToDisplay);
        constructionUI.SetDisplayedObject(displayedObject);
        hangarUI.SetDisplayedObject(displayedObject);
        objectSystemUI.SetDisplayedObject(displayedObject);
        if (objectToDisplay is UnitUI unit) {
            unitUI.gameObject.SetActive(true);
            unitUI.SetUnit(unit);
        } else {
            unitUI.gameObject.SetActive(false);
        }
        DeselectSystem();
    }

    protected override void RefreshMiddlePanel() {
        titleText.text = displayedObject.iObject.GetName();
        objectViewCameraTransform.transform.position = new Vector3(displayedObject.transform.position.x,
            displayedObject.transform.position.y, -10);
        objectViewCameraTransform.eulerAngles = new Vector3(0, 0, displayedObject.transform.eulerAngles.z);
        objectViewCamera.orthographicSize = displayedObject.iObject.GetSize() * 1.2f;
        if (displayedObject.iObject is Unit unit) {
            UpdateModules(unit);
            unitUI.UpdateMenu();
        } else {
            for (int i = 0; i < displayedImageTransform.childCount; i++) {
                displayedImageTransform.GetChild(i).gameObject.SetActive(false);
            }
        }
    }

    private void UpdateModules(Unit unit) {
        ModuleSystem moduleSystem = unit.moduleSystem;
        for (int i = 0; i < moduleSystem.modules.Count; i++) {
            var module = moduleSystem.modules[i];
            if (moduleUIs.Count <= i) {
                moduleUIs.Add(Instantiate(moduleUIPrefab, displayedImageTransform));
                int moduleIndex = i;
                moduleUIs[i].GetComponent<Button>().onClick.AddListener(() => OnModuleButtonPress(moduleIndex));
            }
            moduleUIs[i].SetActive(true);
            moduleUIs[i].GetComponent<RectTransform>().anchoredPosition = module.GetPosition() *
                displayedImageTransform.GetComponent<RectTransform>().rect.size * unit.scale * 42 /
                (100 * unit.GetSpriteSize());
            moduleUIs[i].transform.GetChild(0).eulerAngles = new Vector3(0, 0, module.rotation);
            if (module.componentScriptableObject.sprite != null) {
                moduleUIs[i].transform.GetChild(0).GetComponent<Image>().sprite =
                    module.componentScriptableObject.sprite;
                moduleUIs[i].transform.GetChild(0).gameObject.SetActive(true);
            } else moduleUIs[i].transform.GetChild(0).gameObject.SetActive(false);

            if (selectedSystem != null && selectedSystem == moduleSystem.moduleToSystem[moduleSystem.modules[i]]) {
                moduleUIs[i].GetComponent<Image>().color = Color.white;
            } else {
                moduleUIs[i].GetComponent<Image>().color = Color.grey;
            }
        }
        for (int i = moduleSystem.modules.Count; i < moduleUIs.Count; i++) {
            moduleUIs[i].SetActive(false);
        }
    }

    private void OnModuleButtonPress(int moduleIndex) {
        ModuleSystem moduleSystem = ((Unit)displayedObject.iObject).moduleSystem;
        if (selectedSystem == moduleSystem.moduleToSystem[moduleSystem.modules[moduleIndex]]) {
            DeselectSystem();
        } else {
            selectedSystem = moduleSystem.moduleToSystem[moduleSystem.modules[moduleIndex]];
            rightPanel = objectSystemUI.gameObject;
            objectSystemUI.SelectSystem(selectedSystem);
            objectSystemUI.UpdateMenu();
            hangarUI.gameObject.SetActive(false);
        }
    }

    public void DeselectSystem() {
        selectedSystem = null;
        rightPanel = hangarUI.gameObject;
        objectSystemUI.gameObject.SetActive(false);
    }

    protected override bool ShouldShowLeftPanel() {
        return constructionUI.ShouldShowMenu() && selectedSystem == null;
    }

    protected override void RefreshLeftPanel() {
        constructionUI.UpdateUI();
    }

    protected override bool ShouldShowRightPanel() {
        return (hangarUI.ShouldShowMenu() && selectedSystem == null)
            || (objectSystemUI.ShouldShowMenu() && selectedSystem != null);
    }

    protected override void RefreshRightPanel() {
        if (selectedSystem == null)
            hangarUI.UpdateUI();
        else objectSystemUI.UpdateUI();
    }
}
