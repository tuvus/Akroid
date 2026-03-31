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

        planet.UpdatePlanet(10);
        Assert.Greater(districtFaction.pop.TotalPopulation(), previousPop.TotalPopulation());
        Assert.Greater(districtFaction.control, .5);
        Debug.Log(districtFaction.control);
        Assert.True(district.owner == testPlanetFac1);
    }
}
