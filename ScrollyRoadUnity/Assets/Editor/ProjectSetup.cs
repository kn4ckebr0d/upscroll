using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// One-time (re-runnable) project configuration: render pipeline, player settings,
/// the shared voxel material and the main scene. Run from the menu or with
/// -executeMethod ProjectSetup.Run.
public static class ProjectSetup
{
    const string BundleId = "com.knackebrod.scrollyroad";
    // While the Swift prototype is still on the phone, the Unity build installs next to it.
    const string IOSTestBundleId = "com.knackebrod.scrollyroad.unity";
    const string PipelinePath = "Assets/Settings/Mobile_RPAsset.asset";
    const string VoxelMaterialPath = "Assets/Materials/Voxel.mat";
    const string ShadeMaterialPath = "Assets/Materials/Shade.mat";
    const string CoinMaterialPath = "Assets/Materials/Coin.mat";
    const string ScenePath = "Assets/Scenes/Main.unity";
    const string FontPath = "Assets/Fonts/Fredoka-Bold.ttf";

    [MenuItem("ScrollyRoad/Run Project Setup")]
    public static void Run()
    {
        SetupRenderPipeline();
        SetupPlayer();
        var voxel = SetupVoxelMaterial();
        var shade = SetupShadeMaterial();
        var coin = SetupCoinMaterial();
        SetupScene(voxel, shade, coin);
        AssetDatabase.SaveAssets();
        Debug.Log("[ScrollyRoad] Project setup done.");
    }

    static void SetupRenderPipeline()
    {
        var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PipelinePath);
        GraphicsSettings.defaultRenderPipeline = pipeline;
        int current = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = pipeline;
        }
        QualitySettings.SetQualityLevel(current, false);
    }

    static void SetupPlayer()
    {
        PlayerSettings.companyName = "Knackebrod";
        PlayerSettings.productName = "UPSCROLL";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, IOSTestBundleId);
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleId);
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, BundleId);
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.colorSpace = ColorSpace.Linear;

        // iPhone: sign with the personal team, full screen, and keep edge swipes
        // (Notification Center, home) from interrupting a climb.
        PlayerSettings.iOS.appleDeveloperTeamID = "C85FJAW3P2";
        PlayerSettings.iOS.appleEnableAutomaticSigning = true;
        PlayerSettings.statusBarHidden = true;
        PlayerSettings.iOS.hideHomeButton = true;
        PlayerSettings.iOS.deferSystemGesturesMode = UnityEngine.iOS.SystemGestureDeferMode.All;

        // Android: 64-bit IL2CPP is required by Google Play.
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        // Mac test builds open as a phone-shaped window.
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 450;
        PlayerSettings.defaultScreenHeight = 975;
        PlayerSettings.resizableWindow = true;
    }

    static Material SetupVoxelMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(VoxelMaterialPath);
        if (material != null) return material;
        // Matte, no shine: the flat chunky look.
        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetFloat("_Smoothness", 0f);
        material.SetFloat("_SpecularHighlights", 0f);
        material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        material.SetFloat("_EnvironmentReflections", 0f);
        material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        material.enableInstancing = true;
        AssetDatabase.CreateAsset(material, VoxelMaterialPath);
        return material;
    }

    /// Transparent, unlit: lane stripes.
    static Material SetupShadeMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(ShadeMaterialPath);
        if (material != null) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.SetFloat("_Surface", 1);   // transparent
        material.SetFloat("_Blend", 0);     // alpha
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        material.color = new Color(0, 0, 0, 0.12f);
        AssetDatabase.CreateAsset(material, ShadeMaterialPath);
        return material;
    }

    /// Gold with a little glow, so coins pop even in shadow.
    static Material SetupCoinMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(CoinMaterialPath);
        if (material != null) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = new Color(1f, 0.76f, 0.15f);
        material.SetFloat("_Smoothness", 0.35f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", new Color(0.35f, 0.22f, 0f));
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        AssetDatabase.CreateAsset(material, CoinMaterialPath);
        return material;
    }

    static void SetupScene(Material voxel, Material shade, Material coin)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var game = new GameObject("ScrollyGame").AddComponent<ScrollyGame>();
        game.voxelMaterial = voxel;
        game.shadeMaterial = shade;
        game.coinMaterial = coin;
        game.uiFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }
}
