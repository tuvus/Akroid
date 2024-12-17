using System;
using System.Collections.Generic;

public class Player {
    public Faction faction { get; private set; }
    public HashSet<Unit> ownedUnits { get; private set; }
    public bool lockedOwnedUnits { get; private set; }

    public bool isLocalPlayer { get; private set; }
    public event Action<Faction> OnFactionChanged = delegate { };

    public Player(bool isLocalPlayer) {
        faction = null;
        ownedUnits = new HashSet<Unit>();
        lockedOwnedUnits = true;
        this.isLocalPlayer = isLocalPlayer;
    }

    public void SetFaction(Faction faction) {
        this.faction = faction;
        if (!lockedOwnedUnits) {
            if (faction != null) {
                ownedUnits = faction.units;
            } else {
                ownedUnits.Clear();
            }
        }
    }

    public void AddOwnedUnit(Unit unit) {
        ownedUnits.Add(unit);
        // unit.GetUnitSelection().UpdateFactionColor();
    }

    public void RemoveOwnedUnit(Unit unit) {
        ownedUnits.Remove(unit);
        // unit.GetUnitSelection().UpdateFactionColor();
    }

    public void SetLockedUnits(bool locked) {
        lockedOwnedUnits = locked;
    }
}
