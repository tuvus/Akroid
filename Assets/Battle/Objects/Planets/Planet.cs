using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class Planet : BattleObject, IPositionConfirmer {
    /// <summary> Determines the base amount of population that one territory value can hold. </summary>
    public static readonly long populationPerTerritoryValue = 15000;

    public enum PlanetType {
        Terran,
        Moon,
        GasGiant,
    }

    public Dictionary<Faction, PlanetFaction> planetFactions;

    public float rotationSpeed;
    [SerializeField] private float timeSinceStart;
    // The time until the next update
    private float updateTime;
    // How long between updates
    private float updateSpeed = 10;

    public PlanetScriptableObject planetScriptableObject { get; }
    [field: SerializeField] public long totalArea { get; protected set; }
    public long districtArea { get; protected set; }
    public PlanetMap planetMap;
    public Dictionary<District, DistrictCombat> districtsInCombat;

    public class DistrictCombat {
        public District district;
        public List<DistrictFaction> attackers;
        public List<DistrictFaction> defenders;

        public DistrictCombat(District district) {
            this.district = district;
            attackers = new List<DistrictFaction>();
            defenders = new List<DistrictFaction>();
        }

        public void RemoveAttacker(DistrictFaction attacker) {
            attackers.Remove(attacker);
            if (attackers.Count == 0)
                defenders.Where(d =>
                        d.districtAction == DistrictFaction.DistrictAction.Reinforce && d.targetDistrict == district)
                    .ToList()
                    .ForEach(d => d.StopDistrictAction());
            district.planetMap.planet.districtsInCombat.Remove(district);
        }
    }


    public Planet(PlanetData planetData, BattleManager battleManager, PlanetScriptableObject planetScriptableObject) :
        base(planetData.battleObjectData, battleManager) {
        this.planetScriptableObject = planetScriptableObject;
        rotationSpeed = planetScriptableObject.rotationSpeed;
        rotationSpeed *= random.NextFloat(.5f, 1.5f);
        if (random.NextFloat(-1, 1) < 0) {
            rotationSpeed *= -1;
        }

        districtsInCombat = new Dictionary<District, DistrictCombat>();
        planetFactions = new Dictionary<Faction, PlanetFaction>();
        visible = true;
        Spawn();
        SetSize(SetupSize());

        totalArea = (long)(math.pow(GetSize(), 2) * math.PI);
        districtArea = totalArea / PlanetMap.GetDistrictCountInRadius(planetScriptableObject.radius);
        planetMap = new PlanetMap(this, random, planetScriptableObject.radius);
        updateTime = random.NextFloat(updateSpeed);
    }

    bool IPositionConfirmer.ConfirmPosition(Vector2 position, float minDistanceFromObject) {
        foreach (Star star in battleManager.stars) {
            if (Vector2.Distance(position, star.position) <= minDistanceFromObject + star.GetSize() + GetSize()) {
                return false;
            }
        }

        foreach (AsteroidField asteroidField in battleManager.asteroidFields) {
            if (Vector2.Distance(position, asteroidField.GetPosition()) <=
                minDistanceFromObject + asteroidField.GetSize() + GetSize()) {
                return false;
            }
        }

        foreach (Station station in battleManager.stations) {
            if (Vector2.Distance(position, station.GetPosition()) <=
                minDistanceFromObject + station.GetSize() + GetSize()) {
                return false;
            }
        }

        foreach (Planet planet in battleManager.planets) {
            if (Vector2.Distance(position, planet.GetPosition()) <=
                minDistanceFromObject + planet.GetSize() + GetSize()) {
                return false;
            }
        }

        return true;
    }

    protected override Vector2 GetSetupPosition(BattleManager.PositionGiver positionGiver) {
        if (positionGiver.isExactPosition)
            return positionGiver.position;
        Vector2? targetPosition = battleManager.FindFreeLocationIncrement(positionGiver, this);
        if (targetPosition.HasValue)
            return targetPosition.Value;
        return positionGiver.position;
    }

    public void UpdatePlanet(float deltaTime) {
        timeSinceStart += deltaTime;
        SetRotation(rotation + rotationSpeed * deltaTime);

        updateTime -= Time.fixedDeltaTime;
        if (updateTime > 0)
            return;
        updateTime += updateSpeed;
        deltaTime *= updateSpeed;

        foreach (KeyValuePair<Faction, PlanetFaction> faction in planetFactions) {
            faction.Value.UpdateFaction(deltaTime);
            planetMap.districts.ForEach(d => d.UpdateDistrict(deltaTime));
        }

        foreach (var districtCombat in districtsInCombat.Values.ToList()) {
            var attackingFactions = districtCombat.attackers.Select(df => df.planetFaction).Distinct().ToList();
            var allDefenders = districtCombat.defenders.Union(districtCombat.district.districtFactions.Values).ToList();
            foreach (PlanetFaction attackingFaction in attackingFactions) {
                var attackers = districtCombat.attackers.Where(df => df.planetFaction == attackingFaction)
                    .ToList();
                var defenders =
                    allDefenders.Where(d => attackingFaction.faction.IsAtWarWithFaction(d.planetFaction.faction))
                        .ToList();
                if (!defenders.Any()) {
                    // There is nothing to attack
                    attackers.ForEach(df => { df.StopDistrictAction(); });
                    continue;
                }
                DoCombat(attackingFaction, attackers, defenders, districtCombat.district, deltaTime);
            }
        }
    }

    public void DoCombat(PlanetFaction planetFaction, List<DistrictFaction> attackers,
        List<DistrictFaction> defenders, District district, float deltaTime) {
        Faction attackingFaction = planetFaction.faction;
        // Random factor of the fight, a higher value means the attackers are doing better
        float bias = random.NextFloat(-.5f, .3f);

        long numAttackers =
            (long)(attackers.Select(df => df.pop.marines).Sum() * planetFaction.GetAttackRatioOfStrategy());
        if (numAttackers == 0) return;
        List<(DistrictFaction, long, float)> defenderForces = defenders.Select(defender => (defender,
                (long)(defender.pop.marines * (defender.districtAction == DistrictFaction.DistrictAction.Attack ?
                    1 - defender.planetFaction.GetAttackRatioOfStrategy() : 1))))
            .Select(d => (d.defender, d.Item2,
                d.Item2 * d.defender.planetFaction.faction.GetAllAttackDamageModifiers())).ToList();

        long numDefenders = defenderForces.Select(d => d.Item2).Sum();
        float attackingForce = numAttackers * attackingFaction.GetAllAttackDamageModifiers();
        float defendingForce = defenderForces.Select(d => d.Item3).Sum();

        // Calculate military that was killed in the combat
        long attackersKilled = math.min(numAttackers, (long)(defendingForce * (1 - bias) * deltaTime * .08f));
        long defendersKilled = math.min(numDefenders, (long)(attackingForce * (1 - bias) * deltaTime * .08f));

        var defenderDistrictsByForce = defenders.Where(d => d.district == district).Select(d =>
                (d, defenderForces.Where(df => d.planetFaction == df.Item1.planetFaction).Select(df => df.Item3).Sum()))
            .ToList();

        float controlDelta = 1;
        if (attackingForce > defendingForce) {
            // Attackers have gained some ground
            float controlGained = math.min(math.min(1, .1f * deltaTime),
                (1 - math.pow((defendingForce / attackingForce), .2f)) * deltaTime);
            controlDelta += controlGained;
            var totalDefenderControl = defenderDistrictsByForce.Select(d => d.d.control).Sum();
            controlGained = math.min(totalDefenderControl, controlGained);

            float ungainedControl = 0;
            defenderDistrictsByForce.ForEach(d => {
                var controlToSubtract = controlGained * math.max(1, defendingForce) / math.max(1,d.Item2);
                d.d.control -= controlToSubtract;
                ungainedControl = math.min(0, d.d.control);
                // Check if the defender doesn't have enough control to hold the district
                if (d.d.control <= 0.0001) {
                    district.RemoveFaction(d.d.planetFaction);
                }
            });
            controlGained -= ungainedControl;

            DistrictFaction conqueredDistrict = attackers.Find(df => df.district == district);
            if (conqueredDistrict == null &&
                !district.districtFactions.TryGetValue(planetFaction, out conqueredDistrict)) {
                district.AddFaction(planetFaction, 0, controlGained);
                conqueredDistrict = district.districtFactions[planetFaction];
            } else {
                conqueredDistrict.control += controlGained;
            }

            // Move troops to the territory conquered
            long troopsTransferred =
                (long)((numAttackers - attackersKilled) * math.min(1, deltaTime) * controlGained / 2);
            conqueredDistrict.pop.marines += troopsTransferred;
            attackers.ForEach(df => {
                df.pop.marines -= (long)(troopsTransferred * df.pop.marines *
                    df.planetFaction.GetAttackRatioOfStrategy() / numAttackers);
            });
        } else {
            // Defenders have gained some ground
            DistrictFaction attackerDistrict =
                district.districtFactions.Select(df => df.Value)
                    .FirstOrDefault(df => df.planetFaction == planetFaction);
            float controlGained = 1 - math.pow((attackingForce / defendingForce), .2f) * deltaTime;
            controlDelta -= controlGained;
            controlGained = math.min(attackerDistrict?.control ?? 0, controlGained);
            if (attackerDistrict != null) {
                attackerDistrict.control -= controlGained;
                defenderDistrictsByForce.ForEach(d => d.d.AddControl(controlGained * d.Item2 / attackingForce));
                if (attackerDistrict.control <= .00001f)
                    district.RemoveFaction(attackerDistrict.planetFaction);
            }

            // If the defenders could not gain the full value of the territory they pushed for
            // then they will lose fewer troops as compensation
            defendersKilled = (long)(defendersKilled * (controlDelta + controlGained));
        }

        attackers.ForEach(df => {
            df.pop.marines -= (long)(attackersKilled * df.pop.marines * df.planetFaction.GetAttackRatioOfStrategy() /
                numAttackers);
        });

        var defendingCiviliansKilled = (long)(defendersKilled * 8 * (1 + bias * 2 * controlDelta));
        defenderDistrictsByForce.ForEach(d => {
            if (d.Item2 <= float.Epsilon) {
                d.d.pop.marines = 0;
                d.d.pop.civilians = 0;
                return;
            }
            d.d.pop.marines -= defendersKilled;
            d.d.pop.civilians -=
                math.min(d.d.pop.civilians, (long)(defendingCiviliansKilled * defendingForce / d.Item2));
        });

        // War is bad for everyone
        long totalAttackerCivilians = attackers.Select(df => df.pop.civilians).Sum();
        long attackerCiviliansKilled = math.min(totalAttackerCivilians,
            (long)(attackersKilled * 8 * (1 - bias * 2 * controlDelta)));
        if (totalAttackerCivilians > 0)
            attackers.ToList().ForEach(df =>
                df.pop.civilians -= attackerCiviliansKilled * df.pop.civilians / totalAttackerCivilians);
    }

    /// <summary> Adds a planet faction to the planet with the faction, territory, force given </summary>
    public PlanetFaction AddFaction(Faction faction, Population population, string special,
        PlanetFaction.CombatStrategy combatStrategy = PlanetFaction.CombatStrategy.Balanced) {
        var planetFaction = new PlanetFaction(this, faction, special, combatStrategy);
        planetFactions.Add(faction, planetFaction);
        faction.AddPlanet(this);
        return planetFaction;
    }

    public PlanetFaction AddFaction(Faction faction, long population, string special,
        PlanetFaction.CombatStrategy combatStrategy = PlanetFaction.CombatStrategy.Balanced) {
        return AddFaction(faction, new Population().SetPlanetPopulation(population), special, combatStrategy);
    }

    /// <summary>
    /// Divides the planet's remaining territories to the factions based on the input.
    /// Applies some randomness to the input based on the randomFactor.
    /// Any extra territory will be left as unclaimed.
    /// </summary>
    public void GenerateFactionTerritories(List<(PlanetFaction, float)> factionTerritories, float populationPercent,
        float randomFactor, bool takeoverTerritories) {
        float initialSum = factionTerritories.Select(ft => ft.Item2).Sum();
        // Apply randomness on the territories based on randomFactor
        factionTerritories = factionTerritories.Select(ft =>
            (ft.Item1, ft.Item2 * random.NextFloat(1 - randomFactor, 1 + randomFactor))).ToList();
        // Now we need to normalize the percentage of territories of each planet faction
        float newSum = factionTerritories.Sum(ft => ft.Item2);
        factionTerritories = factionTerritories.Select(ft =>
            (ft.Item1, ft.Item2 / (1 + initialSum - newSum))).ToList();

        List<District> toTake = planetMap.districts.Where(d => d.owner == null || takeoverTerritories)
            .OrderByDescending(d => d.GetDistrictValue()).ToList();
        int totalDistrictValue = toTake.Sum(d => d.GetDistrictValue());

        while (toTake.Count > 0 && factionTerritories.Count > 0) {
            factionTerritories = factionTerritories.OrderByDescending(ft => ft.Item2).ToList();
            PlanetFaction planetFaction = factionTerritories.First().Item1;
            List<District> possibleDistricts = toTake.Where(d =>
                factionTerritories.First().Item2 - d.GetDistrictValue() / (float)totalDistrictValue > 0).ToList();
            if (possibleDistricts.Count == 0) {
                factionTerritories.RemoveAt(0);
                continue;
            }
            District district = possibleDistricts.Aggregate((max, current) =>
                planetFaction.GetValueOfDistrict(current) > planetFaction.GetValueOfDistrict(max) ? current : max
            );
            district.owner = planetFaction;
            district.SetRandomDistrictType(false);
            district.AddFaction(planetFaction,
                populationPercent * random.NextFloat(1 - randomFactor, 1 + randomFactor),
                .5f);
            float newControl =
                factionTerritories.First().Item2 - district.GetDistrictValue() / (float)totalDistrictValue;
            factionTerritories.Add((planetFaction, newControl));
            factionTerritories.RemoveAt(0);
            toTake.Remove(district);
        }
    }

    public void AddColony(Faction faction, Population population, string special) {
        var district = planetMap.districts.Where(d => d.owner == null).Aggregate((mostValuable, next) => {
            return next.GetDistrictValue() > mostValuable.GetDistrictValue() ? next : mostValuable;
        });
        var newPlanetFaction = AddFaction(faction, population, special);
        district.owner = newPlanetFaction;
        district.districtFactions.Add(newPlanetFaction,
            new DistrictFaction(district, newPlanetFaction, population, .03f));
    }

    /// <param name="planetFaction">The resulting bigger planet faction</param>
    /// <param name="toMerge">The planet faction to merge into the other planet faction</param>
    public void MergePlanetFactions(PlanetFaction planetFaction, PlanetFaction toMerge) {
        foreach (District district in planetMap.districts) {
            if (!district.districtFactions.ContainsKey(toMerge)) continue;
            if (!district.districtFactions.ContainsKey(planetFaction)) {
                district.districtFactions.Add(planetFaction, district.districtFactions[toMerge]);
                district.districtFactions[planetFaction].planetFaction = planetFaction;
            } else {
                district.districtFactions[planetFaction].pop.AddPopulation(district.districtFactions[toMerge].pop);
                district.districtFactions[planetFaction].control += district.districtFactions[toMerge].control;
            }
            district.districtFactions.Remove(toMerge);
            if (district.owner == toMerge) district.owner = planetFaction;
        }
        planetFactions.Remove(toMerge.faction);
    }

    /// <param name="planetFaction">The resulting bigger planet faction</param>
    /// <param name="toMerge">The planet faction to merge into the other planet faction</param>
    public void MergePlanetFactions(Faction planetFaction, Faction toMerge) {
        MergePlanetFactions(planetFactions[planetFaction], planetFactions[toMerge]);
    }

    public void RemoveFaction(Faction faction) {
        planetFactions.Remove(faction);
        faction.RemovePlanet(this);
    }

    public long GetPopulation() {
        return planetFactions.Sum(f => f.Value.GetTotalPopulation().TotalPopulation());
    }

    public long GetPopulationWithoutMarines() {
        return planetFactions.Sum(f => f.Value.GetTotalPopulation().TotalPopulationWithoutMarines());
    }

    public override float GetSpriteSize() {
        return Calculator.GetSpriteSizeFromBounds(planetScriptableObject.spriteBounds, scale);
    }

    public override GameObject GetPrefab() {
        return planetScriptableObject.prefab;
    }

    public struct PlanetData {
        public BattleObjectData battleObjectData;

        public PlanetData(BattleObjectData battleObjectData) {
            this.battleObjectData = battleObjectData;
        }
    }
}
