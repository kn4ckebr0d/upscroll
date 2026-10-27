using System.Collections.Generic;
using UnityEngine;

/// The boss planet's meteor storm. A red mark pulses on the wall where a meteor will land, corner
/// brackets close in on it, and the meteor streaks in from the sky. Still in that spot when it hits?
/// It knocks you off like a mob. Higher up they come faster, sometimes two at a time.
public partial class ScrollyGame
{
    class Meteor
    {
        public int Column;
        public float TargetY;
        public float ImpactAt;
        public Vector3 From;
        public Transform Rock;
        public Transform Mark;
        public Renderer MarkRenderer;
        public readonly List<Transform> Brackets = new();
    }

    readonly List<Meteor> meteors = new();
    float nextMeteorAt;
    Material meteorMarkMaterial;

    void ClearMeteors()
    {
        meteors.Clear();   // their objects live in the world, which a reset destroys
        nextMeteorAt = 0;
    }

    void UpdateMeteors()
    {
        bool storm = phase == Phase.Playing && InMeteorStorm(ClimberY) && !TutorialActive;
        if (storm && Time.time >= nextMeteorAt)
        {
            // Faster the higher you get in the storm.
            float local = ClimberY - PlanetBase(PlanetIndex(ClimberY + 0.01f)) - Tuning.ZoneLength;
            float progress = Mathf.Clamp01(local / (Tuning.ZoneLength * (ZonesPerPlanet - 1)));
            int first = SpawnMeteor(-1);
            if (Random.value < progress * 0.55f) SpawnMeteor(first);   // a second one in another column
            nextMeteorAt = Time.time + Mathf.Lerp(Tuning.MeteorIntervalStart, Tuning.MeteorIntervalEnd, progress) * Random.Range(0.85f, 1.15f);
        }
        if (!storm && phase != Phase.Playing && phase != Phase.Falling) nextMeteorAt = Time.time + 1.5f;

        for (int i = meteors.Count - 1; i >= 0; i--)
        {
            var m = meteors[i];
            if (m.Rock == null)
            {
                meteors.RemoveAt(i);
                continue;
            }
            float left = m.ImpactAt - Time.time;
            float u = 1 - Mathf.Clamp01(left / Tuning.MeteorWarning);

            // The mark pulses faster as the meteor gets close; the brackets close in.
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.Lerp(8f, 26f, u));
            m.MarkRenderer.material.color = new Color(1f, 0.22f, 0.12f, Mathf.Lerp(0.3f, 0.75f, pulse));
            float spread = Mathf.Lerp(1.1f, 0.55f, u);
            for (int b = 0; b < m.Brackets.Count; b++)
            {
                float sx = b % 2 == 0 ? -1 : 1, sy = b < 2 ? -1 : 1;
                m.Brackets[b].localPosition = new Vector3(sx * spread * 0.9f, sy * spread * 1.4f, 0);
            }

            // The rock streaks in on an accelerating path, trailing embers.
            var target = new Vector3(ColumnX(m.Column), m.TargetY + 0.6f, -0.9f);
            m.Rock.position = Vector3.Lerp(m.From, target, u * u);
            m.Rock.Rotate(140f * Time.deltaTime, 210f * Time.deltaTime, 0);
            if (u > 0.25f && Random.value < 0.5f)
                Debris.Burst(m.Rock.position, Random.value < 0.5f ? 0xFF8A1Fu : 0xFFD23Du, 1, 0.6f, 0.12f);

            if (left > 0) continue;
            Impact(m);
            meteors.RemoveAt(i);
        }
    }

    /// Sends a meteor at the climber's column (or a neighbor), a little above them. Returns the column.
    int SpawnMeteor(int avoidColumn)
    {
        int c = Random.value < 0.6f ? column : Random.Range(0, Tuning.Columns);
        if (c == avoidColumn) c = (c + Random.Range(1, Tuning.Columns)) % Tuning.Columns;
        float y = ClimberY + Random.Range(0.5f, 4f);

        meteorMarkMaterial ??= new Material(shadeMaterial);
        var m = new Meteor { Column = c, TargetY = y, ImpactAt = Time.time + Tuning.MeteorWarning };

        // The mark: a glowing red patch on the wall, with four corner brackets.
        var mark = new GameObject("Meteor mark").transform;
        mark.SetParent(world, false);
        mark.position = new Vector3(ColumnX(c), y + 0.6f, -0.12f);
        var patch = Voxel.Box(mark, new Vector3(0.95f, 2.4f, 0.02f), 0xFF4422, Vector3.zero, castShadows: false);
        m.MarkRenderer = patch.GetComponent<MeshRenderer>();
        m.MarkRenderer.material = meteorMarkMaterial;   // its own copy, so each mark pulses on its own
        for (int b = 0; b < 4; b++)
        {
            var bracket = new GameObject("Bracket").transform;
            bracket.SetParent(mark, false);
            float sx = b % 2 == 0 ? -1 : 1, sy = b < 2 ? -1 : 1;
            Voxel.Box(bracket, new Vector3(0.32f, 0.08f, 0.04f), 0xFFE14D, new Vector3(-sx * 0.12f, 0, -0.03f), castShadows: false);
            Voxel.Box(bracket, new Vector3(0.08f, 0.32f, 0.04f), 0xFFE14D, new Vector3(0, -sy * 0.12f, -0.03f), castShadows: false);
            m.Brackets.Add(bracket);
        }
        m.Mark = mark;

        // The meteor: a dark rock with a glowing core, coming in from high up on one side.
        var rock = new GameObject("Meteor").transform;
        rock.SetParent(world, false);
        Voxel.Box(rock, new Vector3(0.9f, 0.9f, 0.9f), 0x4A3A33, Vector3.zero);
        Voxel.Box(rock, new Vector3(0.5f, 0.5f, 0.5f), 0x6A5248, new Vector3(0.3f, 0.25f, -0.2f));
        Voxel.Box(rock, new Vector3(0.95f, 0.3f, 0.3f), 0xFF7A1F, new Vector3(0, -0.15f, -0.32f), castShadows: false);
        Voxel.Box(rock, new Vector3(0.3f, 0.3f, 0.95f), 0xFFC24D, new Vector3(-0.32f, 0.1f, 0), castShadows: false);
        float side = Random.value < 0.5f ? -1 : 1;
        m.From = new Vector3(ColumnX(c) + side * 6f, y + 13f, -2.5f);
        rock.position = m.From;
        m.Rock = rock;

        meteors.Add(m);
        Synth.Instance.CountdownBeep(false);
        Haptics.Tick(0.5f);
        return c;
    }

    void Impact(Meteor m)
    {
        var at = new Vector3(ColumnX(m.Column), m.TargetY + 0.6f, -0.8f);
        Debris.Burst(at, 0xFF8A1F, 14, 4f);
        Debris.Burst(at, 0x4A3A33, 10, 3f);
        Debris.Burst(at, 0xFFD23D, 6, 5f, 0.1f);
        Synth.Instance.Thud();
        Haptics.Hit();
        ShakeCamera(0.18f);
        Destroy(m.Rock.gameObject);
        Destroy(m.MarkRenderer.material);
        Destroy(m.Mark.gameObject);

        // Still there? Knocked off, just like a mob hit.
        if (phase != Phase.Playing || Time.time < invulnerableUntil || god) return;
        if (m.Column != column) return;
        float feet = ClimberY;
        if (feet + Tuning.ClimberHeight < m.TargetY - 0.6f || feet > m.TargetY + 1.8f) return;
        if (Progress.ExtraLives > 0) UseExtraLife();
        else KnockOff(Random.value < 0.5f ? -1 : 1);
    }
}
