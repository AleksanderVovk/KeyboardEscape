using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using YG;

[System.Serializable]
public class LanguageDirectory
{
    public string ru;
    public string en;
    public string tr;

    public LanguageDirectory(string _ru, string _en, string _tr)
    {
        ru = _ru;
        en = _en;
        tr = _tr;
    }
}

public class UTF : Interaction
{
    public static UTF manager;

    private void Awake()
    {
        manager = this;
    }

    public static string ReadLanguageDirectory(LanguageDirectory languageDirectory) => manager._ReadLanguageDirectory(languageDirectory);
    public string _ReadLanguageDirectory(LanguageDirectory languageDirectory)
    {
        switch (YG2.envir.language)
        {
            case "ru":
                return languageDirectory.ru;

            case "en":
                return languageDirectory.en;

            case "tr":
                return languageDirectory.tr;
        }

        return null;
    }

    private static readonly string[] suffixes =
    {
    "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No",
    "Dc", "UDc", "DDc", "TDc", "QaDc", "QiDc"
};

    public static string AbbreviateNumber(double number)
    {
        if (number == 0)
            return "0";

        double abs = Math.Abs(number);

        // порядок величины числа
        int magnitude = (int)(Math.Floor(Math.Log10(abs)) / 3); // каждые 3 разряда = новый суффикс

        if (magnitude <= 0)
            return number.ToString("0.##");

        if (magnitude >= suffixes.Length)
            magnitude = suffixes.Length - 1;

        double divisor = Math.Pow(10, magnitude * 3);
        double shortNumber = number / divisor;

        return shortNumber.ToString("0.##") + suffixes[magnitude];
    }

    public string TextToSprite(string input)
    {
        var output = input;

        Regex pattern = new Regex(@"\\u[a-zA-Z0-9]*\\u[a-zA-Z0-9]*");

        while (output.Contains(@"\u"))
        {
            output = pattern.Replace(output,
                m => {
                    var pair = m.Value;
                    var first = pair.Substring(0, 6);
                    var second = pair.Substring(6, 6);
                    var firstInt = Convert.ToInt32(first.Substring(2), 16);
                    var secondInt = Convert.ToInt32(second.Substring(2), 16);
                    var codePoint = (firstInt - 0xD800) * 0x400 + (secondInt - 0xDC00) + 0x10000;
                    return @"\U" + codePoint.ToString("X8");
                },
                1
            );
        }

        return output;
    }

}