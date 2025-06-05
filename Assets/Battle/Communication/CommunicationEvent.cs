using System;
using Random = UnityEngine.Random;

[Serializable]
public class CommunicationEvent {
    public delegate int OptionChoiceLogic(CommunicationEvent communicationEvent);

    public delegate void ReceivedEventLogic(CommunicationEvent communicationEvent);

    public string text;

    public CommunicationEventOption[] options;

    public bool isActive;
    public OptionChoiceLogic optionChoiceLogic;
    public ReceivedEventLogic receivedEventLogic;
    public FactionCommManager receiver;
    public FactionCommManager sender;

    /// <summary>
    ///     Sends a quick message
    /// </summary>
    public CommunicationEvent(FactionCommManager receiver, string text) : this(receiver, text, null, eventLogic => { },
        null, false) { }

    /// <summary>
    ///     Sends a message and calls ReceivedEventLogic
    /// </summary>
    public CommunicationEvent(FactionCommManager receiver, string text, ReceivedEventLogic eventLogic) : this(receiver,
        text, null,
        eventLogic, null, false) { }

    /// <summary>
    ///     Sends a message with options, the AI chooses a random option
    /// </summary>
    public CommunicationEvent(FactionCommManager receiver, string text, CommunicationEventOption[] options,
        bool isActive) : this(receiver,
        text, options, eventLogic => { }, choiceLogic => Random.Range(0, options.Length), isActive) { }

    /// <summary>
    ///     Sends a message with options and calls a ReceivedEventLogic, the AI chooses a random option
    /// </summary>
    public CommunicationEvent(FactionCommManager receiver, string text, CommunicationEventOption[] options,
        ReceivedEventLogic eventLogic,
        bool isActive) : this(receiver, text, options, eventLogic, choiceLogic => Random.Range(0, options.Length),
        isActive) { }

    /// <summary>
    ///     Sends a message with options and calls a ReceivedEventLogic, the AI chooses the option returned by
    ///     OptionChoiceLogic
    /// </summary>
    public CommunicationEvent(FactionCommManager receiver, string text, CommunicationEventOption[] options,
        ReceivedEventLogic eventLogic,
        OptionChoiceLogic choiceLogic, bool isActive) {
        this.receiver = receiver;
        this.text = text;
        this.options = options;
        this.isActive = isActive;
        receivedEventLogic = eventLogic;
        optionChoiceLogic = choiceLogic;
    }

    public bool ChooseOption(int option) {
        if (!options[option].checkStatus(this))
            return false;
        return options[option].chooseOption(this);
    }

    public void DeactivateEvent() {
        if (!isActive) return;
        isActive = false;
        if (receiver.IsLocalPlayer()) receiver.DeactivateCommunicationEvent(this);
    }

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
}
