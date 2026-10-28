using System.Collections;
using UnityEngine;

/// The little voxel climber: any character from Cosmetics, wearing any hat. Origin at the feet, facing the camera (-z).
public class Climber : MonoBehaviour
{
    Transform rig;
    Transform leftArm;
    Transform rightArm;
    float armAngle = 180, armTarget = 180;   // 0: reaching up (climbing), 180: hanging down (standing around)

    public Transform HatAnchor { get; private set; }

    /// Builds (or rebuilds) the climber as this character, with a hat, a backpack and a charm on it.
    public void Build(Cosmetic character, Cosmetic hat, Cosmetic backpack = null, Cosmetic charm = null)
    {
        if (rig != null) DestroyImmediate(rig.gameObject);   // right away: the closet renders several looks in one frame
        var look = character.Look;
        rig = new GameObject("rig").transform;
        rig.SetParent(transform, false);

        foreach (float x in new[] { -0.15f, 0.15f })
            Voxel.Box(rig, new Vector3(0.22f, 0.3f, 0.24f), look.Legs, new Vector3(x, 0.15f, 0));
        Voxel.Box(rig, new Vector3(0.6f, 0.45f, 0.4f), look.Body, new Vector3(0, 0.52f, 0));
        Voxel.Box(rig, new Vector3(0.55f, 0.45f, 0.5f), look.Head, new Vector3(0, 0.97f, 0));
        if (!look.OwnEyes)
            foreach (float x in new[] { -0.12f, 0.12f })
                Voxel.Box(rig, new Vector3(0.08f, 0.1f, 0.02f), 0x111111, new Vector3(x, 1.0f, -0.275f));
        look.Details?.Invoke(rig, look);

        // Caps and ears sit on top of the head, unless a hat goes there.
        bool wearsHat = hat != null && hat.Build != null;
        if (look.Top != null && !wearsHat)
        {
            var top = new GameObject("top").transform;
            top.SetParent(rig, false);
            top.localPosition = new Vector3(0, 1.19f, 0);
            look.Top(top, look);
        }
        HatAnchor = new GameObject("hat").transform;
        HatAnchor.SetParent(rig, false);
        HatAnchor.localPosition = new Vector3(0, 1.19f + look.HatLift, 0);
        if (wearsHat) hat.Build(HatAnchor);

        // The backpack goes on the back (in place of a built-in pack); the charm dangles from its hook.
        bool wearsBackpack = backpack != null && backpack.Build != null;
        var back = new GameObject("back").transform;
        back.SetParent(rig, false);
        if (wearsBackpack)
        {
            back.localPosition = new Vector3(0, 0.52f, 0.2f);
            backpack.Build(back);
            if (charm != null && charm.Build != null)
            {
                var hook = new GameObject("charm").transform;
                hook.SetParent(back, false);
                hook.localPosition = Cosmetics.CharmHook;
                charm.Build(hook);
                hook.gameObject.AddComponent<Dangle>();
            }
        }
        else look.Back?.Invoke(back, look);

        leftArm = MakeArm(-0.38f, look.Arms, look.Hands);
        rightArm = MakeArm(0.38f, look.Arms, look.Hands);
        ApplyArms();
    }

    /// Arms up only while climbing; down when standing, resting, in menus. They swing over the outside.
    public void SetArmsDown(bool down, bool instant = false)
    {
        armTarget = down ? 180 : 0;
        if (instant) armAngle = armTarget;
        ApplyArms();
    }

    void LateUpdate()
    {
        if (armAngle == armTarget || leftArm == null) return;
        armAngle = Mathf.MoveTowards(armAngle, armTarget, Time.deltaTime * 900);
        ApplyArms();
    }

    // Reaching up the arm stretches; hanging down it's shorter, with the hand at the hip.
    const float ReachLength = 0.5f, HangLength = 0.28f;

    void ApplyArms()
    {
        if (leftArm == null) return;
        leftArm.localRotation = Quaternion.Euler(0, 0, armAngle);
        rightArm.localRotation = Quaternion.Euler(0, 0, -armAngle);
        float length = Mathf.Lerp(ReachLength, HangLength, armAngle / 180f);
        foreach (var arm in new[] { leftArm, rightArm })
        {
            var sleeve = arm.GetChild(0);
            sleeve.localScale = new Vector3(0.15f, length, 0.15f);
            sleeve.localPosition = new Vector3(0, length / 2, 0);
            arm.GetChild(1).localPosition = new Vector3(0, length + 0.05f, 0);
        }
    }

    /// Puts the whole climber on one layer (the closet preview renders its own).
    public void SetLayer(int layer)
    {
        foreach (var t in GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
    }

    Transform MakeArm(float x, uint shirt, uint skin)
    {
        var arm = new GameObject("arm").transform;
        arm.SetParent(rig, false);
        arm.localPosition = new Vector3(x, 0.7f, 0);
        Voxel.Box(arm, new Vector3(0.15f, ReachLength, 0.15f), shirt, new Vector3(0, ReachLength / 2, 0));   // the sleeve (sized in ApplyArms)
        Voxel.Box(arm, new Vector3(0.17f, 0.15f, 0.17f), skin, new Vector3(0, ReachLength + 0.05f, 0));      // the hand
        return arm;
    }

    /// Hand over hand: the arms alternate as you climb.
    public void SetClimbPhase(float phase)
    {
        float s = Mathf.Sin(phase);
        leftArm.localScale = new Vector3(1, 0.85f + 0.15f * s, 1);
        rightArm.localScale = new Vector3(1, 0.85f - 0.15f * s, 1);
    }

    GameObject dizzy;

    /// Stars circling the head after a fall.
    public void ShowDizzy(bool visible)
    {
        if (dizzy != null) Destroy(dizzy);
        if (!visible) return;
        dizzy = new GameObject("dizzy");
        dizzy.transform.position = transform.TransformPoint(new Vector3(0, 1.0f, 0)) + Vector3.up * 0.45f;
        dizzy.transform.SetParent(transform, true);   // follows the climber
        for (int i = 0; i < 3; i++)
        {
            float angle = i * Mathf.PI * 2 / 3;
            Voxel.Box(dizzy.transform, Vector3.one * 0.13f, 0xFFE14D,
                      new Vector3(Mathf.Cos(angle) * 0.38f, 0, Mathf.Sin(angle) * 0.38f), castShadows: false);
        }
        dizzy.AddComponent<Spinner>().DegreesPerSecond = 450;
    }

    /// Hide while inside a rocket, or to blink while invulnerable.
    public void SetVisible(bool visible)
    {
        foreach (var r in rig.GetComponentsInChildren<Renderer>()) r.enabled = visible;
    }

    public void ResetPose()
    {
        StopAllCoroutines();
        SetVisible(true);
        ShowDizzy(false);
        rig.localScale = Vector3.one;
        leftArm.localScale = rightArm.localScale = Vector3.one;
        transform.rotation = Quaternion.identity;
    }

    /// Little squash when grabbing a checkpoint.
    public void Land()
    {
        StopAllCoroutines();
        StartCoroutine(Squash());
    }

    IEnumerator Squash()
    {
        for (float t = 0; t < 0.19f; t += Time.deltaTime)
        {
            float k = t < 0.05f ? t / 0.05f : 1 - (t - 0.05f) / 0.14f;
            rig.localScale = new Vector3(1, 1 - 0.18f * k, 1);
            yield return null;
        }
        rig.localScale = Vector3.one;
    }
}

/// Makes a charm swing like a little pendulum: it lags behind every move, overshoots and settles.
public class Dangle : MonoBehaviour
{
    Vector3 lastPosition, lastVelocity;
    Vector2 angle, spin;   // degrees around the local x (forward and back) and z (side to side) axes
    bool started;

    void LateUpdate()
    {
        float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        var position = transform.parent.TransformPoint(transform.localPosition);
        var velocity = (position - lastPosition) / dt;
        if (!started)
        {
            started = true;
            lastPosition = position;
            lastVelocity = velocity = Vector3.zero;
        }
        var acceleration = Vector3.ClampMagnitude((velocity - lastVelocity) / dt, 400f);
        lastPosition = position;
        lastVelocity = velocity;

        // Push the opposite way of the acceleration (in the parent's frame); a bump up or down
        // also rocks it a little. Then a spring pulls it back to hanging straight down.
        var local = transform.parent.InverseTransformDirection(acceleration);
        var push = new Vector2(local.z * 200f + local.y * 150f, -local.x * 220f);   // about 20° per m/s²
        // While it's moving up or down (climbing), the charm sways side to side and bobs a bit.
        float climbing = Mathf.Min(Mathf.Abs(velocity.y), 8f);
        push += new Vector2(Mathf.Cos(Time.time * 6f) * 180f, Mathf.Sin(Time.time * 7.5f) * 320f) * climbing;
        spin += (push - angle * 90f - spin * 2.6f) * dt;
        angle += spin * dt;
        angle = Vector2.ClampMagnitude(angle, 75f);
        transform.localRotation = Quaternion.Euler(angle.x, 0, angle.y);
    }
}
