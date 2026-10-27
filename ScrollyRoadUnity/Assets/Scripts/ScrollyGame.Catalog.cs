using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;

/// -scrollyCatalog <dir>: renders every item model (in the game and candidates) to a PNG, plus catalog.json
/// with names, rarity, prices and notes, for the overview page to pick from. Then quits.
public partial class ScrollyGame
{
    IEnumerator RenderCatalog(string dir)
    {
        yield return null;
        System.IO.Directory.CreateDirectory(dir);
        const int size = 320;
        var target = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
        var json = new StringBuilder("[\n");
        bool first = true;
        foreach (var kind in ClosetTabs)
        {
            foreach (var model in Cosmetics.ModelsOf(kind))
            {
                if (IsNothing(model)) continue;
                var texture = kind == CosmeticKind.Tag ? TagArt.Thumbnail(model.Tag, size, 110) : RenderModel(model, target);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, $"{kind.ToString().ToLower()}_{model.Id}.png"), texture.EncodeToPNG());
                // Items already in the sheet show their sheet values.
                var inGame = Cosmetics.OfKind(kind).FirstOrDefault(i => i.Id == model.Id);
                var item = inGame ?? model;
                string unlock = item.Free ? "free" : item.Price > 0 ? "coins" : item.Achievement != null ? "achievement" : "gift";
                if (!first) json.Append(",\n");
                first = false;
                json.Append($"{{\"id\":\"{model.Id}\",\"kind\":\"{kind.ToString().ToLower()}\",\"name\":\"{Escape(item.Name)}\",\"rarity\":\"{item.Rarity.ToString().ToLower()}\",")
                    .Append($"\"price\":{item.Price},\"unlock\":\"{unlock}\",\"inGame\":{(inGame != null ? "true" : "false")},\"notes\":\"{Escape(model.Notes ?? "")}\"}}");
                yield return null;
            }
        }
        json.Append("\n]\n");
        System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "catalog.json"), json.ToString());
        Debug.Log($"Catalog written to {dir}");
        Application.Quit();
        yield return new WaitForSecondsRealtime(1f);
        System.Diagnostics.Process.GetCurrentProcess().Kill();
    }

    /// -scrollyIcons <dir>: app icon options, each a 1024 px render of a character on a bit of wall.
    IEnumerator RenderIcons(string dir)
    {
        yield return null;
        System.IO.Directory.CreateDirectory(dir);
        var target = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
        var options = new (string name, string character, string hat, ZoneTheme theme)[]
        {
            ("meadow", "climber", "none", ZoneTheme.All[0]),
            ("meadow_cat", "cat", "none", ZoneTheme.All[0]),
            ("snow_penguin", "penguin", "beanie", ZoneTheme.All[1]),
            ("cloud_frog", "frog", "none", ZoneTheme.All[2]),
            ("space_astronaut", "astronaut", "none", ZoneTheme.All[3]),
            ("desert_fox", "fox", "cowboy", ZoneTheme.Hot[0]),
        };
        var background = previewCam.backgroundColor;
        foreach (var option in options)
        {
            var root = new GameObject("Icon scene").transform;
            root.position = previewRoot.position + new Vector3(40, 0, 0);
            var theme = option.theme;
            // A bit of wall (front face at z = 0) with a grassy ledge, and the climber in front of it.
            for (int r = -3; r <= 5; r++)
                for (int c = -5; c <= (r > 2 ? 0 : 1); c++)   // the wall's edge on the right, sky beyond
                {
                    uint hex = theme.Rocks[(int)(Voxel.Hash(r, c) * theme.Rocks.Length) % theme.Rocks.Length];
                    if (Voxel.Hash(r, c, 1) < theme.AccentChance * 1.5f) hex = theme.Accents[0];
                    Voxel.Box(root, Vector3.one, hex, new Vector3(c, r + 0.5f, 0.5f));
                }
            Voxel.Box(root, new Vector3(7, 0.25f, 0.5f), theme.Ledge, new Vector3(-1.5f, -0.12f, -0.25f));
            Voxel.Box(root, new Vector3(7, 0.1f, 0.52f), theme.Cap, new Vector3(-1.5f, 0.03f, -0.25f));
            Voxel.Disc(root, 0.26f, 0.05f, coinMaterial, new Vector3(0.75f, 1.95f, -0.75f));
            var climberRig = new GameObject("Icon climber").AddComponent<Climber>();
            climberRig.transform.SetParent(root, false);
            climberRig.transform.localPosition = new Vector3(-0.15f, 0.05f, -0.6f);
            climberRig.transform.localRotation = Quaternion.Euler(0, 18, 0);
            climberRig.Build(Cosmetics.Character(option.character), Cosmetics.Hat(option.hat));
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = PreviewLayer;

            var rotation = Quaternion.Euler(14, -20, 0);
            var center = root.position + new Vector3(0.05f, 0.95f, -0.6f);
            previewCam.transform.SetPositionAndRotation(center - rotation * Vector3.forward * 10, rotation);
            previewCam.orthographicSize = 1.2f;
            previewCam.backgroundColor = Voxel.Vivid(Voxel.Hex(theme.Sky));
            previewCam.targetTexture = target;
            previewCam.aspect = 1;
            previewCam.Render();
            var texture = new Texture2D(1024, 1024, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, $"icon_{option.name}.png"), texture.EncodeToPNG());
            DestroyImmediate(root.gameObject);
            yield return null;
        }
        previewCam.backgroundColor = background;
        Debug.Log($"Icons written to {dir}");
        Application.Quit();
        yield return new WaitForSecondsRealtime(1f);
        System.Diagnostics.Process.GetCurrentProcess().Kill();
    }

    static string Escape(string text) => text.Replace("\\", "\\\\").Replace("\"", "\\\"");

    /// One model on its own (a character without a hat), framed to fit, on a transparent background.
    Texture2D RenderModel(Cosmetic item, RenderTexture target)
    {
        // Hats are shown on a head and backpacks on a back (the starting climber's); charms on their own.
        var climberLook = Cosmetics.Character("climber");
        var noHat = Cosmetics.Hat("none");
        Transform root = previewClimber.transform, part = null;
        Bounds? around = null;
        switch (item.Kind)
        {
            case CosmeticKind.Character:
                PoseClimber(item, noHat, null, null);
                break;
            case CosmeticKind.Hat:
                PoseClimber(climberLook, item, null, null);
                part = root.Find("rig/hat");
                around = new Bounds(root.TransformPoint(0, 0.97f, 0), new Vector3(0.55f, 0.45f, 0.5f));
                break;
            case CosmeticKind.Backpack:
                PoseClimber(climberLook, noHat, item, null);
                part = root.Find("rig/back");
                around = new Bounds(root.TransformPoint(0, 0.75f, 0), new Vector3(0.6f, 0.9f, 0.4f));
                break;
            default:
                PoseItem(item);
                root = previewItem;
                break;
        }
        float yaw = item.Kind is CosmeticKind.Backpack or CosmeticKind.Charm ? 204 : 24;
        root.localRotation = Quaternion.Euler(0, yaw, 0);
        float minimum = item.Kind switch
        {
            CosmeticKind.Character => 1.0f,
            CosmeticKind.Charm => 0.17f,
            CosmeticKind.Hat => 0.5f,
            _ => 0.6f,
        };
        FitPreview(part ?? root, minimum, around);
        previewCam.targetTexture = target;
        previewCam.aspect = 1;
        previewCam.Render();
        var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        texture.Apply();
        RenderTexture.active = previous;
        return texture;
    }

    /// Aims the preview camera so everything under root fits, but never closer than `minimum` (half height).
    void FitPreview(Transform root, float minimum, Bounds? around = null)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0 && around == null) return;
        var bounds = around ?? renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        var rotation = Quaternion.Euler(12, 0, 0);
        previewCam.transform.SetPositionAndRotation(bounds.center - rotation * Vector3.forward * 10, rotation);
        previewCam.orthographicSize = Mathf.Max(minimum, Mathf.Max(bounds.extents.y, bounds.extents.x) * 1.12f);
    }
}
