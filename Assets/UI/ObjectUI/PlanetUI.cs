public class PlanetUI : BattleObjectUI {
    public Planet planet { get; private set; }
    public override void Setup(BattleObject battleObject, UIManager uIManager) {
        base.Setup(battleObject, uIManager);
        planet = (Planet)battleObject;
        spriteRenderer.sprite = planet.planetScriptableObject.sprite;
        if (!planet.planetScriptableObject.hasAtmosphere) {
            for (int i = 0; i < transform.childCount; i++) {
                transform.GetChild(i).gameObject.SetActive(false);
            }
        }
    }
}
