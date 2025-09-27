using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

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

public class PlayerObjectUI : MonoBehaviour {
    [SerializeField] private Transform displayedImageTransform;
    [SerializeField] private Camera objectViewCamera;
    [SerializeField] private Transform objectViewCameraTransform;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private List<IPlayerUIMenu> uIMenuInput;

    public ObjectUI displayedObject { get; private set; }

    protected LocalPlayer localPlayer;
    protected PlayerUI playerUI;
    protected UIBattleManager uiBattleManager;
    protected UIManager uiManager;
    public Dictionary<Type, IPlayerUIMenu> uIMenus;

    public void SetupPlayerUIMenu(PlayerUI playerUI, LocalPlayer localPlayer, UIManager uiManager) {
        this.playerUI = playerUI;
        this.localPlayer = localPlayer;
        this.uiManager = uiManager;
        uiBattleManager = uiManager.uiBattleManager;
        uIMenus = new Dictionary<Type, IPlayerUIMenu>();
        uIMenuInput.ToList().ForEach(m => m.SetupPlayerUIMenu(playerUI, localPlayer, uiManager));
        uIMenuInput.ToList().ForEach(m => uIMenus.Add(m.GetMenuType(), m));
    }

    public void SetDisplayedObject(ObjectUI objectToDisplay) {
        if (displayedObject != null && displayedObject is BattleObjectUI oldBattleObjectUI)
            oldBattleObjectUI.UnsetDisplayedObject();

        if (objectToDisplay != null) {
            Type currentType = objectToDisplay.GetType();
            while (currentType != null) {
                if (uIMenus.ContainsKey(currentType)) {
                    CloseAllMenus();
                    displayedObject = objectToDisplay;
                    uIMenus[currentType].gameObject.SetActive(true);
                    uIMenus[currentType].SetDisplayedObject(objectToDisplay);
                    if (displayedObject is BattleObjectUI battleObjectUI) {
                        battleObjectUI.SetDisplayedObject();
                    }
                    return;
                }

                currentType = currentType.BaseType;
            }
        }
        CloseAllMenus();
        displayedObject = null;
    }

    private void CloseAllMenus() {
        foreach (KeyValuePair<Type, IPlayerUIMenu> playerUIMenu in uIMenus) {
            playerUIMenu.Value.gameObject.SetActive(false);
        }
    }

    public void RefreshMenu() {
        foreach (KeyValuePair<Type, IPlayerUIMenu> playerUIMenu in uIMenus) {
            if (playerUIMenu.Value.gameObject.activeSelf)
                playerUIMenu.Value.RefreshMenu();
        }
        RefreshStatusPanel();
    }

    protected void RefreshStatusPanel() {
        titleText.text = displayedObject.iObject.GetName();
        objectViewCameraTransform.transform.position = new Vector3(displayedObject.transform.position.x,
            displayedObject.transform.position.y, -10);
        objectViewCameraTransform.eulerAngles = new Vector3(0, 0, displayedObject.transform.eulerAngles.z);
        objectViewCamera.orthographicSize = displayedObject.iObject.GetSize() * 1.2f;
        if (displayedObject.iObject is Unit unit || displayedObject.iObject is Planet planet) { } else {
            for (int i = 0; i < displayedImageTransform.childCount; i++) {
                displayedImageTransform.GetChild(i).gameObject.SetActive(false);
            }
        }
    }
}
