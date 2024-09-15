using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EventManager {
    public HashSet<Tuple<EventCondition, Action>> ActiveEvents { get; private set; }
    public LocalPlayerGameInput playerGameInput { get; private set; }
    public float panDelta;

    public EventManager(LocalPlayerGameInput playerGameInput) {
        this.playerGameInput = playerGameInput;
        ActiveEvents = new HashSet<Tuple<EventCondition, Action>>();
        LocalPlayer.Instance.GetPlayerUI().playerEventUI.SetEventManager(this);
        LocalPlayer.Instance.GetLocalPlayerInput().OnPanEvent += (oldPos, newPos) => panDelta = Vector2.Distance(oldPos, newPos);
    }

    public void UpdateEvents(float deltaTime) {
        foreach (var activeEvent in ActiveEvents.ToList()) {
            if (activeEvent.Item1.CheckCondition(this, deltaTime)) {
                ActiveEvents.Remove(activeEvent);
                activeEvent.Item2();
            }
        }

        panDelta = 0;
    }

    public void AddEvent(EventCondition condition, Action action) {
        ActiveEvents.Add(new Tuple<EventCondition, Action>(condition, action));
    }
}
