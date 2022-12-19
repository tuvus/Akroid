using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static FactionCommManager.CommunicationEvent;

public class FactionCommManager : MonoBehaviour {
    private Faction faction;
    public List<CommunicationEvent> communicationLog;
    [SerializeField] private Character character;

    [Serializable]
    public class CommunicationEvent {
        public FactionCommManager sender;
        public FactionCommManager reciever;
        public string text;
        public CommunicationEventOption[] options;
        public bool isActive;

        [Serializable]
        public struct CommunicationEventOption {
            public string optionName;
            public CheckStatus checkStatus;
            public ChooseOption chooseOption;
            public delegate bool CheckStatus(CommunicationEvent communicationEvent);
            public delegate bool ChooseOption(CommunicationEvent communicationEvent);

            public CommunicationEventOption(string optionName, CheckStatus checkStatus, ChooseOption chooseOption) {
                this.optionName = optionName;
                this.checkStatus = checkStatus;
                this.chooseOption = chooseOption;
            }

        }

        public CommunicationEvent(string text, CommunicationEventOption[] options, bool isActive) {
            this.text = text;
            this.options = options;
            this.isActive = isActive;
        }

        public bool ChooseOption(int option) {
            if (!options[option].checkStatus(this))
                return false;
            return options[option].chooseOption(this);
        }

        public void DeactivateEvent() {
            if (isActive) {
                isActive = false;
                if (reciever.IsLocalPlayer()) {
                    LocalPlayer.Instance.GetPlayerUI().GetPlayerCommsManager().OnCommunicationEventDeactivate(reciever.communicationLog.IndexOf(this));
                }
            }
        }
    }

    public void SetupCommunicationManager(Faction faction, Character character) {
        this.faction = faction;
        this.character = character;
    }

    public void SendCommunication(Faction reciever, string text) {
        SendCommunication(reciever, new CommunicationEvent(text, new CommunicationEventOption[0], false));
    }

    public void SendCommunication(Faction reciever, CommunicationEvent newCommunication) {
        reciever.GetFactionCommManager().RecieveCommunication(this, newCommunication);
    }

    public void RecieveCommunication(FactionCommManager sender, CommunicationEvent newCommunication) {
        newCommunication.sender = sender;
        newCommunication.reciever = this;
        communicationLog.Add(newCommunication);
        if (IsLocalPlayer()) {
            LocalPlayer.Instance.GetPlayerUI().GetPlayerCommsManager().RecieveNewCommEvent(newCommunication);
        }
    }

    bool IsLocalPlayer() {
        return LocalPlayer.Instance.GetFaction() == faction;
    }

    public string GetSenderName() {
        return character.characterName;
    }

    public GameObject GetPortrait() {
        return character.characterModel;
    }
}
