using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

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

    protected Unit mouseOverUnit;
    protected Unit leftClickedUnit;
    protected Unit displayedUnit;
    protected Fleet displayedFleet;
    protected Unit rightClickedUnit;
    protected Unit followUnit;

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

        playerInput.Enable();
        timeStepIndex = 1;
    }

    public virtual void ChangeFaction() {
        displayedUnit = null;
        leftClickedUnit = null;
        rightClickedUnit = null;
        StopFollowingUnit();
        mouseOverUnit = null;
    }

    public virtual void UpdatePlayer() {
        mouseOverUnit = GetUnitOverMouse();
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
            float diference = mainCamera.orthographicSize - targetSize;
            MoveCamera((GetMouseWorldPosition() - (Vector2)mainCamera.transform.position) * diference / mainCamera.orthographicSize);
        }
        mainCamera.orthographicSize = targetSize;

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
        } else if (displayedUnit != null) {
            SetCameraPosition(displayedUnit.GetPosition());
        } else if (LocalPlayer.Instance.GetFaction() != null && LocalPlayer.Instance.GetFaction().stations.Count > 0) {
            SetCameraPosition(LocalPlayer.Instance.GetFaction().stations[0].transform.position);
        }
    }

    protected virtual void PrimaryMouseDown() {
        if (primaryMousePressed == true || LocalPlayer.Instance.GetPlayerUI().IsAMenueShown())
            return;
        primaryMousePressed = true;
        leftClickedUnit = mouseOverUnit;
    }

    protected virtual void PrimaryMouseHeld() {

    }

    protected virtual void PrimaryMouseUp() {
        primaryMousePressed = false;
        if (LocalPlayer.Instance.GetPlayerUI().IsAMenueShown())
            return;
        displayedUnit = null;
        if (leftClickedUnit != null) {
            displayedUnit = leftClickedUnit;
        }
    }

    protected virtual void SecondaryMouseDown() {
        secondaryMousePressed = true;
        pastMousePosition = GetMousePosition();
        rightClickedUnit = mouseOverUnit;
    }

    protected virtual void SecondaryMouseHeld() {
        if (!LocalPlayer.Instance.GetPlayerUI().IsAMenueShown())
            MoveCamera((pastMousePosition - GetMousePosition()) * mainCamera.orthographicSize / 422);
    }

    protected virtual void SecondaryMouseUp() {
        secondaryMousePressed = false;
        if (!LocalPlayer.Instance.GetPlayerUI().IsAMenueShown()) {
            if (rightClickedUnit != null && rightClickedUnit == mouseOverUnit) {
                if (rightClickedUnit.IsShip()) {
                    LocalPlayer.Instance.GetPlayerUI().SetDisplayShip((Ship)rightClickedUnit);
                    rightClickedUnit = null;
                } else if (rightClickedUnit.IsStation()) {
                    LocalPlayer.Instance.GetPlayerUI().SetDisplayStation((Station)rightClickedUnit);
                    rightClickedUnit = null;
                }
            }
        } else {
            LocalPlayer.Instance.GetPlayerUI().CloseAllMenues();
        }
    }

    void SlowdownSimulationButtonPressed() {
        timeStepIndex = Mathf.Max(0, timeStepIndex - 1);
        Time.timeScale = timeSteps[timeStepIndex];
    }

    void SpeedupSimulationButtonPressed() {
        timeStepIndex = Mathf.Min(timeSteps.Length - 1, timeStepIndex + 1);
        Time.timeScale = timeSteps[timeStepIndex];
    }

    public void StopSimulationButtonPressed() {
        timeStepIndex = 0;
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
            LocalPlayer.Instance.GetPlayerUI().CloseAllMenues();
            return;
        }
        LocalPlayer.Instance.GetPlayerUI().ToggleMenueUI();
    }

    void FollowUnitButtonPressed() {
        if (followUnit != null) {
            StopFollowingUnit();
            return;
        }
        if (displayedUnit != null) {
            StartFollowingUnit(displayedUnit);
        }
    }

    void ToggleUnitZoomIndicators() {
        LocalPlayer.Instance.GetPlayerUI().ToggleUnitZoomIndicators();
    }

    Unit GetUnitOverMouse() {
        Unit unit = null;
        float distance = float.MaxValue;
        for (int i = 0; i < BattleManager.Instance.GetAllUnits().Count; i++) {
            Unit targetUnit = BattleManager.Instance.GetAllUnits()[i];
            if (!targetUnit.IsSelectable()) {
                continue;
            }
            float tempDistance = Vector2.Distance(GetMouseWorldPosition(), targetUnit.transform.position);
            if (tempDistance < targetUnit.GetSize() * Mathf.Max(1, targetUnit.GetZoomIndicatorSize()) && tempDistance < distance) {
                unit = targetUnit;
                distance = tempDistance;
            }
        }
        return unit;
    }

    public virtual void UnitDestroyed(Unit unit) {
        if (GetDisplayedUnit() == unit)
            displayedUnit = null;
    }

    public PlayerInput GetPlayerInput() {
        return playerInput;
    }

    public virtual Unit GetDisplayedUnit() {
        return displayedUnit;
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
