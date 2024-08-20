using System;
using System.Collections;
using System.Collections.Generic;
using static CommunicationEvent;

/// <summary>
/// EventChainBuilder handles long sequences of events and communcationEvents by creating a list and putting them together once built.
/// Don't forget to call the method returned after calling build!!!
/// </summary>
public class EventChainBuilder {
    private List<object> events = new List<object>();
    private class CommunicationEventHolder {
        public FactionCommManager commManager;
        public Faction reciever;
        public string text;
        public float delay;

        public CommunicationEventHolder(FactionCommManager commManager, Faction reciever, string text, float delay) {
            this.commManager = commManager;
            this.reciever = reciever;
            this.text = text;
            this.delay = delay;
        }
    }
    private class CommunicationButtonEventHolder {
        public FactionCommManager commManager;
        public Faction reciever;
        public string text;
        public string buttonText;
        public float delay;

        public CommunicationButtonEventHolder(FactionCommManager commManager, Faction reciever, string text, string buttonText, float delay) {
            this.commManager = commManager;
            this.reciever = reciever;
            this.text = text;
            this.buttonText = buttonText;
            this.delay = delay;
        }
    }

    /// <summary> Adds a regular communication event to be sent. </summary>
    public void AddCommEvent(FactionCommManager commManager, Faction reciever, string text, float delay = 0f) {
        events.Add(new CommunicationEventHolder(commManager, reciever, text, delay));
    }

    /// <summary> Adds a comm event with one button. Pressing the button will continue the event chain. </summary>
    public void AddButtonCommEvent(FactionCommManager commManager, Faction reciever, string text, string buttonText, float delay = 0f) {
        events.Add(new CommunicationButtonEventHolder(commManager, reciever, text, buttonText, delay));
    }

    /// <summary> Adds a condition before proceding to the next event. </summary>
    public void AddCondition(EventCondition eventCondition) {
        events.Add(eventCondition);
    }

    /// <summary> Invokes the action given when. </summary>
    /// <param name="action"></param>
    public void AddAction(Action action) {
        events.Add(action);
    }

    /// <summary>
    /// Builds the EventChain with one final multi-option CommunicationEvent.
    /// </summary>
    public Action Build(EventManager eventManager, FactionCommManager commManager, Faction reciever, string text, CommunicationEventOption[] options, float delay = 0f) {
        return Build(eventManager, () => commManager.SendCommunication(new CommunicationEvent(reciever.GetFactionCommManager(), text, options, true), delay));
    }

    /// <summary>
    /// Finalizes constructing the event chain and returns a function to fire it off.
    /// </summary>
    public Action Build(EventManager eventManager) {
        return Build(eventManager, () => { });
    }

    /// <summary>
    /// Builds the event chain in reverse order since we need to know the previous action to call.
    /// </summary>
    public Action Build(EventManager eventManager, Action lastAction) {
        for (int i = events.Count - 1; i >= 0; i--) {
            if (events[i].GetType() == typeof(CommunicationEventHolder)) {
                CommunicationEventHolder communicationEvent = (CommunicationEventHolder)events[i];
                Action temp = lastAction;
                lastAction = () => communicationEvent.commManager.SendCommunication(communicationEvent.reciever, communicationEvent.text, (communicationEvent) => temp(), communicationEvent.delay);
            } else if (events[i].GetType() == typeof(CommunicationButtonEventHolder)) {
                CommunicationButtonEventHolder communicationButtonEvent = (CommunicationButtonEventHolder)events[i];
                Action temp = lastAction;
                lastAction = () => communicationButtonEvent.commManager.SendCommunication(new CommunicationEvent(communicationButtonEvent.reciever.GetFactionCommManager(), communicationButtonEvent.text,
                    new CommunicationEventOption[] {
                        new(communicationButtonEvent.buttonText, (communicationEvent) => { return true; }, (communicationEvent) => {
                            if (!communicationEvent.isActive)
                                return false;
                            communicationEvent.DeactivateEvent();
                            temp();
                            return true;
                        })
                    }, true), communicationButtonEvent.delay);
            } else if (events[i].GetType() == typeof(Action)) {
                Action temp = lastAction;
                Action tempAcion = (Action)events[i];
                lastAction = () => { tempAcion(); temp(); };
            } else {
                EventCondition eventCondition = (EventCondition)events[i];
                Action temp = lastAction;
                lastAction = () => eventManager.AddEvent(eventCondition, temp);
            }
        }
        return lastAction;
    }

}
