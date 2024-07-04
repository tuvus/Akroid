using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public HashSet<Unit> ownedUnits;
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
        ownedUnits = new HashSet<Unit>();
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
        foreach (var unit in BattleManager.Instance.units) {
            unit.GetUnitSelection().UpdateFactionColor();
        }
    }

    public void UpdatePlayer() {
        playerUI.UpdatePlayerUI();
    }

    #region RelationsAndColors
    public enum RelationType {
        Neutral = 0,
        Enemy = 1,
        Friendly = 2,
        Owned = 3,
    }
    Color neutralColor = new Color(1, 1, 1);
    Color friendlyColor = new Color(0, 1, 0);
    Color enemyColor = new Color(1, 0.4f, 0.35f);
    Color ownedColor = new Color(0, 1f, 1f);


    public RelationType GetRelationToUnit(Unit unit) {
        if (GetFaction() == null)
            return RelationType.Neutral;
        if (ownedUnits.Contains(unit))
            return RelationType.Owned;
        if (GetFaction() == unit.faction)
            return RelationType.Friendly;
        if (GetFaction().IsAtWarWithFaction(unit.faction))
            return RelationType.Enemy;
        return RelationType.Neutral;
    }

    public RelationType GetRelationToFaction(Faction faction) {
        if (GetFaction() == null)
            return RelationType.Neutral;
        if (GetFaction() == faction)
            return RelationType.Friendly;
        if (GetFaction().IsAtWarWithFaction(faction))
            return RelationType.Enemy;
        return RelationType.Neutral;
    }

    public Color GetColorOfRelationType(RelationType relationType) {
        switch (relationType) {
            case RelationType.Enemy:
            return enemyColor;
            case RelationType.Friendly:
            return friendlyColor;
            case RelationType.Owned:
            return ownedColor;
            default:
            return neutralColor;
        }
    }

    #endregion

    #region HelperMethods
    public void AddOwnedUnit(Unit unit) {
        ownedUnits.Add(unit);
        unit.GetUnitSelection().UpdateFactionColor();
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