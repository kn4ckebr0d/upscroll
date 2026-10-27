using Unity.VectorGraphics.Editor;
using UnityEditor;
using UnityEngine;

/// Stamp icons and UI icons are SVG drawings (Resources/Stamps, Resources/Icons). Import them as
/// smooth, anti-aliased sprites that keep their 100×100 frame, so every icon lines up the same.
public class StampSvgImport : AssetPostprocessor
{
    void OnPreprocessAsset()
    {
        if (assetImporter is not SVGImporter svg || !(assetPath.Contains("/Stamps/") || assetPath.Contains("/Icons/"))) return;
        svg.SvgType = SVGType.TexturedSprite;
        svg.ViewportOptions = Unity.VectorGraphics.ViewportOptions.PreserveViewport;
        svg.KeepTextureAspectRatio = true;
        svg.TextureSize = 256;
        svg.SampleCount = 8;
        svg.FilterMode = FilterMode.Bilinear;
        svg.WrapMode = TextureWrapMode.Clamp;
    }
}
