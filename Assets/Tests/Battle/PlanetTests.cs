using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class PlanetTests {
    private TestBattleManager battleManager;
    private Planet planet;
    private Faction testFaction1;
    private PlanetFaction testPlanetFac1;
    private Faction testFaction2;
    private PlanetFaction testPlanetFac2;

    void SetupPlanetTests(int radius = 2) {
        battleManager = new TestBattleManager();
        testFaction1 = battleManager.CreateTestFaction();
        testFaction2 = battleManager.CreateTestFaction();
        PlanetScriptableObject planetScriptableObject = ScriptableObject.CreateInstance<PlanetScriptableObject>();
        planetScriptableObject.hasAtmosphere = true;
        planetScriptableObject.planetType = Planet.PlanetType.Terran;
        planetScriptableObject.radius = radius;
        planetScriptableObject.rotationSpeed = .1f;
        planetScriptableObject.SetTestSpriteBounds(new Vector2(1000, 1000));
        planet = battleManager.CreateNewPlanet(new Planet.PlanetData(new BattleObject.BattleObjectData("TestPlanet")),
            planetScriptableObject);
        testPlanetFac1 = planet.AddFaction(testFaction1, "testFaction1");
        testPlanetFac2 = planet.AddFaction(testFaction2, "testFaction2");
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void TestPlanetSetup() {
        SetupPlanetTests();
        planet.GenerateFactionTerritories(new List<(PlanetFaction, float)> {
            (testPlanetFac1, .3f),
            (testPlanetFac2, .4f),
        }, .9f, .1f, false);
        Assert.AreEqual(7, planet.planetMap.districts.Count);
        Assert.Greater(planet.totalArea, 0);
        Assert.Greater(planet.planetMap.GetDistrict(Vector2Int.zero).area, 0);
        Assert.AreEqual(planet.planetMap.GetNeighboringDistricts(planet.planetMap.GetDistrict(Vector2Int.zero)).Count, 6);
        Assert.Greater(testPlanetFac1.GetTotalControl(), .19f);
        Assert.Greater(testPlanetFac2.GetTotalControl(), .29f);
        Assert.Greater(testPlanetFac1.GetTotalPopulation().TotalPopulation(), 0);
        Assert.Greater(testPlanetFac2.GetTotalPopulation().TotalPopulation(), 0);
        Assert.AreEqual(planet.GetPopulation(),
            planet.planetFactions.Select(pf => pf.Value.GetTotalPopulation().TotalPopulation()).Sum());
        planet.planetMap.districts.Where(d => d.owner == null).ToList().ForEach(district => {
            district.SetRandomDistrictType(false);
        });
        Assert.True(planet.planetMap.districts.Any(d => d.districtType != District.DistrictType.Empty));
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void TestPlanetDistrictGrow() {
        SetupPlanetTests(1);
        District district = planet.planetMap.districts.First();
        district.SetRandomDistrictType(false);
        var districtFaction = district.AddFaction(testPlanetFac1, .5f, .5f);
        Population previousPop = new Population(districtFaction.pop);
        Assert.AreEqual(.5f, districtFaction.control);
        Assert.Greater(districtFaction.pop.TotalPopulation(), 0);
        Assert.AreEqual(districtFaction.districtAction, DistrictFaction.DistrictAction.None);

        planet.UpdatePlanet(10);

        Assert.Greater(districtFaction.pop.TotalPopulation(), previousPop.TotalPopulation());
        Assert.Greater(districtFaction.control, .5);
        Assert.True(district.owner == testPlanetFac1);
        Assert.AreEqual(districtFaction.districtAction, DistrictFaction.DistrictAction.None);
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void TestPlanetDistrictExpand() {
        SetupPlanetTests();
        District district = planet.planetMap.districts.First();
        district.SetRandomDistrictType(false);
        var districtFaction = district.AddFaction(testPlanetFac1, 1f, 1f);
        district.owner = testPlanetFac1;

        Assert.True(district.owner == testPlanetFac1);
        planet.UpdatePlanet(10);

        Assert.AreEqual(districtFaction.districtAction, DistrictFaction.DistrictAction.Expand);
        var expandDistrict = districtFaction.targetDistrict;
        Assert.True(expandDistrict.districtFactions.ContainsKey(testPlanetFac1));
        var expandDistrictFaction = expandDistrict.districtFactions[testPlanetFac1];
        Assert.Greater(expandDistrictFaction.pop.TotalPopulation(), 0);
        Assert.Greater(expandDistrictFaction.control, 0);
        Assert.AreEqual(expandDistrictFaction.districtAction, DistrictFaction.DistrictAction.None);

        planet.UpdatePlanet(10);

        // Make sure that the district continues expanding in the same location
        Assert.AreEqual(districtFaction.districtAction, DistrictFaction.DistrictAction.Expand);
        Assert.AreEqual(districtFaction.targetDistrict, expandDistrict);
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void TestPlanetDistrictAttack() {
        SetupPlanetTests();
        District district1 = planet.planetMap.districts.First();
        district1.SetTerrainType(District.TerrainType.Plains);
        district1.SetRandomDistrictType(false);
        var districtFaction1 = district1.AddFaction(testPlanetFac1, 1f, 1f);
        district1.owner = testPlanetFac1;
        District district2 = planet.planetMap.districts[1];
        district2.SetTerrainType(District.TerrainType.Plains);
        district2.SetRandomDistrictType(false);
        var districtFaction2 = district2.AddFaction(testPlanetFac2, .1f, 1f);
        district2.owner = testPlanetFac2;
        testFaction1.StartWar(testFaction2);
        Assert.Greater(districtFaction2.pop.TotalPopulationWithoutMarines(), 0);

        planet.UpdatePlanet(10);

        Assert.AreEqual(districtFaction1.districtAction, DistrictFaction.DistrictAction.Attack);
        Assert.AreEqual(districtFaction1.targetDistrict, district2);
        Assert.Less(districtFaction2.control, 1f);

        Assert.True(district2.districtFactions.ContainsKey(testPlanetFac1));
        var districtFaction12 = district2.districtFactions[testPlanetFac1];
        Assert.Greater(districtFaction12.control, 0);
        Assert.Greater(districtFaction12.control + districtFaction2.control, .99);
        Assert.Greater(districtFaction12.pop.TotalPopulationWithoutMarines(), 0);

        planet.UpdatePlanet(100);
        Assert.False(district2.districtFactions.ContainsKey(testPlanetFac2));
        Assert.AreEqual(1, districtFaction12.control);
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void TestPlanetDistrictNoAttack() {
        SetupPlanetTests();
        testPlanetFac1.combatStrategy = PlanetFaction.CombatStrategy.Cautious;
        testPlanetFac2.combatStrategy = PlanetFaction.CombatStrategy.Cautious;
        District district1 = planet.planetMap.districts.First();
        district1.SetTerrainType(District.TerrainType.Plains);
        district1.SetRandomDistrictType(false);
        var districtFaction1 = district1.AddFaction(testPlanetFac1, 1f, 1f);
        district1.owner = testPlanetFac1;
        District district2 = planet.planetMap.districts[1];
        district2.SetTerrainType(District.TerrainType.Plains);
        district2.SetRandomDistrictType(false);
        var districtFaction2 = district2.AddFaction(testPlanetFac2, 1f, 1f);
        district2.owner = testPlanetFac2;
        testFaction1.StartWar(testFaction2);
        Assert.Greater(districtFaction2.pop.TotalPopulationWithoutMarines(), 0);

        planet.UpdatePlanet(10);

        Assert.AreNotEqual(districtFaction1.districtAction, DistrictFaction.DistrictAction.Attack);
        Assert.AreNotEqual(districtFaction2.districtAction, DistrictFaction.DistrictAction.Attack);
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void TestPlanetDistrictDefenselessAttack() {
        SetupPlanetTests();
        District district1 = planet.planetMap.districts.First();
        district1.SetTerrainType(District.TerrainType.Plains);
        district1.SetRandomDistrictType(false);
        var districtFaction1 = district1.AddFaction(testPlanetFac1, 1f, 1f);
        district1.owner = testPlanetFac1;
        District district2 = planet.planetMap.districts[1];
        district2.SetTerrainType(District.TerrainType.Plains);
        district2.SetRandomDistrictType(false);
        var districtFaction2 = district2.AddFaction(testPlanetFac2, 1f, 1f);
        district2.owner = testPlanetFac2;
        testFaction1.StartWar(testFaction2);
        districtFaction2.pop.marines = 0;

        planet.UpdatePlanet(10);

        Assert.AreEqual(districtFaction1.districtAction, DistrictFaction.DistrictAction.Attack);
        Assert.AreEqual(districtFaction1.targetDistrict, district2);
        Assert.Less(districtFaction2.control, 1f);
    }


    [Explicit] [Category("Unit Tests")]
    [Test]
    public void TestPlanetDistrictAttack2() {
        SetupPlanetTests();

        testPlanetFac1.combatStrategy = PlanetFaction.CombatStrategy.Risky;

        District district1 = planet.planetMap.districts.First();
        district1.SetTerrainType(District.TerrainType.Plains);
        district1.SetRandomDistrictType(false);
        var districtFaction1 = district1.AddFaction(testPlanetFac1, 1f, 1f);
        district1.owner = testPlanetFac1;
        District district2 = planet.planetMap.districts[1];
        district2.SetTerrainType(District.TerrainType.Plains);
        district2.SetRandomDistrictType(false);
        var districtFaction2 = district2.AddFaction(testPlanetFac2, 1f, 1f);
        district2.owner = testPlanetFac2;
        District district3 = planet.planetMap.districts[2];
        district3.SetTerrainType(District.TerrainType.Plains);
        district3.SetRandomDistrictType(false);
        var districtFaction3 = district3.AddFaction(testPlanetFac1, 1f, 1f);
        district3.owner = testPlanetFac1;
        testFaction1.StartWar(testFaction2);
        Assert.Greater(districtFaction2.pop.TotalPopulationWithoutMarines(), 0);

        planet.UpdatePlanet(10);

        Assert.AreEqual(districtFaction1.districtAction, DistrictFaction.DistrictAction.Attack);
        Assert.AreEqual(districtFaction1.targetDistrict, district2);
        Assert.AreEqual(districtFaction3.districtAction, DistrictFaction.DistrictAction.Attack);
        Assert.AreEqual(districtFaction3.targetDistrict, district2);
        Assert.Less(districtFaction2.control, 1f);

        Assert.True(district2.districtFactions.ContainsKey(testPlanetFac1));
        var districtFaction12 = district2.districtFactions[testPlanetFac1];
        Assert.Greater(districtFaction12.control, 0);
        Assert.Greater(districtFaction12.control + districtFaction2.control, .99);
        Assert.Greater(districtFaction12.pop.TotalPopulationWithoutMarines(), 0);

        planet.UpdatePlanet(100);
        Assert.False(district2.districtFactions.ContainsKey(testPlanetFac2));
        Assert.AreEqual(1, districtFaction12.control);
    }

}
