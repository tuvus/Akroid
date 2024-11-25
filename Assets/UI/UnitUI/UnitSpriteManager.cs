using System.Collections.Generic;
using UnityEngine;

public class UnitSpriteManager : MonoBehaviour {
    public BattleManager battleManager { get; private set; }
    public UIManager uIManager { get; private set; }

    public Dictionary<BattleObject, GameObject> objects { get; private set; }

    public void SetupUnitSpriteManager(BattleManager battleManager, UIManager uIManager) {
        this.battleManager = battleManager;
        this.uIManager = uIManager;
        objects = new Dictionary<BattleObject, GameObject>();
    }

    public void UpdateSpriteManager() {
        foreach (var star in battleManager.stars) {
            if (!objects.ContainsKey(star)) {
                objects.Add(star, CreateStar(star));
            }

            GameObject starObj = objects[star];
            if (!star.IsSpawned()) {
                if (starObj.activeSelf) starObj.SetActive(false);
            } else {
                if (!starObj.activeSelf) starObj.SetActive(true);
                starObj.GetComponent<SpriteRenderer>().color = star.color;
                starObj.transform.GetChild(0).GetComponent<SpriteRenderer>().color = star.color;
            }
        }
    }

    public GameObject CreateStar(Star star) {
        var starPrefab= (GameObject)Resources.Load("Prefabs/Star");
        GameObject starObj = Instantiate(starPrefab, uIManager.GetStarTransform());
        starObj.transform.localScale = star.scale;
        starObj.transform.position = star.GetPosition();
        return starObj;
    }
}
