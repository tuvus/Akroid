using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommunicationEvent {
    public FactionCommManager sender;
    public FactionCommManager receiver;
    public string text;
    public ReceivedEventLogic receivedEventLogic;
    public delegate void ReceivedEventLogic(CommunicationEvent communicationEvent);
    public CommunicationEventOption[] options;
    public OptionChoiceLogic optionChoiceLogic;
    public delegate int OptionChoiceLogic(CommunicationEvent communicationEvent);
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

    /// <summary>
    /// Sends a quick message
    /// </summary>
    public CommunicationEvent(FactionCommManager receiver, string text) : this(receiver, text, null, (eventLogic) => { }, null, false) { }
    /// <summary>
    /// Sends a message and calls ReceivedEventLogic
    /// </summary>
    public CommunicationEvent(FactionCommManager receiver, string text, ReceivedEventLogic eventLogic) : this(receiver, text, null, eventLogic, null, false) { }
    /// <summary>
    /// Sends a message with options, the AI chooses a random option
    /// </summary>
    public CommunicationEvent(FactionCommManager receiver, string text, CommunicationEventOption[] options, bool isActive) : this(receiver, text, options, (eventLogic) => { }, (choiceLogic) => UnityEngine.Random.Range(0, options.Length), isActive) { }
    /// <summary>
    /// Sends a message with options and calls a ReceivedEventLogic, the AI chooses a random option
    /// </summary>
    public CommunicationEvent(FactionCommManager receiver, string text, CommunicationEventOption[] options, ReceivedEventLogic eventLogic, bool isActive) : this(receiver, text, options, eventLogic, (choiceLogic) => UnityEngine.Random.Range(0, options.Length), isActive) { }
    /// <summary>
    /// Sends a message with options and calls a ReceivedEventLogic, the AI chooses the option returned by OptionChoiceLogic
    /// </summary>
    public CommunicationEvent(FactionCommManager receiver, string text, CommunicationEventOption[] options, ReceivedEventLogic eventLogic, OptionChoiceLogic choiceLogic, bool isActive) {
        this.receiver = receiver;
        this.text = text;
        this.options = options;
        this.isActive = isActive;
        this.receivedEventLogic = eventLogic;
        this.optionChoiceLogic = choiceLogic;
    }

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
