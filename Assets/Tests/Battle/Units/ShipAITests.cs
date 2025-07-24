using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class ShipAITests {
    private TestBattleManager battleManager;
    private Station testStation;
    private Station testStation2;
    private Faction testFaction;
    private Faction testFaction2;
    private Ship testShip;

    void SetupTradeTests() {
        battleManager = new TestBattleManager();
        testFaction = battleManager.CreateTestFaction();
        CargoBayScriptableObject cargoBayScriptableObject = ScriptableObject.CreateInstance<CargoBayScriptableObject>();
        cargoBayScriptableObject.cargoBaySize = 1000;
        cargoBayScriptableObject.maxCargoBays = 12;
        HangarScriptableObject hangarScriptableObject = ScriptableObject.CreateInstance<HangarScriptableObject>();
        hangarScriptableObject.maxDockSpace = 10000;
        HabitationAreaScriptableObject habitationAreaScriptableObject =
            ScriptableObject.CreateInstance<HabitationAreaScriptableObject>();
        habitationAreaScriptableObject.populationSpace = 100;
        testStation = battleManager.CreateNewStation(new BattleObject.BattleObjectData("TestStation", testFaction),
            ScriptableObject.CreateInstance<TestUtils.TestStationScriptableObject>()
                .SetupScriptableObject(cargoBayScriptableObject, hangarScriptableObject,
                    habitationAreaScriptableObject), true);
        testFaction2 = battleManager.CreateTestFaction();
        testStation2 = battleManager.CreateNewStation(new BattleObject.BattleObjectData("TestStation2", testFaction2),
            ScriptableObject.CreateInstance<TestUtils.TestStationScriptableObject>()
                .SetupScriptableObject(cargoBayScriptableObject, hangarScriptableObject,
                    habitationAreaScriptableObject), true);

        CargoBayScriptableObject cargoBayScriptableObject2 =
            ScriptableObject.CreateInstance<CargoBayScriptableObject>();
        cargoBayScriptableObject2.cargoBaySize = 100;
        cargoBayScriptableObject2.maxCargoBays = 6;
        testShip = testStation.BuildShip(new BattleObject.BattleObjectData("TestShip", testFaction),
            ScriptableObject.CreateInstance<TestUtils.TestShipScriptableObject>()
                .SetupScriptableObject(cargoBayScriptableObject2, habitationAreaScriptableObject));
        testFaction.factionTrade.MakeSellTradeAgreement(testFaction2);
        testFaction2.factionTrade.MakeSellTradeAgreement(testFaction);
        Assert.AreEqual(testStation, testShip.dockedStation);
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void TestTradeBetweenStations() {
        SetupTradeTests();
        ShipAI shipAI = testShip.shipAI;

        var contract = shipAI.GetBestContractsBetweenStations(testStation, testStation2);
        Assert.Null(contract.Item1);
        Assert.Null(contract.Item2);

        testStation2.ReserveCargo(500, CargoBay.CargoType.Metal);
        Assert.Null(contract.Item1);
        Assert.Null(contract.Item2);

        testStation.LoadCargo(1000, CargoBay.CargoType.Metal);
        testStation2.UnReserveCargo(500, CargoBay.CargoType.Metal);
        Assert.Null(contract.Item1);
        Assert.Null(contract.Item2);

        Assert.Zero(testStation.LoadCargo(1000, CargoBay.CargoType.Metal));
        testStation2.ReserveCargo(500, CargoBay.CargoType.Metal);
        contract = shipAI.GetBestContractsBetweenStations(testStation, testStation2);
        Assert.AreEqual(500, contract.Item1.cargo[CargoBay.CargoType.Metal].amount);
        Assert.AreEqual(500, contract.Item2.cargo[CargoBay.CargoType.Metal].amount);

        Assert.Zero(testStation.UseCargo(1800, CargoBay.CargoType.Metal));
        contract = shipAI.GetBestContractsBetweenStations(testStation, testStation2);
        Assert.AreEqual(200, contract.Item1.cargo[CargoBay.CargoType.Metal].amount);
        Assert.AreEqual(200, contract.Item2.cargo[CargoBay.CargoType.Metal].amount);
        Assert.Greater(contract.Item5, 0);
        float previousValue = contract.Item5;

        Assert.Zero(testShip.LoadCargo(200, CargoBay.CargoType.Metal));
        contract = shipAI.GetBestContractsBetweenStations(testStation, testStation2);
        Assert.AreEqual(200, contract.Item1.cargo[CargoBay.CargoType.Metal].amount);
        Assert.AreEqual(400, contract.Item2.cargo[CargoBay.CargoType.Metal].amount);
        Assert.Greater(contract.Item5, previousValue);

        Assert.Zero(testStation.UseCargo(200, CargoBay.CargoType.Metal));
        contract = shipAI.GetBestContractsBetweenStations(testStation, testStation2);
        Assert.Null(contract.Item1);
        Assert.AreEqual(200, contract.Item2.cargo[CargoBay.CargoType.Metal].amount);
        Assert.Greater(contract.Item5, previousValue);

        // Test Hybrid cargo
        testStation2.ReserveCargo(300, CargoBay.CargoType.Gas);
        contract = shipAI.GetBestContractsBetweenStations(testStation, testStation2);
        Assert.Null(contract.Item1);
        Assert.AreEqual(200, contract.Item2.cargo[CargoBay.CargoType.Metal].amount);

        Assert.Zero(testStation.LoadCargo(300, CargoBay.CargoType.Gas));
        contract = shipAI.GetBestContractsBetweenStations(testStation, testStation2);
        Assert.AreEqual(300, contract.Item1.cargo[CargoBay.CargoType.Gas].amount);
        Assert.False(contract.Item1.cargo.ContainsKey(CargoBay.CargoType.Metal));
        Assert.AreEqual(200, contract.Item2.cargo[CargoBay.CargoType.Metal].amount);
        Assert.AreEqual(300, contract.Item2.cargo[CargoBay.CargoType.Gas].amount);

        Assert.Zero(testStation.LoadCargo(200, CargoBay.CargoType.Metal));
        testStation2.UnReserveCargo(300, CargoBay.CargoType.Metal);
        contract = shipAI.GetBestContractsBetweenStations(testStation, testStation2);
        Assert.AreEqual(300, contract.Item1.cargo[CargoBay.CargoType.Gas].amount);
        Assert.False(contract.Item1.cargo.ContainsKey(CargoBay.CargoType.Metal));
        Assert.AreEqual(200, contract.Item2.cargo[CargoBay.CargoType.Metal].amount);
        Assert.AreEqual(300, contract.Item2.cargo[CargoBay.CargoType.Gas].amount);

        Assert.Zero(testStation.LoadCargo(500, CargoBay.CargoType.Metal));
        testStation2.ReserveCargo(300, CargoBay.CargoType.Metal);
        contract = shipAI.GetBestContractsBetweenStations(testStation, testStation2);
        Assert.AreEqual(400, contract.Item1.cargo.Sum(c => c.Value.amount));
        Assert.GreaterOrEqual(contract.Item2.cargo[CargoBay.CargoType.Metal].amount, 300);
        Assert.AreEqual(600, contract.Item2.cargo.Sum(c => c.Value.amount));

        Assert.Zero(testStation.LoadCargo(500, CargoBay.CargoType.Gas));
        contract = shipAI.GetBestContractsBetweenStations(testStation, testStation2);
        Assert.AreEqual(400, contract.Item1.cargo.Sum(c => c.Value.amount));
        Assert.Greater(contract.Item1.cargo[CargoBay.CargoType.Metal].amount, 200);
        Assert.AreEqual(600, contract.Item2.cargo.Sum(c => c.Value.amount));
        previousValue = contract.Item5;

        Assert.Zero(testShip.UseCargo(200, CargoBay.CargoType.Metal));
        contract = shipAI.GetBestContractsBetweenStations(testStation, testStation2);
        Assert.AreEqual(600, contract.Item1.cargo.Sum(c => c.Value.amount));
        Assert.AreEqual(600, contract.Item2.cargo.Sum(c => c.Value.amount));
        Assert.Less(contract.Item5, previousValue);
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void TestTradeBetweenStationsHalfCargoBays() {
        SetupTradeTests();
        ShipAI shipAI = testShip.shipAI;

        Assert.Zero(testStation.LoadCargo(1000, CargoBay.CargoType.Metal));
        testStation2.ReserveCargo(1000, CargoBay.CargoType.Metal);
        Assert.Zero(testShip.LoadCargo(50, CargoBay.CargoType.Metal));
        Assert.Zero(testShip.LoadCargo(50, CargoBay.CargoType.Gas));
        var contract = shipAI.GetBestContractsBetweenStations(testStation, testStation2);
        Assert.AreEqual(450, contract.Item1.cargo[CargoBay.CargoType.Metal].amount);
        Assert.AreEqual(500, contract.Item2.cargo[CargoBay.CargoType.Metal].amount);
        Assert.Greater(contract.Item5, 0);

        testStation2.ReserveCargo(1000, CargoBay.CargoType.Gas);
        Assert.Zero(testStation.LoadCargo(1000, CargoBay.CargoType.Gas));
        contract = shipAI.GetBestContractsBetweenStations(testStation, testStation2);
        Assert.AreEqual(500, contract.Item1.cargo.Sum(c => c.Value.amount));
        Assert.AreEqual(600, contract.Item2.cargo.Sum(c => c.Value.amount));
        Assert.GreaterOrEqual(contract.Item2.cargo[CargoBay.CargoType.Metal].amount, 50);
        Assert.GreaterOrEqual(contract.Item2.cargo[CargoBay.CargoType.Gas].amount, 50);
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void TestTradeBetweenStationsHalfCargoBays2() {
        SetupTradeTests();
        ShipAI shipAI = testShip.shipAI;

        Assert.Zero(testStation.LoadCargo(1000, CargoBay.CargoType.Metal));
        testStation2.ReserveCargo(1000, CargoBay.CargoType.Gas);
        Assert.Zero(testShip.LoadCargo(550, CargoBay.CargoType.Metal));
        var contract = shipAI.GetBestContractsBetweenStations(testStation, testStation2);
        Assert.Null(contract.Item1);
        Assert.Null(contract.Item2);
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void TestTransportBetweenStations() {
        SetupTradeTests();
        ShipAI shipAI = testShip.shipAI;

        var contract = shipAI.GetBestContractsBetweenStations(testStation, testStation2);
        Assert.Null(contract.Item3);
        Assert.Null(contract.Item4);
        Assert.Zero(contract.Item5);

        testFaction2.factionTrade.personnelRequested.Add(testStation2,
            new(new Population(20, 30), new PopulationFloat(2f, 3f, 2f, 2f)));
        contract = shipAI.GetBestContractsBetweenStations(testStation, testStation2);
        Assert.Null(contract.Item3);
        Assert.Null(contract.Item4);
        Assert.Zero(contract.Item5);

        testShip.LoadPopulation(new Population(10));
        testFaction.factionTrade.personnelToHire.Add(testStation,
            new(new Population(20, 30), new PopulationFloat(1f, 1f, 1f, 1f)));
        contract = shipAI.GetBestContractsBetweenStations(testStation, testStation2);
        Assert.AreEqual(10, contract.Item3.transportOffer.personnel.civilians);
        Assert.AreEqual(30, contract.Item3.transportOffer.personnel.pilots);
        Assert.AreEqual(20, contract.Item4.transportOffer.personnel.civilians);
        Assert.AreEqual(30, contract.Item4.transportOffer.personnel.pilots);
        Assert.Greater(contract.Item5, 0);

        testFaction.factionTrade.personnelToHire[testStation].personnel.civilians += 1000;
        contract = shipAI.GetBestContractsBetweenStations(testStation, testStation2);
        Assert.AreEqual(10, contract.Item3.transportOffer.personnel.civilians);
        Assert.AreEqual(30, contract.Item3.transportOffer.personnel.pilots);
        Assert.AreEqual(20, contract.Item4.transportOffer.personnel.civilians);
        Assert.AreEqual(30, contract.Item4.transportOffer.personnel.pilots);

        testFaction2.factionTrade.personnelRequested[testStation2].personnel.civilians += 1000;
        contract = shipAI.GetBestContractsBetweenStations(testStation, testStation2);
        Assert.AreEqual(60, contract.Item3.transportOffer.personnel.civilians);
        Assert.AreEqual(30, contract.Item3.transportOffer.personnel.pilots);
        Assert.AreEqual(70, contract.Item4.transportOffer.personnel.civilians);
        Assert.AreEqual(30, contract.Item4.transportOffer.personnel.pilots);


        testShip.moduleSystem.Get<HabitationArea>().ForEach(h => h.population.civilians = 0);
        testFaction.factionTrade.personnelToHire[testStation].personnel.pilots += 1000;
        testFaction2.factionTrade.personnelRequested[testStation2].personnel.pilots += 1000;
        contract = shipAI.GetBestContractsBetweenStations(testStation, testStation2);
        Assert.AreEqual(100, contract.Item3.transportOffer.personnel.pilots);
        Assert.AreEqual(100, contract.Item4.transportOffer.personnel.pilots);
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void TestTradeAndTransportBetweenStations() {
        SetupTradeTests();
        ShipAI shipAI = testShip.shipAI;

        var contract = shipAI.GetBestContractsBetweenStations(testStation, testStation2);
        Assert.Null(contract.Item3);
        Assert.Null(contract.Item4);
        Assert.Zero(contract.Item5);

        Assert.Zero(testStation.LoadCargo(300, CargoBay.CargoType.Gas));
        testStation2.ReserveCargo(300, CargoBay.CargoType.Gas);
        testFaction.factionTrade.personnelToHire.Add(testStation,
            new(new Population(20, 30), new PopulationFloat(1f, 1f, 1f, 1f)));
        testFaction2.factionTrade.personnelRequested.Add(testStation2,
            new(new Population(20, 30), new PopulationFloat(2f, 3f, 2f, 2f)));
        contract = shipAI.GetBestContractsBetweenStations(testStation, testStation2);

        Assert.AreEqual(300, contract.Item1.cargo[CargoBay.CargoType.Gas].amount);
        Assert.AreEqual(300, contract.Item2.cargo[CargoBay.CargoType.Gas].amount);
        Assert.AreEqual(20, contract.Item3.transportOffer.personnel.civilians);
        Assert.AreEqual(30, contract.Item3.transportOffer.personnel.pilots);
        Assert.AreEqual(20, contract.Item4.transportOffer.personnel.civilians);
        Assert.AreEqual(30, contract.Item4.transportOffer.personnel.pilots);
        Assert.Greater(contract.Item5, 0);

    }
}
