using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerObjectUI : PlayerUIMenu<ObjectUI> {
    [SerializeField] private Transform objectViewCameraTransform;
    [SerializeField] private Camera objectViewCamera;
    [SerializeField] private Transform displayedImageTransform;
    private List<GameObject> moduleUIs;
    [SerializeField] private GameObject moduleUIPrefab;

    public override void SetupPlayerUIMenu(PlayerUI playerUI, LocalPlayer localPlayer, UIManager uiManager) {
        base.SetupPlayerUIMenu(playerUI, localPlayer, uiManager);
        moduleUIs = new List<GameObject>();
    }

    protected override void RefreshMiddlePanel() {
        objectViewCameraTransform.transform.position = new Vector3(displayedObject.transform.position.x,
            displayedObject.transform.position.y, -10);
        objectViewCameraTransform.eulerAngles = new Vector3(0, 0, displayedObject.transform.eulerAngles.z);
        objectViewCamera.orthographicSize = displayedObject.iObject.GetSize() * 1.2f;
        if (displayedObject.iObject is Unit unit)
            UpdateModules(unit);
    }

    private void UpdateModules(Unit unit) {
        for (int i = 0; i < unit.moduleSystem.modules.Count; i++) {
            var module = unit.moduleSystem.modules[i];
            if (moduleUIs.Count <= i) {
                moduleUIs.Add(Instantiate(moduleUIPrefab, displayedImageTransform));
                int moduleIndex = i;
                moduleUIs[i].GetComponent<Button>().onClick.AddListener(() => OnModuleButtonPress(moduleIndex));
            }
            moduleUIs[i].SetActive(true);
            moduleUIs[i].GetComponent<RectTransform>().anchoredPosition = module.GetPosition() *
                displayedImageTransform.GetComponent<RectTransform>().sizeDelta *
                (-3f * unit.unitScriptableObject.sprite.pixelsPerUnit) /
                (unit.scale * unit.unitScriptableObject.spriteBounds);
            moduleUIs[i].transform.GetChild(0).eulerAngles = new Vector3(0, 0, module.rotation);
            if (module.componentScriptableObject.sprite != null) {
                moduleUIs[i].transform.GetChild(0).GetComponent<Image>().sprite =
                    module.componentScriptableObject.sprite;
                moduleUIs[i].transform.GetChild(0).gameObject.SetActive(true);
            } else moduleUIs[i].transform.GetChild(0).gameObject.SetActive(false);
        }
        for (int i = unit.moduleSystem.modules.Count; i < moduleUIs.Count; i++) {
            moduleUIs[i].SetActive(false);
        }
    }

    private void OnModuleButtonPress(int moduleIndex) {
        Debug.Log("Module " + moduleIndex + " pressed");
    }

    protected override void RefreshLeftPanel() { }
    protected override void RefreshRightPanel() { }
}
