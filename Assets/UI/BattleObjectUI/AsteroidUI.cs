using UnityEngine;

public class AsteroidUI : BattleObjectUI {

    public Asteroid asteroid { get; private set; }

    public override void Setup(BattleObject battleObject, UIManager uIManager) {
        base.Setup(battleObject, uIManager);
        asteroid = (Asteroid)battleObject;
        spriteRenderer.sprite = asteroid.asteroidScriptableObject.sprite;

    }
}
