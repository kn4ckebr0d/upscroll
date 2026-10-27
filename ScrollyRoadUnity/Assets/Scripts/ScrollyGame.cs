using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Rendering;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/// The whole game, built procedurally from code. Ported from the Swift prototype.
/// This file: lifecycle, input, climbing, the world (wall, planets, checkpoints) and the camera.
/// Other parts: Hazards (mobs, falling), Coins, Stamina, Planets (rocket launch), Shop, HUD.
public partial class ScrollyGame : MonoBehaviour
{
    public Material voxelMaterial;
    public Material shadeMaterial;   // transparent unlit: lane stripes
    public Material coinMaterial;    // gold with a little glow
    public Font uiFont;              // rounded and heavy (Fredoka Bold)

    enum Phase { Title, Playing, Launching, Falling, GameOver, Reviving }

    class Checkpoint
    {
        public float Y;
        public int Zone;            // the zone it leads into
        public Transform Node;
        public Rocket Rocket;       // set on a launch pad
        public bool IsLaunchPad => Rocket != null;
    }

    // Scene
    Camera cam;
    Light sun;
    Climber climber;
    Transform world;
    readonly Dictionary<int, GameObject> rows = new();
    readonly List<Checkpoint> checkpoints = new();
    float plannedUpTo;
    float cameraFocusY;
    float shakeTime, shakeStrength;
    Weather weather;

    // Climb state
    Phase phase = Phase.Title;
    float anchorY;         // height of the last checkpoint
    float pendingPull;     // finger travel this frame (meters)
    float stretch;         // height above the anchor
    float restBase;        // stretch where you last rested (0 = the checkpoint, more = a ledge); the band pulls from here
    bool isSnapping;       // let go: falling (or sliding to a stop while holding on again)
    float snapTo;          // stretch to land at (0 = the checkpoint, more = a ledge)
    float fallSpeed;       // m/s, downward
    bool safeLanding;      // this fall can't hurt (sent back by an extra life)
    bool awaitingRelease;  // planted on a checkpoint until every finger lifts
    int column = Tuning.Columns / 2;
    float climberX;
    float climberFacing;   // 0 = facing you (title screen), 180 = facing the wall (climbing)
    int lastTouchCount;
    readonly HashSet<int> swipedTouches = new();
    readonly HashSet<int> buttonTouches = new();
    float runBest;
    int bestAtStart;
    int toneStep;
    float lastBlipY;

    // Test harness (Mac builds): -scrollyAutopilot, -scrollyGod, -scrollyStartZone <n>, -scrollyBook, -scrollyBookTurn, -scrollySettings, -scrollyCloset, -scrollyClosetTab <kind>, -scrollyInspect, -scrollyInspectFull, -scrollyWearTag <id>, -scrollyTagTour, -scrollyCatalog <dir>, -scrollyGift, -scrollyGiftTaps, -scrollyAllStamps,
    // -scrollyShotDir <dir> -scrollyShots <n> -scrollyShotInterval <s> -scrollyShotDelay <s>
    bool autopilot;
    bool god;
    int debugStartZone;
    int autopilotFrame;
    bool autopilotHolding;
    float autopilotRegrabAt;

    float StretchLimit
    {
        get
        {
            int zone = ZoneIndex(anchorY + 0.01f);
            float reach = Mathf.Min(Tuning.ZoneReachMax, Tuning.ZoneReachStart + Tuning.ZoneReachPerZone * zone);
            // The band is sized to the climb up to the next checkpoint (three zones in a meteor storm).
            var next = NextCheckpoint();
            float length = next != null ? Mathf.Max(Tuning.ZoneLength, next.Y - anchorY) : Tuning.ZoneLength;
            return length / reach * GravityStretchFactor;
        }
    }

    /// 0 = slack, 1 = the band's limit. Measured from where you last rested, so a rest on a ledge relaxes it.
    float Tension => Mathf.Clamp01((stretch - restBase) / StretchLimit);

    /// 0 = relaxed, 1 = as tight as the band normally gets.
    float Strain => Mathf.Min(1f, Tension / Tuning.FullStrainRatio);

    /// Meters climbed per meter of finger travel: the rubber band's slope (it gets heavier the tighter
    /// it is), and heavier still when you're tired.
    float Grip
    {
        get
        {
            float slack = 1 - Tension;
            return Tuning.RubberBandCoefficient * slack * slack * Mathf.Lerp(Tuning.TiredGrip, 1f, stamina);
        }
    }

    float ClimberY => climber.transform.position.y;

    static float ColumnX(int c) => (c - Tuning.Columns / 2) * Tuning.ColumnSpacing;

    // MARK: - World geometry
    // Each planet is a climbable wall of zones, then empty space the rocket flies through,
    // then the next planet's ground.

    static int ZonesPerPlanet => ZoneTheme.ZonesPerPlanet;
    static float PlanetClimb => Tuning.ZoneLength * ZonesPerPlanet;
    static float PlanetSpan => PlanetClimb + Tuning.RocketFlightMeters;
    static int PlanetIndex(float y) => Mathf.FloorToInt(Mathf.Max(0, y) / PlanetSpan);
    static float PlanetBase(int planet) => planet * PlanetSpan;

    /// Global zone number, counting across planets. Flight space counts as the planet's last zone.
    static int ZoneIndex(float y)
    {
        int planet = PlanetIndex(y);
        float local = Mathf.Max(0, y) - PlanetBase(planet);
        return planet * ZonesPerPlanet + Mathf.Min(ZonesPerPlanet - 1, Mathf.FloorToInt(local / Tuning.ZoneLength));
    }

    enum RowKind { Wall, Void, PlanetGround, PlanetSurface }

    static RowKind KindOfRow(int r)
    {
        int planet = PlanetIndex(r + 0.01f);
        float local = r - PlanetBase(planet);
        if (local < PlanetClimb - 0.01f) return RowKind.Wall;
        if (local >= PlanetSpan - 1.01f) return RowKind.PlanetSurface;
        if (local >= PlanetSpan - Tuning.PlanetGroundRows - 0.01f) return RowKind.PlanetGround;
        return RowKind.Void;
    }

    // MARK: - Lifecycle

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
#if UNITY_EDITOR || UNITY_STANDALONE
        TouchSimulation.Enable();   // mouse acts as a finger when testing on a computer
#endif
    }

    void OnDisable()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        TouchSimulation.Disable();
#endif
        EnhancedTouchSupport.Disable();
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) Progress.Save();
    }

    void Start()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        Voxel.Init(voxelMaterial);
        Synth.Create();
        Haptics.Enabled = Progress.HapticsOn;
        weather = Weather.Create();

        SetupCamera();
        SetupLight();
        BuildGround();
        climber = new GameObject("Climber").AddComponent<Climber>();
        BuildPreview();
        DressClimber();
        BuildHUD();
        ParseTestArguments();
        ResetWorld();
    }

    void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, 1f / 20f);
        HandleInput();
        if (autopilot) RunAutopilot();

        switch (phase)
        {
            case Phase.Title:
            case Phase.Playing:
                UpdateClimb(dt);
                break;
            case Phase.Falling:
                UpdateFall(dt);
                break;
        }
        bool hitStop = phase == Phase.Falling && fallTime < Tuning.HitStop;
        if (!hitStop)
        {
            UpdateLanes(dt);
            UpdateMeteors();
        }
        if (phase == Phase.Playing)
        {
            if (!god) CheckCollisions();
            CollectCoins();
        }
        UpdateCamera(dt);
        GenerateContent();
        UpdateHUD();
    }

    /// Back to the ground for a new run.
    void ResetWorld()
    {
        StopAllCoroutines();
        if (world != null) Destroy(world.gameObject);
        world = new GameObject("World").transform;
        rows.Clear();
        checkpoints.Clear();
        lanes.Clear();
        coins.Clear();
        obstacles.Clear();
        ResetPlanetEffects();
        ClearMeteors();
        fingers.Clear();
        RollFreeHeart();
        pressingCeiling = inWaterfall = false;
        launchRocket = taxiRocket = groundRocket = null;

        phase = Phase.Title;
        anchorY = 0;
        if (debugStartZone > 0)
            anchorY = PlanetBase(debugStartZone / ZonesPerPlanet) + debugStartZone % ZonesPerPlanet * Tuning.ZoneLength;
        plannedUpTo = anchorY;
        stretch = restBase = 0;
        isSnapping = false;
        awaitingRelease = false;
        column = Tuning.Columns / 2;
        climberX = ColumnX(column);
        runBest = 0;
        runCoins = 0;
        continuesUsed = 0;
        invulnerableUntil = 0;
        toneStep = 0;
        lastBlipY = anchorY;
        bestAtStart = Progress.Best;
        PlaceBestFlag();
        ResetStamina();
        PlaceGroundRocket(PlanetBase(PlanetIndex(anchorY + 0.01f)));

        climber.ResetPose();
        climber.transform.SetPositionAndRotation(new Vector3(climberX, anchorY, -0.55f), Quaternion.identity);
        climberFacing = 0;
        ClearTransientHud();
        ShowTitle(true);
        ShowGameOver(false);
        ShowContinueOffer(false);
        RefreshShop();
        UpdateCamera(0);
        GenerateContent();
        // Learn to climb by doing: the full lesson on first launch, a short warm-up on the second run.
        if (Progress.TutorialRuns < 2 && !autopilot) StartTutorial(warmUp: Progress.TutorialRuns == 1);
    }

    // MARK: - Setup

    void SetupCamera()
    {
        cam = new GameObject("Camera").AddComponent<Camera>();
        cam.gameObject.AddComponent<AudioListener>();
        cam.tag = "MainCamera";
        cam.orthographic = true;
        cam.orthographicSize = Tuning.ViewHeight / 2f;
        cam.nearClipPlane = 1f;
        cam.farClipPlane = 80f;
        cam.clearFlags = CameraClearFlags.SolidColor;
    }

    void SetupLight()
    {
        sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        // Balanced (measured) so a block's front face shows its full palette color,
        // tops a bit brighter, and sides and shadows clearly darker for the 3D read.
        sun.intensity = 0.9f;
        sun.color = Color.white;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.85f;
        // From the upper left and in front, so shadows fall down-right onto the wall.
        sun.transform.rotation = Quaternion.LookRotation(new Vector3(0.35f, -0.75f, 0.56f));
        var ambient = new Color(0.92f, 0.92f, 0.92f);   // neutral white: no grey-blue cast over the colors
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = ambient;
        var probe = new SphericalHarmonicsL2();
        probe.AddAmbientLight(ambient);
        RenderSettings.ambientProbe = probe;
    }

    void BuildGround()
    {
        var ground = new GameObject("Ground").transform;
        Voxel.Box(ground, new Vector3(30, 0.4f, 16), 0x6CBF4B, new Vector3(0, -0.2f, -3));
        Voxel.Box(ground, new Vector3(30, 3, 16), 0x7A5236, new Vector3(0, -1.9f, -3));
        foreach (var p in new[] { new Vector2(-4.6f, -2.6f), new Vector2(5.4f, -1.2f), new Vector2(-6.8f, -0.4f), new Vector2(3.4f, -4.5f) })
            BuildTree(ground, new Vector3(p.x, 0, p.y), 0x4E9A34, 0x5DB043);
        Voxel.Box(ground, new Vector3(0.6f, 0.4f, 0.6f), 0x9E9E9E, new Vector3(-2.8f, 0.2f, -3.2f));
    }

    static void BuildTree(Transform parent, Vector3 position, uint leaves, uint top)
    {
        var tree = new GameObject("tree").transform;
        tree.SetParent(parent, false);
        tree.localPosition = position;
        Voxel.Box(tree, new Vector3(0.4f, 0.8f, 0.4f), 0x7A5236, new Vector3(0, 0.4f, 0));
        Voxel.Box(tree, new Vector3(1.2f, 1.2f, 1.2f), leaves, new Vector3(0, 1.4f, 0));
        Voxel.Box(tree, new Vector3(0.8f, 0.6f, 0.8f), top, new Vector3(0, 2.3f, 0));
    }

    static void BuildCactus(Transform parent, Vector3 position)
    {
        var cactus = new GameObject("cactus").transform;
        cactus.SetParent(parent, false);
        cactus.localPosition = position;
        const uint green = 0x5E9E3A;
        Voxel.Box(cactus, new Vector3(0.5f, 2.2f, 0.5f), green, new Vector3(0, 1.1f, 0));
        Voxel.Box(cactus, new Vector3(0.5f, 0.3f, 0.3f), green, new Vector3(-0.45f, 0.9f, 0));
        Voxel.Box(cactus, new Vector3(0.3f, 0.7f, 0.3f), green, new Vector3(-0.6f, 1.3f, 0));
        Voxel.Box(cactus, new Vector3(0.45f, 0.3f, 0.3f), green, new Vector3(0.45f, 1.3f, 0));
        Voxel.Box(cactus, new Vector3(0.3f, 0.6f, 0.3f), green, new Vector3(0.6f, 1.6f, 0));
        Voxel.Box(cactus, new Vector3(0.22f, 0.15f, 0.22f), 0xF26D8F, new Vector3(0, 2.28f, 0));   // a flower on top
    }

    // MARK: - World

    static int HalfWidth(int r)
    {
        double v = System.Math.Sin((r / 3) * 12.9898 + 78.233) * 43758.5453;
        return v - System.Math.Floor(v) < 0.5 ? 2 : 3;
    }

    /// Builds rows around the camera in both directions (so falls and snap-backs never
    /// show gaps), drops the ones far away, and plans checkpoints, mobs and coins ahead.
    void GenerateContent()
    {
        float focus = cameraFocusY;
        int low = Mathf.FloorToInt(focus - Tuning.ViewHeight * 0.75f) - 2;
        int high = Mathf.CeilToInt(focus + Tuning.ViewHeight * 0.75f) + 2;
        for (int r = Mathf.Max(0, low); r <= high; r++)
            if (!rows.ContainsKey(r)) rows[r] = BuildRow(r);

        var stale = new List<int>();
        foreach (var r in rows.Keys)
            if (r < low - 3 || r > high + 3) stale.Add(r);
        foreach (var r in stale)
        {
            Destroy(rows[r]);
            rows.Remove(r);
        }

        PlanContent(high + Tuning.ViewHeight);

        // Keep mobs and coins down to the anchor so sliding back still shows them.
        float bottom = Mathf.Min(low, anchorY - Tuning.ViewHeight * 0.5f) - 3;
        CullHazards(bottom);
        CullObstacles(bottom);
        CullCoins(bottom);
    }

    GameObject BuildRow(int r)
    {
        var row = new GameObject("row " + r);
        row.transform.SetParent(world, false);
        switch (KindOfRow(r))
        {
            case RowKind.Wall: BuildWallRow(row.transform, r); break;
            case RowKind.Void: BuildStars(row.transform, r); break;
            case RowKind.PlanetGround:
            case RowKind.PlanetSurface: BuildPlanetGroundRow(row.transform, r); break;
        }
        return row;
    }

    void BuildWallRow(Transform row, int r)
    {
        var theme = ThemeFor(ZoneIndex(r + 0.01f));
        int half = HalfWidth(r);
        int above = KindOfRow(r + 1) == RowKind.Wall ? HalfWidth(r + 1) : 0;
        for (int c = -half; c <= half; c++)
        {
            bool edge = Mathf.Abs(c) == half;
            uint hex = theme.Rocks[(int)(Voxel.Hash(r, c) * theme.Rocks.Length) % theme.Rocks.Length];
            if (edge) hex = theme.Edge;
            else if (Voxel.Hash(r, c, 1) < theme.AccentChance) hex = theme.Accents[(int)(Voxel.Hash(r, c, 2) * theme.Accents.Length) % theme.Accents.Length];

            // Edge blocks sometimes stick out a bit for relief. The wall's front face is at z = 0.
            float depth = edge && Voxel.Hash(r, c, 3) < 0.5f ? 1.4f : 1f;
            float z = 1f - depth / 2f;
            Voxel.Box(row, new Vector3(1, 1, depth), hex, new Vector3(c, r + 0.5f, z));
            // Where the wall steps in above, the exposed top gets grass or snow.
            if (Mathf.Abs(c) > above)
                Voxel.Box(row, new Vector3(1.02f, 0.18f, depth + 0.02f), theme.Cap, new Vector3(c, r + 1.09f, z));
        }
        if (Voxel.Hash(r, 99) < 0.16f)
        {
            float side = Voxel.Hash(r, 98) < 0.5f ? -1 : 1;
            Voxel.Box(row, new Vector3(0.5f, 0.45f, 0.5f), theme.Accents[0], new Vector3(side * (half + 0.65f), r + 0.4f, 0.3f));
        }
    }

    /// Open space between planets: a few stars.
    void BuildStars(Transform row, int r)
    {
        for (int i = 0; i < 3; i++)
        {
            if (Voxel.Hash(r, i, 7) > 0.5f) continue;
            float x = (Voxel.Hash(r, i, 8) - 0.5f) * 14f;
            Voxel.Box(row, Vector3.one * 0.12f, 0xFFFFFF, new Vector3(x, r + Voxel.Hash(r, i, 9), 3f), castShadows: false);
        }
    }

    /// The ground of the next planet, recolored for that planet.
    void BuildPlanetGroundRow(Transform row, int r)
    {
        int planet = PlanetIndex(r + 0.01f) + 1;
        var theme = ThemeFor(planet * ZonesPerPlanet);
        bool surface = KindOfRow(r) == RowKind.PlanetSurface;
        uint ground = EffectFor(planet) == PlanetEffect.Ice ? theme.Cap : theme.Accents[0];   // snow on icy planets
        Voxel.Box(row, new Vector3(30, 1, 16), surface ? ground : theme.Edge, new Vector3(0, r + 0.5f, -3));
        if (surface)
        {
            var effect = EffectFor(planet);
            foreach (var spot in new[] { new Vector3(-4.6f, r + 1, -2.6f), new Vector3(5.4f, r + 1, -1.2f) })
            {
                if (effect == PlanetEffect.Boss) Voxel.Box(row, new Vector3(1.2f, 0.8f, 1.1f), theme.Edge, spot + new Vector3(0, 0.4f, 0));   // a craggy rock
                else if (effect == PlanetEffect.Hot) BuildCactus(row, spot);
                else if (effect == PlanetEffect.Ice) BuildTree(row, spot, 0x2F6B4F, 0xFFFFFF);   // snowy pine
                else BuildTree(row, spot, theme.Accents[0], theme.Cap);
            }
        }
    }

    /// A checkpoint at the start of every zone, with mob lanes and coins in between.
    /// The last one on a planet is a launch pad with a rocket.
    void PlanContent(float upTo)
    {
        bool inStorm = false;
        while (plannedUpTo < upTo || inStorm)
        {
            int planet = PlanetIndex(plannedUpTo + 0.01f);
            float planetBase = PlanetBase(planet);
            int k = Mathf.RoundToInt((plannedUpTo - planetBase) / Tuning.ZoneLength) + 1;
            float y = planetBase + k * Tuning.ZoneLength;
            bool launchPad = k == ZonesPerPlanet;
            // A boss planet's storm is one big zone: no checkpoints inside it.
            bool storm = EffectFor(planet) == PlanetEffect.Boss && k >= 2;
            AddLanes(plannedUpTo, y);
            AddObstacles(plannedUpTo, y);
            AddCoins(plannedUpTo, y);
            if (!storm || launchPad)
                AddCheckpoint(y, launchPad ? (planet + 1) * ZonesPerPlanet : planet * ZonesPerPlanet + k, launchPad);
            // A storm is planned through to its rocket, so the climb knows where it ends.
            inStorm = storm && !launchPad;
            plannedUpTo = launchPad ? PlanetBase(planet + 1) : y;
        }
    }

    void AddCheckpoint(float y, int zone, bool launchPad)
    {
        var theme = ThemeFor(zone);
        var node = new GameObject("checkpoint " + zone).transform;
        node.SetParent(world, false);
        node.localPosition = new Vector3(0, y, 0);
        var checkpoint = new Checkpoint { Y = y, Zone = zone, Node = node };
        if (launchPad)
        {
            // A metal shelf with hazard stripes, and the rocket on a platform to the side.
            Voxel.Box(node, new Vector3(7.6f, 0.25f, 0.32f), 0x8E9AA6, new Vector3(0, -0.125f, -0.15f));
            for (int i = 0; i < 12; i++)
                Voxel.Box(node, new Vector3(0.3f, 0.1f, 0.02f), i % 2 == 0 ? 0xFFD54Fu : 0x263238u, new Vector3(-3.6f + i * 0.3f, -0.12f, -0.32f), castShadows: false);
            Voxel.Box(node, new Vector3(1.3f, 0.25f, 1.3f), 0x8E9AA6, new Vector3(3.3f, -0.125f, -0.65f));
            checkpoint.Rocket = Rocket.Create(node, node.position + new Vector3(3.3f, 0, -0.65f));
        }
        else
        {
            // A shelf against the wall: thin enough that the climber passes in front of it.
            Voxel.Box(node, new Vector3(6, 0.25f, 0.32f), theme.Ledge, new Vector3(0, -0.125f, -0.15f));
            Voxel.Box(node, new Vector3(0.1f, 1.4f, 0.1f), 0xF2F2F2, new Vector3(-2.6f, 0.7f, -0.2f));
            Voxel.Box(node, new Vector3(0.6f, 0.35f, 0.06f), 0xFFD54F, new Vector3(-2.27f, 1.2f, -0.2f));
        }
        checkpoints.Add(checkpoint);
    }

    Checkpoint NextCheckpoint()
    {
        foreach (var c in checkpoints)
            if (c.Y > anchorY + 0.01f) return c;
        return null;
    }

    // MARK: - Input

    void HandleInput()
    {
        float pull = 0;
        int down = 0;
        int driver = -1;
        pendingPull = 0;
        float metersPerPixel = Tuning.ViewHeight / Mathf.Max(1, Screen.height);
        foreach (var touch in Touch.activeTouches)
        {
            if (touch.phase == TouchPhase.Began && TryPressButton(touch.screenPosition, onRelease: false))
                buttonTouches.Add(touch.touchId);

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                swipedTouches.Remove(touch.touchId);
                bool wasButton = buttonTouches.Remove(touch.touchId);
                bool scrolled = closetPanel.activeSelf && EndClosetScroll(touch);   // a scroll isn't a tap
                if (wasButton && !scrolled) TryPressButton(touch.screenPosition, onRelease: true);
                else if (touch.touchId == bookDragTouch) EndBookDrag(touch);
                if (TutorialActive) TutorialTouchEnded(touch);
                FingerLifted(touch.touchId);
                continue;
            }
            if (closetPanel.activeSelf) DragClosetGrid(touch);
            if (buttonTouches.Contains(touch.touchId)) continue;
            if (closetPanel.activeSelf) continue;   // spinning, zooming or scrolling: not climbing
            if (bookPanel.activeSelf)
            {
                DragBookPage(touch);   // the open stamp book takes the touch: swipe between pages
                continue;
            }
            if (TutorialActive) TutorialTouch(touch);
            down++;
            // Whichever finger moved the most drives the climb, so hand-offs feel seamless,
            // as long as it's that finger's turn (see ScrollyGame.Fingers.cs).
            float dy = AllowedPull(touch, -touch.delta.y * metersPerPixel);   // dragging down climbs up
            if (Mathf.Abs(dy) > Mathf.Abs(pull))
            {
                pull = dy;
                driver = touch.touchId;
            }
            DetectSwipe(touch);
        }

        if (down == 0)
        {
            awaitingRelease = false;
            if (lastTouchCount > 0) Release();
        }
        lastTouchCount = down;

        if (closetPanel.activeSelf) PreviewInput();
        if (driver != -1) FingerDrove(driver, pull);
        if (pull == 0) return;
        if (phase == Phase.Title && pull > 0 && OpenModal == null) StartRun();
        if (phase != Phase.Playing || awaitingRelease || isExhausted) return;
        pendingPull = pull;
    }

    /// A quick, mostly sideways flick moves you one column. Each finger can swipe once per touch.
    void DetectSwipe(Touch touch)
    {
        if (phase != Phase.Playing || swipedTouches.Contains(touch.touchId)) return;
        if (Time.realtimeSinceStartupAsDouble - touch.startTime > Tuning.SwipeMaxTime) return;
        Vector2 d = touch.screenPosition - touch.startScreenPosition;
        if (Mathf.Abs(d.x) < Tuning.SwipeDistance * Screen.width || Mathf.Abs(d.x) < 2 * Mathf.Abs(d.y)) return;
        swipedTouches.Add(touch.touchId);
        ShiftColumn(d.x > 0 ? 1 : -1);
    }

    void ShiftColumn(int direction)
    {
        int target = Mathf.Clamp(column + direction, 0, Tuning.Columns - 1);
        if (target == column) return;
        if (ColumnBlockedAt(target))
        {
            Synth.Instance.Nope();
            Haptics.Slip();
            return;
        }
        column = target;
        Synth.Instance.Swoosh();
        Haptics.Tick(0.5f);
        OnSwiped();
    }

    // MARK: - Climbing

    void StartRun()
    {
        phase = Phase.Playing;
        ShowTitle(false);
        ShowZoneStamp(ZoneIndex(anchorY + 0.01f), collect: false);
    }

    void Release(bool slipped = false)
    {
        if (phase != Phase.Playing || stretch <= 0.02f || isSnapping) return;
        if (TutorialActive)
        {
            // Letting go while learning: you slide back down (safely) and the lesson starts over.
            SnapTo(0, toneStep, safe: true);
            TutorialLetGo();
            return;
        }
        // Let go above a rest ledge in this column? You land on it instead of the checkpoint.
        float floor = LedgeBelow(column, ClimberY);
        float target = floor > anchorY ? floor - anchorY : 0;
        if (stretch - target <= 0.02f) return;   // already standing on it: just rest
        var next = NextCheckpoint();
        bool highUp = next != null && stretch > (next.Y - anchorY) * Tuning.SafeFallShare;
        SnapTo(target, slipped ? 0 : toneStep);
        if (!slipped) OnLetGo(stretch - target, highUp);
    }

    /// Fall back down toward `target` height above the anchor, until you land or grab on again.
    void SnapTo(float target, int fromStep, bool safe = false)
    {
        isSnapping = true;
        snapTo = target;
        fallSpeed = 0;
        safeLanding = safe;
        restBase = Mathf.Min(restBase, target);
        Synth.Instance.SnapBack(fromStep);
        Haptics.Snap();
    }

    /// The fastest landing you survive: the speed of an uncaught fall from halfway up a zone.
    static float LethalFallSpeed => Mathf.Sqrt(2 * Tuning.FallGravity * Tuning.ZoneLength * Tuning.SafeFallShare);

    void UpdateFalling(float dt)
    {
        bool holding = (lastTouchCount > 0 || autopilot && autopilotHolding) && !isExhausted && !awaitingRelease;
        if (holding)
        {
            // Grabbed on again: you slide to a stop (tired hands brake less).
            fallSpeed -= Tuning.GripFriction * Mathf.Lerp(0.6f, 1f, stamina) * dt;
            if (fallSpeed <= 0)
            {
                fallSpeed = 0;
                isSnapping = false;
                if (autopilot) Debug.Log($"[fall] caught at {stretch:F1} m above the checkpoint");
                Haptics.Tick(0.6f);
                return;
            }
        }
        else
        {
            fallSpeed += Tuning.FallGravity * FallGravityFactor * dt;
        }
        stretch -= fallSpeed * dt;
        if (stretch > snapTo) return;

        // Landed on the checkpoint or a ledge.
        stretch = snapTo;
        isSnapping = false;
        if (autopilot) Debug.Log($"[fall] landed at {fallSpeed:F1} m/s (lethal above {LethalFallSpeed:F1})");
        if (!safeLanding && fallSpeed > LethalFallSpeed)
        {
            Crash();
            return;
        }
        climber.Land();
        if (fallSpeed > LethalFallSpeed * 0.5f)
        {
            Synth.Instance.Thud();
            Haptics.Land();
        }
    }

    void UpdateClimb(float dt)
    {
        float previousY = ClimberY;
        if (isSnapping)
        {
            UpdateFalling(dt);
            if (phase != Phase.Playing) return;
            BumpOffRocks(previousY, anchorY + stretch);
        }
        else
        {
            stretch = Mathf.Max(0, stretch + pendingPull * Grip);
        }
        ApplyObstacles(dt, previousY);
        if (perched) restBase = stretch;
        else if (!isSnapping) restBase = Mathf.Min(restBase, stretch);

        var next = NextCheckpoint();
        if (next != null && anchorY + stretch >= next.Y)
        {
            Latch(next);
            if (phase != Phase.Playing) return;
        }

        if (phase == Phase.Playing) UpdateStamina(dt);

        // Jitter when the band gets tight or you're running out of stamina.
        float shake = 0;
        if (!isSnapping && Strain > 0.7f) shake = (Strain - 0.7f) * 0.2f;
        if (phase == Phase.Playing && stamina < 0.25f && !isSnapping) shake = Mathf.Max(shake, 0.04f);

        float targetX = ColumnX(column);
        climberX = Mathf.Lerp(climberX, targetX, Mathf.Min(1, dt * 18));
        float y = anchorY + stretch;
        climber.transform.position = new Vector3(climberX + Random.Range(-1f, 1f) * shake, y, -0.55f);
        // Facing you on the title screen, turning to the wall to climb; leaning into a sideways move.
        climberFacing = Mathf.MoveTowards(climberFacing, phase == Phase.Title ? 0 : 180, dt * 900f);
        climber.transform.rotation = Quaternion.Euler(0, 0, (climberX - targetX) * 14f) * Quaternion.Euler(0, climberFacing, 0);
        climber.SetClimbPhase(y * 4f);

        if (phase != Phase.Playing) return;
        runBest = Mathf.Max(runBest, y);
        PlayClimbTone(y);
    }

    /// Rising arcade tone: a note every half meter, pitched by how close the next checkpoint is.
    void PlayClimbTone(float y)
    {
        if (y >= lastBlipY + Tuning.BlipDistance)
        {
            var next = NextCheckpoint();
            if (next != null)
            {
                float progress = (y - anchorY) / (next.Y - anchorY);
                toneStep = Mathf.Min(Tuning.ToneSteps, (int)(progress * (Tuning.ToneSteps + 1)));
            }
            Synth.Instance.ClimbBlip(toneStep, Tuning.ToneSteps);
            Haptics.Tick(0.35f + 0.65f * toneStep / Tuning.ToneSteps);
            lastBlipY = y;
        }
        else if (y < lastBlipY)
        {
            lastBlipY = y;
        }
    }

    void Latch(Checkpoint checkpoint)
    {
        anchorY = checkpoint.Y;
        stretch = restBase = 0;
        isSnapping = false;
        awaitingRelease = lastTouchCount > 0;
        toneStep = 0;
        climber.Land();
        StartCoroutine(Pop(checkpoint.Node));
        Progress.Save();
        if (checkpoint.IsLaunchPad)
        {
            if (EffectAt(checkpoint.Y - 1) == PlanetEffect.Boss)
            {
                // Made it through the meteor storm.
                Progress.Coins += Tuning.BossReward;
                Progress.BossesBeaten++;
                runCoins += Tuning.BossReward;
                RefreshShop();
                Debris.Burst(climber.transform.position + Vector3.up, 0xFFD54F, 24, 4f);
                ShowBanner("BOSS BEATEN!", $"+{Tuning.BossReward} COINS");
            }
            StartCoroutine(LaunchSequence(checkpoint));
            return;
        }
        Synth.Instance.ZoneFanfare();
        Haptics.Success();
        ShowZoneStamp(checkpoint.Zone, collect: true);
    }

    IEnumerator Pop(Transform node)
    {
        for (float t = 0; t < 0.18f; t += Time.deltaTime)
        {
            float k = t < 0.06f ? t / 0.06f : 1 - (t - 0.06f) / 0.12f;
            node.localScale = Vector3.one * (1 + 0.15f * k);
            yield return null;
        }
        node.localScale = Vector3.one;
    }

    // MARK: - Small animation helpers

    /// A little jump along an arc.
    IEnumerator Hop(Transform target, Vector3 from, Vector3 to, float duration)
    {
        var control = (from + to) / 2 + Vector3.up * 1.5f;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float u = t / duration;
            target.position = Vector3.Lerp(Vector3.Lerp(from, control, u), Vector3.Lerp(control, to, u), u);
            yield return null;
        }
        target.position = to;
    }

    /// Moves a transform vertically with smooth easing.
    IEnumerator MoveY(Transform target, float toY, float duration, bool easeIn = true, bool easeOut = true)
    {
        float fromY = target.position.y;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float u = t / duration;
            float e = easeIn && easeOut ? u * u * (3 - 2 * u) : easeIn ? u * u : 1 - (1 - u) * (1 - u);
            var p = target.position;
            target.position = new Vector3(p.x, Mathf.LerpUnclamped(fromY, toY, e), p.z);
            yield return null;
        }
        var end = target.position;
        target.position = new Vector3(end.x, toY, end.z);
    }

    // MARK: - Camera and sky

    void UpdateCamera(float dt)
    {
        float focusY = phase switch
        {
            Phase.Falling => Mathf.Max(fallFloorY, ClimberY),
            Phase.Launching => launchRocket != null ? launchRocket.transform.position.y : ClimberY,
            Phase.Reviving => taxiRocket != null ? taxiRocket.transform.position.y : ClimberY,
            Phase.GameOver => cameraFocusY - Tuning.ViewHeight * (0.5f - Tuning.ClimberScreenFraction),
            _ => anchorY + stretch,
        };
        focusY += Tuning.ViewHeight * (0.5f - Tuning.ClimberScreenFraction);
        cameraFocusY = focusY;

        var offset = Vector3.zero;
        if (shakeTime > 0)
        {
            shakeTime -= dt;
            offset = (Vector3)Random.insideUnitCircle * shakeStrength * Mathf.Clamp01(shakeTime / 0.2f);
        }
        var target = new Vector3(0, focusY, 0) + offset;
        var rotation = Quaternion.Euler(Tuning.CameraPitch, -Tuning.CameraYaw, 0);
        cam.transform.SetPositionAndRotation(target - rotation * Vector3.forward * Tuning.CameraDistance, rotation);
        cam.backgroundColor = SkyColor(focusY);
        weather.CenterY = focusY;
        weather.Kind = WeatherAt(focusY);
    }

    void ShakeCamera(float strength)
    {
        if (!Progress.ShakeOn) return;   // the screen shake setting
        shakeTime = 0.2f;
        shakeStrength = strength;
    }

    WeatherKind WeatherAt(float y)
    {
        float local = Mathf.Max(0, y) - PlanetBase(PlanetIndex(y));
        return local >= PlanetClimb ? WeatherKind.None : ThemeFor(ZoneIndex(y)).Weather;
    }

    Color SkyColor(float y)
    {
        int planet = PlanetIndex(y);
        float local = Mathf.Max(0, y) - PlanetBase(planet);
        int last = (planet + 1) * ZonesPerPlanet - 1;
        if (local >= PlanetClimb)
        {
            // Flying between planets: space fades into the next planet's sky as you come in to land.
            float t = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(PlanetClimb + Tuning.RocketFlightMeters * 0.35f, PlanetSpan - 2, local));
            return Color.Lerp(Voxel.Hex(ThemeFor(last).Sky), Voxel.Hex(ThemeFor(last + 1).Sky), t);
        }
        int zone = ZoneIndex(y);
        if (zone == last) return Voxel.Hex(ThemeFor(zone).Sky);
        float frac = (local - zone % ZonesPerPlanet * Tuning.ZoneLength) / Tuning.ZoneLength;
        float blend = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.75f, 1f, frac));
        return Color.Lerp(Voxel.Hex(ThemeFor(zone).Sky), Voxel.Hex(ThemeFor(zone + 1).Sky), blend);
    }

    // MARK: - Test harness

    void ParseTestArguments()
    {
        var args = System.Environment.GetCommandLineArgs();
        string shotDir = null;
        int shots = 1;
        float interval = 2f, delay = 2f;
        var invariant = System.Globalization.CultureInfo.InvariantCulture;
        var number = System.Globalization.NumberStyles.Float;
        for (int i = 0; i < args.Length; i++)
        {
            string next = i + 1 < args.Length ? args[i + 1] : null;
            switch (args[i])
            {
                case "-scrollyAutopilot": autopilot = true; break;
                case "-scrollyGod": god = true; break;
                case "-scrollyStartZone": int.TryParse(next, out debugStartZone); break;
                case "-scrollyEffect":
                    if (System.Enum.TryParse(next, out PlanetEffect effect)) forcedEffect = effect;
                    break;
                case "-scrollyShotDir": shotDir = next; break;
                case "-scrollyShots": int.TryParse(next, out shots); break;
                case "-scrollyShotInterval": float.TryParse(next, number, invariant, out interval); break;
                case "-scrollyShotDelay": float.TryParse(next, number, invariant, out delay); break;
                case "-scrollyAllStamps":
                    foreach (var page in BookPages)
                        for (int z = 0; z < ZonesPerPlanet; z++)
                            if (Progress.StampCount(StampId(page, z)) == 0) Progress.CollectStamp(StampId(page, z));
                    break;
                case "-scrollyBook": ShowStampBook(true); break;
                case "-scrollySettings": ShowSettings(true); break;
                case "-scrollyCloset": ShowCloset(true); break;
                case "-scrollyClosetTab":
                    ShowCloset(true);
                    if (System.Enum.TryParse(next, true, out CosmeticKind tab)) ShowClosetTab(tab);
                    break;
                case "-scrollyInspect":
                    // The closet's charm tab, zoomed in and spinning, to check the dangling.
                    ShowCloset(true);
                    ShowClosetTab(CosmeticKind.Charm);
                    previewSpunAt = Time.unscaledTime + 30;
                    inspectZoom = 2.2f;
                    inspectYawVelocity = 160;
                    break;
                case "-scrollyCatalog":
                    new GameObject("Catalog").AddComponent<CoroutineHost>().StartCoroutine(RenderCatalog(next));
                    break;
                case "-scrollyWearTag": Progress.Tag = next; break;
                case "-scrollyInspectFull":
                    // The closet in full screen inspect mode.
                    ShowCloset(true);
                    ToggleInspect();
                    break;
                case "-scrollyTagTour":
                    // The tag tab, picking each tag in turn, to check how they look.
                    ShowCloset(true);
                    ShowClosetTab(CosmeticKind.Tag);
                    new GameObject("TagTest").AddComponent<CoroutineHost>().StartCoroutine(TagTourForTest());
                    break;
                case "-scrollyGift": ShowGift(true); break;
                case "-scrollyGiftTaps":
                    // Opens the gift and taps it three times by itself, to check the animation.
                    Progress.GiftDay = "";
                    ShowGift(true);
                    new GameObject("GiftTest").AddComponent<CoroutineHost>().StartCoroutine(TapGiftForTest());
                    break;
                case "-scrollyBookTurn":
                    // Turns to the next pages by itself, to check the page animation.
                    new GameObject("BookTest").AddComponent<CoroutineHost>().StartCoroutine(TurnPagesForTest());
                    break;
            }
        }
        // Runs on its own object so world resets (which stop the game's coroutines) don't interrupt it.
        if (shotDir != null)
            new GameObject("TestHarness").AddComponent<CoroutineHost>().StartCoroutine(TakeScreenshots(shotDir, shots, interval, delay));
    }

    IEnumerator TagTourForTest()
    {
        foreach (var tag in Cosmetics.Tags)
        {
            SelectClosetItem(tag);
            yield return new WaitForSecondsRealtime(1f);
        }
    }

    IEnumerator TapGiftForTest()
    {
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSecondsRealtime(0.9f);
            TapGift();
        }
    }

    IEnumerator TurnPagesForTest()
    {
        for (int page = 1; page < BookPages.Length; page++)
        {
            yield return new WaitForSecondsRealtime(1.2f);
            TurnBookPage(page);
        }
    }

    IEnumerator TakeScreenshots(string dir, int count, float interval, float delay)
    {
        System.IO.Directory.CreateDirectory(dir);
        yield return new WaitForSecondsRealtime(delay);
        for (int i = 1; i <= count; i++)
        {
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir, $"shot_{i}.png"));
            yield return new WaitForSecondsRealtime(interval);
        }
        Application.Quit();
        yield return new WaitForSecondsRealtime(1f);
        System.Diagnostics.Process.GetCurrentProcess().Kill();   // in case Quit is ignored
    }

    void RunAutopilot()
    {
        autopilotFrame++;
        if (phase == Phase.Title) StartRun();
        if (phase == Phase.GameOver && Time.time - gameOverAt > 4f) ResetWorld();
        if (phase != Phase.Playing || isExhausted) return;
        // Now and then let go, and grab again after a moment, to show off (and test) falling.
        if (isSnapping)
        {
            if (!autopilotHolding && Time.time >= autopilotRegrabAt) autopilotHolding = true;
            return;
        }
        autopilotHolding = true;
        if (!god && autopilotFrame % 360 == 0 && stretch > 4f)
        {
            autopilotHolding = false;
            autopilotRegrabAt = Time.time + Random.Range(0.15f, 1.1f);
            Release();
            return;
        }
        if (god) stamina = 1;
        // Hand-over-hand strokes with a short pause.
        if (autopilotFrame % 40 < 32) pendingPull += 0.45f;
        if (autopilotFrame % 70 == 0) ShiftColumn(Random.value < 0.5f ? -1 : 1);
    }
}
