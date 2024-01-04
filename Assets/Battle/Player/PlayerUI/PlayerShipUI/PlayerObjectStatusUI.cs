using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerObjectStatusUI : MonoBehaviour {
	[SerializeField] Image objectImage;
	[SerializeField] Text healthText;
	[SerializeField] Text shieldText;
	[SerializeField] Text nameText;
	public void RefreshPlayerObjectStatusUI(BattleObject battleObject, int objectCount) {
		objectImage.enabled = true;
		objectImage.sprite = battleObject.GetSpriteRenderer().sprite;
        objectImage.SetNativeSize();
        float sizeRatio = objectImage.rectTransform.sizeDelta.y / objectImage.rectTransform.sizeDelta.x;
        objectImage.rectTransform.sizeDelta = new Vector2(70 / sizeRatio, 70);
        if (battleObject.IsUnit())
            RefreshUIForUnit((Unit)battleObject);
        else objectImage.color = battleObject.GetSpriteRenderer().color;
        if (objectCount <= 1) {
            nameText.GetComponent<Text>().text = battleObject.objectName;
        } else {
            nameText.GetComponent<Text>().text = battleObject.objectName + " (" + objectCount + ")";
        }
        gameObject.SetActive(true);
	}

	private void RefreshUIForUnit(Unit unit) {
        objectImage.color = new Color(unit.GetUnitSelection().GetColor().r, unit.GetUnitSelection().GetColor().g, unit.GetUnitSelection().GetColor().b, 1);
        healthText.GetComponent<Text>().text = "Hull " + NumFormatter.ConvertNumber(unit.GetHealth()) + "/" + NumFormatter.ConvertNumber(unit.GetMaxHealth());
        shieldText.GetComponent<Text>().text = "Shields " + NumFormatter.ConvertNumber(unit.GetShields()) + "/" + NumFormatter.ConvertNumber(unit.GetMaxShields());
    }

    public void RefreshPlayerObjectStatusUI(Fleet fleet, Unit unit, int unitCount) {
        objectImage.enabled = true;
        objectImage.sprite = unit.GetSpriteRenderer().sprite;
        objectImage.SetNativeSize();
        objectImage.color = new Color(unit.GetUnitSelection().GetColor().r, unit.GetUnitSelection().GetColor().g, unit.GetUnitSelection().GetColor().b, 1);
        float sizeRatio = objectImage.rectTransform.sizeDelta.y / objectImage.rectTransform.sizeDelta.x;
        objectImage.rectTransform.sizeDelta = new Vector2(70 / sizeRatio, 70);
        healthText.GetComponent<Text>().text = "Hull " + NumFormatter.ConvertNumber(fleet.GetFleetHealth()) + "/" + NumFormatter.ConvertNumber(fleet.GetMaxFleetHealth());
        shieldText.GetComponent<Text>().text = "Shields " + NumFormatter.ConvertNumber(fleet.GetFleetShields()) + "/" + NumFormatter.ConvertNumber(fleet.GetMaxFleetShields());
        nameText.GetComponent<Text>().text = fleet.GetFleetName() + " (" + unitCount + ")";
        gameObject.SetActive(true);
    }

    public void DeselectPlayerObjectStatusUI() {
		healthText.GetComponent<Text>().text = "Hull 0/0";
		shieldText.GetComponent<Text>().text = "Shields 0/0";
		nameText.GetComponent<Text>().text = "ShipName";
		objectImage.sprite = null;
		objectImage.enabled = false;
		gameObject.SetActive(false);
	}
}
