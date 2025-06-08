using NUnit.Framework;
using UnityEngine;

public class CalculatorTests {
    [Test]
    public void TestClosestPointOnALineToAPoint() {
        Assert.True(Vector2.zero == Calculator.GetClosestPointToAPointOnALine(Vector2.zero, 315f, Vector2.zero));
        Assert.True(Vector2.one == Calculator.GetClosestPointToAPointOnALine(Vector2.zero, 315f, Vector2.one));
        Assert.True(
            new Vector2(2, 2) == Calculator.GetClosestPointToAPointOnALine(Vector2.one, 315f, new Vector2(2, 2)));
        Assert.True(new Vector2(4, 4) ==
            Calculator.GetClosestPointToAPointOnALine(Vector2.zero, 315f, new Vector2(6, 2)));
        Assert.True(
            new Vector2(5, 0) == Calculator.GetClosestPointToAPointOnALine(Vector2.zero, 270, new Vector2(5, 0)));
        Assert.True(new Vector2(1, -1) ==
            Calculator.GetClosestPointToAPointOnALine(Vector2.zero, 225f, new Vector2(1, -1)));
        Assert.True(-Vector2.one == Calculator.GetClosestPointToAPointOnALine(Vector2.zero, 135f, -Vector2.one));
    }

    [Test]
    public void TestConvertWorldToLocalPosition() {
        Assert.True(new Vector2(30, 100) == Calculator.ConvertWorldPositionToLocal(new(10, 4), 270,
            Calculator.ConvertLocalPositionToWorld(new(10, 4), 270, new(30, 100))));
        for (int i = 0; i < 100; i++) {
            Vector2 localPos = new Vector2(Random.Range(-100000, 100000), Random.Range(-100000, 100000));
            Vector2 parentPos = new Vector2(Random.Range(-100000, 100000), Random.Range(-100000, 100000));
            float rotation = Random.Range(0, 360);
            Vector2 result = Calculator.ConvertWorldPositionToLocal(parentPos, rotation,
                Calculator.ConvertLocalPositionToWorld(parentPos, rotation, localPos));
            Assert.True((localPos - result).sqrMagnitude < .001f,
                "Failed converting world to local position with local position: " + localPos.ToString() +
                " parent position: " + parentPos.ToString() + " rotation: " + rotation + " was: " + result +
                " attempt: " + i);
        }
    }
}
