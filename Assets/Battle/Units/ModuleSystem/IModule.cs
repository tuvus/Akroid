using UnityEngine;

public interface IModule {
    public Vector2 GetPosition();
    public float GetRotation();
    public float GetMinRotation();
    public float GetMaxRotation();
    public int GetSystemIndex();
}
