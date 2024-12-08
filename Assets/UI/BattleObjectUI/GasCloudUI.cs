public class GasCloudUI : BattleObjectUI {
    public GasCloud gasCloud { get; private set; }

    public override void Setup(BattleObject battleObject, UIManager uIManager) {
        base.Setup(battleObject, uIManager);
        this.gasCloud = (GasCloud)battleObject;
        spriteRenderer.sprite = gasCloud.gasCloudScriptableObject.sprite;
        spriteRenderer.color = gasCloud.color;
    }
}
