using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class NumFormatter {
    static string[] postfixes = new string[] { "", "K", "M", "B", "T", "Q" };
    public static string ConvertNumber(long number) {
        return ConvertNumber((double)number);
    }

    public static string ConvertNumber(double number) {
        int postfixIndex = 0;
        double floating = math.abs(number);
        while(floating >= 1000) {
            floating /= 1000;
            postfixIndex++;
        }
        string prefix;
        if (floating >= 100 || math.abs(number) < 1000) {
            prefix = floating.ToString("n0");
        } else if (floating >= 10 && math.abs(number) >= 1000) {
            prefix = floating.ToString("n1");
        } else if (floating > 1 && math.abs(number) >= 1000) {
            prefix = floating.ToString("n2");
        } else {
            prefix = "0";
        }
        string sign = "";
        if (number < 0) sign = "-";
        return sign + prefix + postfixes[postfixIndex];
    }

}
