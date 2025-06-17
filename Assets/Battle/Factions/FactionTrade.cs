using System;
using System.Collections.Generic;
using System.Linq;

public class FactionTrade {
    private Faction faction;

    public struct Offer {
        public CargoBay.CargoTypes cargoType;
        public long amount;
        public float price;

        public Offer(CargoBay.CargoTypes cargoType, long amount, float price) {
            this.cargoType = cargoType;
            this.amount = amount;
            this.price = price;
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
    /// The tuple represents how much is being offered and at what the base price per unit is.
    /// </summary>
    public Dictionary<CargoBay.CargoTypes, Dictionary<Unit, Offer>> resourcesOffered;

    /// <summary>
    /// The resources being requested by each station in the faction.
    /// The tuple represents how much is being offered and at what the base price per unit is.
    /// </summary>
    public Dictionary<CargoBay.CargoTypes, Dictionary<Unit, Contract>> resourcesRequested;

    /// <summary>
    /// The factions that this faction can buy from their price modifier.
    /// </summary>
    public Dictionary<Faction, float> tradeBuyAgreements;

    public FactionTrade(Faction faction) {
        this.faction = faction;
        resourcesOffered = new();
        resourcesRequested = new();
        foreach (CargoBay.CargoTypes cargoType in Enum.GetValues(typeof(CargoBay.CargoTypes)).Cast<CargoBay.CargoTypes>()) {
            resourcesOffered.Add(cargoType, new());
            resourcesRequested.Add(cargoType, new());
        }
        tradeBuyAgreements = new();
    }

    public void MakeSellTradeAgreement(Faction tradePartner, float markupPrice = 1.2f) {
        if (!tradeBuyAgreements.TryAdd(tradePartner, markupPrice))
            throw new Exception(
                "Trying to start a trade agreement that already exists with " + tradePartner.name + "!");
    }

    public void BreakSellTradeAgreement(Faction tradePartner) {
        if (!tradePartner.factionTrade.tradeBuyAgreements.Remove(faction))
            throw new Exception("Trying to remove a trade agreement with " + tradePartner.name +
                " but the agreement doesn't exist!");
    }


}
