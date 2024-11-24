using UnityEngine;

public abstract class CampaingController : MonoBehaviour {
    public BattleManager battleManager { get; private set; }
    public EventManager eventManager { get; private set; }
    public float researchModifier;
    public float systemSizeModifier;

    public virtual void SetupBattle(BattleManager battleManager) {
        this.battleManager = battleManager;
        eventManager = new EventManager();
    }

    public virtual void UpdateController(float deltaTime) {
        eventManager.UpdateEvents(deltaTime);
    }

    public abstract string GetPathToChapterFolder();
}
