using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUnitStatusUI : MonoBehaviour {
	[SerializeField] Image unitImage;
	[SerializeField] Text healthText;
	[SerializeField] Text shieldText;
	[SerializeField] Text nameText;
	public void RefreshPlayerUnitStatusUI(Unit unit, int unitCount) {
		unitImage.enabled = true;
		unitImage.sprite = unit.GetSpriteRenderer().sprite;
        unitImage.SetNativeSize();
		unitImage.color = new Color(unit.GetUnitSelection().GetColor().r, unit.GetUnitSelection().GetColor().g, unit.GetUnitSelection().GetColor().b,1);
        float sizeRatio = unitImage.rectTransform.sizeDelta.y / unitImage.rectTransform.sizeDelta.x;
		unitImage.rectTransform.sizeDelta = new Vector2(70 / sizeRatio, 70);
		healthText.GetComponent<Text>().text = "Hull " + unit.GetHealth() + "/" + unit.GetMaxHealth();
		shieldText.GetComponent<Text>().text = "Shields " + unit.GetShields() + "/" + unit.GetMaxShields();
		nameText.GetComponent<Text>().text = unit.GetUnitName() + " (" + unitCount + ")";
		gameObject.SetActive(true);
	}

	public void DeselectPlayerUnitStatusUI() {
		healthText.GetComponent<Text>().text = "Hull 0/0";
		shieldText.GetComponent<Text>().text = "Shields 0/0";
		nameText.GetComponent<Text>().text = "ShipName";
		unitImage.sprite = null;
		unitImage.enabled = false;
		gameObject.SetActive(false);
	}
}
