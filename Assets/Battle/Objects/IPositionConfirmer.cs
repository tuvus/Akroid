using UnityEngine;

public interface IPositionConfirmer {
    /// <summary>
    /// Confirms the position of the object, returns false if it is close to certain other object or true otherwise.
    /// </summary>
    /// <param name="position">the given position the object should be placed around</param>
    /// <param name="minDistanceFromObject">the given distance the object should be away from other objects</param>
    /// <returns>true if the position is valid, otherwise returns false </returns>
    public bool ConfirmPosition(Vector2 position, float minDistanceFromObject);

    public Vector2 GetPosition();

    public float GetSize();
}
