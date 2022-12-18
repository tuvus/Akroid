using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Character {
    public string characterName;
    public GameObject characterModel;

    public Character(string characterName, GameObject characterModel) {
        this.characterName = characterName;
        this.characterModel = characterModel;
    }

    public static Character GenerateCharacter() {
        int random = UnityEngine.Random.Range(0, 2);
        if (random == 0) {
            GameObject prefab = (GameObject)Resources.Load("Prefabs/Characters/Fire Boy");
            return new Character(prefab.name, prefab);
        } else {
            GameObject prefab = (GameObject)Resources.Load("Prefabs/Characters/Moth Man");
            return new Character(prefab.name, prefab);
        }
    }
}
