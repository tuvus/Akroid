using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ColorPicker {

    private static List<Color> colors = new List<Color>() { 
        Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta, 
        new Color(1, .5f, 0), // Orange
        new Color(.4f, 0, 1), // Purple
        new Color(.4f, 1, 0), // Light green
    };

    private List<Color> availableColors = new List<Color>();
    public ColorPicker() {
        availableColors = new List<Color>(colors);
    }

    public Color pickColor() {
        if (availableColors.Count == 0) {
            availableColors.AddRange(colors);
        }
        int index = Random.Range(0, availableColors.Count);
        Color temp = availableColors[index];
        availableColors.RemoveAt(index);
        return temp;
    }

}
