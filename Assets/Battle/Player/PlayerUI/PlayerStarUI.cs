using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerStarUI : MonoBehaviour {
    PlayerUI playerUI;

    [SerializeField] GameObject starStatsUI;

    public Star displayedStar { get; private set; }
    [SerializeField] TMP_Text starName;

    public void SetupPlayerStarUI(PlayerUI playerUI) {
        this.playerUI = playerUI;
    }

    public void DisplayStar(Star star) {
        displayedStar = star;
        starStatsUI.SetActive(star != null);
        UpdateStarDisplay();
    }

    public void UpdateStarUI() {
        UpdateStarDisplay();
    }

    public void UpdateStarDisplay() {
        if (starStatsUI.activeSelf) {
            starName.text = displayedStar.objectName;
        }
    }
}
