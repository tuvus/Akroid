using UnityEngine;

/// <summary>
/// LocalPlayer should contain all the relevant information that any part of the simulation/game
/// below it needs to interact with the player. This allows the simulation/game to be oblivious
/// to the PlayerUI and it's functions.
/// </summary>
public class LocalPlayer : MonoBehaviour {
    private BattleManager battleManager;
    private UnitSpriteManager unitSpriteManager;
    public Player player { get; private set; }
    public static LocalPlayer Instance { get; private set; }
    private LocalPlayerInput localPlayerInput;
    public PlayerUI playerUI { get; private set; }

    public enum PlayerState {
        free = 1,
        following = 2,
    }

    public void PreBattleManagerSetup(BattleManager battleManager, UnitSpriteManager unitSpriteManager) {
        this.battleManager = battleManager;
        this.unitSpriteManager = unitSpriteManager;
        localPlayerInput = GetComponent<LocalPlayerInput>();
        playerUI = transform.GetChild(1).GetComponent<PlayerUI>();
    }

    public void SetUpPlayer() {
        if (Instance != null) {
            Destroy(this);
            return;
        }
        player = battleManager.GetLocalPlayer();
        player.OnFactionChanged += SetupFaction;
        Instance = this;
        localPlayerInput.Setup(this, unitSpriteManager);
        playerUI.SetUpUI(localPlayerInput, this, unitSpriteManager);
        SetupFaction(player.faction);
        localPlayerInput.CenterCamera();
    }

    public void SetupFaction(Faction faction) {
        UpdateFactionColors();
        playerUI.playerCommsManager.SetupFaction(faction);
    }

    /// <summary>
    /// Refreshes the colors of the unit displays to thier proper color.
    /// Call after the player faction or the player faction's enemies list is modified.
    /// </summary>
    public void UpdateFactionColors() {
        foreach (var unit in BattleManager.Instance.units) {
            // unit.GetUnitSelection().UpdateFactionColor();
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
        if (player.ownedUnits.Contains(unit))
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

    public LocalPlayerInput GetInputManager() {
        return localPlayerInput;
    }

    public PlayerUI GetPlayerUI() {
        return playerUI;
    }

    public LocalPlayerInput GetLocalPlayerInput() {
        return localPlayerInput;
    }

    public LocalPlayerGameInput GetLocalPlayerGameInput() {
        return (LocalPlayerGameInput)localPlayerInput;
    }

    public Faction GetFaction() {
        return player.faction;
    }

    public FactionUI GetFactionUI() {
        return unitSpriteManager.factionUIs[player.faction];
    }

    #endregion
}
