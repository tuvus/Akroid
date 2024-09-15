using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StartMenuTestUtils {
    public static Button GetButtonByName(string name) {
        List<Button> buttons = Object.FindObjectsOfType<Button>(false).ToList();
        return buttons.FirstOrDefault(b => b.gameObject.name == name);
    }

    public static void ClickButton(string name) {
        GetButtonByName(name).OnPointerClick(new PointerEventData(EventSystem.current));
    }
}
