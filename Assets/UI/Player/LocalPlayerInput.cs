using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

public class LocalPlayerInput : MonoBehaviour {
    PlayerInput playerInput;
    protected UIBattleManager uiBattleManager;
    protected LocalPlayer localPlayer;

    public enum ActionType {
        None,
        Selecting,
        MoveCommand,
        UndockCombatAtCommand,
        AttackCommand,
        UndockTransportAtCommand,
        FormationCommand,
        UndockAllCombatCommand,
        StationBuilderCommand,
        ResearchCommand,
        CollectGasCommand,
        TransportCommand,
        ColonizeCommand,
    }

    [SerializeField] protected ActionType actionType;

    private Camera mainCamera;
    private Background background;

    [Tooltip("Player input on how fast scroling should be.")]
    public float scrollModifyer;

    public float scrollFactor = 1;

    protected bool primaryMousePressed;
    protected bool secondaryMousePressed;

    Vector2 rightClickStartPosition;
    float maxRightClickDistance;

    public bool AltButtonPressed { get; private set; }

    protected Vector2 pastMousePosition;

    public BattleObjectUI mouseOverBattleObject { get; protected set; }
    public BattleObjectUI leftClickedBattleObject { get; protected set; }
    public BattleObjectUI displayedBattleObject { get; protected set; }
    public FleetUI displayedFleet { get; protected set; }
    public BattleObjectUI rightClickedBattleObject { get; protected set; }
    public UnitUI followUnit { get; protected set; }

    public event Action<Vector2, Vector2> OnPanEvent = delegate { };

    /// <summary>Determines if the unit should be deselected on mouse up</summary>
    protected bool doingUnitClickAction;

    private int[] timeSteps = new int[] { 0, 1, 2, 5, 10, 15, 20, 25 };
    int timeStepIndex;
    protected CanvasScaler canvasScaler;

    public virtual void Setup(LocalPlayer localPlayer, UIBattleManager uiBattleManager) {
        this.localPlayer = localPlayer;
        this.uiBattleManager = uiBattleManager;
        mainCamera = transform.GetChild(0).GetComponent<Camera>();
        background = GetComponentInChildren<Background>();
        background.SetupBackground();
        playerInput = new PlayerInput();
        playerInput.Player.ZoomMovement.performed += context => UpdateZoom(context.ReadValue<Vector2>().y);

        playerInput.Player.PrimaryMouseButton.started += context => PrimaryMouseDown();
        playerInput.Player.PrimaryMouseButton.canceled += context => PrimaryMouseUp();

        playerInput.Player.SeccondaryMouseButton.started += context => SecondaryMouseDown();
        playerInput.Player.SeccondaryMouseButton.canceled += context => SecondaryMouseUp();
        playerInput.Player.DecreaseSpeed.performed += context => SlowdownSimulationButtonPressed();
        playerInput.Player.IncreaseSpeed.performed += context => SpeedupSimulationButtonPressed();
        playerInput.Player.PauseSimulation.performed += context => StopSimulationButtonPressed();

        GetPlayerInput().Player.AltModifier.started += context => AltButtonDown();
        GetPlayerInput().Player.AltModifier.canceled += context => AltButtonUp();

        playerInput.Player.Escape.performed += context => EscapeButtonPressed();

        playerInput.Player.CenterCamera.performed += context => CenterCamera();
        playerInput.Player.FollowUnit.performed += context => FollowUnitButtonPressed();
        playerInput.Player.ToggleZoomIndicators.performed += context => ToggleUnitZoomIndicators();
        playerInput.Player.ToggleFactionColors.performed += context => ToggleFactionColor();

        playerInput.Enable();
        timeStepIndex = 1;
        doingUnitClickAction = false;
        canvasScaler = localPlayer.playerUI.GetComponentInParent<CanvasScaler>();
    }

    public virtual void ChangeFaction() {
        displayedBattleObject = null;
        leftClickedBattleObject = null;
        rightClickedBattleObject = null;
        StopFollowingUnit();
        mouseOverBattleObject = null;
    }

    public virtual void UpdatePlayer() {
        mouseOverBattleObject = GetBattleObjectOverMouse();
        if (primaryMousePressed)
            PrimaryMouseHeld();
        if (secondaryMousePressed)
            SecondaryMouseHeld();
        pastMousePosition = GetMousePosition();
        if (followUnit != null)
            transform.position = followUnit.unit.position;
    }

    void UpdateZoom(float scroll) {
        if (localPlayer.GetPlayerUI().FreezeZoom())
            return;
        float targetSize = Mathf.Min(50000, Mathf.Max(1, mainCamera.orthographicSize + scroll * scrollModifyer * scrollFactor * 50));

        // Zoom to the mouse position
        if (!AltButtonPressed) {
            float difference = mainCamera.orthographicSize - targetSize;
            MoveCamera((GetMouseWorldPosition() - (Vector2)mainCamera.transform.position) * difference / mainCamera.orthographicSize);
        }

        mainCamera.orthographicSize = targetSize;

        scrollFactor = mainCamera.orthographicSize / -130;

        mainCamera.transform.GetChild(0).localScale =
            new Vector3(mainCamera.orthographicSize / 3.8f, mainCamera.orthographicSize / 3.8f, 10);
        background.UpdateBackground(mainCamera.orthographicSize / 5, 10 / Mathf.Sqrt(mainCamera.orthographicSize));
    }

    public void SetZoom(float zoom) {
        mainCamera.orthographicSize = Mathf.Min(50000, Mathf.Max(1, zoom));

        scrollFactor = mainCamera.orthographicSize / -130;

        mainCamera.transform.GetChild(0).localScale =
            new Vector3(mainCamera.orthographicSize / 3.8f, mainCamera.orthographicSize / 3.8f, 10);
        background.UpdateBackground(mainCamera.orthographicSize / 5, 10 / Mathf.Sqrt(mainCamera.orthographicSize));
    }

    protected void MoveCamera(Vector2 movement) {
        SetCameraPosition(new Vector2(mainCamera.transform.position.x + movement.x, mainCamera.transform.position.y + movement.y));
    }

    public void SetCameraPosition(Vector2 position) {
        mainCamera.transform.position = new Vector3(position.x, position.y, -10);
    }

    public void StartFollowingUnit(UnitUI unit) {
        StopFollowingUnit();
        if (unit == null || unit == followUnit) {
            return;
        }

        followUnit = unit;
        SetCameraPosition(Vector2.zero);
        transform.position = followUnit.unit.position;
    }

    public void StopFollowingUnit() {
        followUnit = null;
        MoveCamera(transform.position);
        transform.position = Vector2.zero;
    }

    public void CenterCamera() {
        if (followUnit != null) {
            UnitUI targetUnit = followUnit;
            StopFollowingUnit();
            StartFollowingUnit(targetUnit);
            transform.position = followUnit.unit.position;
        } else if (displayedBattleObject != null) {
            SetCameraPosition(displayedBattleObject.battleObject.position);
        } else if (localPlayer.GetFaction() != null && localPlayer.GetFaction().stations.Count > 0) {
            SetCameraPosition(localPlayer.GetFaction().stations.First().position);
        } else if (localPlayer.GetFaction() != null && localPlayer.GetFaction().units.Count > 0) {
            SetCameraPosition(localPlayer.GetFaction().units.First().position);
        } else {
            SetCameraPosition(Vector2.zero);
        }
    }

    protected virtual void PrimaryMouseDown() {
        if (primaryMousePressed == true || localPlayer.GetPlayerUI().IsAMenueShown())
            return;
        primaryMousePressed = true;
        leftClickedBattleObject = mouseOverBattleObject;
    }

    protected virtual void PrimaryMouseHeld() { }

    protected virtual void PrimaryMouseUp() {
        primaryMousePressed = false;
        if (localPlayer.GetPlayerUI().IsAMenueShown())
            return;
        if (leftClickedBattleObject != null && !doingUnitClickAction) {
            displayedBattleObject = leftClickedBattleObject;
        } else if (!doingUnitClickAction) {
            displayedBattleObject = null;
        }
    }

    protected virtual void SecondaryMouseDown() {
        secondaryMousePressed = true;
        pastMousePosition = GetMousePosition();
        rightClickedBattleObject = mouseOverBattleObject;
        rightClickStartPosition = GetMousePosition();
    }

    protected virtual void SecondaryMouseHeld() {
        maxRightClickDistance = Mathf.Max(maxRightClickDistance, Vector2.Distance(rightClickStartPosition, GetMousePosition()));
        if (!localPlayer.GetPlayerUI().IsAMenueShown()) {
            Vector2 oldPosition = GetCamera().transform.position;
            MoveCamera((pastMousePosition - GetMousePosition()) * mainCamera.orthographicSize / GetScreenScale() / 1200);
            OnPanEvent(oldPosition, GetCamera().transform.position);
        }
    }

    protected virtual void SecondaryMouseUp() {
        secondaryMousePressed = false;
        if (maxRightClickDistance < 1) {
            if (!localPlayer.GetPlayerUI().IsAMenueShown()) {
                if (rightClickedBattleObject != null && rightClickedBattleObject == mouseOverBattleObject) {
                    localPlayer.GetPlayerUI().SetDisplayedObject(rightClickedBattleObject);
                    rightClickedBattleObject = null;
                }
            } else {
                localPlayer.GetPlayerUI().CloseAllMenus();
            }
        }

        maxRightClickDistance = 0;
    }

    public void SlowdownSimulationButtonPressed() {
        timeStepIndex = Mathf.Max(0, timeStepIndex - 1);
        Time.timeScale = timeSteps[timeStepIndex];
    }

    public void SpeedupSimulationButtonPressed() {
        timeStepIndex = Mathf.Min(timeSteps.Length - 1, timeStepIndex + 1);
        Time.timeScale = timeSteps[timeStepIndex];
    }

    public void StopSimulationButtonPressed() {
        if (timeStepIndex != 0) {
            timeStepIndex = 0;
            Time.timeScale = timeSteps[timeStepIndex];
        } else {
            timeStepIndex = 1;
            Time.timeScale = timeSteps[timeStepIndex];
        }
    }

    public void ResetTimeScale() {
        timeStepIndex = 1;
        Time.timeScale = timeSteps[timeStepIndex];
    }

    void AltButtonDown() {
        AltButtonPressed = true;
    }

    void AltButtonUp() {
        AltButtonPressed = false;
    }

    void EscapeButtonPressed() {
        if (localPlayer.GetPlayerUI().IsAMenueShown()) {
            localPlayer.GetPlayerUI().CloseAllMenus();
            return;
        }

        PlayerUI.Instance.ToggleMenueUI();
    }

    void FollowUnitButtonPressed() {
        if (followUnit != null) {
            StopFollowingUnit();
            return;
        }

        if (displayedBattleObject != null && displayedBattleObject.battleObject.IsUnit()) {
            StartFollowingUnit((UnitUI)displayedBattleObject);
        }
    }

    void ToggleUnitZoomIndicators() {
        PlayerUI.Instance.ToggleUnitZoomIndicators();
    }

    void ToggleFactionColor() {
        PlayerUI.Instance.SetFactionColor(!PlayerUI.Instance.factionColoring);
    }

    BattleObjectUI GetBattleObjectOverMouse() {
        BattleObjectUI objectUI = null;
        float distance = float.MaxValue;
        Profiler.BeginSample("BattleObjectOverMouse");
        Vector2 mouseWorldPosition = GetMouseWorldPosition();
        foreach (BattleObjectUI targetObject in uiBattleManager.battleObjects.Values) {
            if (!targetObject.IsSelectable()) continue;

            float tempDistance = Vector2.Distance(mouseWorldPosition, targetObject.battleObject.position);
            float size = targetObject.battleObject.GetSize();
            if (targetObject is UnitUI unitUI) size *= Mathf.Max(1, unitUI.unitSelection.GetSize());

            if (tempDistance < size && tempDistance < distance) {
                objectUI = targetObject;
                distance = tempDistance;
            }
        }

        Profiler.EndSample();
        return objectUI;
    }

    public virtual void UnitDestroyed(Unit unit) {
        if (GetDisplayedBattleObject() == uiBattleManager.units[unit]) displayedBattleObject = null;
    }

    public PlayerInput GetPlayerInput() {
        return playerInput;
    }

    public virtual BattleObjectUI GetDisplayedBattleObject() {
        return displayedBattleObject;
    }

    public FleetUI GetDisplayedFleet() {
        return displayedFleet;
    }

    private void OnDisable() {
        playerInput.Disable();
    }

    public ActionType GetActionType() {
        return actionType;
    }

    /// <returns> True if the object is visible by the camera, false otherwise. </returns>
    public bool IsObjectInViewingField(ObjectUI objectUI) {
        Vector2 position = mainCamera.WorldToScreenPoint(objectUI.iObject.GetPosition());
        // We can find the object screen size of the object by taking a point [size] distance away and getting its screen position
        float objectScreenSize = position.x -
            mainCamera.WorldToScreenPoint(objectUI.iObject.GetPosition() - new Vector2(objectUI.iObject.GetSize(), 0)).x;
        // Check if the position is within all four bounds of the screen
        return position.y >= -objectScreenSize && position.y - objectScreenSize <= Screen.height &&
            position.x >= -objectScreenSize && position.x - objectScreenSize <= Screen.width;
    }

    /// <returns> Determines if close up detailed graphics should be shown depending on how zoomed out the camera is. </returns>
    public bool ShouldShowCloseUpGraphics() {
        return mainCamera.orthographicSize < 3000;
    }

    public Vector2 GetMousePosition() {
        return playerInput.Player.MousePosition.ReadValue<Vector2>();
    }

    public Vector2 GetMouseWorldPosition() {
        return mainCamera.ScreenToWorldPoint(playerInput.Player.MousePosition.ReadValue<Vector2>());
    }

    protected Vector2 GetScreenScale() {
        return canvasScaler.GetComponent<RectTransform>().localScale;
    }

    public Camera GetCamera() {
        return mainCamera;
    }
}
