using System;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "Resources/Components/Thruster", menuName = "Components/Thruster", order = 3)]
public class ThrusterScriptableObject : ComponentScriptableObject {
    public float thrustSpeed;
    public Color color;
    public Color startThrustColor;
    public Color endThrustColor;
    public GameObject thrustEffect;
    public AudioResource thrustSound;

    private void Awake() {
        if (thrustSound == null) thrustSound = Resources.Load<AudioResource>("Prefabs/Audio/Engine");
    }

    public override Type GetComponentType() {
        return typeof(Thruster);
    }

    protected override void UpdateCosts() {
        base.UpdateCosts();
        cost += (long)(thrustSpeed / 10);
        AddResourceCost(CargoBay.CargoType.Metal, (long)(thrustSpeed / 50));
        AddResourceCost(CargoBay.CargoType.Gas, (long)(thrustSpeed / 200));
    }

    public override ModuleSystem.SystemType GetSystemType() {
        return ModuleSystem.SystemType.Thruster;
    }
}
