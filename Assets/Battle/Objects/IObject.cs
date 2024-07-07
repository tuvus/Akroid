using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines the basic properties of an object in the game.
/// This is required in order to have ObjectGroups and Objects in the same list.
/// </summary>
public interface IObject {
    public float GetSize();
    public Vector2 GetPosition();
    public bool IsGroup() { return false; }
}
