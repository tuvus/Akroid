using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class Character {
    public static Dictionary<string, GameObject> characterPrefabs;
    public string characterName;
    public GameObject characterModel;

    public Character(string characterName, GameObject characterModel) {
        this.characterName = characterName;
        this.characterModel = characterModel;
    }

    public static Character GenerateCharacter() {
        int random = Random.Range(0, 3);
        if (random == 0) {
            return CreateCharacter("Firon");
        }
        if (random == 1) {
            return CreateCharacter("Thom");
        }
        return CreateCharacter("Lwo");
    }

    public static Character CreateCharacter(string prefabName) {
        return new Character(prefabName, characterPrefabs[prefabName]);
    }
}
