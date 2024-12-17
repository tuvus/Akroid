using UnityEngine;

public abstract class CampaingController : MonoBehaviour {
    public BattleManager battleManager { get; private set; }
    public EventManager eventManager { get; private set; }
    public float researchModifier;
    public float systemSizeModifier;

    public virtual void SetupBattle(BattleManager battleManager) {
        this.battleManager = battleManager;
        eventManager = battleManager.eventManager;
    }

    public abstract string GetPathToChapterFolder();
}
