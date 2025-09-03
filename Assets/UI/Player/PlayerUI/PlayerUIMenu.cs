using System;
using UnityEngine;

/// <summary>
///     Why is this abstract class here? Why do we need an abstract class inheriting another abstract class?
///     Well first C# does not allow us to make lists of generic constraints.
///     So we can't make List where we only care that T of PlayerUIMenu is of the type BattleObject.
///     To solve this we need to have a non generic interface to use in PlayerUI.
///     Unity, however, does not support lists of interfaces in the editor.
///     Therefore we must use an abstract class instead. Thankfully this workaround actually works.
/// </summary>
public abstract class IPlayerUIMenu : MonoBehaviour {
    public abstract void SetupPlayerUIMenu(PlayerUI playerUI, LocalPlayer localPlayer, UIManager uiManager);

    public abstract void SetDisplayedObject(ObjectUI objectUI);

    public abstract void UpdateUI();

    public abstract void RefreshMenu();

    public abstract bool IsShown();

    public abstract Type GetMenuType();
}

public abstract class PlayerUIMenu<T> : IPlayerUIMenu where T : ObjectUI {
    [SerializeField] private float updateSpeed;

    [SerializeField] protected GameObject statusPanel;
    [SerializeField] protected GameObject leftPanel;
    [SerializeField] protected GameObject rightPanel;
    protected LocalPlayer localPlayer;
    protected PlayerUI playerUI;
    protected UIBattleManager uiBattleManager;
    protected UIManager uiManager;
    private float updateTime;
    public T displayedObject { get; protected set; }

    public override void SetupPlayerUIMenu(PlayerUI playerUI, LocalPlayer localPlayer, UIManager uiManager) {
        this.playerUI = playerUI;
        this.localPlayer = localPlayer;
        this.uiManager = uiManager;
        uiBattleManager = uiManager.uiBattleManager;
    }

    public override void SetDisplayedObject(ObjectUI objectUI) {
        SetDisplayedObjectT((T)objectUI);
    }

    private void SetDisplayedObjectT(T objectToDisplay) {
        displayedObject = objectToDisplay;
        if (displayedObject == null) {
            ShowMenu(false);
        } else {
            ShowMenu(true);
            if (leftPanel != null) leftPanel.SetActive(false);
            if (statusPanel != null) statusPanel.SetActive(false);
            if (rightPanel != null) rightPanel.SetActive(false);
            updateTime = 0;
        }
    }

    /// <summary>
    ///     Call this in LateUpdate to refresh the UI respecting the update speed
    /// </summary>
    public override void UpdateUI() {
        updateTime -= Time.deltaTime;
        if (updateTime <= 0) {
            updateTime += updateSpeed;
            RefreshMenu();
        }
    }

    /// <summary>
    ///     Immediately refreshes the UI with the information of the displayedBattleObject.
    ///     If the object is no longer viable then the menu will be closed.
    /// </summary>
    public override void RefreshMenu() {
        if (!IsObjectViable()) {
            playerUI.CloseAllMenus();
            return;
        }

        if (ShouldShowStatusPanel()) {
            if (!statusPanel.activeSelf) statusPanel.SetActive(true);
            RefreshStatusPanel();
        }

        if (ShouldShowLeftPanel()) {
            if (!leftPanel.activeSelf) leftPanel.SetActive(true);
            RefreshLeftPanel();
        } else if (leftPanel != null) {
            leftPanel.SetActive(false);
        }

        if (ShouldShowRightPanel()) {
            if (!rightPanel.activeSelf) rightPanel.SetActive(true);
            RefreshRightPanel();
        } else if (leftPanel != null) {
            rightPanel.SetActive(false);
        }
    }

    /// <summary> Determines if the displayed object is viable or not, if it can or should still be displayed. </summary>
    /// <returns> True if the object is still viable, false otherwise </returns>
    protected virtual bool IsObjectViable() {
        return true;
    }

    /// <summary>
    ///     We don't necessarily have to have all three panels in every menu.
    ///     So we allow extentions of this class to leave some of them unimplemented.
    ///     Unimplemented panels shouldn't be refreshed since they shouldn't be shown.
    ///     However if the program somehow tries to refresh a panel that shouldn't exist we should throw an error.
    /// </summary>
    protected virtual void RefreshStatusPanel() {
        throw new InvalidProgramException("The status panel was refreshed without any logic to refresh the panel.");
    }

    protected virtual void RefreshLeftPanel() {
        throw new InvalidProgramException("The left panel was refreshed without any logic to refresh the panel.");
    }

    protected virtual void RefreshRightPanel() {
        throw new InvalidProgramException("The right panel was refreshed without any logic to refresh the panel.");
    }

    protected virtual bool ShouldShowStatusPanel() {
        return statusPanel != null;
    }

    protected virtual bool ShouldShowLeftPanel() {
        return leftPanel != null;
    }

    protected virtual bool ShouldShowRightPanel() {
        return rightPanel != null;
    }

    public void ShowMenu(bool shown) {
        if (shown) {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
        } else {
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }
    }

    public override bool IsShown() {
        return gameObject.activeSelf;
    }

    public override Type GetMenuType() {
        return typeof(T);
    }
}
