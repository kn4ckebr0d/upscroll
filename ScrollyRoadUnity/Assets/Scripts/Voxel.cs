using System.Collections.Generic;
using UnityEngine;

/// Builds chunky voxel boxes that share one matte material per color.
public static class Voxel
{
    static Material baseMaterial, outlineMaterial;
    static Mesh cubeMesh;
    static Mesh cylinderMesh;
    static readonly Dictionary<uint, Material> materials = new();

    public static void Init(Material material)
    {
        baseMaterial = material;
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubeMesh = cube.GetComponent<MeshFilter>().sharedMesh;
        Object.Destroy(cube);
        var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinderMesh = cylinder.GetComponent<MeshFilter>().sharedMesh;
        Object.Destroy(cylinder);
    }

    /// The outline's material: flat (unlit) cream, drawing only the back faces (front faces culled) of a
    /// slightly bigger copy of each box.
    public static void InitOutline(Material unlit)
    {
        outlineMaterial = new Material(unlit) { color = new Color(1f, 0.97f, 0.88f, 1f) };
        outlineMaterial.SetFloat("_Cull", 1);   // front faces culled: only the rim around the box shows
    }

    /// A thin cream rim around a voxel model, so it stands out on any wall (the "inverted hull" trick:
    /// each box gets a slightly bigger twin that only draws its inside). `width` is in meters.
    public static void Outline(Transform root, float width)
    {
        if (outlineMaterial == null) return;
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
        {
            if (renderer.sharedMaterial == outlineMaterial) continue;
            var t = renderer.transform;
            var size = t.lossyScale;
            var shell = new GameObject("outline");
            shell.transform.SetParent(t, false);
            shell.transform.localScale = new Vector3(
                1 + 2 * width / Mathf.Max(0.001f, Mathf.Abs(size.x)),
                1 + 2 * width / Mathf.Max(0.001f, Mathf.Abs(size.y)),
                1 + 2 * width / Mathf.Max(0.001f, Mathf.Abs(size.z)));
            shell.layer = t.gameObject.layer;
            shell.AddComponent<MeshFilter>().sharedMesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            var rim = shell.AddComponent<MeshRenderer>();
            rim.sharedMaterial = outlineMaterial;
            rim.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rim.receiveShadows = false;
        }
    }

    public static Color Hex(uint hex) =>
        new Color(((hex >> 16) & 0xFF) / 255f, ((hex >> 8) & 0xFF) / 255f, (hex & 0xFF) / 255f);

    /// Shading in 3D eats some color, so the palette gets a saturation and brightness boost (Crossy Road style).
    public const float Saturation = 1.2f;
    public const float Brightness = 1.06f;

    public static Color Vivid(Color color)
    {
        Color.RGBToHSV(color, out float h, out float s, out float v);
        return Color.HSVToRGB(h, Mathf.Clamp01(s * Saturation), Mathf.Clamp01(v * Brightness));
    }

    public static Material Material(uint hex)
    {
        if (materials.TryGetValue(hex, out var cached)) return cached;
        var material = new Material(baseMaterial) { color = Vivid(Hex(hex)) };
        materials[hex] = material;
        return material;
    }

    public static GameObject Box(Transform parent, Vector3 size, uint hex, Vector3 localPosition, bool castShadows = true)
    {
        var box = new GameObject("voxel");
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPosition;
        box.transform.localScale = size;
        box.AddComponent<MeshFilter>().sharedMesh = cubeMesh;
        var renderer = box.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = Material(hex);
        renderer.shadowCastingMode = castShadows
            ? UnityEngine.Rendering.ShadowCastingMode.On
            : UnityEngine.Rendering.ShadowCastingMode.Off;
        return box;
    }

    /// A coin-shaped disc facing the camera (a flattened cylinder) with its own material.
    public static GameObject Disc(Transform parent, float diameter, float thickness, Material material, Vector3 localPosition)
    {
        var holder = new GameObject("disc");
        holder.transform.SetParent(parent, false);
        holder.transform.localPosition = localPosition;
        var disc = new GameObject("face");
        disc.transform.SetParent(holder.transform, false);
        disc.transform.localRotation = Quaternion.Euler(90, 0, 0);
        disc.transform.localScale = new Vector3(diameter, thickness / 2f, diameter);   // the cylinder mesh is 2 units tall
        disc.AddComponent<MeshFilter>().sharedMesh = cylinderMesh;
        disc.AddComponent<MeshRenderer>().sharedMaterial = material;
        return holder;
    }

    /// Stable pseudo-random 0..1 for a grid cell, so rebuilt rows look the same.
    public static float Hash(int a, int b, int salt = 0)
    {
        double v = System.Math.Sin(a * 12.9898 + b * 78.233 + salt * 37.719) * 43758.5453;
        return (float)(v - System.Math.Floor(v));
    }
}
