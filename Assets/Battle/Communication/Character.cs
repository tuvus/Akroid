using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class Character {
    public string characterName;
    public GameObject characterModel;
    public static Dictionary<String, GameObject> characterPrefabs;

    public Character(string characterName, GameObject characterModel) {
        this.characterName = characterName;
        this.characterModel = characterModel;
    }

    public static Character GenerateCharacter() {
        int random = UnityEngine.Random.Range(0, 3);
        if (random == 0) {
            return CreateCharacter("Firon");
        } else if (random == 1) {
            return CreateCharacter("Thom");
        } else {
            return CreateCharacter("Lwo");
        }
    }

    public static Character CreateCharacter(String prefabName) {
        return new Character(prefabName, characterPrefabs[prefabName]);
    }
}
