using System;
using UnityEngine;

/// <summary>
/// Why is this abstract class here? Why do we need an abstract class inheriting another abstract class?
/// Well first C# does not allow us to make lists of generic constraints.
/// So we can't make List<PlayerUIMenu> where we only care that T of PlayerUIMenu is of the type BattleObject.
/// To solve this we need to have a non generic interface to use in PlayerUI.
/// Unity, however, does not support lists of interfaces in the editor.
/// Therefore we must use an abstract class instead. Thankfully this workaround actually works.
/// </summary>
public abstract class IPlayerUIMenu : MonoBehaviour {
    public abstract void SetupPlayerUIMenu(PlayerUI playerUI, LocalPlayer localPlayer, UnitSpriteManager unitSpriteManager,
        float updateSpeed);

    public abstract void SetDisplayedObject(ObjectUI objectUI);

    public abstract void UpdateUI();

    public abstract void RefreshUI();

    public abstract bool IsShown();

    public abstract Type GetMenuType();
}

public abstract class PlayerUIMenu<T> : IPlayerUIMenu where T : ObjectUI {
    protected LocalPlayer localPlayer;
    protected PlayerUI playerUI;
    protected UnitSpriteManager unitSpriteManager;
    [SerializeField] private float updateSpeed;
    private float updateTime;
    public T displayedObject { get; protected set; }

    [SerializeField] protected GameObject middlePanel;
    [SerializeField] protected GameObject leftPanel;
    [SerializeField] protected GameObject rightPanel;

    public override void SetupPlayerUIMenu(PlayerUI playerUI, LocalPlayer localPlayer, UnitSpriteManager unitSpriteManager,
        float updateSpeed) {
        this.playerUI = playerUI;
        this.localPlayer = localPlayer;
        this.updateSpeed = updateSpeed;
        this.unitSpriteManager = unitSpriteManager;
    }

    public override void SetDisplayedObject(ObjectUI objectUI) {
        SetDisplayedObject((T)objectUI);
    }

    public void SetDisplayedObject(T objectToDisplay) {
        displayedObject = objectToDisplay;
        if (displayedObject == null) {
            ShowMenu(false);
        } else {
            ShowMenu(true);
            if (leftPanel != null) leftPanel.SetActive(false);
            if (middlePanel != null) middlePanel.SetActive(false);
            if (rightPanel != null) rightPanel.SetActive(false);
            RefreshUI();
        }
    }

    /// <summary>
    /// Call this in LateUpdate to referesh the UI respecting the update speed
    /// </summary>
    public override void UpdateUI() {
        updateTime -= Time.deltaTime;
        if (updateTime <= 0) {
            updateTime += updateSpeed;
            RefreshUI();
        }
    }

    /// <summary>
    /// Immediately refreshes the UI with the information of the displayedBattleObject.
    /// If the object is no longer viable then the menu will be closed.
    /// </summary>
    public override void RefreshUI() {
        if (!IsObjectViable()) {
            playerUI.CloseAllMenus();
            return;
        }

        if (ShouldShowMiddlePanel()) {
            if (!middlePanel.activeSelf) middlePanel.SetActive(true);
            RefreshMiddlePanel();
        }

        if (ShouldShowLeftPanel()) {
            if (!leftPanel.activeSelf) leftPanel.SetActive(true);
            RefreshLeftPanel();
        }

        if (ShouldShowRightPanel()) {
            if (!rightPanel.activeSelf) rightPanel.SetActive(true);
            RefreshRightPanel();
        }
    }

    /// <summary> Determines if the displayed object is viable or not, if it can or should still be displayed. </summary>
    /// <returns> True if the object is still viable, false otherwise </returns>
    protected virtual bool IsObjectViable() {
        return true;
    }

    /// <summary>
    /// We don't necessarily have to have all three panels in every menu.
    /// So we allow extentions of this class to leave some of them unimplemented.
    /// Unimplemented panels shouldn't be refreshed since they shouldn't be shown.
    /// However if the program somehow tries to refresh a panel that shouldn't exist we should throw an error.
    /// </summary>
    protected virtual void RefreshMiddlePanel() {
        throw new InvalidProgramException("The middle panel was refreshed without any logic to refresh the panel.");
    }

    protected virtual void RefreshLeftPanel() {
        throw new InvalidProgramException("The left panel was refreshed without any logic to refresh the panel.");
    }

    protected virtual void RefreshRightPanel() {
        throw new InvalidProgramException("The right panel was refreshed without any logic to refresh the panel.");
    }

    protected virtual bool ShouldShowMiddlePanel() {
        return middlePanel != null;
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
