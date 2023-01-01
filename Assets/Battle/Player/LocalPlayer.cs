using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    public void UpdateFactionColors() {
        for (int i = 0; i < BattleManager.Instance.GetAllUnits().Count; i++) {
            BattleManager.Instance.GetAllUnits()[i].GetUnitSelection().UpdateFactionColor();
        }
    }

    public void UpdatePlayer() {
        playerUI.UpdatePlayerUI();
    }

    public void AddOwnedUnit(Unit unit) {
        ownedUnits.Add(unit);
    }

    #region GetMethods
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