using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-1)]
public class LocalPlayerInput : MonoBehaviour {
    PlayerInput playerInput;

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

    public bool AltButtonPressed { get; private set; }

    protected Vector2 pastMousePosition;

    public BattleObject mouseOverBattleObject { get; protected set; }
    public BattleObject leftClickedBattleObject { get; protected set; }
    public BattleObject displayedBattleObject { get; protected set; }
    public Fleet displayedFleet { get; protected set; }
    public BattleObject rightClickedBattleObject { get; protected set; }
    public Unit followUnit { get; protected set; }

    public event Action<Vector2, Vector2> OnPanEvent = delegate { };

    /// <summary>Determines if the unit should be deselected on mouse up</summary>
    protected bool doingUnitClickAction;

    private int[] timeSteps = new int[] { 0, 1, 2, 5, 10, 15, 20, 25 };
    int timeStepIndex;

    public virtual void Setup() {
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
            transform.position = followUnit.transform.position;
    }

    void UpdateZoom(float scroll) {
        if (LocalPlayer.Instance.GetPlayerUI().FreezeZoom())
            return;
        float targetSize = Mathf.Min(50000, Mathf.Max(1, mainCamera.orthographicSize + scroll * scrollModifyer * scrollFactor));
        if (!AltButtonPressed) {
            float difference = mainCamera.orthographicSize - targetSize;
            MoveCamera((GetMouseWorldPosition() - (Vector2)mainCamera.transform.position) * difference / mainCamera.orthographicSize);
        }
        mainCamera.orthographicSize = targetSize;

        scrollFactor = mainCamera.orthographicSize / -130;

        mainCamera.transform.GetChild(0).localScale = new Vector3(mainCamera.orthographicSize / 3.8f, mainCamera.orthographicSize / 3.8f, 10);
        background.UpdateBackground(mainCamera.orthographicSize / 5, 10 / Mathf.Sqrt(mainCamera.orthographicSize));
    }

    public void SetZoom(float zoom) {
        mainCamera.orthographicSize = Mathf.Min(50000, Mathf.Max(1, zoom));

        scrollFactor = mainCamera.orthographicSize / -130;

        mainCamera.transform.GetChild(0).localScale = new Vector3(mainCamera.orthographicSize / 3.8f, mainCamera.orthographicSize / 3.8f, 10);
        background.UpdateBackground(mainCamera.orthographicSize / 5, 10 / Mathf.Sqrt(mainCamera.orthographicSize));

    }

    protected void MoveCamera(Vector2 movement) {
        SetCameraPosition(new Vector2(mainCamera.transform.position.x + movement.x, mainCamera.transform.position.y + movement.y));
    }

    public void SetCameraPosition(Vector2 position) {
        mainCamera.transform.position = new Vector3(position.x, position.y, -10);
    }

    public void StartFollowingUnit(Unit unit) {
        StopFollowingUnit();
        if (unit == null || unit == followUnit) {
            return;
        }
        followUnit = unit;
        SetCameraPosition(Vector2.zero);
        transform.position = followUnit.GetPosition();
    }

    public void StopFollowingUnit() {
        followUnit = null;
        MoveCamera(transform.position);
        transform.position = Vector2.zero;
    }

    public void CenterCamera() {
        if (followUnit != null) {
            Unit targetUnit = followUnit;
            StopFollowingUnit();
            StartFollowingUnit(targetUnit);
            transform.position = followUnit.GetPosition();
        } else if (displayedBattleObject != null) {
            SetCameraPosition(displayedBattleObject.GetPosition());
        } else if (LocalPlayer.Instance.GetFaction() != null && LocalPlayer.Instance.GetFaction().stations.Count > 0) {
            SetCameraPosition(LocalPlayer.Instance.GetFaction().stations.First().transform.position);
        }
    }

    protected virtual void PrimaryMouseDown() {
        if (primaryMousePressed == true || LocalPlayer.Instance.GetPlayerUI().IsAMenueShown())
            return;
        primaryMousePressed = true;
        leftClickedBattleObject = mouseOverBattleObject;
    }

    protected virtual void PrimaryMouseHeld() {

    }

    protected virtual void PrimaryMouseUp() {
        primaryMousePressed = false;
        if (LocalPlayer.Instance.GetPlayerUI().IsAMenueShown())
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
    }

    protected virtual void SecondaryMouseHeld() {
        if (!LocalPlayer.Instance.GetPlayerUI().IsAMenueShown()) {
            Vector2 oldPosition = GetCamera().transform.position;
            MoveCamera((pastMousePosition - GetMousePosition()) * mainCamera.orthographicSize / 422);
            OnPanEvent(oldPosition, GetCamera().transform.position);

        }
    }

    protected virtual void SecondaryMouseUp() {
        secondaryMousePressed = false;
        if (!LocalPlayer.Instance.GetPlayerUI().IsAMenueShown()) {
            if (rightClickedBattleObject != null && rightClickedBattleObject == mouseOverBattleObject) {
                if (rightClickedBattleObject.IsShip()) {
                    LocalPlayer.Instance.GetPlayerUI().SetDisplayShip((Ship)rightClickedBattleObject);
                    rightClickedBattleObject = null;
                } else if (rightClickedBattleObject.IsStation()) {
                    LocalPlayer.Instance.GetPlayerUI().SetDisplayStation((Station)rightClickedBattleObject);
                    rightClickedBattleObject = null;
                } else if (rightClickedBattleObject.IsPlanet()) {
                    LocalPlayer.Instance.GetPlayerUI().SetDisplayedPlanet((Planet)rightClickedBattleObject);
                    rightClickedBattleObject = null;
                }
            }
        } else {
            LocalPlayer.Instance.GetPlayerUI().CloseAllMenus();
        }
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
        if (LocalPlayer.Instance.GetPlayerUI().IsAMenueShown()) {
            LocalPlayer.Instance.GetPlayerUI().CloseAllMenus();
            return;
        }
        PlayerUI.Instance.ToggleMenueUI();
    }

    void FollowUnitButtonPressed() {
        if (followUnit != null) {
            StopFollowingUnit();
            return;
        }
        if (displayedBattleObject != null && displayedBattleObject.IsUnit()) {
            StartFollowingUnit((Unit)displayedBattleObject);
        }
    }

    void ToggleUnitZoomIndicators() {
        PlayerUI.Instance.ToggleUnitZoomIndicators();
    }

    void ToggleFactionColor() {
        PlayerUI.Instance.SetFactionColor(!PlayerUI.Instance.factionColoring);
    }

    BattleObject GetBattleObjectOverMouse() {
        BattleObject battleObject = null;
        float distance = float.MaxValue;
        foreach (Unit targetUnit in BattleManager.Instance.units) {
            if (!targetUnit.IsSelectable()) {
                continue;
            }
            float tempDistance = Vector2.Distance(GetMouseWorldPosition(), targetUnit.transform.position);
            if (tempDistance < targetUnit.GetSize() * Mathf.Max(1, (targetUnit).GetZoomIndicatorSize()) && tempDistance < distance) {
                battleObject = targetUnit;
                distance = tempDistance;
            }
        }
        List<BattleObject> battleObjects = new List<BattleObject>(new List<BattleObject>(BattleManager.Instance.stars));
        battleObjects.AddRange(BattleManager.Instance.planets);
        foreach (BattleObject targetObject in battleObjects) {
            if (!targetObject.IsSelectable()) {
                continue;
            }
            float tempDistance = Vector2.Distance(GetMouseWorldPosition(), targetObject.transform.position);
            if (tempDistance < targetObject.GetSize() && tempDistance < distance) {
                battleObject = targetObject;
                distance = tempDistance;
            }
        }
        return battleObject;
    }

    public virtual void UnitDestroyed(Unit unit) {
        if (GetDisplayedBattleObject() == unit)
            displayedBattleObject = null;
    }

    public PlayerInput GetPlayerInput() {
        return playerInput;
    }

    public virtual BattleObject GetDisplayedBattleObject() {
        return displayedBattleObject;
    }

    public Fleet GetDisplayedFleet() {
        return displayedFleet;
    }

    private void OnDisable() {
        playerInput.Disable();
    }

    public ActionType GetActionType() {
        return actionType;
    }

    public Vector2 GetMousePosition() {
        return playerInput.Player.MousePosition.ReadValue<Vector2>();
    }

    public Vector2 GetMouseWorldPosition() {
        return mainCamera.ScreenToWorldPoint(playerInput.Player.MousePosition.ReadValue<Vector2>());
    }

    public Camera GetCamera() {
        return mainCamera;
    }
}
