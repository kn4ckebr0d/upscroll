using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// Real taptic feedback on iPhone (via a tiny native plugin) and short vibrations on Android.
public static class Haptics
{
    public static bool Enabled = true;   // the haptics setting

    enum Style { Light = 0, Medium = 1, Heavy = 2, Soft = 3, Rigid = 4 }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void ScrollyHapticImpact(int style, float intensity);
    [DllImport("__Internal")] static extern void ScrollyHapticNotify(int type);
#endif

    /// Climb tick; pass a higher intensity as the next checkpoint gets closer.
    public static void Tick(float intensity) => Impact(Style.Light, Mathf.Clamp(intensity, 0.1f, 1f));
    public static void Snap() => Impact(Style.Soft, 1f);
    public static void Slip() => Impact(Style.Rigid, 0.8f);
    public static void Hit() => Impact(Style.Heavy, 1f);
    public static void Land() => Impact(Style.Heavy, 1f);
    public static void Rumble(float intensity) => Impact(Style.Rigid, intensity);

    public static void Success()
    {
        if (!Enabled) return;
#if UNITY_IOS && !UNITY_EDITOR
        ScrollyHapticNotify(0);
#else
        Impact(Style.Medium, 1f);
#endif
    }

    static void Impact(Style style, float intensity)
    {
        if (!Enabled) return;
#if UNITY_IOS && !UNITY_EDITOR
        ScrollyHapticImpact((int)style, intensity);
#elif UNITY_ANDROID && !UNITY_EDITOR
        long ms = style switch { Style.Light => 8, Style.Soft => 12, Style.Rigid => 15, Style.Medium => 22, _ => 35 };
        AndroidPulse(ms, Mathf.RoundToInt(40 + 215 * intensity));
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    static AndroidJavaObject vibrator;
    static int sdk;
    static bool initialized;

    static void AndroidPulse(long ms, int amplitude)
    {
        try
        {
            if (!initialized)
            {
                initialized = true;
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                sdk = version.GetStatic<int>("SDK_INT");
            }
            if (vibrator == null) return;
            if (sdk >= 26)
            {
                using var effects = new AndroidJavaClass("android.os.VibrationEffect");
                using var effect = effects.CallStatic<AndroidJavaObject>("createOneShot", ms, Mathf.Clamp(amplitude, 1, 255));
                vibrator.Call("vibrate", effect);
            }
            else
            {
                vibrator.Call("vibrate", ms);
            }
        }
        catch (System.Exception)
        {
            vibrator = null;   // no vibrator or no permission: stay quiet
        }
    }

    // Referencing Handheld.Vibrate makes Unity add the VIBRATE permission to the Android manifest.
    static void RequireVibratePermission() => Handheld.Vibrate();
#endif
}
