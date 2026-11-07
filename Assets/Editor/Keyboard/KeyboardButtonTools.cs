using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class KeyboardButtonTools : EditorWindow
{
    private const string MenuPath = "Tools/Keyboard Button Tool";

    [MenuItem(MenuPath)]
    public static void OpenWindow()
    {
        var window = GetWindow<KeyboardButtonTools>();
        window.titleContent = new GUIContent("KB Tool");
        window.minSize = new Vector2(420, 220);
        window.Show();
    }

    private bool _useRandomColor = true;
    private bool _useRandomGlyph = true;
    private bool _useRandomAll = true;

    private int _colorIndex = 0;
    private int _glyphIndex = 0;

    private void OnGUI()
    {
        DrawHeader();
        DrawInfo();
        DrawControls();
    }

    private void DrawHeader()
    {
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Refresh Scene"))
        {
            Repaint();
        }

        GUILayout.EndHorizontal();
    }

    private void DrawInfo()
    {
        var all = FindObjectsOfType<KeyboardButton>();

        GUILayout.Space(10);
        GUILayout.Label($"Found Keyboard Buttons: {all.Length}", EditorStyles.boldLabel);
    }

    private void DrawControls()
    {
        GUILayout.Space(10);
        GUILayout.Label("Apply To ALL Buttons", EditorStyles.boldLabel);

        _useRandomAll = GUILayout.Toggle(_useRandomAll, "Random Color + Glyph");

        if (!_useRandomAll)
        {
            _useRandomColor = GUILayout.Toggle(_useRandomColor, "Random Color");
            _useRandomGlyph = GUILayout.Toggle(_useRandomGlyph, "Random Glyph");

            if (!_useRandomColor)
                _colorIndex = EditorGUILayout.IntField("Color Index", _colorIndex);

            if (!_useRandomGlyph)
                _glyphIndex = EditorGUILayout.IntField("Glyph Index", _glyphIndex);
        }

        GUILayout.Space(15);

        if (GUILayout.Button("APPLY TO ALL", GUILayout.Height(40)))
        {
            ApplyToAll();
        }
    }

    private void ApplyToAll()
    {
        var all = FindObjectsOfType<KeyboardButton>();

        if (all.Length == 0)
        {
            EditorUtility.DisplayDialog("KB Tool", "No KeyboardButtons found in scene.", "OK");
            return;
        }

        Undo.RecordObjects(all, "Keyboard Button Batch Apply");

        int applied = 0;

        foreach (var kb in all)
        {
            if (kb == null)
                continue;

            if (kb.palette == null || kb.palette.Length == 0)
                continue;

            if (_useRandomAll)
            {
                kb.SetRandomSymbolAndColor();
            }
            else
            {
                if (_useRandomColor)
                    kb.SetRandomColor();
                else
                    kb.SetColorIndex(_colorIndex);

                if (_useRandomGlyph)
                    kb.SetRandomSymbol();
                else
                    kb.SetGlyphIndex(_glyphIndex);
            }

            kb.ApplyEditor();
            applied++;
        }

        EditorUtility.DisplayDialog("KB Tool", $"Applied to {applied} buttons", "OK");
    }
}