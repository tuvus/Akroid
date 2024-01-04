using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CommunicationEvent;

public class FactionCommManager : MonoBehaviour {
    public Faction faction { get; private set; }
    public List<CommunicationEvent> communicationLog;
    public List<DelayCommunication> delayedCommunications;
    [SerializeField] private Character character;

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

    public bool IsLocalPlayer() {
        return LocalPlayer.Instance.GetFaction() == faction;
    }

    public string GetSenderName() {
        return character.characterName;
    }

    public GameObject GetPortrait() {
        return character.characterModel;
    }
}
