using NUnit.Framework;
using UnityEngine;

public class CalculatorTests {
    [Test]
    public void TestClosestPointOnALineToAPoint() {
        // Assert.AreEqual(Vector2.one, Calculator.GetClosestPointToAPointOnALine(Vector2.zero, 315f, Vector2.one));
        Assert.True(Vector2.one == Calculator.GetClosestPointToAPointOnALine(Vector2.zero, 315f, Vector2.one));
        Assert.True(new Vector2(2,2) == Calculator.GetClosestPointToAPointOnALine(Vector2.one, 315f, new Vector2(2,2)));
        Assert.True(new Vector2(4,4) == Calculator.GetClosestPointToAPointOnALine(Vector2.zero, 315f, new Vector2(6,2)));
        Assert.AreEqual(new Vector2(5,0), Calculator.GetClosestPointToAPointOnALine(Vector2.zero, 270, new Vector2(5,0)));
        Assert.True(new Vector2(5,0) == Calculator.GetClosestPointToAPointOnALine(Vector2.zero, 270, new Vector2(5,0)));
        // Assert.True(new Vector2(4,2) == Calculator.GetClosestPointToAPointOnALine(Vector2.zero, 300f, new Vector2(5,0)));
    }
}
