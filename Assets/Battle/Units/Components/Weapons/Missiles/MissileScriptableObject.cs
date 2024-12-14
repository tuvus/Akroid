using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Missile", menuName = "Components/Missile", order = 4)]
public class MissileScriptableObject : ScriptableObject {
    public Missile.MissileType type;
    public int damage;
    public float thrust;
    public float turnSpeed;
    public float fuelRange;
    public bool retarget;
    public Sprite sprite;
    public DestroyEffectScriptableObject destroyEffect;
    public float timeAfterExpire;
}
