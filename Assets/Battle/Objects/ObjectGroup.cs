using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectGroup<T> : MonoBehaviour where T : BattleObject {
    [SerializeField] List<T> battleObjects;
    [SerializeField] Vector2 position;
    [SerializeField] Vector2 averagePosition;
    [SerializeField] float size;
    //public Transform sizeIndicator { get; private set; }

    public void SetupObjectGroup(List<T> objects, bool setupGroupPositionAndSize = true, bool changeSizeIndicatorPosition = false) {
        battleObjects = objects;
        //sizeIndicator = Instantiate(BattleManager.GetSizeIndicatorPrefab(), transform).transform;
        if (setupGroupPositionAndSize)
            UpdateObjectGroup(changeSizeIndicatorPosition);
    }

    public void SetPosition(Vector2 position) {
        this.position = position;
    }

    public void UpdateObjectGroup(bool changeSizeIndicatorPosition = false) {
        CalculateObjectGroupCenters();
        size = CalculateObjectGroupSize();
        //sizeIndicator.localScale = new Vector3(GetSize() / transform.localScale.x * 2, GetSize() / transform.localScale.y * 2, 1);
        //if (changeSizeIndicatorPosition)
        //    sizeIndicator.position = position;
    }


    void CalculateObjectGroupCenters() {
        Vector2 min = new Vector2(int.MaxValue, int.MaxValue);
        Vector2 max = new Vector2(int.MinValue, int.MinValue);
        Vector2 sum = Vector2.zero;
        for (int i = 0; i < battleObjects.Count; i++) {
            Vector2 tempPos = battleObjects[i].GetPosition();
            sum += tempPos;
            min = Vector2.Min(min, tempPos);
            max = Vector2.Max(max, tempPos);
        }
        position = (min + max) / 2;
        averagePosition = sum / battleObjects.Count;
    }

    private float CalculateObjectGroupSize() {
        float size = 0;
        for (int i = 0; i < battleObjects.Count; i++) {
            size = Math.Max(size, Vector2.Distance(position, battleObjects[i].GetPosition()) + battleObjects[i].GetSize());
        }
        return size;
    }

    public List<T> GetBattleObjects() {
        return battleObjects;
    }

    protected void AddBattleObject(T battleObject) {
        battleObjects.Add(battleObject);
    }

    protected void RemoveBattleObject(T battleObject) {
        battleObjects.Remove(battleObject);
    }

    public Vector2 GetPosition() { return position; }

    public Vector2 GetAveragePosition() { return averagePosition; }

    public float GetSize() {
        return size;
    }

}
