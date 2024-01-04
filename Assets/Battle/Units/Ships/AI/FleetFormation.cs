using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using static Command;

public class FleetFormation {
    public enum FormationType {
        Line,
        Circular,
        VerticalOval,
        HorizontalOval,
    }

    public FormationType formationType;

    public static (List<Ship>,List<Vector2>) GetFormationShipPosition(Fleet fleet, Vector2 position, float rotation, float spread, FormationType formationType) {
        List<Vector2> shipPositions = new List<Vector2>();
        List<Ship> shipsSorted = new List<Ship>(fleet.GetShips()).OrderBy((ship) => ship.GetMaxHealth()).ToList();
        if (shipsSorted.Count == 1) {
            shipPositions.Add(position);
            return (shipsSorted, shipPositions);
        }
        if (formationType == FormationType.Line) {
            float shipSize = fleet.GetMaxShipSize();
            Vector2 startPosition = position - Calculator.GetPositionOutOfAngleAndDistance(rotation - 90, spread + shipSize * fleet.GetShips().Count);
            Vector2 endPosition = position - Calculator.GetPositionOutOfAngleAndDistance(rotation + 90, spread + shipSize * fleet.GetShips().Count);
            for (int i = shipsSorted.Count - 2; i >= 0; i-=2) {
                Ship ship = shipsSorted[i];
                shipsSorted.RemoveAt(i);
                shipsSorted.Add(ship);
            }
            for (int i = 0; i < shipsSorted.Count; i++) {
                shipPositions.Add(Vector2.Lerp(startPosition, endPosition, i / (float)(fleet.GetShips().Count - 1)));
            }
        }
        return (shipsSorted, shipPositions);
    }
}
