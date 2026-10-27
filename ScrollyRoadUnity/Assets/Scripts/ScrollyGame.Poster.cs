using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// -scrollyPoster <dir>: the splash poster. We hang just off the tower's top and look down its face:
/// the climber is right below us, reaching up and looking at us, with a trail of coins behind; further down the
/// gang is still climbing, the rocket roars up past them, and the meadow is far, far below.
/// Writes poster.png (1290 x 2796, an iPhone's screen) and quits; the logo goes on top afterwards.
public partial class ScrollyGame
{
    IEnumerator RenderPoster(string dir)
    {
        yield return null;
        System.IO.Directory.CreateDirectory(dir);
        const int width = 1290, height = 2796;
        var root = new GameObject("Poster scene").transform;
        root.position = new Vector3(500, 0, 500);
        var theme = ZoneTheme.All[0];
        var eye = new Vector3(0, 0.5f, -3.6f);   // in the scene's own space

        // The wall: the tower's front face (z = 0, facing -z), 8 blocks wide, from its grassy top down 60 m.
        const int half = 4, depth = 70;
        var wall = new VoxelBatch();
        for (int y = -1; y >= -depth; y--)
            for (int x = -half; x < half; x++)
            {
                uint hex = theme.Rocks[(int)(Voxel.Hash(x, y, 15) * 3) % 3];
                if (Voxel.Hash(x, y, 16) < (y > -6 ? 0.03f : theme.AccentChance)) hex = theme.Accents[0];   // calm right behind the climber
                float bulge = Voxel.Hash(x, y, 17) < 0.08f ? 0.25f : 0;
                wall.Add(new Vector3(1, 1, 1 + bulge), hex, new Vector3(x + 0.5f, y + 0.5f, 0.5f - bulge / 2));
            }
        // The top: grass over rock, and the tower's insides (never seen, but they keep the light out).
        for (int x = -half; x < half; x++)
            for (int z = 0; z < 6; z++)
            {
                wall.Add(new Vector3(1, 0.3f, 1), new uint[] { 0x6CBF4B, 0x63B443, 0x75C955 }[(int)(Voxel.Hash(x, z, 5) * 3) % 3], new Vector3(x + 0.5f, 0.15f, z + 0.5f));
                wall.Add(new Vector3(1, 0.7f, 1), theme.Rocks[(x + z + 8) % 3], new Vector3(x + 0.5f, -0.35f, z + 0.5f));
            }
        wall.Add(new Vector3(2 * half, depth, 6), theme.Edge, new Vector3(0, -depth / 2f - 0.7f, 4));
        wall.Build(root, "wall", castShadows: true);

        // A few of the tower's secrets on the way down (built as on a side face, then turned to face us).
        foreach (var (x, y, kind) in new[] { (-2.5f, -4, 0), (2.5f, -8, 1), (-3.5f, -12, 3), (1.5f, -17, 4), (-1.5f, -21, 7), (3.5f, -27, 2) })
        {
            var holder = new GameObject("secret").transform;
            holder.SetParent(root, false);
            holder.localPosition = new Vector3(x, 0, 0);
            holder.localRotation = Quaternion.Euler(0, 90, 0);   // +x (out of a side face) becomes -z (out of the front)
            var batch = new VoxelBatch();
            AddTowerSecret(batch, theme, kind, 0, y, 0);
            batch.Build(holder, "secret", castShadows: true);
        }

        // Far below: the meadow, its fields, a river, trees, and clouds on the way down.
        // Hazy with distance: the greens lean toward the sky.
        float floor = -depth - 1;
        var land = new VoxelBatch();
        land.Add(new Vector3(200, 1, 200), 0x74C27E, new Vector3(0, floor - 0.5f, -40));
        for (int i = 0; i < 70; i++)
            land.Add(new Vector3(3 + Voxel.Hash(i, 1, 18) * 5, 0.1f, 3 + Voxel.Hash(i, 2, 18) * 5), i % 3 == 0 ? 0x86CB8Cu : i % 3 == 1 ? 0x69B877u : 0xA6D69Au,
                     new Vector3(-30 + Voxel.Hash(i, 3, 18) * 60, floor, -60 + Voxel.Hash(i, 4, 18) * 60));
        for (int i = 0; i < 30; i++)   // a winding river
            land.Add(new Vector3(2.4f, 0.12f, 1.4f), 0x7FCFF0, new Vector3(-30 + i * 2.1f, floor + 0.04f, -20 + Mathf.Sin(i * 0.45f) * 4));
        for (int i = 0; i < 160; i++)
        {
            var p = new Vector3(-30 + Voxel.Hash(i, 1, 11) * 60, floor, -60 + Voxel.Hash(i, 2, 11) * 60);
            if (Mathf.Abs(p.z + 20 - Mathf.Sin((p.x + 30) / 2.1f * 0.45f) * 4) < 2) continue;   // not in the river
            land.Add(new Vector3(0.5f, 1f, 0.5f), 0x8A6A4E, p + new Vector3(0, 0.5f, 0));
            land.Add(new Vector3(1.4f, 1.4f, 1.4f), 0x5AA86A, p + new Vector3(0, 1.7f, 0));
            land.Add(new Vector3(0.9f, 0.7f, 0.9f), 0x6DBB78, p + new Vector3(0, 2.7f, 0));
        }
        land.Build(root, "meadow", castShadows: false);
        var clouds = new VoxelBatch();
        foreach (var (cx, cy, cz) in new[] { (-4.5f, -34f, -7f), (4.5f, -44f, -11f), (-1.5f, -55f, -18f) })
            for (int k = 0; k < 4; k++)
                clouds.Add(new Vector3(1.2f + Voxel.Hash(k, 3, 12), 0.6f + Voxel.Hash(k, 7, 12) * 0.4f, 1.0f + Voxel.Hash(k, 4, 12) * 0.8f), 0xFFFFFF,
                           new Vector3(cx + (k - 1.5f) * 0.8f, cy + Voxel.Hash(k, 5, 12) * 0.4f, cz + Voxel.Hash(k, 6, 12) * 1.0f - 0.5f));
        clouds.Build(root, "clouds", castShadows: false);

        // Right below us: the climber, hugging the wall, reaching up and looking back up at us (a bit bigger than life).
        var hero = AddPosterClimber(root, "climber", "none", new Vector3(0.2f, -6.3f, -0.85f), 18, -10, lookUp: 140);
        hero.transform.localScale = Vector3.one * 1.6f;
        // The rest of the gang, further down.
        AddPosterClimber(root, "cat", "crown", new Vector3(-2.4f, -10.5f, -0.45f), 22, 12);
        AddPosterClimber(root, "fox", "cowboy", new Vector3(2.3f, -13f, -0.45f), 22, -14);
        AddPosterClimber(root, "penguin", "beanie", new Vector3(-0.6f, -16.5f, -0.45f), 20, 6);
        AddPosterClimber(root, "axolotl", "bow", new Vector3(2.5f, -20.5f, -0.45f), 18, -6);
        AddPosterClimber(root, "frog", "party", new Vector3(-2.5f, -25f, -0.45f), 18, 8);

        // The coin trail the climber has been following, and a few loose ones, all turned to us.
        var coinSpots = new System.Collections.Generic.List<Vector3>();
        for (int i = 0; i < 6; i++) coinSpots.Add(new Vector3(0.3f + Mathf.Sin(i * 0.9f) * 0.6f, -7.6f - i * 1.1f, -0.5f));
        coinSpots.AddRange(new[] { new Vector3(-2.3f, -4.6f, -1.8f), new Vector3(2.0f, -7.5f, -2.0f), new Vector3(-0.8f, -14f, -1.2f), new Vector3(1.0f, -18f, -1.4f) });
        for (int i = 0; i < coinSpots.Count; i++)
        {
            var coin = Voxel.Disc(root, 0.42f, 0.08f, coinMaterial, coinSpots[i]);
            coin.transform.localRotation = Quaternion.LookRotation(eye - coinSpots[i]) * Quaternion.Euler(Voxel.Hash(i, 1, 19) * 30, Voxel.Hash(i, 2, 19) * 40 - 20, 0);
        }

        // The rocket, roaring up past the gang.
        var rocket = Rocket.Create(root, Vector3.zero);
        rocket.transform.localPosition = new Vector3(-1.0f, -15f, -1.8f);
        rocket.transform.localScale = Vector3.one * 1.2f;
        rocket.transform.localRotation = Quaternion.Euler(0, 0, -38) * Quaternion.Euler(-12, 0, 0);   // side-on to us, heading up the tower
        rocket.SetThrust(true, 1.5f);
        var smoke = new VoxelBatch();
        for (int i = 0; i < 10; i++)
        {
            var behind = rocket.transform.localRotation * Vector3.down * (1.0f + i * 0.85f) * 1.3f;
            float s = 0.45f + i * 0.14f;
            smoke.Add(Vector3.one * s, i % 2 == 0 ? 0xFFFFFFu : 0xE8EEF2u, rocket.transform.localPosition + behind
                      + new Vector3(Voxel.Hash(i, 1, 14) - 0.5f, 0, Voxel.Hash(i, 2, 14) - 0.5f) * 0.5f);
        }
        smoke.Build(root, "smoke", castShadows: false);

        foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = PreviewLayer;

        // Down the wall, turned so the wall is underneath the gang (bottom of the picture) and the drop is above.
        var cam = new GameObject("Poster camera").AddComponent<Camera>();
        cam.cullingMask = 1 << PreviewLayer;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Voxel.Vivid(Voxel.Hex(theme.Sky));
        cam.fieldOfView = 64;
        cam.nearClipPlane = 0.2f;
        cam.farClipPlane = 200;
        var look = new Vector3(0, -14, 0.6f) - eye;
        cam.transform.SetPositionAndRotation(root.TransformPoint(eye), Quaternion.LookRotation(look, Vector3.back));
        cam.GetUniversalAdditionalCameraData().renderShadows = true;
        var pipeline = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
        float shadowDistance = pipeline.shadowDistance;
        pipeline.shadowDistance = 40;
        // Light from over our shoulder, so the faces looking up are lit.
        var lightRotation = sun.transform.rotation;
        sun.transform.rotation = Quaternion.LookRotation(new Vector3(0.45f, -0.8f, 0.4f));

        var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
        cam.targetTexture = target;
        cam.aspect = (float)width / height;
        yield return null;
        cam.Render();
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        RenderTexture.active = target;
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture.Apply();
        RenderTexture.active = null;
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "poster.png"), texture.EncodeToPNG());
        pipeline.shadowDistance = shadowDistance;
        sun.transform.rotation = lightRotation;
        Debug.Log($"Poster written to {dir}");
        Application.Quit();
        yield return new WaitForSecondsRealtime(1f);
        System.Diagnostics.Process.GetCurrentProcess().Kill();
    }

    /// One of the gang on the wall: feet at `position`, facing the wall and hugging it (leaning in by `lean`
    /// degrees), with the head tipped back to look up at us.
    Climber AddPosterClimber(Transform root, string character, string hat, Vector3 position, float lean, float turn, float lookUp = 105)
    {
        var climber = new GameObject("Poster climber").AddComponent<Climber>();
        climber.transform.SetParent(root, false);
        climber.Build(Cosmetics.Character(character), Cosmetics.Hat(hat));
        climber.transform.localPosition = position;
        climber.transform.localRotation = Quaternion.Euler(-lean, 180 + turn, 0);
        // Everything above the shoulders (head, face, ears, hat) goes on a neck, which tips back.
        var rig = climber.transform.Find("rig");
        var neck = new GameObject("neck").transform;
        neck.SetParent(rig, false);
        neck.localPosition = new Vector3(0, 0.76f, 0.05f);
        var above = new System.Collections.Generic.List<Transform>();
        foreach (Transform part in rig)
            if (part != neck && part.name != "arm" && part.localPosition.y > 0.78f) above.Add(part);
        foreach (var part in above) part.SetParent(neck, true);
        neck.localRotation = Quaternion.Euler(lookUp, 0, 0);
        return climber;
    }
}
