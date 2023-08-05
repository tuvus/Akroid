using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static FactionCommManager.CommunicationEvent;
using static FactionCommManager.CommunicationEvent.CommunicationEventOption;

public class FactionCommManager : MonoBehaviour {
    private Faction faction;
    public List<CommunicationEvent> communicationLog;
    public List<DelayCommunication> delayedCommunications;
    [SerializeField] private Character character;

    [Serializable]
    public class CommunicationEvent {
        public FactionCommManager sender;
        public FactionCommManager receiver;
        public string text;
        public EventLogic eventLogic;
        public delegate void EventLogic(CommunicationEvent communicationEvent);
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

        public CommunicationEvent(FactionCommManager receiver, string text, CommunicationEventOption[] options, EventLogic eventLogic, bool isActive) {
            this.receiver = receiver;
            this.text = text;
            this.options = options;
            this.isActive = isActive;
            this.eventLogic = eventLogic;
        }
        
        public CommunicationEvent(Faction receiver, string text, CommunicationEventOption[] options, EventLogic eventLogic, bool isActive) : this(receiver.GetFactionCommManager(), text, options, eventLogic, isActive) { }

        public CommunicationEvent(FactionCommManager receiver, string text, CommunicationEventOption[] options, bool isActive) : this(receiver, text, options, (eventLogic) => { }, isActive) { }
        
        public CommunicationEvent(Faction receiver, string text, CommunicationEventOption[] options, bool isActive) : this(receiver.GetFactionCommManager(), text, options, (eventLogic) => { }, isActive) { }
       
        public CommunicationEvent(FactionCommManager receiver, string text, EventLogic eventLogic) : this(receiver, text, new CommunicationEventOption[0], eventLogic, false) { }
        
        public CommunicationEvent(Faction receiver, string text, EventLogic eventLogic) : this(receiver.GetFactionCommManager(), text, new CommunicationEventOption[0], eventLogic, false) { }
        
        public CommunicationEvent(FactionCommManager receiver, string text) : this(receiver, text, new CommunicationEventOption[0], (eventLogic) => { }, false) { }
        
        public CommunicationEvent(Faction receiver, string text) : this(receiver.GetFactionCommManager(), text, new CommunicationEventOption[0], (eventLogic) => { }, false) { }

        public bool ChooseOption(int option) {
            if (!options[option].checkStatus(this))
                return false;
            return options[option].chooseOption(this);
        }

        public void DeactivateEvent() {
            if (isActive) {
                isActive = false;
                if (receiver.IsLocalPlayer()) {
                    LocalPlayer.Instance.GetPlayerUI().GetPlayerCommsManager().OnCommunicationEventDeactivate(receiver.communicationLog.IndexOf(this));
                }
            }
        }
    }

    public class DelayCommunication {
        public CommunicationEvent newCommunication;
        public double targetTime;

        public DelayCommunication(CommunicationEvent newCommunication, float delay = 0) {
            this.newCommunication = newCommunication;
            this.targetTime = BattleManager.Instance.GetSimulationTime() + delay;
        }
    }

    public void SetupCommunicationManager(Faction faction, Character character) {
        this.faction = faction;
        this.character = character;
        delayedCommunications = new List<DelayCommunication>();
    }

    public void UpdateCommunications() {
        while (delayedCommunications.Count > 0 && BattleManager.Instance.GetSimulationTime() >= delayedCommunications[0].targetTime) {
            SendCommunication(delayedCommunications[0]);
            delayedCommunications.RemoveAt(0);
        }
    }

    public void SendCommunication(Faction receiver, string text, float delay = 0) {
        SendCommunication(new CommunicationEvent(receiver.GetFactionCommManager(), text, new CommunicationEventOption[0], false), delay);
    }

    public void SendCommunication(Faction receiver, string text, EventLogic eventLogic, float delay = 0) {
        SendCommunication(new CommunicationEvent(receiver.GetFactionCommManager(), text, new CommunicationEventOption[0], eventLogic, false), delay);
    }


    public void SendCommunication(DelayCommunication delayedCommunication) {
        SendCommunication(delayedCommunication.newCommunication);
    }

    public void SendCommunication(CommunicationEvent newCommunication, float delay = 0) {
        if (delay > 0) {
            delayedCommunications.Add(new DelayCommunication(newCommunication, delay));
            delayedCommunications.Sort((a,b) => a.targetTime.CompareTo(b.targetTime));
        } else
            newCommunication.receiver.ReceiveCommunication(this, newCommunication);
    }

    public void ReceiveCommunication(FactionCommManager sender, CommunicationEvent newCommunication) {
        newCommunication.sender = sender;
        newCommunication.receiver = this;
        communicationLog.Add(newCommunication);
        if (IsLocalPlayer()) {
            LocalPlayer.Instance.GetPlayerUI().GetPlayerCommsManager().RecieveNewCommEvent(newCommunication);
        }
        newCommunication.eventLogic(newCommunication);
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
