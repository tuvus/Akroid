using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPositionConfirmer {

   public bool ConfirmPosition(Vector2 position, float minDistanceFromObject);
}
