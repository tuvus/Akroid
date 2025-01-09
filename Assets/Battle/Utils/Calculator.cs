using UnityEngine;

public static class Calculator {
    /// <summary>
    /// Returns 180 to -180 deg rotation where 0 is upwards and 90 is to the left.
    /// </summary>
    public static float GetAngleOutOfPosition(Vector2 pos) {
        float angleRad = Mathf.Atan2(pos.x, pos.y);
        float angleDeg = (180 / Mathf.PI) * angleRad;
        return -angleDeg;
    }

    /// <summary>
    /// Returns 180 to -180 deg rotation where 0 is upwards. and 90 is to the left.
    /// </summary>
    /// <param name="from">The from position</param>
    /// <param name="to">The to position</param>
    /// <returns></returns>
    public static float GetAngleOutOfTwoPositions(Vector2 from, Vector2 to) {
        return GetAngleOutOfPosition(to - from);
    }

    /// <summary>
    /// Returns distance from an angle in degrees and a distance.
    /// </summary>
    /// <param name="angle">Angle in degrees from 0 to 360</param>
    /// <param name="distance">Distance to point</param>
    /// <returns></returns>
    public static Vector2 GetPositionOutOfAngleAndDistance(float angle, float distance) {
        return new Vector2(-Mathf.Sin(angle * Mathf.Deg2Rad) * distance, Mathf.Cos(angle * Mathf.Deg2Rad) * distance);
    }

    /// <summary>
    /// Takes in 0 to 360 deg rotation and converts to -180 to 180 deg rotation.
    /// </summary>
    public static float ConvertTo180DegRotation(float rotation) {
        SimplifyRotation360(rotation);
        if (rotation > 180) {
            rotation = -180 + (rotation - 180);
        }

        if (rotation < -180) {
            rotation = 180 + (rotation + 180);
        }

        return rotation;
    }

    /// <summary>
    /// Takes in any deg rotation and converts to 0 to 360 deg rotation.
    /// </summary>
    public static float ConvertTo360DegRotation(float rotation) {
        rotation = SimplifyRotation360(rotation);
        if (rotation < 0)
            rotation = 180 + (rotation + 180);
        return rotation;
    }

    /// <summary>
    /// Gets the distance to the relative position.
    /// </summary>
    public static float GetDistanceToPosition(Vector2 pos) {
        return Vector2.Distance(Vector2.zero, pos);
    }

    /// <summary>
    /// Converts a local position to the world's position given the parents position and rotation.
    /// </summary>
    public static Vector2 ConvertLocalPositionToWorld(Vector2 parentPos, float parentRot, Vector2 localPos) {
        parentRot -= 180;
        if (parentRot < 0) parentRot += 360;
        float radians = parentRot * Mathf.Deg2Rad;
        return new Vector2(parentPos.x + localPos.y * Mathf.Sin(radians) - localPos.x * Mathf.Cos(radians),
            parentPos.y - localPos.y * Mathf.Cos(radians) - localPos.x * Mathf.Sin(radians));
    }

    /// <summary>
    /// Simplifies the rotation so that it is not higher or lower than 360,-360.
    /// </summary>
    public static float SimplifyRotation360(float rotation) {
        if (rotation > 360) {
            rotation = rotation % 360;
        } else if (rotation < 0) {
            rotation = -(-rotation % 360);
        }

        return rotation;
    }

    public static float SimplifyPositiveRotation360(float rotation) {
        if (rotation > 360) {
            rotation = rotation % 360;
        } else if (rotation < 0) {
            rotation = -(-rotation % 360) + 360;

        }
        return rotation;
    }

    public static Vector2 GetClosestPointToAPointOnALine(Vector2 lineOrigin, float lineRotation, Vector2 point) {
        Vector2 lineVector = GetPositionOutOfAngleAndDistance(lineRotation, 1);
        Vector2 vectorToPoint = point - lineOrigin;
        return lineOrigin + lineVector * Vector2.Dot(lineVector, vectorToPoint) / Vector2.Dot(lineVector, lineVector);
    }

    /// <summary>
    /// Returns the target rotation relative to the current rotation. Input both as 360, -360 degrees.
    /// </summary>
    public static float GetLocalTargetRotation(float currentRotation, float targetRotation) {
        if (currentRotation > 180) currentRotation = -180 + (currentRotation - 180);

        if (targetRotation > 180) targetRotation = -180 + (targetRotation - 180);

        if (targetRotation < -180) targetRotation = 180 + (targetRotation + 180);

        targetRotation -= currentRotation;
        if (targetRotation > 180) targetRotation = -180 + (targetRotation - 180);

        if (targetRotation < -180) targetRotation = 180 + (targetRotation + 180);

        return targetRotation;
    }

    /// <summary>
    /// Returns true if the relativePosition is in the field of view of the turret's firing angle.
    /// </summary>
    public static bool IsTargetInSight(float currentShipRotation, Vector2 relativeTargetPos, float minRotate, float maxRotate) {
        float realDeg = GetAngleOutOfPosition(relativeTargetPos);
        realDeg -= currentShipRotation;
        realDeg = SimplifyRotation360(realDeg);
        if (minRotate < maxRotate) {
            if (realDeg <= maxRotate && realDeg >= minRotate) {
                return true;
            }

            return false;
        }

        if (minRotate > maxRotate) {
            if (realDeg <= maxRotate || realDeg >= minRotate) {
                return true;
            }

            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the local target position when the target is in range of a direct line to it
    /// </summary>
    public static Vector2 GetTargetPositionIntersect(Vector2 relativeTargetPos, Vector2 localVelocity, float projectileVelocity) {
        return -relativeTargetPos;
        //float incrementValue = 1f;
        //float calculatedTime = 0;
        //while (true) {
        //    Vector2 localTargetPosition = FindLocalPositionAfterTime(relativeTargetPos, localVelocity, calculatedTime);
        //    if (GetDistanceToPosition(localTargetPosition) <= projectileVelocity * calculatedTime) {
        //        calculatedTime -= incrementValue;
        //        incrementValue /= 10;
        //    }
        //    if (incrementValue <= 0.0001f) {
        //        return -localTargetPosition;
        //    }
        //    calculatedTime += incrementValue;
        //}
    }

    /// <summary>
    /// Returns the localTargetPosition over time from the velocity
    /// </summary>
    public static Vector2 FindLocalPositionAfterTime(Vector2 localTargetPosition, Vector2 localVelocity, float time) {
        return localTargetPosition + (localVelocity * time);
    }

    /// <summary>
    /// Gets the closest way to rotate to an angle with a deadzone.
    /// A deadzone is the area that the turret can't rotate to or through.
    /// </summary>
    public static float GetRotationWithDeadzone(float localRotation, float localTargetRotation, float minRotate, float maxRotate) {
        if (localRotation != localTargetRotation) {
            float localMin = SimplifyRotation360(minRotate - localRotation);
            if (localMin > 180) {
                localMin = -360 + localMin;
            }

            float localMax = SimplifyRotation360(maxRotate - localRotation);
            if (localMax > 180) {
                localMax = -360 + localMax;
            }

            if (minRotate < maxRotate) {
                if (localMax > 0 && localMin > 0 && localMax < localTargetRotation && localTargetRotation > 0) {
                    return -360 + localTargetRotation;
                }

                if (localMax < 0 && localMin < 0 && localMin > localTargetRotation && localTargetRotation > 0) {
                    return 360 + localTargetRotation;
                }
            }

            if (minRotate > maxRotate) {
                if (localMax > 0 && localMin > 0 && localMin < localTargetRotation && localTargetRotation > 0) {
                    return -360 + localTargetRotation;
                }

                if (localMax < 0 && localMin < 0 && localMax > localTargetRotation && localTargetRotation < 0) {
                    return 360 + localTargetRotation;
                }
            }
        }

        return localTargetRotation;
    }

    public static Vector2 GetTargetPositionAfterTimeAndVelocity(Vector2 position, Vector2 targetPosition, Vector2 velocity,
        Vector2 targetVelocity, float fireVelocity, float offSet) {
        Vector2 targetLocalPosition = position - targetPosition;
        Vector2 localVelocity = velocity - targetVelocity;
        if (localVelocity.magnitude < 1)
            return targetPosition;

        float calculatedTime = (targetLocalPosition.magnitude - offSet) / fireVelocity;
        Vector2 localTargetPos = targetLocalPosition;
        for (int i = 0; i < 6; i++) {
            localTargetPos = FindLocalPosAfterTime(targetLocalPosition, localVelocity, calculatedTime);
            float targetDist = localTargetPos.magnitude - offSet;
            float bulletDist = fireVelocity * calculatedTime;
            if (Mathf.Abs(targetDist - bulletDist) <= .001) {
                return position - localTargetPos;
            }

            calculatedTime = (localTargetPos.magnitude - offSet) / fireVelocity;
        }

        return position - localTargetPos;
    }

    public static Vector2 FindLocalPosAfterTime(Vector2 targetPosition, Vector2 localVelocity, float time) {
        return targetPosition + (localVelocity * time);
    }

    public static float GetSpriteSize(Sprite sprite, Vector2 scale) {
        return Mathf.Max(sprite.rect.size.x / 2, sprite.rect.size.y / 2) / sprite.pixelsPerUnit;
    }
}
