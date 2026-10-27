using System.Collections.Generic;
using UnityEngine;

public enum WeatherKind { None, Snow, Embers }

/// Falling snowflakes or rising lava embers in front of the wall, following the camera.
/// Fades in and out as you climb into and out of a zone that has weather.
public class Weather : MonoBehaviour
{
    const int Count = 70;
    const float HalfWidth = 8f, HalfHeight = 11f;

    public WeatherKind Kind;
    public float CenterY;

    WeatherKind shown;
    float strength;   // 0..1, scales the particles in and out
    readonly List<Transform> particles = new();
    readonly List<Vector3> offsets = new();   // per particle: speed, drift phase, size

    public static Weather Create()
    {
        var weather = new GameObject("Weather").AddComponent<Weather>();
        for (int i = 0; i < Count; i++)
        {
            var p = Voxel.Box(weather.transform, Vector3.one, 0xFFFFFF, Vector3.zero, castShadows: false);
            p.transform.localScale = Vector3.zero;
            weather.particles.Add(p.transform);
            weather.offsets.Add(new Vector3(Random.Range(0.7f, 1.3f), Random.value * 10f, Random.Range(0.7f, 1.3f)));
        }
        return weather;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (Kind != shown)
        {
            // Fade out the old weather before switching to the new one.
            strength = Mathf.Max(0, strength - dt * 2f);
            if (strength <= 0)
            {
                shown = Kind;
                Recolor();
                Scatter();
            }
        }
        else if (shown != WeatherKind.None)
        {
            strength = Mathf.Min(1, strength + dt * 1.2f);
        }
        if (shown == WeatherKind.None) return;

        bool snow = shown == WeatherKind.Snow;
        for (int i = 0; i < particles.Count; i++)
        {
            var p = particles[i];
            var o = offsets[i];
            var pos = p.position;
            float t = Time.time + o.y;
            if (snow)
            {
                pos.y -= 2.4f * o.x * dt;
                pos.x += (Mathf.Sin(t * 1.3f) * 0.6f + 0.5f) * dt;
            }
            else
            {
                pos.y += 1.6f * o.x * dt;
                pos.x += Mathf.Sin(t * 2.1f) * 0.4f * dt;
            }
            // Wrap around the view, so there are always particles on screen.
            if (pos.y < CenterY - HalfHeight) pos.y += HalfHeight * 2;
            if (pos.y > CenterY + HalfHeight) pos.y -= HalfHeight * 2;
            if (pos.x > HalfWidth) pos.x -= HalfWidth * 2;
            if (pos.x < -HalfWidth) pos.x += HalfWidth * 2;
            p.position = pos;
            float size = (snow ? 0.13f : 0.09f) * o.z * strength;
            if (!snow) size *= 0.75f + 0.25f * Mathf.Sin(t * 9f);   // embers flicker
            p.localScale = Vector3.one * size;
        }
    }

    void Recolor()
    {
        for (int i = 0; i < particles.Count; i++)
        {
            uint hex = shown == WeatherKind.Snow ? 0xFFFFFFu : i % 3 == 0 ? 0xFFD23Du : 0xFF6A1Fu;
            particles[i].GetComponent<MeshRenderer>().sharedMaterial = Voxel.Material(hex);
            if (shown == WeatherKind.None) particles[i].localScale = Vector3.zero;
        }
    }

    void Scatter()
    {
        foreach (var p in particles)
            p.position = new Vector3(Random.Range(-HalfWidth, HalfWidth), CenterY + Random.Range(-HalfHeight, HalfHeight), Random.Range(-2.6f, -1.6f));
    }
}
