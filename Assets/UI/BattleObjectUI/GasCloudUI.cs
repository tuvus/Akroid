public class GasCloudUI : BattleObjectUI {
    public GasCloud gasCloud { get; private set; }

    public override void Setup(BattleObject battleObject) {
        base.Setup(battleObject);
        this.gasCloud = (GasCloud)battleObject;
        spriteRenderer.sprite = gasCloud.gasCloudScriptableObject.sprite;
        spriteRenderer.color = gasCloud.color;
    }
}
