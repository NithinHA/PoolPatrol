using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// One-click: fills every Android adaptive icon slot (Unity 6.6 dropped Round/Legacy) from PoolPatrol_icon.png.
/// Adaptive background = the icon, adaptive foreground = a transparent layer, so the launcher
/// never crops the artwork. Run via Tools > Pool Patrol > Set Android Icons, then rebuild.
/// </summary>
public static class SetAndroidIcons
{
    const string IconPath = "Assets/Imports/Icons/PoolPatrol_icon.png";
    const string BlankPath = "Assets/Imports/Icons/AndroidAdaptiveForeground_blank.png";

    [MenuItem("Tools/Pool Patrol/Set Android Icons")]
    static void Run()
    {
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (icon == null) { Debug.LogError($"Icon not found at {IconPath}"); return; }

        if (!File.Exists(BlankPath))
        {
            var blank = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var clear = new Color32[16];
            blank.SetPixels32(clear);
            File.WriteAllBytes(BlankPath, blank.EncodeToPNG());
            Object.DestroyImmediate(blank);
            AssetDatabase.ImportAsset(BlankPath);
        }
        var fg = AssetDatabase.LoadAssetAtPath<Texture2D>(BlankPath);

        var target = NamedBuildTarget.Android;

        var adaptive = PlayerSettings.GetPlatformIcons(target, AndroidPlatformIconKind.Adaptive);
        foreach (var p in adaptive) { p.SetTexture(icon, 0); p.SetTexture(fg, 1); }
        PlayerSettings.SetPlatformIcons(target, AndroidPlatformIconKind.Adaptive, adaptive);

        AssetDatabase.SaveAssets();
        Debug.Log("Android icons set (adaptive).");
    }
}
