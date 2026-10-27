using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// Command-line builds, e.g.
/// Unity -batchmode -quit -projectPath . -executeMethod BuildScript.BuildMac
public static class BuildScript
{
    static string[] Scenes => EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

    [MenuItem("ScrollyRoad/Build Mac Test App")]
    public static void BuildMac()
    {
        Build("Builds/Mac/ScrollyRoad.app", BuildTarget.StandaloneOSX, BuildOptions.Development);
    }

    [MenuItem("ScrollyRoad/Build iOS Xcode Project")]
    public static void BuildIOS()
    {
        Build("Builds/iOS", BuildTarget.iOS, BuildOptions.None);
    }

    [MenuItem("ScrollyRoad/Build iOS Simulator Xcode Project")]
    public static void BuildIOSSimulator()
    {
        PlayerSettings.iOS.sdkVersion = iOSSdkVersion.SimulatorSDK;
        try { Build("Builds/iOS-Simulator", BuildTarget.iOS, BuildOptions.None); }
        finally { PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK; }
    }

    [MenuItem("ScrollyRoad/Build Android APK")]
    public static void BuildAndroid()
    {
        EditorUserBuildSettings.buildAppBundle = false;
        Build("Builds/Android/ScrollyRoad.apk", BuildTarget.Android, BuildOptions.None);
    }

    /// The game's name and icon on the phone, set on every build so they can't drift.
    static void ApplyBranding()
    {
        PlayerSettings.productName = "UPSCROLL";
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/AppIcon/AppIcon.png");
        if (icon != null) PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
    }

    static void Build(string path, BuildTarget target, BuildOptions options)
    {
        ApplyBranding();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".");
        var report = BuildPipeline.BuildPlayer(Scenes, path, target, options);
        Debug.Log($"[ScrollyRoad] Build {target}: {report.summary.result}, {report.summary.totalErrors} errors → {path}");
        if (Application.isBatchMode && report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
