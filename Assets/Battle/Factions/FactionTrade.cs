using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Components.DictionaryAdapter.Xml;

public class FactionTrade {
    public Faction faction { get; private set; }

    public struct Offer {
        public CargoBay.CargoType cargoType;
        public long amount;
        public float price;

        public Offer(CargoBay.CargoType cargoType, long amount, float price) {
            this.cargoType = cargoType;
            this.amount = amount;
            this.price = price;
        }

        public Offer(Offer offer, long newAmount) {
            this.cargoType = offer.cargoType;
            this.amount = newAmount;
            this.price = offer.price;
        }
    }

    public struct Contract {
        public Unit provider;
        public Unit receiver;
        public Dictionary<CargoBay.CargoType, Offer> cargo;

        public Contract(Unit provider, Unit reciever, params Offer[] offers) {
            this.provider = provider;
            this.receiver = reciever;
            cargo = new Dictionary<CargoBay.CargoType, Offer>();
            foreach (Offer offer in offers) {
                cargo.Add(offer.cargoType, offer);
            }
        }
    }

    /// <summary>
    /// The resources being offered by each station in the faction.
    /// </summary>
    public Dictionary<CargoBay.CargoType, Dictionary<Unit, Offer>> resourcesOffered;

    /// <summary>
    /// The resources being requested by each station in the faction.
    /// </summary>
    public Dictionary<CargoBay.CargoType, Dictionary<Unit, Offer>> resourcesRequested;

    /// <summary>
    /// The factions that we can sell to and how much of a markup we have.
    /// </summary>
    public Dictionary<Faction, float> tradeSellAgreements;
    /// <summary>
    /// The factions that we can buy from and how much of a markup they have.
    /// </summary>
    public Dictionary<Faction, float> tradeBuyAgreements;
    public HashSet<Contract> activeContracts;

    public FactionTrade(Faction faction) {
        this.faction = faction;
        resourcesOffered = new();
        resourcesRequested = new();
        foreach (CargoBay.CargoType cargoType in CargoBay.allCargoTypes) {
            resourcesOffered.Add(cargoType, new());
            resourcesRequested.Add(cargoType, new());
        }
        tradeSellAgreements = new();
        tradeBuyAgreements = new();
        activeContracts = new();
    }


    public void MakeSellTradeAgreement(Faction tradePartner, float markupPrice = 1.2f) {
        if (!tradeSellAgreements.TryAdd(tradePartner, markupPrice) || !tradePartner.factionTrade.tradeBuyAgreements.TryAdd(faction, markupPrice))
            throw new Exception("Trying to start a trade agreement that already exists with " + tradePartner.name +
                "!");
    }

    public void BreakSellTradeAgreement(Faction tradePartner) {
        if (!tradeSellAgreements.Remove(tradePartner) || !tradePartner.factionTrade.tradeBuyAgreements.Remove(faction))
            throw new Exception("Trying to remove a trade agreement with " + tradePartner.name +
                " but the agreement doesn't exist!");
    }

    public bool AddContract(Contract contract, bool mustHaveImmediateResources = true) {
        if (!contract.provider.AddContract(contract, mustHaveImmediateResources)) return false;
        if (!contract.receiver.AddContract(contract, mustHaveImmediateResources)) {
            contract.provider.RemoveContract(contract);
            return false;
        }
        activeContracts.Add(contract);
        Faction otherFaction = contract.provider.faction;
        if (otherFaction == faction) otherFaction = contract.receiver.faction;
        if (otherFaction != faction) otherFaction.factionTrade.activeContracts.Add(contract);
        return true;
    }

    public void RemoveContract(Contract contract) {
        if (!activeContracts.Contains(contract)) return;
        contract.provider.RemoveContract(contract);
        contract.receiver.RemoveContract(contract);
        activeContracts.Remove(contract);
        Faction otherFaction = contract.provider.faction;
        if (otherFaction == faction) otherFaction = contract.receiver.faction;
        if (otherFaction != faction) otherFaction.factionTrade.activeContracts.Remove(contract);
    }

    public float GetBuyCostOfOffer(Faction otherFaction, Offer offer) {
        if (otherFaction == faction) {
            return faction.battleManager.baseResourcePrice[offer.cargoType] + offer.price * .8f;
        }
        return offer.price * tradeBuyAgreements[otherFaction];
    }

    public float GetSellCostOfOffer(Faction otherFaction, Offer offer) {
        if (otherFaction == faction) {
            return faction.battleManager.baseResourcePrice[offer.cargoType] + offer.price * 1.2f;
        }
        return offer.price;
    }

    public float GetOurBuyValueOfOffer(Faction otherFaction, Offer offer) {
        if (otherFaction == faction) {
            return 0.7f * offer.price;
        }
        return offer.price;
    }

    public float GetOurSellValueOfOffer(Faction otherFaction, Offer offer) {
        if (otherFaction == faction) {
            return offer.price * 1.3f;
        }
        return offer.price;
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
