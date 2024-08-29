using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotFactionAI : FactionAI {
    Chapter1 chapter1;

    public void SetupPlayerFactionAI(BattleManager battleManager, Faction faction, Chapter1 chapter1, MiningStation playerMiningStation) {
        base.SetupFactionAI(battleManager, faction);
        this.chapter1 = chapter1;
    }
}
