using System.Collections.Generic;
using UnityEngine;

public enum MobKind { Bird, Eagle, Plane, Ufo, Vulture, Skeleton }

/// Voxel mobs that fly across the wall. Models face +x; the game flips them for leftward lanes.
public static class Mobs
{
    /// Earth's lineup per zone; the game swaps in planet-specific mobs.
    public static MobKind ForZone(int zone) => (MobKind)(Mathf.Max(0, zone) % 4);

    /// Meters per second (2D prototype speeds ÷ 40).
    public static float BaseSpeed(MobKind kind) => kind switch
    {
        MobKind.Bird => 2.6f,
        MobKind.Eagle => 3.75f,
        MobKind.Plane => 4.4f,
        MobKind.Vulture => 3.2f,
        MobKind.Skeleton => 3.75f,
        _ => 3.1f,
    };

    public static Vector2 HitSize(MobKind kind) => kind switch
    {
        MobKind.Bird => new Vector2(0.75f, 0.45f),
        MobKind.Eagle => new Vector2(1.05f, 0.5f),
        MobKind.Plane => new Vector2(1.1f, 0.45f),
        MobKind.Vulture => new Vector2(1.0f, 0.5f),
        _ => new Vector2(1.05f, 0.5f),
    };

    public static Mob Create(MobKind kind, Transform parent)
    {
        var root = new GameObject(kind.ToString());
        root.transform.SetParent(parent, false);
        var mob = root.AddComponent<Mob>();
        mob.Kind = kind;
        mob.Body = new GameObject("body").transform;
        mob.Body.SetParent(root.transform, false);
        switch (kind)
        {
            case MobKind.Bird: BuildBird(mob, 1f, 0xE5533D, 0xB83A28, 0xE5533D); break;
            case MobKind.Eagle: BuildBird(mob, 1.35f, 0x6D4C2F, 0x5A3D24, 0xFFFFFF); break;
            case MobKind.Plane: BuildPlane(mob); break;
            case MobKind.Ufo: BuildUfo(mob); break;
            case MobKind.Vulture: BuildVulture(mob); break;
            case MobKind.Skeleton: BuildSkeleton(mob); break;
        }
        return mob;
    }

    static void BuildBird(Mob mob, float s, uint body, uint wing, uint head)
    {
        var t = mob.Body;
        Voxel.Box(t, new Vector3(0.55f, 0.33f, 0.38f) * s, body, Vector3.zero);
        Voxel.Box(t, new Vector3(0.28f, 0.28f, 0.3f) * s, head, new Vector3(0.32f, 0.1f, 0) * s);
        Voxel.Box(t, new Vector3(0.15f, 0.1f, 0.12f) * s, 0xF5B731, new Vector3(0.52f, 0.07f, 0) * s);
        foreach (float z in new[] { -0.16f, 0.16f })
            Voxel.Box(t, new Vector3(0.06f, 0.06f, 0.02f), 0x111111, new Vector3(0.38f, 0.16f, z) * s);
        Voxel.Box(t, new Vector3(0.18f, 0.15f, 0.22f) * s, wing, new Vector3(-0.34f, 0.05f, 0) * s);
        foreach (float side in new[] { -1f, 1f })
        {
            var hinge = new GameObject("wing").transform;
            hinge.SetParent(t, false);
            hinge.localPosition = new Vector3(0, 0.1f, side * 0.19f) * s;
            Voxel.Box(hinge, new Vector3(0.32f, 0.06f, 0.34f) * s, wing, new Vector3(0, 0, side * 0.17f) * s);
            mob.Wings.Add((hinge, side));
        }
    }

    /// A scavenger: dark feathers, a bald pink head and a fluffy white collar.
    static void BuildVulture(Mob mob)
    {
        const float s = 1.3f;
        BuildBird(mob, s, 0x3B2A20, 0x2A1D16, 0xE8A0A0);
        Voxel.Box(mob.Body, new Vector3(0.12f, 0.3f, 0.42f) * s, 0xEDE4D3, new Vector3(0.2f, 0.04f, 0) * s);
    }

    /// Just bones: a skull with dark eye sockets, a spine with ribs and bony wings.
    static void BuildSkeleton(Mob mob)
    {
        const float s = 1.35f;
        const uint bone = 0xEDE6D6, boneShade = 0xCFC6B2;
        var t = mob.Body;
        Voxel.Box(t, new Vector3(0.62f, 0.08f, 0.08f) * s, bone, new Vector3(-0.05f, 0.08f, 0) * s);   // spine
        for (int i = 0; i < 3; i++)
            Voxel.Box(t, new Vector3(0.05f, 0.24f, 0.32f) * s, boneShade, new Vector3(-0.15f + i * 0.13f, -0.02f, 0) * s);
        Voxel.Box(t, new Vector3(0.28f, 0.26f, 0.28f) * s, bone, new Vector3(0.34f, 0.12f, 0) * s);   // skull
        Voxel.Box(t, new Vector3(0.18f, 0.08f, 0.1f) * s, boneShade, new Vector3(0.55f, 0.06f, 0) * s);   // beak
        foreach (float z in new[] { -0.15f, 0.15f })
        {
            Voxel.Box(t, new Vector3(0.1f, 0.1f, 0.02f) * s, 0x1A1414, new Vector3(0.38f, 0.16f, z) * s, castShadows: false);
            Voxel.Box(t, new Vector3(0.04f, 0.04f, 0.02f) * s, 0xFF7A22, new Vector3(0.38f, 0.16f, z * 1.07f) * s, castShadows: false);
        }
        Voxel.Box(t, new Vector3(0.2f, 0.05f, 0.18f) * s, bone, new Vector3(-0.4f, 0.08f, 0) * s);   // tail
        foreach (float side in new[] { -1f, 1f })
        {
            var hinge = new GameObject("wing").transform;
            hinge.SetParent(t, false);
            hinge.localPosition = new Vector3(0, 0.1f, side * 0.06f) * s;
            Voxel.Box(hinge, new Vector3(0.06f, 0.05f, 0.42f) * s, bone, new Vector3(0, 0, side * 0.21f) * s);
            for (int i = 0; i < 3; i++)
                Voxel.Box(hinge, new Vector3(0.24f - i * 0.04f, 0.03f, 0.04f) * s, boneShade, new Vector3(-0.1f, 0, side * (0.14f + i * 0.12f)) * s);
            mob.Wings.Add((hinge, side));
        }
    }

    static void BuildPlane(Mob mob)
    {
        var t = mob.Body;
        const uint red = 0xE53935;
        Voxel.Box(t, new Vector3(0.85f, 0.25f, 0.3f), 0xF4F4F4, Vector3.zero);
        Voxel.Box(t, new Vector3(0.15f, 0.25f, 0.3f), red, new Vector3(0.5f, 0, 0));
        Voxel.Box(t, new Vector3(0.15f, 0.3f, 0.06f), red, new Vector3(-0.38f, 0.2f, 0));
        Voxel.Box(t, new Vector3(0.3f, 0.05f, 1.0f), 0x1E88E5, new Vector3(0.05f, -0.02f, 0));
        Voxel.Box(t, new Vector3(0.2f, 0.1f, 0.02f), 0x81D4FA, new Vector3(0.18f, 0.07f, -0.16f));
        mob.Propeller = Voxel.Box(t, new Vector3(0.04f, 0.4f, 0.08f), 0x333333, new Vector3(0.6f, 0, 0)).transform;
    }

    static void BuildUfo(Mob mob)
    {
        var t = mob.Body;
        Voxel.Box(t, new Vector3(1.05f, 0.22f, 1.05f), 0xB0BEC5, Vector3.zero);
        Voxel.Box(t, new Vector3(0.45f, 0.25f, 0.45f), 0x7FE7FF, new Vector3(0, 0.22f, 0));
        foreach (var p in new[] { new Vector3(-0.35f, -0.1f, -0.35f), new Vector3(0.35f, -0.1f, -0.35f), new Vector3(0, -0.1f, -0.45f) })
            Voxel.Box(t, new Vector3(0.1f, 0.08f, 0.1f), 0xFFE14D, p, castShadows: false);
    }
}

/// Animates a mob: flapping wings, a spinning propeller or a hovering saucer.
public class Mob : MonoBehaviour
{
    public MobKind Kind;
    public Transform Body;
    public Transform Propeller;
    public readonly List<(Transform hinge, float side)> Wings = new();
    float phase;
    Vector3 propellerSize;

    void Start()
    {
        phase = Random.value * 10f;
        if (Propeller != null) propellerSize = Propeller.localScale;
    }

    void Update()
    {
        phase += Time.deltaTime;
        foreach (var (hinge, side) in Wings)
            hinge.localRotation = Quaternion.Euler(side * Mathf.Sin(phase * 22f) * 40f, 0, 0);
        if (Propeller != null)
            Propeller.localScale = Vector3.Scale(propellerSize, new Vector3(1, 0.2f + 0.8f * Mathf.Abs(Mathf.Sin(phase * 40f)), 1));
        if (Kind == MobKind.Ufo)
            Body.localPosition = new Vector3(0, Mathf.Sin(phase * 4f) * 0.1f, 0);
    }
}
