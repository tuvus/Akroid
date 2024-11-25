using UnityEngine;

public class UIManager : MonoBehaviour {
    public BattleManager battleManager { get; private set; }
    public LocalPlayer localPlayer { get; private set; }

    public void SetupUIManager(BattleManager battleManager) {
        this.battleManager = battleManager;
        localPlayer = GameObject.Find("Player").GetComponent<LocalPlayer>();
        localPlayer.SetUpPlayer(battleManager);
    }
}
