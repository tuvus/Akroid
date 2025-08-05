using TMPro;
using UnityEngine;

public class ObjectSystemUI : PlayerObjectUIMenu {
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text componentName;
    [SerializeField] private TMP_Text systemType;
    [SerializeField] private TMP_Text moduleCount;
    [SerializeField] private TMP_Text maxComponentSize;
    private Unit unit;
    private ModuleSystem.System displayedSystem;

    public override void SetDisplayedObject(ObjectUI objectUI) {
        base.SetDisplayedObject(objectUI);
        if (objectUI.iObject is Unit newUnit) unit = newUnit;
    }

    public void SelectSystem(ModuleSystem.System displayedSystem) {
        this.displayedSystem = displayedSystem;
    }

    public override bool ShouldShowMenu() {
        return unit != null && displayedSystem != null;
    }

    public override void UpdateMenu() {
        title.text = displayedSystem.name;
        componentName.text = displayedSystem.component.name;
        systemType.text = "System type: " + displayedSystem.type;
        moduleCount.text = "Module count: " + displayedSystem.moduleCount;
        maxComponentSize.text = "Max component size: " + displayedSystem.moduleSize;
    }
}
