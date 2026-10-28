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
    static void ApplyBranding(BuildTarget target)
    {
        PlayerSettings.productName = "UPSCROLL";
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/AppIcon/AppIcon.png");
        if (icon != null) PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        ApplySplash(target);
    }

    /// The splash: our poster (rendered with -scrollyPoster, logo on top) instead of the Unity logo. On iOS it's
    /// the launch screen and then stays up natively until the title is drawn (no Unity splash, which fades in
    /// from dark); on Android it's Unity's splash, with a slow zoom.
    static void ApplySplash(BuildTarget target)
    {
        const string path = "Assets/Splash/Splash.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null) return;
        if (importer.textureType != TextureImporterType.Sprite || importer.maxTextureSize != 4096 || importer.mipmapEnabled)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.maxTextureSize = 4096;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        var poster = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        PlayerSettings.SplashScreen.show = target != BuildTarget.iOS;
        PlayerSettings.SplashScreen.showUnityLogo = false;
        PlayerSettings.SplashScreen.background = poster;
        PlayerSettings.SplashScreen.backgroundPortrait = poster;
        PlayerSettings.SplashScreen.backgroundColor = new Color32(0x89, 0xFF, 0xFF, 0xFF);
        PlayerSettings.SplashScreen.blurBackgroundImage = false;
        PlayerSettings.SplashScreen.overlayOpacity = 0;
        PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Custom;
        PlayerSettings.SplashScreen.animationBackgroundZoom = 0.95f;
        PlayerSettings.SplashScreen.animationLogoZoom = 1;
        PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(2.5f, null) };   // nothing on top, just time to look

        // iPhone launch screen: Unity's full-screen, aspect-fill image view, pointed at the poster after the build.
        PlayerSettings.iOS.SetiPhoneLaunchScreenType(iOSLaunchScreenType.ImageAndBackgroundRelative);
        PlayerSettings.iOS.SetLaunchScreenImage(poster.texture, iOSLaunchScreenImageType.iPhonePortraitImage);
    }

    [UnityEditor.Callbacks.PostProcessBuild]
    static void CopyLaunchPoster(BuildTarget target, string path)
    {
        if (target != BuildTarget.iOS || !File.Exists("Assets/Splash/Splash.png")) return;

        // The poster goes in the asset catalog as "LaunchPoster" (a loose PNG in a launch screen didn't draw), and
        // the launch screen shows it instead of Unity's image (which Unity exports as one blank pixel anyway).
        string imageSet = Path.Combine(path, "Unity-iPhone/Images.xcassets/LaunchPoster.imageset");
        Directory.CreateDirectory(imageSet);
        File.Copy("Assets/Splash/Splash.png", Path.Combine(imageSet, "LaunchPoster.png"), true);
        File.WriteAllText(Path.Combine(imageSet, "Contents.json"),
            "{\n  \"images\" : [ { \"filename\" : \"LaunchPoster.png\", \"idiom\" : \"universal\" } ],\n  \"info\" : { \"author\" : \"xcode\", \"version\" : 1 }\n}\n");
        string storyboard = Path.Combine(path, "LaunchScreen-iPhone.storyboard");
        File.WriteAllText(storyboard, File.ReadAllText(storyboard)
            .Replace("image=\"LaunchScreen-iPhonePortrait.png\"", "image=\"LaunchPoster\"")
            .Replace("<image name=\"LaunchScreen-iPhonePortrait.png\"", "<image name=\"LaunchPoster\""));

        // Unity shows its empty game view as soon as the app starts, a dark gap until the engine draws its first
        // frame. Keep the poster on top through that; the game fades it once the title is drawn.
        string file = Path.Combine(path, "Classes/UI/UnityAppController+ViewHandling.mm");
        string code = File.ReadAllText(file);
        const string shown = "    // make window visible only after we have set up initial controller we want to show\n    [_window makeKeyAndVisible];\n";
        const string drawn = "    [UIView setAnimationsEnabled: YES];\n}\n\n#if UNITY_SUPPORT_ROTATION";
        if (code.Contains("upscrollPoster")) return;   // already there (an appended build)
        if (!code.Contains(shown) || !code.Contains(drawn))
        {
            Debug.LogWarning("[ScrollyRoad] Launch poster: Unity's startup code changed, the poster isn't held over the gap");
            return;
        }
        code = code.Replace(shown, shown +
            "    UIImageView* upscrollPoster = [[UIImageView alloc] initWithImage: [UIImage imageNamed: @\"LaunchPoster\"]];\n" +
            "    upscrollPoster.contentMode = UIViewContentModeScaleAspectFill;\n" +
            "    upscrollPoster.frame = _window.bounds;\n" +
            "    upscrollPoster.tag = 0x5C0;\n" +
            "    [_window addSubview: upscrollPoster];\n");
        code = code.Replace(drawn,
            "    UIView* upscrollPoster = [_window viewWithTag: 0x5C0];\n" +
            "    if (upscrollPoster)\n" +
            "    {\n" +
            "        [_window bringSubviewToFront: upscrollPoster];\n" +
            "        // The game fades it (ScrollyHideLaunchPoster); this is only a fallback.\n" +
            "        dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(12 * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{ [upscrollPoster removeFromSuperview]; });\n" +
            "    }\n" + drawn);
        File.WriteAllText(file, code);
    }

    static void Build(string path, BuildTarget target, BuildOptions options)
    {
        ApplyBranding(target);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".");
        var report = BuildPipeline.BuildPlayer(Scenes, path, target, options);
        Debug.Log($"[ScrollyRoad] Build {target}: {report.summary.result}, {report.summary.totalErrors} errors → {path}");
        if (Application.isBatchMode && report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
