using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Windows.Speech;

public class FactionTrade {
    public Faction faction { get; private set; }

    public struct TradeOffer {
        public CargoBay.CargoType cargoType;
        public long amount;
        public float price;

        public TradeOffer(CargoBay.CargoType cargoType, long amount, float price) {
            this.cargoType = cargoType;
            this.amount = amount;
            this.price = price;
        }

        public TradeOffer(TradeOffer tradeOffer, long newAmount) {
            this.cargoType = tradeOffer.cargoType;
            this.amount = newAmount;
            this.price = tradeOffer.price;
        }
    }

    public class TradeContract {
        public Unit provider;
        public Unit receiver;
        public Dictionary<CargoBay.CargoType, TradeOffer> cargo;

        public TradeContract(Unit provider, Unit reciever, params TradeOffer[] offers) {
            this.provider = provider;
            this.receiver = reciever;
            cargo = new Dictionary<CargoBay.CargoType, TradeOffer>();
            foreach (TradeOffer offer in offers) {
                cargo.Add(offer.cargoType, offer);
            }
        }
    }

    public class TransportOffer {
        public Population clients;
        public PopulationFloat payment;

        public TransportOffer(Population clients, PopulationFloat payment) {
            this.clients = clients;
            this.payment = payment;
        }
    }

    public class TransportContract {
        public Unit provider;
        public Unit receiver;
        public TransportOffer transportOffer;

        public TransportContract(Unit provider, Unit receiver, TransportOffer transportOffer) {
            this.provider = provider;
            this.receiver = receiver;
            this.transportOffer = transportOffer;
        }
    }

    /// <summary> The resources being offered by each station in the faction. </summary>
    public Dictionary<CargoBay.CargoType, Dictionary<Unit, TradeOffer>> resourcesOffered;

    /// <summary> The resources being requested by each station in the faction. </summary>
    public Dictionary<CargoBay.CargoType, Dictionary<Unit, TradeOffer>> resourcesRequested;

    /// <summary> The personnel that are open to being hired by each station in the faction. </summary>
    public Dictionary<Unit, TransportOffer> personnelToHire;

    /// <summary> The personnel requested by each station in the faction. </summary>
    public Dictionary<Unit, TransportOffer> personnelRequested;

    /// <summary> The factions that we can sell to and how much of a markup we have. </summary>
    public Dictionary<Faction, float> tradeSellAgreements;
    /// <summary> The factions that we can buy from and how much of a markup they have. </summary>
    public Dictionary<Faction, float> tradeBuyAgreements;
    public HashSet<TradeContract> activeContracts;

    public FactionTrade(Faction faction) {
        this.faction = faction;
        resourcesOffered = new();
        resourcesRequested = new();
        foreach (CargoBay.CargoType cargoType in CargoBay.allCargoTypes) {
            resourcesOffered.Add(cargoType, new());
            resourcesRequested.Add(cargoType, new());
        }
        personnelToHire = new();
        personnelRequested = new();
        tradeSellAgreements = new();
        tradeBuyAgreements = new();
        activeContracts = new();
    }


    public void MakeSellTradeAgreement(Faction tradePartner, float markupPrice = 1.2f) {
        if (!tradeSellAgreements.TryAdd(tradePartner, markupPrice) ||
            !tradePartner.factionTrade.tradeBuyAgreements.TryAdd(faction, markupPrice))
            throw new Exception("Trying to start a trade agreement that already exists with " + tradePartner.name +
                "!");
    }

    public void BreakSellTradeAgreement(Faction tradePartner) {
        if (!tradeSellAgreements.Remove(tradePartner) || !tradePartner.factionTrade.tradeBuyAgreements.Remove(faction))
            throw new Exception("Trying to remove a trade agreement with " + tradePartner.name +
                " but the agreement doesn't exist!");
    }

    public bool AddContract(TradeContract tradeContract, bool mustHaveImmediateResources = true) {
        if (!tradeContract.provider.AddContract(tradeContract, mustHaveImmediateResources)) return false;
        if (!tradeContract.receiver.AddContract(tradeContract, mustHaveImmediateResources)) {
            tradeContract.provider.RemoveContract(tradeContract);
            return false;
        }
        activeContracts.Add(tradeContract);
        Faction otherFaction = tradeContract.provider.faction;
        if (otherFaction == faction) otherFaction = tradeContract.receiver.faction;
        if (otherFaction != faction) otherFaction.factionTrade.activeContracts.Add(tradeContract);
        return true;
    }

    public void RemoveContract(TradeContract tradeContract) {
        if (!activeContracts.Contains(tradeContract)) return;
        tradeContract.provider.RemoveContract(tradeContract);
        tradeContract.receiver.RemoveContract(tradeContract);
        activeContracts.Remove(tradeContract);
        Faction otherFaction = tradeContract.provider.faction;
        if (otherFaction == faction) otherFaction = tradeContract.receiver.faction;
        if (otherFaction != faction) otherFaction.factionTrade.activeContracts.Remove(tradeContract);
    }

    public float GetBuyCostOfOffer(Faction otherFaction, TradeOffer tradeOffer) {
        if (otherFaction == faction) {
            return faction.battleManager.baseResourcePrice[tradeOffer.cargoType] + tradeOffer.price * .8f;
        }
        return tradeOffer.price * tradeBuyAgreements[otherFaction];
    }

    public float GetSellCostOfOffer(Faction otherFaction, TradeOffer tradeOffer) {
        if (otherFaction == faction) {
            return faction.battleManager.baseResourcePrice[tradeOffer.cargoType] + tradeOffer.price * 1.2f;
        }
        return tradeOffer.price;
    }

    public float GetOurBuyValueOfOffer(Faction otherFaction, TradeOffer tradeOffer) {
        if (otherFaction == faction) {
            return 0.7f * tradeOffer.price;
        }
        return tradeOffer.price;
    }

    public float GetOurSellValueOfOffer(Faction otherFaction, TradeOffer tradeOffer) {
        if (otherFaction == faction) {
            return tradeOffer.price * 1.3f;
        }
        return tradeOffer.price;
    }


    public IEnumerable<FactionTrade> GetFactionsWeCanBuyFrom() {
        return tradeBuyAgreements.Select(t => t.Key.factionTrade).Append(this);
    }

    public IEnumerable<FactionTrade> GetFactionsWeCanSellTo() {
        return tradeSellAgreements.Select(t => t.Key.factionTrade).Append(this);
    }

    public void RemoveStationOffersAndRequests(Station station) {
        foreach (CargoBay.CargoType c in CargoBay.allCargoTypes) {
            resourcesOffered[c].Remove(station);
            resourcesRequested[c].Remove(station);
        }
    }
}
