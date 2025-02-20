using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerObjectStatusUI : MonoBehaviour {
    [SerializeField] Image objectImage;
    [SerializeField] TMP_Text healthText;
    [SerializeField] TMP_Text shieldText;
    [SerializeField] TMP_Text nameText;
    [SerializeField] private GameObject healthObject;
    [SerializeField] private GameObject shieldObject;


    public void RefreshPlayerObjectStatusUI(BattleObjectUI battleObjectUI, int objectCount) {
        objectImage.enabled = true;
        objectImage.sprite = battleObjectUI.GetSprite();
        objectImage.SetNativeSize();
        float sizeRatio = objectImage.rectTransform.sizeDelta.y / objectImage.rectTransform.sizeDelta.x;
        objectImage.rectTransform.sizeDelta = new Vector2(100 / sizeRatio, 100);
        if (battleObjectUI.battleObject.IsUnit()) {
            RefreshUIForUnit((UnitUI)battleObjectUI);
        } else {
            healthObject.SetActive(false);
            shieldObject.SetActive(false);
            objectImage.color = battleObjectUI.GetSpriteColor();
        }

        if (objectCount <= 1) {
            nameText.GetComponent<TMP_Text>().text = battleObjectUI.battleObject.objectName;
        } else {
            nameText.GetComponent<TMP_Text>().text = battleObjectUI.battleObject.objectName + " (" + objectCount + ")";
        }

        gameObject.SetActive(true);
    }

    private void RefreshUIForUnit(UnitUI unitUI) {
        healthObject.SetActive(true);
        shieldObject.SetActive(true);
        objectImage.color = new Color(unitUI.unitIconUI.GetColor().r, unitUI.unitIconUI.GetColor().g, unitUI.unitIconUI.GetColor().b, 1);
        healthText.GetComponent<TMP_Text>().text = "Hull " + NumFormatter.ConvertNumber(unitUI.unit.GetHealth()) + "/" +
            NumFormatter.ConvertNumber(unitUI.unit.GetMaxHealth());
        shieldText.GetComponent<TMP_Text>().text = "Shields " + NumFormatter.ConvertNumber(unitUI.unit.GetShields()) + "/" +
            NumFormatter.ConvertNumber(unitUI.unit.GetMaxShields());
    }

    public void RefreshPlayerObjectStatusUI(FleetUI fleetUI, UnitUI unitUI, int unitCount) {
        objectImage.enabled = true;
        objectImage.sprite = unitUI.GetSprite();
        objectImage.SetNativeSize();
        objectImage.color = new Color(unitUI.unitIconUI.GetColor().r, unitUI.unitIconUI.GetColor().g, unitUI.unitIconUI.GetColor().b, 1);
        float sizeRatio = objectImage.rectTransform.sizeDelta.y / objectImage.rectTransform.sizeDelta.x;
        objectImage.rectTransform.sizeDelta = new Vector2(70 / sizeRatio, 70);
        healthText.GetComponent<TMP_Text>().text = "Hull " + NumFormatter.ConvertNumber(fleetUI.fleet.GetFleetHealth()) + "/" +
            NumFormatter.ConvertNumber(fleetUI.fleet.GetMaxFleetHealth());
        shieldText.GetComponent<TMP_Text>().text = "Shields " + NumFormatter.ConvertNumber(fleetUI.fleet.GetFleetShields()) + "/" +
            NumFormatter.ConvertNumber(fleetUI.fleet.GetMaxFleetShields());
        nameText.GetComponent<TMP_Text>().text = fleetUI.fleet.GetFleetName() + " (" + unitCount + ")";
        gameObject.SetActive(true);
    }

    public void DeselectPlayerObjectStatusUI() {
        healthText.GetComponent<TMP_Text>().text = "Hull 0/0";
        shieldText.GetComponent<TMP_Text>().text = "Shields 0/0";
        nameText.GetComponent<TMP_Text>().text = "ShipName";
        objectImage.sprite = null;
        objectImage.enabled = false;
        gameObject.SetActive(false);
    }
}
