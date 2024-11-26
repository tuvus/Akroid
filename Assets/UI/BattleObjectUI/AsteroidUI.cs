using UnityEngine;

public class AsteroidUI : BattleObjectUI {

    public Asteroid asteroid { get; private set; }

    public override void Setup(BattleObject battleObject) {
        base.Setup(battleObject);
        asteroid = (Asteroid)battleObject;
        spriteRenderer.sprite = asteroid.asteroidScriptableObject.sprite;
    }

    public override void UpdateObject() {
    }
}
