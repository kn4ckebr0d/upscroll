using System.Collections.Generic;
using UnityEngine;

/// Column obstacles: rocks you can't climb through, the odd rest ledge on top of one, and waterfalls
/// that push you down.
public partial class ScrollyGame
{
    enum ObstacleKind { Rock, Ledge, Waterfall }

    static bool IsSolid(ObstacleKind kind) => kind is ObstacleKind.Rock or ObstacleKind.Ledge;

    class Obstacle
    {
        public ObstacleKind Kind;
        public int Column;
        public float Y0, Y1;
        public GameObject Node;
    }

    readonly List<Obstacle> obstacles = new();
    bool pressingCeiling;
    bool perched;   // standing on a rest ledge
    bool shownLedgeTip;
    bool inWaterfall;
    float nextSplash;

    void AddObstacles(float lower, float upper)
    {
        if (PlanetIndex(lower + 0.01f) == 0) return;   // Earth stays easy: no obstacles at all
        if (InMeteorStorm(lower + 0.01f))
        {
            // The storm is one long climb: a rest ledge where each of its upper zones starts, nothing else.
            if (lower - PlanetBase(PlanetIndex(lower + 0.01f)) > Tuning.ZoneLength * 1.5f)
            {
                float top = lower + Random.Range(1.5f, 3f);
                AddObstacle(ObstacleKind.Ledge, Random.Range(0, Tuning.Columns), top - 3f, top);
            }
            return;
        }
        int zone = ZoneIndex(lower + 0.01f);
        bool planetStart = Mathf.Abs(lower - PlanetBase(PlanetIndex(lower + 0.01f))) < 0.5f;
        float y = lower + (planetStart ? Tuning.SafeStartMeters : Tuning.ZoneSafeMeters) + Random.Range(0f, 3f);
        float maxY = upper - 3f;
        int count = Mathf.Min(1 + zone / 2, 4);
        bool ledgePlaced = false;   // resting makes climbing easy again, so at most one ledge per zone

        // Heavy gravity: every zone gets a rest ledge halfway up, so the climb is long but doable.
        float ledgeBottom = float.MaxValue, ledgeTop = float.MinValue;
        if (EffectAt(lower) == PlanetEffect.HeavyGravity)
        {
            ledgeTop = lower + Tuning.ZoneLength / 2 + Random.Range(-1f, 1f);
            ledgeBottom = ledgeTop - Random.Range(2.5f, 4f);
            AddObstacle(ObstacleKind.Ledge, Random.Range(0, Tuning.Columns), ledgeBottom, ledgeTop);
            ledgePlaced = true;
        }

        for (int i = 0; i < count; i++)
        {
            float length = Mathf.Min(Random.Range(3f, 6f) + zone * 0.25f, 10f);
            if (y < ledgeTop + 3f && y + length > ledgeBottom - 3f)
            {
                y = ledgeTop + Random.Range(4f, 7f);   // keep clear of the guaranteed ledge
                continue;
            }
            if (y + length > maxY) break;
            ObstacleKind kind;
            if (Random.value < 0.5f) kind = !ledgePlaced && Random.value < Tuning.LedgeChance ? ObstacleKind.Ledge : ObstacleKind.Rock;
            else kind = ObstacleKind.Waterfall;
            ledgePlaced |= kind == ObstacleKind.Ledge;
            AddObstacle(kind, Random.Range(0, Tuning.Columns), y, y + length);
            y += length + Random.Range(4f, 9f);
        }
    }

    void AddObstacle(ObstacleKind kind, int column, float y0, float y1)
    {
        var node = new GameObject(kind.ToString()).transform;
        node.SetParent(world, false);
        float x = ColumnX(column);
        var theme = ThemeFor(ZoneIndex(y0));
        switch (kind)
        {
            case ObstacleKind.Rock:
            case ObstacleKind.Ledge:
                // A boulder sticking out of the wall, in front of the climber's plane, in colors that
                // contrast with the planet so it reads as "you can't go through here" at a glance.
                var (rockA, rockB, crack) = theme.Boulder != null ? (theme.Boulder[0], theme.Boulder[1], theme.Boulder[2]) : EffectAt(y0) switch
                {
                    PlanetEffect.Hot => (0x3A2F2Bu, 0x2C2421u, 0xFF8A1Fu),   // volcanic rock with glowing cracks
                    PlanetEffect.Ice => (0x4E5C6Eu, 0x3F4B5Bu, 0x2A3340u),
                    _ => (0x5B534Cu, 0x4B4540u, 0x2E2925u),
                };
                for (float yy = y0; yy < y1 - 0.01f; yy += 1f)
                {
                    float h = Mathf.Min(1f, y1 - yy);
                    uint hex = Voxel.Hash((int)yy, column, 5) < 0.5f ? rockA : rockB;
                    Voxel.Box(node, new Vector3(1.04f, h, 1.25f), hex, new Vector3(x, yy + h / 2, -0.6f));
                    // A crack on the front face.
                    if (Voxel.Hash((int)yy, column, 6) < 0.6f)
                        Voxel.Box(node, new Vector3(0.08f, h * 0.6f, 0.02f), crack, new Vector3(x + (Voxel.Hash((int)yy, column, 7) - 0.5f) * 0.6f, yy + h / 2, -1.235f), castShadows: false);
                }
                Voxel.Box(node, new Vector3(1.08f, 0.12f, 1.29f), 0x3C3632, new Vector3(x, y0 + 0.06f, -0.6f));
                if (kind == ObstacleKind.Rock)
                {
                    // A jagged peak: nowhere to stand.
                    Voxel.Box(node, new Vector3(0.7f, 0.25f, 1.05f), rockB, new Vector3(x - 0.08f, y1 + 0.12f, -0.6f));
                    Voxel.Box(node, new Vector3(0.32f, 0.22f, 0.8f), rockA, new Vector3(x + 0.05f, y1 + 0.35f, -0.6f));
                }
                else
                {
                    // A flat, grassy ledge with a little flag: a place to catch your breath.
                    Voxel.Box(node, new Vector3(1.2f, 0.14f, 1.4f), theme.Ledge, new Vector3(x, y1 + 0.07f, -0.6f));
                    Voxel.Box(node, new Vector3(1.14f, 0.1f, 1.34f), theme.Cap, new Vector3(x, y1 + 0.19f, -0.6f));
                    Voxel.Box(node, new Vector3(0.07f, 1.15f, 0.07f), 0xF2E6CF, new Vector3(x + 0.42f, y1 + 0.8f, -1.05f));
                    Voxel.Box(node, new Vector3(0.42f, 0.3f, 0.05f), theme.Stamp, new Vector3(x + 0.18f, y1 + 1.2f, -1.05f));
                    Voxel.Box(node, new Vector3(0.18f, 0.14f, 0.05f), theme.Stamp, new Vector3(x - 0.1f, y1 + 1.2f, -1.05f));
                }
                break;

            case ObstacleKind.Waterfall:
                var water = new Material(shadeMaterial) { color = new Color(0.45f, 0.75f, 1f, 0.6f) };
                var flow = node.gameObject.AddComponent<Waterfall>();
                flow.Bottom = y0;
                flow.Top = y1;
                for (float yy = y0; yy < y1; yy += 0.5f)
                {
                    var streak = Voxel.Box(node, new Vector3(0.8f, 0.35f, 0.05f), 0xFFFFFF, new Vector3(x, yy, -0.25f), castShadows: false);
                    streak.GetComponent<MeshRenderer>().sharedMaterial = water;
                    flow.Streaks.Add(streak.transform);
                }
                Voxel.Box(node, new Vector3(1f, 0.35f, 0.6f), 0x7A808C, new Vector3(x, y1 + 0.17f, -0.3f));   // the spout
                break;
        }
        obstacles.Add(new Obstacle { Kind = kind, Column = column, Y0 = y0, Y1 = y1, Node = node.gameObject });
    }

    Obstacle ObstacleAt(ObstacleKind kind, int column, float bottom, float top)
    {
        foreach (var o in obstacles)
            if (o.Kind == kind && o.Column == column && o.Y0 < top && o.Y1 > bottom) return o;
        return null;
    }

    bool SolidAt(int column, float bottom, float top)
    {
        foreach (var o in obstacles)
            if (IsSolid(o.Kind) && o.Column == column && o.Y0 < top && o.Y1 > bottom) return true;
        return false;
    }

    /// The highest the climber's feet can go in this column before the head bumps a boulder.
    float CeilingAbove(int column, float feetY)
    {
        float ceiling = float.MaxValue;
        foreach (var o in obstacles)
            if (IsSolid(o.Kind) && o.Column == column && o.Y0 >= feetY + Tuning.ClimberHeight - 0.05f)
                ceiling = Mathf.Min(ceiling, o.Y0 - Tuning.ClimberHeight);
        return ceiling;
    }

    /// The top of the highest boulder (or just rest ledge) in this column at or below the climber's feet.
    float FloorBelow(int column, float feetY, bool ledgesOnly = false)
    {
        float floor = float.MinValue;
        foreach (var o in obstacles)
            if ((ledgesOnly ? o.Kind == ObstacleKind.Ledge : IsSolid(o.Kind)) && o.Column == column && o.Y1 <= feetY + 0.05f)
                floor = Mathf.Max(floor, o.Y1);
        return floor;
    }

    float LedgeBelow(int column, float feetY) => FloorBelow(column, feetY, ledgesOnly: true);

    /// Sliding back down onto a rock: you bounce off it into a free column next to it and keep sliding.
    void BumpOffRocks(float fromY, float toY)
    {
        foreach (var o in obstacles)
        {
            if (o.Kind != ObstacleKind.Rock || o.Column != column || o.Y1 > fromY + 0.01f || o.Y1 <= toY) continue;
            int first = Random.value < 0.5f ? -1 : 1;
            foreach (int dir in new[] { first, -first })
            {
                int c = column + dir;
                if (c < 0 || c >= Tuning.Columns || SolidAt(c, o.Y0, o.Y1 + Tuning.ClimberHeight)) continue;
                column = c;
                Synth.Instance.Thud();
                Haptics.Slip();
                Debris.Burst(new Vector3(ColumnX(o.Column), o.Y1 + 0.3f, -0.9f), 0x8A8178, 6, 2.5f, 0.12f);
                return;
            }
            return;   // boxed in (zones never place boulders side by side): slide on past it
        }
    }

    /// Applied each frame while climbing: boulders are ceilings from below and floors from above,
    /// waterfalls push you down.
    void ApplyObstacles(float dt, float previousY)
    {
        if (phase != Phase.Playing || isSnapping) return;

        float ceiling = CeilingAbove(column, previousY);
        if (anchorY + stretch > ceiling)
        {
            stretch = Mathf.Max(0, ceiling - anchorY);
            if (!pressingCeiling)
            {
                pressingCeiling = true;
                Synth.Instance.Thud();
                Haptics.Slip();
                ShakeCamera(0.08f);
            }
        }
        else
        {
            pressingCeiling = false;
        }

        // You can't sink into the top of a boulder, and on a rest ledge you can stand and rest.
        float floor = FloorBelow(column, previousY);
        if (floor > anchorY && anchorY + stretch < floor) stretch = floor - anchorY;
        float ledge = LedgeBelow(column, previousY);
        bool wasPerched = perched;
        perched = ledge > anchorY && anchorY + stretch - ledge < 0.05f;
        if (perched && !wasPerched && !shownLedgeTip)
        {
            shownLedgeTip = true;
            ShowTip("REST LEDGE!\nCatch your breath here.");
        }

        // Stepped off a ledge with no finger on the screen: now you do slide back.
        if (wasPerched && !perched && lastTouchCount == 0 && stretch > 0.02f) Release();

        float y = anchorY + stretch;
        inWaterfall = ObstacleAt(ObstacleKind.Waterfall, column, y, y + Tuning.ClimberHeight) != null;
        if (inWaterfall)
        {
            stretch = Mathf.Max(0, stretch - Tuning.WaterfallPush * dt);
            if (Time.time >= nextSplash)
            {
                nextSplash = Time.time + 0.12f;
                Debris.Burst(climber.transform.position + new Vector3(0, 1.3f, -0.3f), 0xBFE6FF, 2, 2f, 0.1f);
            }
        }

        // Ice: unless you keep pulling, you slowly slip back down.
        if (CurrentEffect == PlanetEffect.Ice && lastTouchCount > 0 && !perched)
            stretch = Mathf.Max(0, stretch - Tuning.IceSlip * EffectStrength * dt);
    }

    /// Swiping into a column that's blocked right where you are doesn't work.
    bool ColumnBlockedAt(int target)
    {
        float y = ClimberY;
        return SolidAt(target, y + 0.05f, y + Tuning.ClimberHeight);
    }

    void CullObstacles(float bottom)
    {
        obstacles.RemoveAll(o =>
        {
            if (o.Y1 >= bottom) return false;
            Destroy(o.Node);
            return true;
        });
    }
}

/// Animates waterfall streaks sliding down and looping.
public class Waterfall : MonoBehaviour
{
    public float Bottom, Top;
    public readonly List<Transform> Streaks = new();

    void Update()
    {
        float span = Top - Bottom;
        foreach (var streak in Streaks)
        {
            var p = streak.position;
            p.y -= 4f * Time.deltaTime;
            if (p.y < Bottom) p.y += span;
            streak.position = p;
        }
    }
}
