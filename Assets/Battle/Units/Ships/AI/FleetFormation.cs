using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BattleManager;

public class FleetFormation {
    public enum FormationType {
        Line,
        Circle,
        VerticalOval,
        HorizontalOval,
    }

    public FormationType formationType;

    public static (List<Ship>, List<Vector2>) GetFormationShipPosition(Fleet fleet, Vector2 position, float rotation, float spread,
        FormationType formationType) {
        List<Ship> shipsSorted = new List<Ship>(fleet.GetShips()).OrderBy((ship) => ship.GetMaxHealth()).ToList();
        if (shipsSorted.Count == 1) {
            return (shipsSorted, new List<Vector2> { position });
        }

        if (formationType == FormationType.Line) {
            return GetLineFormation(shipsSorted, fleet, position, rotation, spread);
        } else if (formationType == FormationType.Circle) {
            return GetOvalFormation(shipsSorted, fleet, position, rotation, spread, Vector2.one);
        } else if (formationType == FormationType.VerticalOval) {
            return GetOvalFormation(shipsSorted, fleet, position, rotation, spread, new Vector2(.5f, 1));
        } else if (formationType == FormationType.HorizontalOval) {
            return GetOvalFormation(shipsSorted, fleet, position, rotation, spread, new Vector2(1, .5f));
        }

        return (null, null);
    }

    private static (List<Ship>, List<Vector2>) GetLineFormation(List<Ship> shipsSorted, Fleet fleet, Vector2 position, float rotation,
        float spread) {
        List<Vector2> shipPositions = new List<Vector2>();
        float shipSize = fleet.GetMaxShipSize();
        Vector2 startPosition =
            position - Calculator.GetPositionOutOfAngleAndDistance(rotation - 90, spread + shipSize * fleet.GetShips().Count);
        Vector2 endPosition =
            position - Calculator.GetPositionOutOfAngleAndDistance(rotation + 90, spread + shipSize * fleet.GetShips().Count);
        for (int i = shipsSorted.Count - 2; i >= 0; i -= 2) {
            Ship ship = shipsSorted[i];
            shipsSorted.RemoveAt(i);
            shipsSorted.Add(ship);
        }

        for (int i = 0; i < shipsSorted.Count; i++) {
            shipPositions.Add(Vector2.Lerp(startPosition, endPosition, i / (float)(fleet.GetShips().Count - 1)));
        }

        return (shipsSorted, shipPositions);
    }

    private static (List<Ship>, List<Vector2>) GetOvalFormation(List<Ship> shipsSorted, Fleet fleet, Vector2 position, float rotation,
        float spread, Vector2 scalar) {
        shipsSorted.Reverse();
        List<Vector2> shipPositions = new List<Vector2>();
        PositionGiver positionGiver = new PositionGiver(position, 0, 500, 10, 10, 5);
        for (int i = 0; i < shipsSorted.Count; i++) {
            Vector2? newPosition = FindFreeShipLocationIncrement(positionGiver, shipsSorted, shipPositions, scalar);
            if (newPosition.HasValue) {
                shipPositions.Add(newPosition.Value);
            } else {
                shipPositions.Add(position);
            }
        }

        return (shipsSorted, shipPositions);
    }

    public static Vector2? FindFreeShipLocationIncrement(PositionGiver positionGiver, List<Ship> shipsSorted, List<Vector2> shipPositions,
        Vector2 scalar) {
        float distance = positionGiver.minDistance;
        if (positionGiver.numberOfTries == 0) return positionGiver.position;
        while (true) {
            Vector2? targetPosition = FindFreeShipLocation(positionGiver, distance, distance + positionGiver.incrementDistance, shipsSorted,
                shipPositions, scalar);
            if (targetPosition.HasValue) {
                return targetPosition.Value;
            }

            distance += positionGiver.incrementDistance;
            if (distance > (positionGiver.maxDistance - positionGiver.incrementDistance)) {
                return null;
            }
        }
    }

    public static Vector2? FindFreeShipLocation(PositionGiver positionGiver, float minRange, float maxRange, List<Ship> shipsSorted,
        List<Vector2> shipPositions, Vector2 scalar) {
        for (int i = 0; i < positionGiver.numberOfTries; i++) {
            float distance = Random.Range(minRange, maxRange);
            Vector2 tryPos = positionGiver.position +
                             Calculator.GetPositionOutOfAngleAndDistance(Random.Range(0f, 360f), distance) * scalar;
            if (ConfirmShipLocation(tryPos, positionGiver.distanceFromObject, shipsSorted, shipPositions)) {
                return tryPos;
            }
        }

        return null;
    }

    public static bool ConfirmShipLocation(Vector2 pos, float distanceFromObject, List<Ship> shipsSorted, List<Vector2> shipPositions) {
        Ship ship = shipsSorted[shipPositions.Count];
        for (int i = 0; i < shipPositions.Count; i++) {
            Ship targetShip = shipsSorted[i];
            if (Vector2.Distance(pos, shipPositions[i]) < ship.GetSize() + targetShip.GetSize() + distanceFromObject) {
                return false;
            }
        }

        return true;
    }

    public static FormationType ChooseRandomFormation() {
        int value = Random.Range(0, 5);
        if (value == 0) {
            return FormationType.Circle;
        } else if (value == 1) {
            return FormationType.VerticalOval;
        }

        return FormationType.HorizontalOval;
    }
}
