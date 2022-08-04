using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerShipStatusUI : MonoBehaviour {
	public void RefreshPlayerShipStatusUI(Unit unit, int unitCount) {
		Image unitImage = GetShipImageTransform().GetComponent<Image>();
		unitImage.enabled = true;
		unitImage.sprite = unit.GetSpriteRenderer().sprite;
		unitImage.SetNativeSize();
		float sizeRatio = unitImage.rectTransform.sizeDelta.y / unitImage.rectTransform.sizeDelta.x;
		unitImage.rectTransform.sizeDelta = new Vector2(70 / sizeRatio, 70);
		GetShipHealthTransform().GetComponent<Text>().text = "Hull " + unit.GetHealth() + "/" + unit.GetMaxHealth();
		GetShipShieldsTransform().GetComponent<Text>().text = "Shields " + unit.GetShields() + "/" + unit.GetMaxShields();
		GetShipNameTransform().GetComponent<Text>().text = unit.GetUnitName() + " (" + unitCount + ")";
		gameObject.SetActive(true);
	}

	public void DeselectPlayerShipStatusUI() {
		Image unitImage = GetShipImageTransform().GetComponent<Image>();
		GetShipHealthTransform().GetComponent<Text>().text = "Hull 0/0";
		GetShipShieldsTransform().GetComponent<Text>().text = "Shields 0/0";
		GetShipNameTransform().GetComponent<Text>().text = "ShipName";
		unitImage.sprite = null;
		unitImage.enabled = false;
		gameObject.SetActive(false);
	}

	public Transform GetShipImageTransform() {
		return transform.GetChild(0).GetChild(0).GetChild(0);
	}

	public Transform GetShipHealthTransform() {
		return transform.GetChild(1).GetChild(0);
	}

	public Transform GetShipShieldsTransform() {
		return transform.GetChild(2).GetChild(0);
	}

	public Transform GetShipNameTransform() {
		return transform.GetChild(3).GetChild(0);
	}
}
