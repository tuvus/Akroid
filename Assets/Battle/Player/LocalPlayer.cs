using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LocalPlayer should contain all the relevant information that any part of the simulation/game
/// below it needs to interact with the player. This allows the simulation/game to be oblivious
/// to the PlayerUI and it's functions.
/// </summary>
public class LocalPlayer : MonoBehaviour {
    public static LocalPlayer Instance { get; private set; }
    private LocalPlayerInput localPlayerInput;
    private PlayerUI playerUI;

    public enum PlayerState {
        free = 1,
        following = 2,
    }
    public Faction faction;
    public List<Unit> ownedUnits;
    public bool lockedOwnedUnits;

    public void SetUpPlayer() {
        if (Instance != null) {
            Destroy(this);
            return;
        }
        Instance = this;
        localPlayerInput = GetComponent<LocalPlayerInput>();
        playerUI = transform.GetChild(1).GetComponent<PlayerUI>();
        localPlayerInput.Setup();
        playerUI.SetUpUI(localPlayerInput);
        ownedUnits = new List<Unit>();
    }

    public void SetupFaction(Faction faction) {
        this.faction = faction;
        if (!lockedOwnedUnits) {
            if (faction != null) {
                ownedUnits = faction.units;
            } else {
                ownedUnits.Clear();
            }
        }
        UpdateFactionColors();
        playerUI.GetPlayerCommsManager().SetupFaction(faction);
    }

    /// <summary>
    /// Refreshes the colors of the unit displays to thier proper color.
    /// Call after the player faction or the player faction's enemies list is modified.
    /// </summary>
    public void UpdateFactionColors() {
        for (int i = 0; i < BattleManager.Instance.GetAllUnits().Count; i++) {
            BattleManager.Instance.GetAllUnits()[i].GetUnitSelection().UpdateFactionColor();
        }
    }

    public void UpdatePlayer() {
        playerUI.UpdatePlayerUI();
    }

    #region HelperMethods
    public void AddOwnedUnit(Unit unit) {
        ownedUnits.Add(unit);
    }

    public LocalPlayerInput GetInputManager() {
        return localPlayerInput;
    }

    public PlayerUI GetPlayerUI() {
        return playerUI;
    }

    public LocalPlayerInput GetLocalPlayerInput() {
        return localPlayerInput;
    }

    public Faction GetFaction() {
        return faction;
    }
    #endregion
}