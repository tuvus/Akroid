using System.Collections.Generic;
using UnityEngine;

public class ColorPicker {
    private static readonly List<Color> colors = new List<Color> {
        Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta,
        new Color(1, .5f, 0), // Orange
        new Color(.4f, 0, 1), // Purple
        new Color(.4f, 1, 0) // Light green
    };
    private readonly List<Color> availableColors = new List<Color>();

    private readonly List<Color> preferedColors = new List<Color>();

    /// <summary>
    ///     Creates a new ColorPicker which manages selecting unique colors from a pre-determined pool.
    ///     If all colors in the pre-determined pool have been used then it will pick from a duplicate pool.
    /// </summary>
    /// <param name="preferBetterColors">Determines if higher contrast colors should be prefered over lower contrast colors.</param>
    public ColorPicker(bool preferBetterColors = true) {
        availableColors = new List<Color>(colors);
        if (preferBetterColors) {
            preferedColors = new List<Color> { Color.red, Color.green, Color.yellow, Color.cyan, Color.magenta };
            preferedColors.ForEach(c => availableColors.Remove(c));
        }
    }

    public Color TakeColor(Color color) {
        var index = preferedColors.IndexOf(color);
        if (index > 0)
            preferedColors.RemoveAt(index);
        return color;
    }

    public Color PickColor() {
        if (preferedColors.Count > 0) {
            int index = Random.Range(0, preferedColors.Count);
            Color temp = preferedColors[index];
            preferedColors.RemoveAt(index);
            return temp;
        } else {
            if (availableColors.Count == 0) {
                availableColors.AddRange(colors);
            }

            int index = Random.Range(0, availableColors.Count);
            Color temp = availableColors[index];
            availableColors.RemoveAt(index);
            return temp;
        }
    }
}
