using UnityEngine;

/// All the feel knobs in one place. Ported 1:1 from the Swift prototype (1 unit = 1 meter).
public static class Tuning
{
    public const float ZoneLength = 40f;                 // meters per zone

    // The climb is an iOS-style rubber band: easy at first, heavier the further you get above the
    // checkpoint or ledge you last rested on.
    public const float RubberBandCoefficient = 0.55f;    // the constant UIScrollView uses
    public const float FullStrainRatio = 0.6f;           // band share that counts as fully strained (climber shakes)
    public const float TiredGrip = 0.65f;                // with no stamina left the band pulls this much less far

    // How much of the band's max stretch one zone uses. Higher = the last meters get heavier. Grows each zone.
    public const float ZoneReachStart = 0.45f;
    public const float ZoneReachPerZone = 0.025f;       // gentle, so thumbs don't tire on the early planets
    public const float ZoneReachMax = 0.58f;            // the heaviest it gets: still climbable at a slow pace

    // Sideways movement
    public const int Columns = 5;
    public const float ColumnSpacing = 1f;
    public const float SwipeDistance = 0.075f;           // share of screen width a finger must travel sideways
    public const float SwipeMaxTime = 0.4f;              // ...within this many seconds of touching down

    // Stamina, 0..1
    public const float StaminaBaseDrain = 0.018f;        // per second while hanging between checkpoints
    public const float StaminaTensionDrain = 0.12f;      // extra per second, scaled by how tight the band is
    public const float StaminaRegen = 0.5f;              // per second while resting on a checkpoint
    public const float StaminaRecoverAt = 0.35f;         // after slipping, you can grab again from here

    // Letting go: you fall, speeding up. Grabbing again slows you to a stop (a slide, not an instant hold).
    // Hitting the checkpoint (or a ledge) faster than a fall from halfway up a zone ends the run.
    public const float FallGravity = 60f;                // m/s² while falling
    public const float GripFriction = 110f;              // m/s² of braking while you hold on again (fresh hands)
    public const float SafeFallShare = 0.5f;             // a fall from this share of a zone is the most you survive uncaught

    // Mobs
    public const float SafeStartMeters = 8f;             // no mobs below this
    public const float ZoneSafeMeters = 5f;              // no mobs this close above a checkpoint
    public const float LaneHalfSpan = 7f;                // mobs wrap around between -7 and +7 m
    public const float HitStop = 0.12f;                  // freeze frame when a mob hits you

    // Coins
    public const float CoinSpacing = 0.8f;               // meters between coins in a pattern
    public const float CoinGapMin = 4f, CoinGapMax = 9f; // meters between patterns

    // Planets: after the last zone a rocket flies you through empty space to the next planet.
    public const float RocketFlightMeters = 40f;         // empty space between planets (long enough to keep the next planet out of view at liftoff)
    public const int PlanetGroundRows = 8;               // ground thickness under a new planet's surface
    public const float PlanetHueShift = 0.18f;           // each planet recolors the zone palettes by this hue

    // Obstacles and planet effects
    public const float ClimberHeight = 1.3f;             // head height, for bumping into blocks
    public const float WaterfallPush = 1.5f;             // meters per second a waterfall pushes you down
    public const float WaterfallDrain = 0.08f;           // extra stamina drain per second in a waterfall
    public const float WaterDropStamina = 0.3f;          // stamina from one water drop (hot planets)
    public const float IceSlip = 0.35f;                  // meters per second you slip on ice
    public const float LedgeChance = 0.3f;               // share of boulders that are a rest ledge (at most one per zone)
    public const float FreeHeartChance = 0.35f;          // share of runs that hide a free heart somewhere

    // Boss planets: the meteor storm
    public const float MeteorWarning = 1.3f;             // seconds between the red mark and the impact
    public const float MeteorIntervalStart = 2.4f;       // seconds between meteors at the bottom of the storm...
    public const float MeteorIntervalEnd = 1.3f;         // ...and at the top
    public const int BossReward = 50;                    // coins for reaching the rocket

    // Shop
    public const int ExtraLifePrice = 60;
    public const int MaxExtraLives = 3;
    public const int ContinueBasePrice = 30;             // doubles with each continue in the same run
    public const float InvulnerableTime = 1.5f;          // after using a life or continuing

    // Sound
    public const float BlipDistance = 0.5f;              // meters climbed per note of the rising tone
    public const int ToneSteps = 12;                     // top note, reached just before the next checkpoint

    // Camera: tilted orthographic, Crossy Road style.
    public const float ViewHeight = 18f;                 // meters visible vertically
    public const float ClimberScreenFraction = 0.3f;     // where the climber sits on screen, from the bottom
    public const float CameraPitch = 18f;                // degrees looking down
    public const float CameraYaw = 22f;                  // degrees looking in from the right
    public const float CameraDistance = 20f;
}
