using System;
using System.Collections.Generic;
using System.Linq;

public class FactionTrade {
    public Faction faction { get; private set; }

    public struct Offer {
        public CargoBay.CargoTypes cargoType;
        public long amount;
        public float price;

        public Offer(CargoBay.CargoTypes cargoType, long amount, float price) {
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
        public Dictionary<CargoBay.CargoTypes, Offer> cargo;

        public Contract(Unit provider, Unit reciever, params Offer[] offers) {
            this.provider = provider;
            this.receiver = reciever;
            cargo = new Dictionary<CargoBay.CargoTypes, Offer>();
            foreach (Offer offer in offers) {
                cargo.Add(offer.cargoType, offer);
            }
        }
    }

    /// <summary>
    /// The resources being offered by each station in the faction.
    /// </summary>
    public Dictionary<CargoBay.CargoTypes, Dictionary<Unit, Offer>> resourcesOffered;

    /// <summary>
    /// The resources being requested by each station in the faction.
    /// </summary>
    public Dictionary<CargoBay.CargoTypes, Dictionary<Unit, Offer>> resourcesRequested;

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
        foreach (CargoBay.CargoTypes cargoType in CargoBay.allCargoTypes) {
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
        if (!tradeBuyAgreements.Remove(tradePartner) || !tradePartner.factionTrade.tradeSellAgreements.Remove(faction))
            throw new Exception("Trying to remove a trade agreement with " + tradePartner.name +
                " but the agreement doesn't exist!");
    }

    public float GetOurBuyCostOfOffer(Faction otherFaction, Offer offer) {
        if (otherFaction == faction) {
            return offer.price * .8f;
        }
        return offer.price * tradeBuyAgreements[otherFaction];
    }

    public float GetOurSellCostOfOffer(Faction otherFaction, Offer offer) {
        if (otherFaction == faction) {
            return offer.price * 1.2f;
        }
        return offer.price;
    }


    public IEnumerable<FactionTrade> GetFactionsWeCanBuyFrom() {
        return tradeBuyAgreements.Select(t => t.Key.factionTrade).Append(this);
    }

    public IEnumerable<FactionTrade> GetFactionsWeCanSellTo() {
        return tradeSellAgreements.Select(t => t.Key.factionTrade).Append(this);
    }
}
