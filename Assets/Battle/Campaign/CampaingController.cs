using UnityEngine;

public abstract class CampaingController : MonoBehaviour {
    public float researchModifier;
    public float systemSizeModifier;
    public BattleManager battleManager { get; private set; }
    public EventManager eventManager { get; private set; }

    public virtual void SetupBattle(BattleManager battleManager) {
        this.battleManager = battleManager;
        eventManager = battleManager.eventManager;
    }
}
