using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static FactionCommManager;

public class PlayerCommsManager : MonoBehaviour {
    PlayerUI playerUI;
    [SerializeField] private GameObject communicationEventPrefab;
    [SerializeField] private GameObject optionPrefab;
    [SerializeField] private GameObject characterPortraitPanel;
    [SerializeField] private Transform characterPortraitFrame;
    [SerializeField] private Image characterNameBackground;
    [SerializeField] private Image factionNameBackground;
    [SerializeField] private Text characterName;
    [SerializeField] private Text factionName;
    private GameObject characterPortrait;
    [SerializeField] private GameObject communicationPanel;
    [SerializeField] private Transform communicationLogTransform;
    [SerializeField] private Transform communicationToggleTransform;
    [SerializeField] private Scrollbar verticleScrollbar;
    bool shown;
    float portraitTime;

    public void SetupPlayerCommsManager(PlayerUI playerUI) {
        this.playerUI = playerUI;
        lockToBottom = true;
        HidePanel();
    }

    public void SetupFaction(Faction faction) {
        for (int i = communicationLogTransform.childCount - 1; i >= 0; i--) {
            DestroyImmediate(communicationLogTransform.GetChild(i).gameObject);
        }
        if (faction != null) {
            FactionCommManager factionCommManager = faction.GetFactionCommManager();
            if (factionCommManager.communicationLog.Count > 0) {
                ShowPanel();
            } else {
                HidePanel();
            }
            for (int i = 0; i < factionCommManager.communicationLog.Count; i++) {
                CreateCommEvent(factionCommManager.communicationLog[i]);
            }
            if (factionCommManager.communicationLog.Count > 0) {
                SetPortrait(factionCommManager.communicationLog[factionCommManager.communicationLog.Count - 1].sender);
            }
        } else {
            HidePanel();
        }
    }
    bool lockToBottom = false;

    public void RecieveNewCommEvent(CommunicationEvent communicationEvent) {
        lockToBottom = verticleScrollbar.value <= 0.1;
        ShowPanel();
        CreateCommEvent(communicationEvent);
        SetPortrait(communicationEvent.sender);
    }

    private void LateUpdate() {
        if (lockToBottom && verticleScrollbar.value > 0.000001) {
            verticleScrollbar.value = 0;
            lockToBottom = false;
        }
        if (portraitTime > 0) {
            portraitTime -= Time.deltaTime;
            if (portraitTime <= 0) {
                portraitTime = 0;
                characterPortraitPanel.SetActive(false);
            }
        }
    }

    void CreateCommEvent(CommunicationEvent communicationEvent) {
        GameObject newCommEvent = Instantiate(communicationEventPrefab, communicationLogTransform);
        newCommEvent.transform.GetChild(0).GetComponent<Text>().text = communicationEvent.sender.GetSenderName() + ": " + communicationEvent.text;
        newCommEvent.GetComponent<Image>().color = communicationEvent.sender.faction.GetColorBackgroundTint();
        if (communicationEvent.isActive) {
            for (int i = 0; i < communicationEvent.options.Length; i++) {
                GameObject newOption = Instantiate(optionPrefab, newCommEvent.transform.GetChild(1));
                newOption.transform.GetChild(0).GetComponent<Text>().text = communicationEvent.options[i].optionName;
                newOption.GetComponent<Button>().interactable = communicationEvent.options[i].checkStatus(communicationEvent);
                newOption.GetComponent<Button>().onClick.AddListener(() => ChooseCommuncationEventOption(communicationEvent, newCommEvent, newOption.transform.GetSiblingIndex()));
            }
        }
    }

    void ChooseCommuncationEventOption(CommunicationEvent communicationEvent, GameObject commEvent, int optionIndex) {
        communicationEvent.ChooseOption(optionIndex);
        if (!communicationEvent.isActive) {
            for (int i = commEvent.transform.GetChild(1).childCount - 1; i >= 0; i--) {
                DestroyImmediate(commEvent.transform.GetChild(1).GetChild(i).gameObject);
            }
        }
    }

    public void OnCommunicationEventDeactivate(int communicationEventIndex) {
        for (int i = communicationLogTransform.GetChild(communicationEventIndex).GetChild(1).childCount - 1; i >= 0; i--) {
            DestroyImmediate(communicationLogTransform.GetChild(communicationEventIndex).GetChild(1).GetChild(i).gameObject);
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
            characterNameBackground.color = factionCommManager.faction.GetColorBackgroundTint(.4f);
            characterName.text = factionCommManager.GetSenderName();
            Color factionColor = factionCommManager.faction.color;
            factionNameBackground.color = new Color(factionColor.r * .4f, factionColor.g * .4f, factionColor.b * .4f, .8f);
            factionName.text = factionCommManager.faction.name;
            portraitTime = 10;
        }
    }

    public bool FreezeScrolling() {
        if (!shown)
            return false;
        List<RaycastResult> raycastResults = new List<RaycastResult>();
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
        pointerEventData.position = playerUI.GetLocalPlayerInput().GetMousePosition();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);
        foreach (var result in raycastResults) {
            if (result.gameObject.tag.Equals("FreezeScroll"))
                return true;
        }
        return false;
    }

    public void ShowPanel() {
        shown = true;
        communicationPanel.SetActive(true);
        if (portraitTime > 0) {
            characterPortraitPanel.SetActive(true);
        }
        for (int i = 0; i < communicationToggleTransform.childCount; i++) {
            communicationToggleTransform.GetChild(i).transform.eulerAngles = new Vector3(0, 0, 270);
        }
    }

    public void HidePanel() {
        shown = false;
        communicationPanel.SetActive(false);
        characterPortraitPanel.SetActive(false);
        for (int i = 0; i < communicationToggleTransform.childCount; i++) {
            communicationToggleTransform.GetChild(i).transform.eulerAngles = new Vector3(0, 0, 90);
        }
    }

    public void ToggleVisibility() {
        if (!communicationPanel.activeSelf && communicationLogTransform.childCount > 0) {
            ShowPanel();
        } else {
            HidePanel();
        }
    }
}