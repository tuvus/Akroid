using UnityEngine;

public class UIManager : MonoBehaviour {
    public BattleManager battleManager { get; private set; }
    public LocalPlayer localPlayer { get; private set; }
    public UnitSpriteManager unitSpriteManager { get; private set; }
    public UIEventManager uIEventManager { get; private set; }

    /// <summary>
    /// Subscription to BattleManager events needs to occur before the system is created.
    /// Also inject the UIEventManager into BattleManager so that BattleManager doesn't create the non UI one.
    /// This method should be called before the BattleManager is set up.
    /// </summary>
    public void PreBattleManagerSetup(BattleManager battleManager) {
        this.battleManager = battleManager;
        unitSpriteManager = GetComponent<UnitSpriteManager>();
        unitSpriteManager.SetupUnitSpriteManager(battleManager, this);
        localPlayer = GameObject.Find("Player").GetComponent<LocalPlayer>();
        localPlayer.PreBattleManagerSetup(battleManager, this);
        uIEventManager = new UIEventManager(battleManager, localPlayer, localPlayer.GetLocalPlayerGameInput(), unitSpriteManager);
        battleManager.SetEventManager(uIEventManager);
    }

    /// <summary>
    /// Setup that should be done after the BattleManager is set up
    /// </summary>
    public void SetupUIManager() {
        localPlayer.SetUpPlayer();
    }

    public void LateUpdate() {
        if (battleManager.battleState != BattleManager.BattleState.Setup) unitSpriteManager.UpdateSpriteManager();
        localPlayer.GetLocalPlayerInput().UpdatePlayer();
        localPlayer.UpdatePlayer();
        uIEventManager.UpdateUIEvents();
    }

    /// <summary>
    /// Determines whether the effects will be shown or not.
    /// </summary>
    /// <param name="shown"></param>
    public void ShowEffects(bool shown) {
        // ShowParticles(shown && LocalPlayer.Instance.GetPlayerUI().particles);
        // foreach (var unit in units) {
        //     unit.ShowEffects(shown);
        // }
        // foreach (var projectile in projectiles) {
        //     projectile.ShowEffects(shown);
        // }
        // foreach (var missile in missiles) {
        //     missile.ShowEffects(shown);
        // }
        // foreach (var destroyedUnit in destroyedUnits) {
        //     destroyedUnit.ShowEffects(shown);
        // }
    }

    /// <summary>
    /// Determines whether or not the particles in the game are rendered or not.
    /// Will not be called with the same shown value twice in a row.
    /// </summary>
    /// <param name="shown"></param>
    public void ShowParticles(bool shown) {
        // foreach (var unit in units) {
        //     unit.ShowParticles(shown);
        // }
        // foreach (var projectile in projectiles) {
        //     projectile.ShowParticles(shown);
        // }
        // foreach (var missile in missiles) {
        //     missile.ShowParticles(shown);
        // }
        // foreach (var destroyedUnits in destroyedUnits) {
        //     destroyedUnits.ShowParticles(shown);
        // }
    }

    /// <summary>
    /// Determines whether all units and icorns should show their unique faction color
    /// or their diplomatic status.
    /// </summary>
    /// <param name="shown"></param>
    public void ShowFactionColoring(bool shown) {
        // foreach (var unit in units) {
        // unit.ShowFactionColor(shown);
        // }
    }

    public bool GetEffectsShown() {
        return PlayerUI.Instance.effects;
    }

    /// <summary>
    /// For particle emitters to figure out if they should emit when begging their emissions.
    /// </summary>
    /// <returns>whether or not the particles should be shown</returns>
    public bool GetParticlesShown() {
        return PlayerUI.Instance.effects && PlayerUI.Instance.particles;
    }

    public bool GetFactionColoringShown() {
        return PlayerUI.Instance.factionColoring;
    }

    public Transform GetFactionsTransform() {
        return transform.GetChild(0);
    }

    public Transform GetAsteroidFieldTransform() {
        return transform.GetChild(1);
    }

    public Transform GetGasCloudsTransform() {
        return transform.GetChild(2);
    }

    public Transform GetStarTransform() {
        return transform.GetChild(3);
    }

    public Transform GetPlanetsTransform() {
        return transform.GetChild(4);
    }

    public Transform GetProjectileTransform() {
        return transform.GetChild(5);
    }

    public Transform GetMissileTransform() {
        return transform.GetChild(6);
    }

    public Transform GetEventVisulationTransform() {
        return transform.GetChild(7);
    }
}
