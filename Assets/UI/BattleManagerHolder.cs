using UnityEngine;

/// <summary>
/// A simple class to add the BattleManager to the scene without making it a MonoBehaviour.
/// This allows us to instantiate the BattleManager easily in tests and removes its dependency from unity-specific c#.
/// </summary>
public class BattleManagerHolder : MonoBehaviour {
    public BattleManager battleManager;

    public void FixedUpdate() {
        battleManager.UpdateBattle();
    }
}
