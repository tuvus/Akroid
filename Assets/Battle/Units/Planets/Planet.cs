using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planet : BattleObject, IPositionConfirmer {
    public string planetName { get; protected set; }
    public Faction faction { get; protected set; }

    public void SetupPlanet(string name, Faction faction, BattleManager.PositionGiver positionGiver, float rotation) {
        this.faction = faction;
        base.SetupBattleObject(positionGiver, rotation);
        this.planetName = name;
    }

    protected override Vector2 GetSetupPosition(BattleManager.PositionGiver positionGiver) {
        if (positionGiver.isExactPosition)
            return positionGiver.position;
        Vector2? targetPosition = BattleManager.Instance.FindFreeLocationIncrament(positionGiver, this);
        if (targetPosition.HasValue)
            return targetPosition.Value;
        return positionGiver.position;
    }

    bool IPositionConfirmer.ConfirmPosition(Vector2 position, float minDistanceFromObject) {
        foreach (var star in BattleManager.Instance.stars) {
            if (Vector2.Distance(position, star.position) <= minDistanceFromObject + star.GetSize() + size) {
                return false;
            }
        }
        foreach (var asteroidField in BattleManager.Instance.asteroidFields) {
            if (Vector2.Distance(position, asteroidField.GetPosition()) <= minDistanceFromObject + asteroidField.GetSize() + size) {
                return false;
            }
        }
        foreach (var station in BattleManager.Instance.stations) {
            if (Vector2.Distance(position, station.GetPosition()) <= minDistanceFromObject + station.GetSize() + size) {
                return false;
            }
        }
        return true;
    }

    public override float GetSpriteSize() {
        return spriteRenderer.sprite.bounds.size.x / 2;
    }
}
