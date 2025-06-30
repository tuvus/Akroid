using System.Collections.Generic;

public class Player {
    public Player(bool isLocalPlayer) {
        faction = null;
        ownedUnits = new HashSet<Unit>();
        lockedOwnedUnits = false;
        this.isLocalPlayer = isLocalPlayer;
    }
    public Faction faction { get; private set; }
    public HashSet<Unit> ownedUnits { get; }
    public bool lockedOwnedUnits { get; private set; }

    public bool isLocalPlayer { get; private set; }

    public void SetFaction(Faction faction) {
        if (faction == this.faction) return;
        ownedUnits.Clear();
        if (this.faction != null) {
            this.faction.OnUnitAdded -= AddOwnedUnit;
            this.faction.OnUnitRemoved -= RemoveOwnedUnit;
        }

        this.faction = faction;

        if (lockedOwnedUnits || faction == null) return;

        ownedUnits.UnionWith(faction.units);
        faction.OnUnitAdded += AddOwnedUnit;
        faction.OnUnitRemoved += RemoveOwnedUnit;
    }

    public void AddOwnedUnit(Unit unit) {
        ownedUnits.Add(unit);
    }

    public void RemoveOwnedUnit(Unit unit) {
        ownedUnits.Remove(unit);
    }

    public void SetLockedUnits(bool locked) {
        if (locked && !lockedOwnedUnits && faction != null) {
            faction.OnUnitAdded -= AddOwnedUnit;
            faction.OnUnitRemoved -= RemoveOwnedUnit;
        } else if (!locked && lockedOwnedUnits && faction != null) {
            faction.OnUnitAdded += AddOwnedUnit;
            faction.OnUnitRemoved += RemoveOwnedUnit;
        }

        lockedOwnedUnits = locked;
    }
}
