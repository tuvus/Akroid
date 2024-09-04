using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class StartMenuTests {
    [SetUp]
    public void Setup() {
         SceneManager.LoadScene("Start", LoadSceneMode.Single);
    }

    [UnityTest]
    [Explicit, Category("integration")]
    public IEnumerator BattleSimulationLoads() {
        yield return new WaitForSeconds(0.1f);
        Assert.NotNull(GameObject.Find("Akroid"));
        StartMenuTestUtils.ClickButton("Simulation");
        StartMenuTestUtils.ClickButton("DefaultSimulation");
        yield return new WaitForSeconds(1f);
        Assert.NotNull(GameObject.Find("Battle"));
        Assert.NotNull(GameObject.Find("Player"));
    }
}