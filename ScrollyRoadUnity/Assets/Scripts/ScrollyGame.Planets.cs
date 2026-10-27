using System.Collections;
using UnityEngine;

/// Rockets: the one parked on the ground, and the launch to the next planet.
public partial class ScrollyGame
{
    Rocket groundRocket;   // parked on the current planet's ground; continues fly it up to you
    Rocket launchRocket;   // the one carrying you to the next planet
    Rocket taxiRocket;     // the one flying you back up after a continue

    Rocket PlaceGroundRocket(float groundY)
    {
        if (groundRocket != null) Destroy(groundRocket.gameObject);
        groundRocket = Rocket.Create(world, new Vector3(3.9f, groundY, -2f));
        return groundRocket;
    }

    /// Reached the launch pad: hop in, countdown, fly through space, land on the next planet.
    IEnumerator LaunchSequence(Checkpoint pad)
    {
        var rocket = pad.Rocket;
        int planet = PlanetIndex(pad.Y - 0.01f);
        float landingY = PlanetBase(planet + 1);
        phase = Phase.Launching;
        launchRocket = rocket;
        rocket.transform.SetParent(world, true);
        Synth.Instance.ZoneFanfare();
        Haptics.Success();
        ShowBanner($"PLANET {planet + 1} COMPLETE", $"NEXT: {EffectTitle(EffectFor(planet + 1))}");

        yield return new WaitForSeconds(0.4f);
        yield return Hop(climber.transform, climber.transform.position, rocket.Door, 0.4f);
        climber.SetVisible(false);
        yield return new WaitForSeconds(0.6f);

        rocket.SetRumble(true);
        foreach (var number in new[] { "3", "2", "1" })
        {
            ShowCountdown(number, false);
            yield return new WaitForSeconds(0.6f);
        }
        ShowCountdown("GO!", true);

        rocket.SetThrust(true);
        Synth.Instance.Liftoff(3.4f);
        Haptics.Land();
        Debris.Burst(rocket.transform.position, 0xFFFFFF, 12, 3f);
        StartCoroutine(RumbleHaptics(3.2f));
        // Shoot up out of view, then cut to above the new planet and come down onto it
        // (flying straight up would pass through the new planet's ground).
        yield return MoveY(rocket.transform, pad.Y + 14f, 1.6f, easeOut: false);
        var p = rocket.transform.position;
        rocket.transform.position = new Vector3(p.x, landingY + 10f, p.z);
        rocket.SetThrust(true, 0.6f);
        yield return MoveY(rocket.transform, landingY, 1.6f, easeIn: false);

        rocket.SetThrust(false);
        rocket.SetRumble(false);
        Synth.Instance.Thud();
        Haptics.Land();
        Debris.Burst(new Vector3(rocket.transform.position.x, landingY + 0.1f, rocket.transform.position.z), 0xFFFFFF, 12, 3f);
        // This rocket stays parked on the new planet.
        if (groundRocket != null) Destroy(groundRocket.gameObject);
        groundRocket = rocket;

        // Hop out onto the new planet.
        column = Tuning.Columns / 2;
        climberX = ColumnX(column);
        climber.transform.rotation = Quaternion.identity;
        climber.SetVisible(true);
        yield return new WaitForSeconds(0.2f);
        yield return Hop(climber.transform, rocket.Door, new Vector3(climberX, landingY, -0.55f), 0.4f);
        Arrive(planet + 1, landingY);
    }

    void Arrive(int planet, float y)
    {
        launchRocket = null;
        anchorY = y;
        stretch = restBase = 0;
        isSnapping = false;
        ResetStamina();
        awaitingRelease = lastTouchCount > 0;
        toneStep = 0;
        lastBlipY = y;
        runBest = Mathf.Max(runBest, y);
        phase = Phase.Playing;
        climber.Land();
        Synth.Instance.ZoneFanfare();
        Haptics.Success();
        ShowZoneStamp(planet * ZonesPerPlanet, collect: true);
        var effect = EffectFor(planet);
        if (effect != PlanetEffect.None) ShowTip(EffectTip(effect), 4.5f);
    }

    IEnumerator RumbleHaptics(float duration)
    {
        for (float t = 0; t < duration; t += 0.1f)
        {
            Haptics.Rumble(0.45f);
            yield return new WaitForSeconds(0.1f);
        }
    }
}
