using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IUnitGroup {
    public void UnitUpdated();

    public void ChangeGroupTotalHealth(int health);

    public void AddUnit(Unit unit);

    public void RemoveUnit(Unit unit);
}