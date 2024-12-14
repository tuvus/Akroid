using UnityEngine;

public class StationUI : UnitUI {
    public Station station { get; private set; }
    private bool built = false;

    public override void Setup(BattleObject battleObject, UIManager uIManager) {
        base.Setup(battleObject, uIManager);
        this.station = (Station)battleObject;
        built = station.IsBuilt();
        if (!built) {
            transform.localScale = new Vector3(station.scale.x / 3, station.scale.y / 3, 1);
            spriteRenderer.color = new Color(.3f, 1f, .3f, .5f);
        }
    }

    public override void UpdateObject() {
        base.UpdateObject();
        if (!built && station.IsBuilt()) {
            transform.localScale = new Vector3(station.scale.x, station.scale.y, 1);
            spriteRenderer.color = Color.white;

        }
    }

    public override void SelectObject(UnitSelection.SelectionStrength selectionStrength = UnitSelection.SelectionStrength.Unselected) {
        unitSelection.SetSelected(selectionStrength);
    }

    public override bool IsSelectable() {
        return base.IsSelectable() && station.IsBuilt();
    }

    public override void UnselectObject() {
        unitSelection.SetSelected();
    }
}
