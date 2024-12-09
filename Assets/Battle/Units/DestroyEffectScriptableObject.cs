using UnityEngine;

[CreateAssetMenu(fileName = "DestroyEffect", menuName = "Effects/DestroyEffect", order = 1)]
public class DestroyEffectScriptableObject : ScriptableObject {
    [Tooltip("The time until the flare reaches the max flare up size.")]
    public float flareUpSpeed;
    [Tooltip("The time until the flare reaches the normal size after the flareup.")]
    public float flareUpFadeSpeed;
    [Tooltip("The time the flare stays at its normal size before fading out.")]
    public float flareNormalSpeed;
    [Tooltip("The time until the flare fades completely out.")]
    public float flareFadeSpeed;
    public float flareSizeMult;
    [Tooltip("The size of the maxiumum flare up size multipied to the base size.")]
    public float flareUpSizeMult;
    public float scrapSizeMult;
    public float explosionSizeMult;
    public Color explosionColor;
}
