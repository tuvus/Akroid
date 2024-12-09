public class DestroyEffect {
    private DestroyEffectScriptableObject destroyEffectScriptableObject;
    public FlareState flareState { get; private set; }
    public float flareTime { get; private set; }

    public enum FlareState {
        FlaringUp,
        FadeToNormal,
        KeepNormal,
        Fade,
        End,
    }


    public DestroyEffect(DestroyEffectScriptableObject destroyEffectScriptableObject) {
        this.destroyEffectScriptableObject = destroyEffectScriptableObject;
        flareTime = 0;
        flareState = FlareState.FlaringUp;
        if (this.destroyEffectScriptableObject.flareUpSpeed == 0) flareState = FlareState.FadeToNormal;
    }

    /// <summary>
    /// Updates the state and time on the destroyed effect.
    /// </summary>
    /// <returns>True if the effect is active, false otherwise.</returns>
    public bool UpdateDestroyEffect(float deltaTime) {
        if (flareState == FlareState.End) return false;
        flareTime += deltaTime;
        switch (flareState) {
            case FlareState.FlaringUp:
                if (flareTime < destroyEffectScriptableObject.flareUpSpeed) return true;
                flareTime -= destroyEffectScriptableObject.flareUpSpeed;
                flareState = FlareState.FadeToNormal;
                break;
            case FlareState.FadeToNormal:
                if (flareTime < destroyEffectScriptableObject.flareUpFadeSpeed) return true;
                flareTime -= destroyEffectScriptableObject.flareUpFadeSpeed;
                flareState = FlareState.KeepNormal;
                break;
            case FlareState.KeepNormal:
                if (flareTime < destroyEffectScriptableObject.flareNormalSpeed) return true;
                flareTime -= destroyEffectScriptableObject.flareNormalSpeed;
                flareState = FlareState.Fade;
                break;
            case FlareState.Fade:
                if (flareTime < destroyEffectScriptableObject.flareFadeSpeed) return true;
                flareState = FlareState.End;
                return false;
        }

        return true;
    }
}
