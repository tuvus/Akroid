using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotFactionAI : FactionAI {
    Chapter1 chapter1;

    public RobotFactionAI(BattleManager battleManager, Faction faction, Chapter1 chapter1, MiningStation playerMiningStation) :
        base(battleManager, faction) {
        this.chapter1 = chapter1;
    }
}
