using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectGroup<T> : MonoBehaviour where T : BattleObject {
    [SerializeField] List<T> battleObjects;
    [SerializeField] Vector2 position;
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
        position = CalculateObjectGroupCenter();
        size = CalculateObjectGroupSize();
        //sizeIndicator.localScale = new Vector3(GetSize() / transform.localScale.x * 2, GetSize() / transform.localScale.y * 2, 1);
        //if (changeSizeIndicatorPosition)
        //    sizeIndicator.position = position;
    }


    public Vector2 CalculateObjectGroupCenter() {
        Vector2 min = new Vector2(int.MaxValue, int.MaxValue);
        Vector2 max = new Vector2(int.MinValue, int.MinValue);
        for (int i = 0; i < battleObjects.Count; i++) {
            Vector2 tempPos = battleObjects[i].GetPosition();
            if (min.x > tempPos.x)
                min = new Vector2(tempPos.x, min.y);
            if (min.y > tempPos.y)
                min = new Vector2(min.x, tempPos.y);
            if (max.x < tempPos.x)
                max = new Vector2(tempPos.x, max.y);
            if (max.y < tempPos.y)
                max = new Vector2(max.x, tempPos.y);
        }
        return (min + max) / 2;
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

    public Vector2 GetPosition() {
        return position;
    }

    public float GetSize() {
        return size;
    }

}
