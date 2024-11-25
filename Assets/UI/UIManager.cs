using UnityEngine;

public class UIManager : MonoBehaviour {
    public BattleManager battleManager { get; private set; }
    public LocalPlayer localPlayer { get; private set; }
    public UnitSpriteManager unitSpriteManager { get; private set; }

    public void SetupUIManager(BattleManager battleManager) {
        this.battleManager = battleManager;
        localPlayer = GameObject.Find("Player").GetComponent<LocalPlayer>();
        localPlayer.SetUpPlayer(battleManager);
        unitSpriteManager = GetComponent<UnitSpriteManager>();
        unitSpriteManager.SetupUnitSpriteManager(battleManager, this);
    }

    public void LateUpdate() {
        localPlayer.GetLocalPlayerInput().UpdatePlayer();
        localPlayer.UpdatePlayer();
        if (battleManager.battleState != BattleManager.BattleState.Setup) unitSpriteManager.UpdateSpriteManager();
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
