using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static FactionCommManager;

public class PlayerCommsManager : MonoBehaviour {
    PlayerUI playerUI;
    [SerializeField] private GameObject communicationEventPrefab;
    [SerializeField] private GameObject optionPrefab;
    [SerializeField] private GameObject characterPortraitPanel;
    [SerializeField] private Transform characterPortraitFrame;
    [SerializeField] private Text characterName;
    private GameObject characterPortrait;
    [SerializeField] private GameObject communicationPanel;
    [SerializeField] private Transform communicationLogTransform;
    public void SetupPlayerCommsManager(PlayerUI playerUI) {
        this.playerUI = playerUI;
        characterPortraitPanel.SetActive(false);
        communicationPanel.SetActive(false);
    }

    public void SetupFaction(Faction faction) {
        for (int i = 0; i < communicationLogTransform.childCount; i++) {
            DestroyImmediate(communicationLogTransform.GetChild(0).gameObject);
        }
        if (faction != null) {
            FactionCommManager factionCommManager = faction.GetFactionCommManager();
            communicationPanel.SetActive(factionCommManager.communicationLog.Count > 0);
            for (int i = 0; i < factionCommManager.communicationLog.Count; i++) {
                CreateCommEvent(factionCommManager.communicationLog[i]);
            }
        } else {
            characterPortraitPanel.SetActive(false);
            communicationPanel.SetActive(false);
        }
    }

    public void RecieveNewCommEvent(CommunicationEvent communicationEvent) {
        communicationPanel.SetActive(true);
        CreateCommEvent(communicationEvent);
        SetPortrait(communicationEvent.sender);
    }

    void CreateCommEvent(CommunicationEvent communicationEvent) {
        GameObject newCommEvent = Instantiate(communicationEventPrefab, communicationLogTransform);
        newCommEvent.transform.GetChild(0).GetComponent<Text>().text = communicationEvent.sender.GetSenderName() + ": " + communicationEvent.text;
        for (int i = 0; i < communicationEvent.options.Length; i++) {
            GameObject newOption = Instantiate(optionPrefab, newCommEvent.transform.GetChild(1));
            newOption.transform.GetChild(0).GetComponent<Text>().text = communicationEvent.options[i].optionName;
            newOption.GetComponent<Button>().interactable = communicationEvent.options[i].checkStatus(communicationEvent);
            newOption.GetComponent<Button>().onClick.AddListener(() => ChooseCommuncationEventOption(communicationEvent, newCommEvent, newOption.transform.GetSiblingIndex()));
        }
    }

    void ChooseCommuncationEventOption(CommunicationEvent communicationEvent, GameObject commEvent, int optionIndex) {
        communicationEvent.ChooseOption(optionIndex);
        if (!communicationEvent.isActive) {
            for (int i = commEvent.transform.transform.GetChild(1).childCount - 1; i >= 0; i--) {
                DestroyImmediate(commEvent.transform.GetChild(1).transform.GetChild(i).gameObject);
            }
        }
    }

    void SetPortrait(FactionCommManager factionCommManager) {
        if (characterPortrait != null) {
            DestroyImmediate(characterPortrait);
            characterPortraitPanel.SetActive(false);
        }
        if (factionCommManager.GetPortrait() != null) {
            characterPortrait = Instantiate(factionCommManager.GetPortrait(), characterPortraitFrame);
            characterPortraitPanel.SetActive(true);
            characterName.text = factionCommManager.GetSenderName();
        }
    }
}