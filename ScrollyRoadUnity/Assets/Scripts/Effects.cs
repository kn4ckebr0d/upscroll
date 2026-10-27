using UnityEngine;

/// A little cube that flies off, shrinks and disappears: dust puffs and coin sparkles.
public class Debris : MonoBehaviour
{
    public Vector3 Velocity;
    public float Gravity = 9f;
    public float Life = 0.5f;
    float age;
    Vector3 startScale;

    void Start() => startScale = transform.localScale;

    void Update()
    {
        age += Time.deltaTime;
        Velocity.y -= Gravity * Time.deltaTime;
        transform.position += Velocity * Time.deltaTime;
        transform.localScale = startScale * Mathf.Max(0, 1 - age / Life);
        if (age >= Life) Destroy(gameObject);
    }

    public static void Burst(Vector3 at, uint hex, int count = 10, float speed = 2.5f, float size = 0.15f)
    {
        for (int i = 0; i < count; i++)
        {
            var bit = Voxel.Box(null, Vector3.one * size, hex, at, castShadows: false);
            var debris = bit.AddComponent<Debris>();
            var dir = new Vector3(Random.Range(-1f, 1f), Random.Range(0.3f, 1f), Random.Range(-0.6f, 0.2f));
            debris.Velocity = dir * speed;
        }
    }
}

/// Spins around the world up axis (coins).
public class Spinner : MonoBehaviour
{
    public float DegreesPerSecond = 300f;

    void Update() => transform.Rotate(Vector3.up, DegreesPerSecond * Time.deltaTime, Space.World);
}

/// A collected coin: hops up, grows and fades out.
public class CoinPop : MonoBehaviour
{
    float age;
    Vector3 start;

    void Start() => start = transform.position;

    void Update()
    {
        age += Time.deltaTime;
        float t = age / 0.25f;
        transform.position = start + Vector3.up * 0.8f * t;
        transform.localScale = Vector3.one * (1 + 0.6f * t) * Mathf.Max(0, 1 - t * t);
        if (t >= 1) Destroy(gameObject);
    }
}

/// An empty object to run coroutines that must outlive the game's own.
public class CoroutineHost : MonoBehaviour { }
