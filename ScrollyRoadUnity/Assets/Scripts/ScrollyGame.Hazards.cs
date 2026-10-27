using System.Collections.Generic;
using UnityEngine;

/// Mob lanes, getting hit (or saved by an extra life), the long tumble down, and game over.
public partial class ScrollyGame
{
    /// A horizontal row where mobs fly across, like a road in a lane-crossing game.
    class Lane
    {
        public float Y;
        public float Speed;   // m/s, sign = direction
        public MobKind Kind;
        public readonly List<Mob> Mobs = new();
        public GameObject Stripe;
    }

    readonly List<Lane> lanes = new();

    float fallTime, fallStartY, fallPeakY, fallFloorY, fallDuration, fallSpin;
    bool fallLanded;
    int fallZone;
    float gameOverAt;
    int lastRunMeters;
    bool lastRunNewBest;
    float invulnerableUntil;

    // MARK: - Lanes

    void AddLanes(float lower, float upper)
    {
        // Keep the start of a zone clear so its first danger is always visible (longer on a new planet),
        // and keep lanes off the checkpoint ledge at the top.
        if (InMeteorStorm(lower + 0.01f)) return;   // the storm has meteors instead
        bool planetStart = Mathf.Abs(lower - PlanetBase(PlanetIndex(lower + 0.01f))) < 0.5f;
        float minY = lower + (planetStart ? Tuning.SafeStartMeters : Tuning.ZoneSafeMeters);
        float maxY = upper - 1f;
        if (maxY < minY) return;
        float y = minY + Random.Range(0f, Mathf.Min(2f, maxY - minY));
        while (y <= maxY)
        {
            int zone = ZoneIndex(y);
            float chance = Mathf.Min(0.75f, 0.45f + 0.04f * zone);
            if (MobFor(zone) == MobKind.Plane) chance *= 0.7f;   // planes are fast: fewer of them
            if (Random.value < chance) MakeLane(y);
            y += Random.Range(4.5f, 7.5f);
        }
    }

    void MakeLane(float y)
    {
        int zone = ZoneIndex(y);
        var kind = MobFor(zone);
        float direction = Random.value < 0.5f ? -1 : 1;
        var lane = new Lane
        {
            Y = y,
            Kind = kind,
            Speed = Mobs.BaseSpeed(kind) * Random.Range(0.85f, 1.2f) * (1 + 0.06f * Mathf.Min(zone, 10)) * MobSpeedFactor(y) * direction,
        };

        // A faint stripe across the wall so you can read where the danger row is.
        lane.Stripe = Voxel.Box(world, new Vector3(Tuning.LaneHalfSpan * 2, 0.62f, 0.02f), 0x000000, new Vector3(0, y, -0.02f), castShadows: false);
        var stripeRenderer = lane.Stripe.GetComponent<MeshRenderer>();
        stripeRenderer.sharedMaterial = shadeMaterial;
        stripeRenderer.receiveShadows = false;

        int count = kind != MobKind.Plane && Random.value < Mathf.Min(0.45f, 0.2f + 0.06f * zone) ? 2 : 1;
        float span = Tuning.LaneHalfSpan * 2;
        float start = Random.Range(-Tuning.LaneHalfSpan, Tuning.LaneHalfSpan);
        for (int k = 0; k < count; k++)
        {
            var mob = Mobs.Create(kind, world);
            float x = start + k * span / count;
            if (x > Tuning.LaneHalfSpan) x -= span;
            // In front of the wall's rock blocks, so they never fly through them.
            mob.transform.SetPositionAndRotation(new Vector3(x, y, -1.3f), Quaternion.Euler(0, direction > 0 ? 0 : 180, 0));
            lane.Mobs.Add(mob);
        }
        lanes.Add(lane);
    }

    void UpdateLanes(float dt)
    {
        float span = Tuning.LaneHalfSpan * 2;
        foreach (var lane in lanes)
            foreach (var mob in lane.Mobs)
            {
                var p = mob.transform.position;
                p.x += lane.Speed * dt;
                if (lane.Speed > 0 && p.x > Tuning.LaneHalfSpan) p.x -= span;
                if (lane.Speed < 0 && p.x < -Tuning.LaneHalfSpan) p.x += span;
                mob.transform.position = p;
            }
    }

    void CullHazards(float bottom)
    {
        lanes.RemoveAll(lane =>
        {
            if (lane.Y >= bottom) return false;
            foreach (var mob in lane.Mobs) Destroy(mob.gameObject);
            Destroy(lane.Stripe);
            return true;
        });
    }

    void CheckCollisions()
    {
        if (Time.time < invulnerableUntil || TutorialActive) return;
        var p = climber.transform.position;
        var body = new Rect(p.x - 0.28f, p.y + 0.1f, 0.56f, 1.0f);
        foreach (var lane in lanes)
        {
            if (Mathf.Abs(lane.Y - body.center.y) > 2.5f) continue;
            var size = Mobs.HitSize(lane.Kind);
            foreach (var mob in lane.Mobs)
            {
                var m = mob.transform.position;
                var hitbox = new Rect(m.x - size.x / 2, lane.Y - size.y / 2, size.x, size.y);
                if (!body.Overlaps(hitbox)) continue;
                if (Progress.ExtraLives > 0) UseExtraLife();
                else KnockOff(lane.Speed > 0 ? -1 : 1);
                return;
            }
        }
    }

    // MARK: - Getting hit and falling

    /// Hit too fast on the checkpoint or a ledge: you bounce off and tumble down like after a hit.
    bool shownCrashTip;

    void Crash()
    {
        Synth.Instance.Thud();
        Debris.Burst(climber.transform.position, 0xD7C4A3, 10, 3f);
        KnockOff(Random.value < 0.5f ? -1 : 1);
        if (!shownCrashTip)
        {
            shownCrashTip = true;
            ShowTip("Fell too fast!\nGrab the screen again to slow down.", 3.5f);
        }
    }

    void KnockOff(float spin)
    {
        phase = Phase.Falling;
        isSnapping = false;
        fallSpin = spin;
        fallTime = 0;
        fallLanded = false;
        fallStartY = ClimberY;
        fallPeakY = fallStartY + 1.5f;
        fallFloorY = PlanetBase(PlanetIndex(anchorY + 0.01f));   // you fall to this planet's surface
        fallDuration = Mathf.Clamp(0.5f + (fallPeakY - fallFloorY) * 0.012f, 0.6f, 2.2f);
        fallZone = ZoneIndex(fallStartY);
        ShakeCamera(0.25f);
        Synth.Instance.Hit();
        Synth.Instance.FallWhistle(0.3f + fallDuration, Tuning.HitStop);
        Haptics.Hit();
    }

    /// Hit-stop, a little pop up, then the long tumble all the way down to the ground.
    void UpdateFall(float dt)
    {
        fallTime += dt;
        if (fallLanded)
        {
            if (fallTime > 0.9f) EndRun();
            return;
        }
        float t = fallTime - Tuning.HitStop;
        if (t <= 0) return;
        const float pop = 0.3f;
        var pos = climber.transform.position;
        if (t < pop)
        {
            pos.y = fallStartY + (fallPeakY - fallStartY) * Mathf.Sin(t / pop * Mathf.PI / 2);
        }
        else
        {
            float u = Mathf.Min(1, (t - pop) / fallDuration);
            pos.y = fallPeakY + (fallFloorY + 0.32f - fallPeakY) * u * u;
            if (u >= 1)
            {
                climber.transform.position = pos;
                LandFall();
                return;
            }
        }
        pos.x = climberX + fallSpin * Mathf.Min(1, t) * 1.0f;
        climber.transform.position = pos;
        climber.transform.Rotate(0, 0, fallSpin * 520f * dt, Space.World);

        // A tick for every zone you tumble past.
        int zone = ZoneIndex(pos.y);
        if (zone < fallZone)
        {
            fallZone = zone;
            Haptics.Tick(0.8f);
            Synth.Instance.ClimbBlip(3, Tuning.ToneSteps);
        }
    }

    void LandFall()
    {
        fallLanded = true;
        fallTime = 0;
        var pos = climber.transform.position;
        // Don't land on top of the parked rocket.
        pos.x = Mathf.Clamp(pos.x, -3f, 2.4f);
        climber.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, 0, fallSpin > 0 ? 90 : -90));
        ShakeCamera(0.4f);
        Debris.Burst(new Vector3(pos.x, fallFloorY + 0.1f, pos.z), 0xD7C4A3, 12, 3f);
        climber.ShowDizzy(true);
        Synth.Instance.Thud();
        Haptics.Land();
    }

    void EndRun()
    {
        phase = Phase.GameOver;
        gameOverAt = Time.time;
        lastRunMeters = Mathf.FloorToInt(runBest);
        lastRunNewBest = lastRunMeters > bestAtStart;
        if (lastRunNewBest) Progress.Best = lastRunMeters;
        Progress.Save();
        if (!autopilot && !god) SubmitRun(lastRunMeters);
        // First the one question that matters (keep going?), then the results.
        if (CanContinue && Progress.Coins >= ContinuePrice) ShowContinueOffer(true);
        else ShowGameOver(true);
    }
}
