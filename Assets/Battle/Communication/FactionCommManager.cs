using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static CommunicationEvent;

public class FactionCommManager {
    private readonly BattleManager battleManager;
    private readonly Character character;
    public List<CommunicationEvent> communicationLog;
    public List<DelayCommunication> delayedCommunications;

    public FactionCommManager(BattleManager battleManager, Faction faction, Character character) {
        this.battleManager = battleManager;
        this.faction = faction;
        this.character = character;
        communicationLog = new List<CommunicationEvent>();
        delayedCommunications = new List<DelayCommunication>();
    }
    public Faction faction { get; }
    public event Action<CommunicationEvent> OnCommunicationReceived = delegate { };
    public event Action<int> OnCommunicationEventDeactivated = delegate { };

    public void UpdateCommunications() {
        while (delayedCommunications.Count > 0 &&
            battleManager.GetSimulationTime() >= delayedCommunications[0].targetTime) {
            SendCommunication(delayedCommunications[0]);
            delayedCommunications.RemoveAt(0);
        }
    }

    public class DelayCommunication {
        public CommunicationEvent newCommunication;
        public double targetTime;

        public DelayCommunication(CommunicationEvent newCommunication, BattleManager battleManager, float delay = 0) {
            this.newCommunication = newCommunication;
            targetTime = battleManager.GetSimulationTime() + delay;
        }
    }

    #region CommunicationTransfer

    public void SendCommunication(Faction receiver, string text, float delay = 0) {
        SendCommunication(new CommunicationEvent(receiver.GetFactionCommManager(), text), delay);
    }

    public void SendCommunication(Faction receiver, string text, ReceivedEventLogic eventLogic, float delay = 0) {
        SendCommunication(new CommunicationEvent(receiver.GetFactionCommManager(), text, eventLogic), delay);
    }

    public void SendCommunication(DelayCommunication delayedCommunication) {
        SendCommunication(delayedCommunication.newCommunication);
    }

    public void SendCommunication(CommunicationEvent newCommunication, float delay = 0) {
        if (delay > 0) {
            delayedCommunications.Add(new DelayCommunication(newCommunication, battleManager, delay));
            delayedCommunications.Sort((a, b) => a.targetTime.CompareTo(b.targetTime));
        } else {
            newCommunication.receiver.ReceiveCommunication(this, newCommunication);
        }
    }

    public void ReceiveCommunication(FactionCommManager sender, CommunicationEvent receivedCommunication) {
        receivedCommunication.sender = sender;
        receivedCommunication.receiver = this;
        communicationLog.Add(receivedCommunication);
        if (IsLocalPlayer()) {
            OnCommunicationReceived(receivedCommunication);
        } else if (receivedCommunication.options != null && receivedCommunication.optionChoiceLogic != null) {
            receivedCommunication.ChooseOption(receivedCommunication.optionChoiceLogic(receivedCommunication));
        }

        receivedCommunication.receivedEventLogic(receivedCommunication);
    }

    #endregion

    #region HelperMethods

    public void DeactivateCommunicationEvent(CommunicationEvent communicationEvent) {
        OnCommunicationEventDeactivated(communicationLog.IndexOf(communicationEvent));
    }

    public bool IsLocalPlayer() {
        return battleManager.players.Any(p => p.faction == faction && p.isLocalPlayer);
    }

    public string GetSenderName() {
        return character.characterName;
    }

    public GameObject GetPortrait() {
        return character.characterModel;
    }

    #endregion
}
