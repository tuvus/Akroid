using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
