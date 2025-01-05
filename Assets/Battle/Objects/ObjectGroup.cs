using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectGroup<T> : IObject, IObjectGroupLink where T : BattleObject {
    public BattleManager battleManager { get; private set; }
    public HashSet<T> battleObjects { get; private set; }
    public Vector2 position { get; private set; }
    public Vector2 averagePosition { get; private set; }
    public float size { get; private set; }
    public bool deleteGroupWhenEmpty { get; private set; }

    public ObjectGroup() {
        battleObjects = new HashSet<T>(10);
    }

    public ObjectGroup(BattleManager battleManager, HashSet<T> objects, bool deleteGroupWhenEmpty, bool setupGroupPositionAndSize = true,
        bool changeSizeIndicatorPosition = false) {
        this.battleManager = battleManager;
        battleObjects = objects;
        this.deleteGroupWhenEmpty = deleteGroupWhenEmpty;
        if (setupGroupPositionAndSize)
            UpdateObjectGroup(changeSizeIndicatorPosition);
    }

    public void SetPosition(Vector2 position) {
        this.position = position;
    }

    public void UpdateObjectGroup(bool changeSizeIndicatorPosition = false) {
        CalculateObjectGroupCenters();
        size = CalculateObjectGroupSize();
    }


    void CalculateObjectGroupCenters() {
        Vector2 min = new Vector2(int.MaxValue, int.MaxValue);
        Vector2 max = new Vector2(int.MinValue, int.MinValue);
        Vector2 sum = Vector2.zero;
        foreach (var battleObject in battleObjects) {
            Vector2 tempPos = battleObject.GetPosition();
            sum += tempPos;
            min = Vector2.Min(min, tempPos);
            max = Vector2.Max(max, tempPos);
        }

        position = (min + max) / 2;
        averagePosition = sum / battleObjects.Count;
    }

    private float CalculateObjectGroupSize() {
        if (!battleObjects.Any()) return 0;
        return battleObjects.Max(battleObject => Vector2.Distance(position, battleObject.GetPosition()) + battleObject.GetSize());
    }

    /// <summary>
    /// Adds the BattleObject and calls AddGroup on it
    /// </summary>
    /// <param name="battleObject"></param>
    public virtual void AddBattleObject(BattleObject battleObject) {
        if (!battleObject.IsInGroup(this)) {
            battleObjects.Add((T)battleObject);
            battleObject.AddGroup(this);
        }
    }

    public virtual void RemoveBattleObject(BattleObject battleObject) {
        if (battleObject.IsInGroup(this)) {
            if (battleObjects.Count == 0)
                Debug.LogError("The group was already empty!");
            battleObjects.Remove((T)battleObject);
            battleObject.RemoveGroup(this);
            if (battleObjects.Count == 0 && deleteGroupWhenEmpty) {
                deleteGroupWhenEmpty = false;
            }
        }
    }

    public Vector2 GetPosition() {
        return position;
    }

    public Vector2 GetAveragePosition() {
        return averagePosition;
    }

    public float GetSize() {
        return size;
    }

    public bool IsGroup() {
        return true;
    }
}
