using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerShipFuelCellsUI : MonoBehaviour {
	public GameObject fuelCellObj;
	public GameObject fuelCellGroup;

	public List<Vector2> hydrazineFuelCells = new List<Vector2>();
	public List<Vector2> uDMHFuelCells = new List<Vector2>();
	public List<Vector2> keroseneFuelCells = new List<Vector2>();
	public List<Vector2> liquidHydrogenFuelCells = new List<Vector2>();
	public List<Vector2> hydrogenPeroxideFuelCells = new List<Vector2>();
	/*
	public void RefreshShipFuelCells(List<FuelTank> shipFuelTank) {
		DeleteFuelCellUI();
		foreach (var tank in shipFuelTank) {
			for (int i = 0; i < tank.GetFuelCellTypes().Count; i++) {
				switch (tank.GetFuelCellTypes()[i]) {
					case FuelTank.FuelTypes.Hydrazine:
					hydrazineFuelCells.Add(new Vector2(tank.GetFuelCellFuel()[i], tank.GetMaxFuelStorage()));
					break;
					case FuelTank.FuelTypes.UDMH:
					uDMHFuelCells.Add(new Vector2(tank.GetFuelCellFuel()[i], tank.GetMaxFuelStorage()));
					break;
					case FuelTank.FuelTypes.Kerosene:
					keroseneFuelCells.Add(new Vector2(tank.GetFuelCellFuel()[i], tank.GetMaxFuelStorage()));
					break;
					case FuelTank.FuelTypes.LiquidHydrogen:
					liquidHydrogenFuelCells.Add(new Vector2(tank.GetFuelCellFuel()[i], tank.GetMaxFuelStorage()));
					break;
					case FuelTank.FuelTypes.HydrogenPeroxide:
					hydrogenPeroxideFuelCells.Add(new Vector2(tank.GetFuelCellFuel()[i], tank.GetMaxFuelStorage()));
					break;
				}
			}
		}
		int heighestFuelCellCount = 0;
		if (hydrazineFuelCells.Count > 0) {
			CreateFuelCellsUI(hydrazineFuelCells, FuelTank.FuelTypes.Hydrazine.ToString());
			if (hydrazineFuelCells.Count > heighestFuelCellCount)
				heighestFuelCellCount = hydrazineFuelCells.Count;
		}
		if (uDMHFuelCells.Count > 0) {
			CreateFuelCellsUI(uDMHFuelCells, FuelTank.FuelTypes.UDMH.ToString());
			if (uDMHFuelCells.Count > heighestFuelCellCount)
				heighestFuelCellCount = uDMHFuelCells.Count;
		}
		if (keroseneFuelCells.Count > 0) {
			CreateFuelCellsUI(keroseneFuelCells, FuelTank.FuelTypes.Kerosene.ToString());
			if (keroseneFuelCells.Count > heighestFuelCellCount)
				heighestFuelCellCount = keroseneFuelCells.Count;
		}
		if (liquidHydrogenFuelCells.Count > 0) {
			CreateFuelCellsUI(liquidHydrogenFuelCells, FuelTank.FuelTypes.LiquidHydrogen.ToString());
			if (liquidHydrogenFuelCells.Count > heighestFuelCellCount)
				heighestFuelCellCount = liquidHydrogenFuelCells.Count;
		}
		if (hydrogenPeroxideFuelCells.Count > 0) {
			CreateFuelCellsUI(hydrogenPeroxideFuelCells, FuelTank.FuelTypes.HydrogenPeroxide.ToString());
			if (hydrogenPeroxideFuelCells.Count > heighestFuelCellCount)
				heighestFuelCellCount = hydrogenPeroxideFuelCells.Count;
		}
		GetComponent<RectTransform>().anchoredPosition = new Vector2(-110, 0 + (heighestFuelCellCount * 10));
	}
	public void CreateFuelCellsUI(List<Vector2> fuelcells, string fuelType) {
		GameObject newGroup = Instantiate(fuelCellGroup, transform);
		newGroup.name = fuelType + "FuelCells";
		for (int i = 0; i < fuelcells.Count; i++) {
			GameObject newFuelCell = Instantiate(fuelCellObj, newGroup.transform);
			newFuelCell.transform.GetChild(0).localPosition = new Vector2(0, -20 + 20 * (fuelcells[i].x / fuelcells[i].y));
		}
	}

	public void DeleteFuelCellUI() {
		hydrazineFuelCells.Clear();
		uDMHFuelCells.Clear();
		keroseneFuelCells.Clear();
		liquidHydrogenFuelCells.Clear();
		hydrogenPeroxideFuelCells.Clear();
		for (int i = 0; i < transform.childCount; i++) {
			Destroy(transform.GetChild(i).gameObject);
		}
	}
	*/
}
