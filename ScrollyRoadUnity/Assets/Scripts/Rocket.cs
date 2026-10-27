using UnityEngine;

/// The voxel rocket: parked on the ground for continues, on the launch pad for new planets.
/// Origin at the bottom of the fins.
public class Rocket : MonoBehaviour
{
    Transform body;
    Transform flame;
    float flameStrength = 1;
    bool rumbling;

    public static Rocket Create(Transform parent, Vector3 position)
    {
        var rocket = new GameObject("Rocket").AddComponent<Rocket>();
        rocket.transform.SetParent(parent, false);
        rocket.transform.position = position;
        rocket.Build();
        return rocket;
    }

    /// The point a climber hops into.
    public Vector3 Door => transform.position + new Vector3(0, 0.9f, -0.3f);

    void Build()
    {
        const uint red = 0xE53935;
        body = new GameObject("body").transform;
        body.SetParent(transform, false);
        Voxel.Box(body, new Vector3(0.3f, 0.15f, 0.3f), 0x546E7A, new Vector3(0, 0.075f, 0));
        foreach (float x in new[] { -0.4f, 0.4f })
            Voxel.Box(body, new Vector3(0.25f, 0.5f, 0.25f), red, new Vector3(x, 0.25f, 0));
        Voxel.Box(body, new Vector3(0.65f, 1.6f, 0.65f), 0xF4F4F4, new Vector3(0, 0.95f, 0));
        Voxel.Box(body, new Vector3(0.67f, 0.15f, 0.67f), red, new Vector3(0, 1.45f, 0));
        Voxel.Box(body, new Vector3(0.3f, 0.3f, 0.05f), 0x7FE7FF, new Vector3(0, 1.0f, -0.33f));
        Voxel.Box(body, new Vector3(0.5f, 0.2f, 0.5f), red, new Vector3(0, 1.85f, 0));
        Voxel.Box(body, new Vector3(0.34f, 0.18f, 0.34f), red, new Vector3(0, 2.04f, 0));
        Voxel.Box(body, new Vector3(0.18f, 0.15f, 0.18f), red, new Vector3(0, 2.2f, 0));

        flame = new GameObject("flame").transform;
        flame.SetParent(transform, false);
        Voxel.Box(flame, new Vector3(0.36f, 0.6f, 0.36f), 0xFF8A00, new Vector3(0, -0.3f, 0), castShadows: false);
        Voxel.Box(flame, new Vector3(0.2f, 0.4f, 0.2f), 0xFFE14D, new Vector3(0, -0.22f, -0.1f), castShadows: false);
        flame.gameObject.SetActive(false);
    }

    public void SetThrust(bool on, float strength = 1)
    {
        flame.gameObject.SetActive(on);
        flameStrength = strength;
    }

    public void SetRumble(bool on)
    {
        rumbling = on;
        if (!on) body.localPosition = Vector3.zero;
    }

    void Update()
    {
        if (flame.gameObject.activeSelf)
            flame.localScale = new Vector3(1, flameStrength * (0.8f + 0.5f * Mathf.Abs(Mathf.Sin(Time.time * 60f))), 1);
        if (rumbling)
            body.localPosition = new Vector3(Random.Range(-0.03f, 0.03f), 0, 0);
    }
}
