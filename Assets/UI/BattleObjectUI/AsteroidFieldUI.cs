public class AsteroidFieldUI : ObjectUI {
    public AsteroidField asteroidField { get; private set; }

    public void Setup(AsteroidField asteroidField) {
        base.Setup(asteroidField);
        this.asteroidField = asteroidField;
        transform.position = asteroidField.GetPosition();
    }

    public override void UpdateObject() { }

    public override bool IsSelectable() {
        return false;
    }
}
