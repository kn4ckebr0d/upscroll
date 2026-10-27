import SpriteKit
import UIKit

/// The safe ledge at the start of each zone. The last one on a planet is a launch pad with a rocket.
private final class Checkpoint {
    let y: CGFloat
    let zone: Int          // the zone it leads into
    let node: SKNode
    let rocket: RocketNode?

    var isLaunchPad: Bool { rocket != nil }

    init(y: CGFloat, zone: Int, node: SKNode, rocket: RocketNode?) {
        self.y = y
        self.zone = zone
        self.node = node
        self.rocket = rocket
    }
}

/// Where and when a finger touched down, to tell sideways swipes from climbing drags.
private struct TouchStart {
    let point: CGPoint
    let time: TimeInterval
    var swiped = false
}

/// What a row of the world is made of. Each planet is a climbable wall of zones,
/// then empty space the rocket flies through, then the next planet's ground.
private enum RowKind {
    case earthGround(top: Bool)
    case wall(zone: Int)
    case void
    case planetGround(planet: Int, top: Bool)
}

final class GameScene: SKScene {

    private enum Phase { case title, playing, launching, falling, gameOver, reviving }

    private let ppm = Tuning.pointsPerMeter
    private var tile: CGFloat { Tuning.pointsPerMeter }
    private var zoneHeight: CGFloat { Tuning.zoneLength * Tuning.pointsPerMeter }
    private var zonesPerPlanet: Int { ZoneTheme.zonesPerPlanet }
    private var planetClimb: CGFloat { zoneHeight * CGFloat(zonesPerPlanet) }
    private var planetSpan: CGFloat { planetClimb + Tuning.rocketFlightMeters * Tuning.pointsPerMeter }

    // MARK: Nodes

    private let cam = SKCameraNode()
    private let worldLayer = SKNode()
    private let climber = Climber()
    private let tether = SKShapeNode()

    private let heightLabel = ShadowLabel(text: "0 m", fontSize: 46)
    private let bestLabel = ShadowLabel(text: "BEST 0 m", fontSize: 15)
    private let titleNode = SKNode()
    private let menuNode = SKNode()
    private let rulesLabel = SKLabelNode(fontNamed: "AvenirNext-DemiBold")
    private var soundButton: SKShapeNode!
    private var lifeButton: SKShapeNode!
    private var test3DButton: SKShapeNode!
    private var continueButton: SKShapeNode!
    private let livesLabel = ShadowLabel(fontSize: 18)
    private let gameOverNode = SKNode()
    private let gameOverScore = ShadowLabel(fontSize: 34)
    private let gameOverBest = ShadowLabel(text: "NEW BEST!", fontSize: 20, color: UIColor(hex: 0xFFD54F))
    private let zoneBanner = SKNode()
    private let zoneBannerTop = ShadowLabel(fontSize: 14)
    private let zoneBannerName = ShadowLabel(fontSize: 30)
    private let tipLabel = ShadowLabel(fontSize: 15, color: UIColor(hex: 0xFFD54F))
    private let nextLabel = ShadowLabel(fontSize: 12)
    private let countdownLabel = ShadowLabel(fontSize: 72)
    private let coinLabel = ShadowLabel(text: "0", fontSize: 20)
    private let gameOverCoins = ShadowLabel(fontSize: 18, color: UIColor(hex: 0xFFD54F))
    private let staminaBar = SKNode()
    private let staminaFill = SKSpriteNode(color: .green, size: CGSize(width: 40, height: 4))

    // MARK: State

    private var isSetUp = false
    private var phase: Phase = .title
    private var safeTop: CGFloat = 50

    /// Every finger on screen and its last y position (in view points).
    private var activeTouches: [UITouch: CGFloat] = [:]
    private var touchStarts: [UITouch: TouchStart] = [:]
    private var menuTouch: UITouch?

    private var column = Tuning.columns / 2
    private var climberX: CGFloat = 0
    private var coins: [SKNode] = []
    private var runCoins = 0
    private var shownSwipeTip = false

    private var anchorY: CGFloat = 0      // height of the last checkpoint
    private var rawPull: CGFloat = 0      // finger travel since the anchor
    private var stretch: CGFloat = 0      // rubber-banded height above the anchor
    private var isSnapping = false
    private var snapFrom: CGFloat = 0
    private var snapT: TimeInterval = 0
    private var snapDuration: TimeInterval = 0.3

    private var checkpoints: [Checkpoint] = []
    private var lanes: [Lane] = []
    private var rows: [Int: SKNode] = [:]
    private var plannedUpTo: CGFloat = 0

    private var lastUpdate: TimeInterval = 0
    private var now: TimeInterval = 0
    private var toneStep = 0
    private var lastBlipY: CGFloat = 0

    private var stamina: CGFloat = 1
    private var isExhausted = false
    /// After reaching a checkpoint you stay planted until every finger lifts,
    /// so the leftover drag can't launch you into the next zone.
    private var awaitingRelease = false
    private var nextWarningBeep: TimeInterval = 0
    private var shownSlipTip = false

    private var launchRocket: RocketNode?

    private var runBest: CGFloat = 0
    private var bestAtStart = 0
    private var celebratedBest = false
    private var fallSpin: CGFloat = 0
    private var fallTime: TimeInterval = 0
    private var fallStartY: CGFloat = 0
    private var fallPeakY: CGFloat = 0
    private var fallFloorY: CGFloat = 0
    private var fallDuration: TimeInterval = 1
    private var fallLanded = false
    private var fallZone = 0
    private var gameOverAt: TimeInterval = 0
    private var dizzy: SKNode?
    private var continuesUsed = 0
    private var invulnerableUntil: TimeInterval = 0
    /// The rocket parked on the current planet's ground. Continuing flies it back up to your checkpoint.
    private var groundRocket: RocketNode?
    private var taxiRocket: RocketNode?
    private var shownSnapTip = false

    /// The rubber band's asymptote. Each zone uses a bigger share of it, so later zones feel heavier.
    private var stretchLimit: CGFloat {
        let zone = CGFloat(zoneIndex(at: anchorY + 1))
        let reach = min(Tuning.zoneReachMax, Tuning.zoneReachStart + Tuning.zoneReachPerZone * zone)
        return zoneHeight / reach
    }

    /// 0 = relaxed, 1 = as tight as the band normally gets.
    private var strain: CGFloat {
        min(1, stretch / (stretchLimit * Tuning.fullStrainRatio))
    }


    // Debug autopilot for testing without hands, e.g. launch with environment
    // SCROLLY_AUTOPILOT=climb|god|continue|demo and SCROLLY_START_ZONE=3.
    // "god" ignores mobs and stamina. "demo" plays endless human-like runs for showing the game off.
    #if DEBUG
    private let autopilot = ProcessInfo.processInfo.environment["SCROLLY_AUTOPILOT"]
    private let debugStartZone = Int(ProcessInfo.processInfo.environment["SCROLLY_START_ZONE"] ?? "") ?? 0
    private var isInvincible: Bool { autopilot == "god" }

    private var demoWaitUntil: TimeInterval = 0
    private var demoStrokeEnd: TimeInterval = 0
    private var demoPauseEnd: TimeInterval = 0
    private var demoNextSwipe: TimeInterval = 0
    private var demoCarefulness = 0.85
    private var demoIgnoredLanes = Set<ObjectIdentifier>()
    private var demoLanesDecided = Set<ObjectIdentifier>()
    private var demoPhase: Phase = .title
    private var demoWaitingSince: TimeInterval?
    /// SCROLLY_DEMO_RUNS=1 plays that many runs, then stays on the game-over screen.
    private let demoRunLimit = Int(ProcessInfo.processInfo.environment["SCROLLY_DEMO_RUNS"] ?? "")
    private var demoRunsDone = 0

    private func runAutopilot() {
        guard autopilot != nil else { return }
        if autopilot == "demo" {
            runDemo()
            return
        }
        if phase == .title { startRun() }
        if phase == .gameOver && autopilot == "continue" && now - gameOverAt > 1 && !continueButton.isHidden {
            continueFromCheckpoint()
        }
        guard phase == .playing, !isSnapping else { return }
        rawPull += 22
        if isInvincible { stamina = 1 }
        if Int.random(in: 0..<45) == 0 { shiftColumn(Bool.random() ? 1 : -1) }
    }

    /// Plays like a person: hand-over-hand strokes, usually waits for mobs, chases coins,
    /// sometimes buys a life or continues, and loops forever.
    private func runDemo() {
        if phase != demoPhase {
            demoPhase = phase
            demoWaitUntil = now + (phase == .title ? 2.5 : 2.8)
            if phase == .title {
                demoCarefulness = Double.random(in: 0.7...0.97)
                demoIgnoredLanes.removeAll()
                demoLanesDecided.removeAll()
            }
        }
        switch phase {
        case .title:
            guard now >= demoWaitUntil else { return }
            if Progress.extraLives == 0 && Progress.coins >= Tuning.extraLifePrice && Bool.random() {
                buyExtraLife()
                demoWaitUntil = now + 1.2
                return
            }
            startRun()
        case .gameOver:
            guard now >= demoWaitUntil else { return }
            if !continueButton.isHidden && Progress.coins >= continuePrice && continuesUsed < 2 && Double.random(in: 0..<1) < 0.6 {
                continueFromCheckpoint()
            } else {
                demoRunsDone += 1
                if let limit = demoRunLimit, demoRunsDone >= limit {
                    demoWaitUntil = .infinity   // done: leave the final score on screen
                    return
                }
                resetWorld()
            }
        case .playing:
            demoPlay()
        default:
            break
        }
    }

    private func demoPlay() {
        guard !isSnapping, !isExhausted else { return }

        // Chase the nearest coin a little way above.
        if now >= demoNextSwipe,
           let coin = coins.filter({ $0.position.y > climber.position.y + 20 && $0.position.y < climber.position.y + 220 })
                           .min(by: { $0.position.y < $1.position.y }) {
            let target = Int(((coin.position.x - columnX(0)) / Tuning.columnSpacing).rounded())
            if target != column {
                shiftColumn(target > column ? 1 : -1)
                demoNextSwipe = now + Double.random(in: 0.18...0.35)
            }
        }

        // Wait for mobs that are about to cross the lane just above (unless this run is feeling reckless).
        // Lanes already overlapping the body get pushed through instead.
        let head = climber.position.y + 44
        for lane in lanes where lane.y > head + 8 && lane.y < head + 80 {
            let id = ObjectIdentifier(lane)
            if !demoLanesDecided.contains(id) {
                demoLanesDecided.insert(id)
                if Double.random(in: 0..<1) > demoCarefulness { demoIgnoredLanes.insert(id) }
            }
            guard !demoIgnoredLanes.contains(id) else { continue }
            for mob in lane.mobs {
                // Only wait for mobs that would reach us in about the time it takes to climb past.
                let ahead = (climber.position.x - mob.position.x) * (lane.speed > 0 ? 1 : -1)
                guard ahead > -45 && ahead < abs(lane.speed) * 0.45 + 45 else { continue }
                let since = demoWaitingSince ?? now
                demoWaitingSince = since
                if now - since < 3.5 { return }   // patience runs out: go for it
            }
        }
        demoWaitingSince = nil

        // Hand-over-hand strokes with a short pause to switch fingers.
        if now >= demoStrokeEnd {
            if now < demoPauseEnd { return }
            if Int.random(in: 0..<160) == 0 && stretch > ppm {
                releaseAll()   // oops, let go
                return
            }
            demoStrokeEnd = now + Double.random(in: 0.3...0.55)
            demoPauseEnd = demoStrokeEnd + Double.random(in: 0.08...0.2)
        }
        rawPull += CGFloat.random(in: 14...24)
    }
    #else
    private let isInvincible = false
    #endif

    // MARK: - World geometry

    private func planetIndex(at y: CGFloat) -> Int {
        Int(floor(max(0, y) / planetSpan))
    }

    private func planetBase(_ planet: Int) -> CGFloat {
        CGFloat(planet) * planetSpan
    }

    /// Global zone number, counting across planets. Flight space counts as the planet's last zone.
    private func zoneIndex(at y: CGFloat) -> Int {
        let planet = planetIndex(at: y)
        let local = max(0, y) - planetBase(planet)
        return planet * zonesPerPlanet + min(zonesPerPlanet - 1, Int(local / zoneHeight))
    }

    private func rowKind(_ r: Int) -> RowKind {
        if r < 0 { return .earthGround(top: r == -1) }
        let y = CGFloat(r) * tile
        let planet = planetIndex(at: y)
        let local = y - planetBase(planet)
        if local < planetClimb { return .wall(zone: zoneIndex(at: y)) }
        let groundDepth = CGFloat(Tuning.planetGroundRows) * tile
        if local >= planetSpan - groundDepth {
            return .planetGround(planet: planet + 1, top: local >= planetSpan - tile)
        }
        return .void
    }

    private func columnX(_ c: Int) -> CGFloat {
        size.width / 2 + CGFloat(c - Tuning.columns / 2) * Tuning.columnSpacing
    }

    private func wallHalfWidth(forRow r: Int) -> Int {
        // Deterministic so rows rebuilt after scrolling away look the same shape.
        let v = sin(Double(r / 3) * 12.9898 + 78.233) * 43758.5453
        return v - floor(v) < 0.5 ? 2 : 3
    }

    // MARK: - Setup

    override func didMove(to view: SKView) {
        guard !isSetUp else { return }
        isSetUp = true
        safeTop = max(view.safeAreaInsets.top, 20)
        backgroundColor = ZoneTheme.forZone(0).sky

        addChild(worldLayer)
        addChild(cam)
        camera = cam

        tether.zPosition = 8
        tether.lineWidth = 3
        tether.lineCap = .round
        addChild(tether)
        climber.zPosition = 10
        addChild(climber)

        staminaBar.zPosition = 30
        staminaBar.alpha = 0
        staminaBar.addChild(SKSpriteNode(color: UIColor(white: 0, alpha: 0.5), size: CGSize(width: 44, height: 8)))
        staminaFill.anchorPoint = CGPoint(x: 0, y: 0.5)
        staminaFill.position = CGPoint(x: -20, y: 0)
        staminaBar.addChild(staminaFill)
        addChild(staminaBar)

        buildHUD()
        resetWorld()
    }

    private func buildHUD() {
        let h = size.height
        let top = h / 2 - safeTop

        heightLabel.position = CGPoint(x: 0, y: top - 34)
        bestLabel.position = CGPoint(x: 0, y: top - 66)
        heightLabel.zPosition = 100
        bestLabel.zPosition = 100
        cam.addChild(heightLabel)
        cam.addChild(bestLabel)
        nextLabel.position = CGPoint(x: 0, y: top - 86)
        nextLabel.zPosition = 100
        nextLabel.alpha = 0.85
        cam.addChild(nextLabel)

        // Total coins, top right.
        let coinIcon = Coin.makeIcon()
        coinIcon.position = CGPoint(x: size.width / 2 - 26, y: top - 18)
        coinIcon.zPosition = 100
        cam.addChild(coinIcon)
        coinLabel.setAlignment(.right)
        coinLabel.position = CGPoint(x: size.width / 2 - 40, y: top - 18)
        coinLabel.zPosition = 100
        coinLabel.text = "\(Progress.coins)"
        cam.addChild(coinLabel)

        // Extra lives, top left.
        livesLabel.setAlignment(.left)
        livesLabel.position = CGPoint(x: -size.width / 2 + 18, y: top - 18)
        livesLabel.zPosition = 100
        cam.addChild(livesLabel)

        // Zone banner
        zoneBanner.zPosition = 100
        zoneBanner.alpha = 0
        zoneBanner.position = CGPoint(x: 0, y: top - 130)
        zoneBannerTop.position = CGPoint(x: 0, y: 20)
        zoneBanner.addChild(zoneBannerTop)
        zoneBanner.addChild(zoneBannerName)
        cam.addChild(zoneBanner)

        countdownLabel.zPosition = 100
        countdownLabel.alpha = 0
        countdownLabel.position = CGPoint(x: 0, y: h * 0.08)
        cam.addChild(countdownLabel)

        tipLabel.zPosition = 100
        tipLabel.alpha = 0
        tipLabel.position = CGPoint(x: 0, y: -h / 2 + 90)
        cam.addChild(tipLabel)

        // Title
        titleNode.zPosition = 90
        let title = ShadowLabel(text: "SCROLLY ROAD", fontSize: 40)
        title.position = CGPoint(x: 0, y: h * 0.24)
        titleNode.addChild(title)
        let alpha = ShadowLabel(text: "ALPHA", fontSize: 14, color: UIColor(hex: 0xFFD54F))
        alpha.position = CGPoint(x: 0, y: h * 0.24 - 32)
        titleNode.addChild(alpha)
        let hint = ShadowLabel(text: "DRAG DOWN TO CLIMB", fontSize: 18)
        hint.position = CGPoint(x: 0, y: h * 0.13)
        titleNode.addChild(hint)
        let hint2 = ShadowLabel(text: "Keep pulling with your other hand!", fontSize: 14, font: "AvenirNext-DemiBold")
        hint2.position = CGPoint(x: 0, y: h * 0.13 - 24)
        titleNode.addChild(hint2)
        let hand = SKLabelNode(text: "👆")
        hand.fontSize = 44
        hand.position = CGPoint(x: 0, y: h * 0.06)
        titleNode.addChild(hand)
        hand.run(.repeatForever(.sequence([
            .group([.moveBy(x: 0, y: -70, duration: 0.8), .sequence([.wait(forDuration: 0.5), .fadeOut(withDuration: 0.3)])]),
            .moveBy(x: 0, y: 70, duration: 0),
            .fadeIn(withDuration: 0.15),
            .wait(forDuration: 0.25),
        ])))
        cam.addChild(titleNode)

        // Menu (title + game over)
        menuNode.zPosition = 95
        rulesLabel.text = "Reach the next zone to save your progress.\nLet go or run out of stamina and you slide back."
        rulesLabel.fontSize = 13
        rulesLabel.fontColor = .white
        rulesLabel.numberOfLines = 0
        rulesLabel.preferredMaxLayoutWidth = size.width - 40
        rulesLabel.horizontalAlignmentMode = .center
        rulesLabel.verticalAlignmentMode = .center
        rulesLabel.position = CGPoint(x: 0, y: -h * 0.255)
        menuNode.addChild(rulesLabel)
        lifeButton = makeButton(lifeTitle)
        lifeButton.position = CGPoint(x: 0, y: -h * 0.32)
        menuNode.addChild(lifeButton)
        soundButton = makeButton(soundTitle)
        soundButton.position = CGPoint(x: 0, y: -h * 0.32 - 46)
        menuNode.addChild(soundButton)
        test3DButton = makeButton("3D LOOK TEST")
        test3DButton.position = CGPoint(x: 0, y: -h * 0.32 - 92)
        test3DButton.strokeColor = UIColor(hex: 0x7FE7FF)
        menuNode.addChild(test3DButton)
        cam.addChild(menuNode)

        // Game over
        gameOverNode.zPosition = 92
        let dim = SKSpriteNode(color: UIColor(white: 0, alpha: 0.45), size: CGSize(width: size.width * 2, height: h * 2))
        gameOverNode.addChild(dim)
        let ouch = ShadowLabel(text: "OUCH!", fontSize: 48)
        ouch.position = CGPoint(x: 0, y: h * 0.2)
        gameOverNode.addChild(ouch)
        gameOverScore.position = CGPoint(x: 0, y: h * 0.11)
        gameOverNode.addChild(gameOverScore)
        gameOverBest.position = CGPoint(x: 0, y: h * 0.05)
        gameOverNode.addChild(gameOverBest)
        gameOverCoins.position = CGPoint(x: 0, y: 0)
        gameOverNode.addChild(gameOverCoins)
        continueButton = makeButton("CONTINUE")
        continueButton.position = CGPoint(x: 0, y: -h * 0.07)
        continueButton.strokeColor = UIColor(hex: 0xFFD54F)
        gameOverNode.addChild(continueButton)
        let retry = ShadowLabel(text: "TAP ANYWHERE TO START OVER", fontSize: 15)
        retry.position = CGPoint(x: 0, y: -h * 0.14)
        retry.run(.repeatForever(.sequence([.fadeAlpha(to: 0.35, duration: 0.5), .fadeAlpha(to: 1, duration: 0.5)])))
        gameOverNode.addChild(retry)
        gameOverNode.alpha = 0
        cam.addChild(gameOverNode)
    }

    private var soundTitle: String { Synth.shared.isEnabled ? "SOUND: ON" : "SOUND: OFF" }

    private func makeButton(_ text: String) -> SKShapeNode {
        let button = SKShapeNode(rectOf: CGSize(width: 290, height: 38), cornerRadius: 12)
        button.fillColor = UIColor(white: 0, alpha: 0.35)
        button.strokeColor = UIColor(white: 1, alpha: 0.85)
        button.lineWidth = 2
        let label = SKLabelNode(fontNamed: "AvenirNext-Heavy")
        label.name = "label"
        label.fontSize = 15
        label.fontColor = .white
        label.verticalAlignmentMode = .center
        label.text = text
        button.addChild(label)
        return button
    }

    private func setButton(_ button: SKShapeNode, text: String) {
        (button.childNode(withName: "label") as? SKLabelNode)?.text = text
    }

    // MARK: - World building

    private func resetWorld() {
        removeAllActions()
        cam.removeAllActions()
        worldLayer.removeAllChildren()
        checkpoints.removeAll()
        lanes.removeAll()
        coins.removeAll()
        rows.removeAll()
        activeTouches.removeAll()
        touchStarts.removeAll()
        menuTouch = nil
        launchRocket = nil

        plannedUpTo = 0
        anchorY = 0
        rawPull = 0
        stretch = 0
        isSnapping = false
        toneStep = 0
        lastBlipY = 0
        stamina = 1
        isExhausted = false
        awaitingRelease = false
        staminaBar.alpha = 0
        runBest = 0
        runCoins = 0
        continuesUsed = 0
        invulnerableUntil = 0
        dizzy = nil
        column = Tuning.columns / 2
        climberX = columnX(column)
        celebratedBest = false
        bestAtStart = Progress.best

        #if DEBUG
        if debugStartZone > 0 {
            let planet = debugStartZone / zonesPerPlanet
            anchorY = planetBase(planet) + CGFloat(debugStartZone % zonesPerPlanet) * zoneHeight
            plannedUpTo = anchorY
        }
        #endif

        climber.resetPose()
        climber.position = CGPoint(x: size.width / 2, y: anchorY)
        tether.path = nil

        taxiRocket = nil
        placeGroundRocket(at: planetBase(planetIndex(at: anchorY + 1)))

        phase = .title
        updateCamera()
        generateContent()

        setButton(soundButton, text: soundTitle)
        refreshShop()
        countdownLabel.removeAllActions()
        countdownLabel.alpha = 0
        gameOverNode.removeAllActions()
        gameOverNode.alpha = 0
        titleNode.removeAllActions()
        titleNode.alpha = 1
        menuNode.removeAllActions()
        menuNode.alpha = 1
        heightLabel.text = "0 m"
        updateHUD()
    }

    /// Builds rows around the camera in both directions (so falls and snap-backs never show gaps)
    /// and plans checkpoints and mobs ahead.
    private func generateContent() {
        let low = Int(floor((cam.position.y - size.height / 2) / tile)) - 3
        let high = Int(ceil((cam.position.y + size.height / 2) / tile)) + 3
        for r in max(-12, low)...high where rows[r] == nil {
            buildRow(r)
        }
        let stale = rows.keys.filter { $0 < low - 2 || $0 > high + 2 }
        for r in stale {
            rows[r]?.removeFromParent()
            rows[r] = nil
        }

        planContent(upTo: CGFloat(high) * tile + size.height)

        // Keep mobs down to the anchor so sliding back still shows them.
        let laneBottom = min(cam.position.y - size.height / 2, anchorY - size.height * 0.5) - tile * 3
        lanes.removeAll { lane in
            guard lane.y < laneBottom else { return false }
            lane.removeNodes()
            return true
        }
        coins.removeAll { coin in
            guard coin.position.y < laneBottom else { return false }
            coin.removeFromParent()
            return true
        }
    }

    private func buildRow(_ r: Int) {
        let row = SKNode()
        row.position = CGPoint(x: 0, y: CGFloat(r) * tile + tile / 2)
        switch rowKind(r) {
        case .earthGround(let top):
            fillGround(row, top: top,
                       grass: [0x6CBF4B, 0x62B443].map { UIColor(hex: $0) },
                       dirt: [0x7A5236, 0x6E4A30, 0x845A3C].map { UIColor(hex: $0) })
        case .planetGround(let planet, let top):
            let theme = ZoneTheme.forZone(planet * zonesPerPlanet)
            fillGround(row, top: top, grass: theme.accents,
                       dirt: theme.rocks.map { $0.adjusted(brightness: -0.12) })
        case .void:
            addDecoration(.stars, to: row, halfWidth: 0)
        case .wall(let zone):
            buildWall(row, r: r, zone: zone)
        }
        worldLayer.addChild(row)
        rows[r] = row
    }

    private func fillGround(_ row: SKNode, top: Bool, grass: [UIColor], dirt: [UIColor]) {
        let columns = Int(size.width / tile) + 2
        for c in 0..<columns {
            let block = SKSpriteNode(color: (top ? grass : dirt).randomElement()!, size: CGSize(width: tile, height: tile))
            block.position = CGPoint(x: CGFloat(c) * tile + tile / 2, y: 0)
            row.addChild(block)
        }
        guard top else { return }
        for _ in 0..<4 {
            let x = CGFloat.random(in: 20...(size.width - 20))
            guard abs(x - size.width / 2) > 40 else { continue }
            let tuft = Voxel.block(grass[0].adjusted(brightness: -0.12), CGSize(width: 8, height: 8), anchor: CGPoint(x: 0.5, y: 0))
            tuft.position = CGPoint(x: x, y: tile / 2)
            row.addChild(tuft)
        }
    }

    private func buildWall(_ row: SKNode, r: Int, zone: Int) {
        let theme = ZoneTheme.forZone(zone)
        let halfWidth = wallHalfWidth(forRow: r)
        let cx = size.width / 2
        for i in -halfWidth...halfWidth {
            var color = theme.rocks.randomElement()!
            if abs(i) == halfWidth {
                color = theme.edge
            } else if Double.random(in: 0..<1) < theme.accentChance {
                color = theme.accents.randomElement()!
            }
            let block = SKSpriteNode(color: color, size: CGSize(width: tile, height: tile))
            block.position = CGPoint(x: cx + CGFloat(i) * tile, y: 0)
            row.addChild(block)
        }
        addDecoration(theme.decoration, to: row, halfWidth: halfWidth)

        if r > 0 && r % 10 == 0 {
            let label = SKLabelNode(fontNamed: "AvenirNext-Bold")
            label.text = "\(r) m"
            label.fontSize = 12
            label.fontColor = UIColor(white: 1, alpha: 0.75)
            label.horizontalAlignmentMode = .left
            label.verticalAlignmentMode = .center
            label.position = CGPoint(x: cx + CGFloat(halfWidth) * tile + tile / 2 + 4, y: 0)
            row.addChild(label)
        }
    }

    private func addDecoration(_ decoration: ZoneTheme.Decoration, to row: SKNode, halfWidth: Int) {
        let cx = size.width / 2
        let edgeX = CGFloat(halfWidth) * tile + tile / 2
        let side: CGFloat = Bool.random() ? 1 : -1
        switch decoration {
        case .bushes:
            guard Double.random(in: 0..<1) < 0.14 else { return }
            let bush = Voxel.block(UIColor(hex: 0x5DB043), CGSize(width: 16, height: 12))
            bush.position = CGPoint(x: cx + side * (edgeX + 6), y: CGFloat.random(in: -10...10))
            row.addChild(bush)
        case .snow:
            guard Double.random(in: 0..<1) < 0.2 else { return }
            let snow = Voxel.block(.white, CGSize(width: 22, height: 8))
            snow.position = CGPoint(x: cx + side * (edgeX - 4), y: tile / 2 - 2)
            row.addChild(snow)
        case .clouds:
            guard Double.random(in: 0..<1) < 0.08 else { return }
            let cloud = SKNode()
            cloud.zPosition = -1
            cloud.alpha = 0.85
            for (dx, dy, w) in [(CGFloat(0), CGFloat(0), CGFloat(40)), (18, 10, 28), (-16, 6, 24)] {
                let puff = SKSpriteNode(color: .white, size: CGSize(width: w, height: 18))
                puff.position = CGPoint(x: dx, y: dy)
                cloud.addChild(puff)
            }
            cloud.position = CGPoint(x: side > 0 ? CGFloat.random(in: (cx + edgeX)...size.width) : CGFloat.random(in: 0...(cx - edgeX)), y: 0)
            row.addChild(cloud)
        case .stars:
            for _ in 0..<Int.random(in: 0...2) {
                let star = SKSpriteNode(color: .white, size: CGSize(width: 3, height: 3))
                star.alpha = CGFloat.random(in: 0.3...1)
                star.zPosition = -1
                star.position = CGPoint(x: CGFloat.random(in: 0...size.width), y: CGFloat.random(in: -20...20))
                row.addChild(star)
            }
        }
    }

    /// Places a checkpoint at the start of every zone (a launch pad at the top of each planet),
    /// with mob lanes in between.
    private func planContent(upTo target: CGFloat) {
        while plannedUpTo < target {
            let planet = planetIndex(at: plannedUpTo + 1)
            let base = planetBase(planet)
            let k = Int(((plannedUpTo - base) / zoneHeight).rounded()) + 1
            let y = base + CGFloat(k) * zoneHeight
            let isLaunchPad = k == zonesPerPlanet
            addLanes(from: plannedUpTo, to: y)
            addCoins(from: plannedUpTo, to: y)
            addCheckpoint(at: y, zone: isLaunchPad ? (planet + 1) * zonesPerPlanet : planet * zonesPerPlanet + k,
                          isLaunchPad: isLaunchPad)
            plannedUpTo = isLaunchPad ? planetBase(planet + 1) : y
        }
    }

    private func addCheckpoint(at y: CGFloat, zone: Int, isLaunchPad: Bool) {
        let node = SKNode()
        node.position = CGPoint(x: size.width / 2, y: y)
        node.zPosition = 5
        var rocket: RocketNode?
        let label = SKLabelNode(fontNamed: "AvenirNext-Heavy")
        label.fontSize = 11
        label.fontColor = .white
        label.horizontalAlignmentMode = .left

        if isLaunchPad {
            node.addChild(Voxel.block(UIColor(hex: 0x8E9AA6), CGSize(width: 280, height: 12), anchor: CGPoint(x: 0.5, y: 1)))
            for i in 0..<10 {
                let stripe = SKSpriteNode(color: i % 2 == 0 ? UIColor(hex: 0xFFD54F) : UIColor(hex: 0x263238),
                                          size: CGSize(width: 10, height: 3))
                stripe.position = CGPoint(x: -135 + CGFloat(i) * 10 + 5, y: -2)
                node.addChild(stripe)
            }
            let ship = RocketNode()
            ship.position = CGPoint(x: 118, y: 0)
            node.addChild(ship)
            rocket = ship
            label.text = "LAUNCH PAD"
            label.position = CGPoint(x: -135, y: 6)
        } else {
            let theme = ZoneTheme.forZone(zone)
            node.addChild(Voxel.block(theme.ledge, CGSize(width: 220, height: 10), anchor: CGPoint(x: 0.5, y: 1)))
            let pole = SKSpriteNode(color: UIColor(white: 0.95, alpha: 1), size: CGSize(width: 3, height: 50))
            pole.anchorPoint = CGPoint(x: 0.5, y: 0)
            pole.position = CGPoint(x: -104, y: 0)
            node.addChild(pole)
            let flag = Voxel.block(UIColor(hex: 0xFFD54F), CGSize(width: 24, height: 14), anchor: CGPoint(x: 0, y: 1))
            flag.position = CGPoint(x: -102, y: 50)
            node.addChild(flag)
            flag.run(.repeatForever(.sequence([.scaleX(to: 0.8, duration: 0.3), .scaleX(to: 1, duration: 0.3)])))
            label.text = theme.name
            label.position = CGPoint(x: -96, y: -22)
        }
        node.addChild(label)
        worldLayer.addChild(node)
        checkpoints.append(Checkpoint(y: y, zone: zone, node: node, rocket: rocket))
    }

    private func addLanes(from lower: CGFloat, to upper: CGFloat) {
        // Keep the start of a zone clear so its first danger is always visible (longer on a new planet),
        // and keep lanes off the checkpoint ledge at the top.
        let isPlanetStart = abs(lower - planetBase(planetIndex(at: lower + 1))) < 1
        let buffer = isPlanetStart ? Tuning.safeStartMeters : Tuning.zoneSafeMeters
        let minY = lower + buffer * ppm
        let maxY = upper - 1.0 * ppm
        guard maxY >= minY else { return }
        var y = minY + CGFloat.random(in: 0...min(2 * ppm, maxY - minY))
        while y <= maxY {
            let chance = min(0.85, 0.5 + 0.1 * Double(zoneIndex(at: y)))
            if Double.random(in: 0..<1) < chance { makeLane(at: y) }
            y += CGFloat.random(in: 4.5...7.5) * ppm
        }
    }

    private func makeLane(at y: CGFloat) {
        let zone = zoneIndex(at: y)
        let kind = ZoneTheme.forZone(zone).mob
        let direction: CGFloat = Bool.random() ? 1 : -1
        let speed = kind.baseSpeed * CGFloat.random(in: 0.85...1.2) * (1 + 0.08 * CGFloat(zone)) * direction
        let lane = Lane(y: y, speed: speed, kind: kind)

        // A faint stripe so you can read where the danger row is.
        let stripe = SKSpriteNode(color: UIColor(white: 0, alpha: 0.12), size: CGSize(width: size.width, height: 26))
        stripe.position = CGPoint(x: size.width / 2, y: y)
        stripe.zPosition = 1
        worldLayer.addChild(stripe)
        lane.decor.append(stripe)

        let count = Double.random(in: 0..<1) < 0.3 + 0.1 * Double(zone) ? 2 : 1
        let span = size.width + 2 * Lane.margin
        let start = CGFloat.random(in: -Lane.margin...(size.width + Lane.margin))
        for k in 0..<count {
            let mob = kind.makeNode()
            mob.xScale = direction
            var x = start + CGFloat(k) * span / CGFloat(count)
            if x > size.width + Lane.margin { x -= span }
            mob.position = CGPoint(x: x, y: y)
            mob.zPosition = 20
            worldLayer.addChild(mob)
            lane.mobs.append(mob)
        }
        lanes.append(lane)
    }

    /// Coin patterns between checkpoints: straight lines, diagonals and zigzags across the columns.
    private func addCoins(from lower: CGFloat, to upper: CGFloat) {
        var y = lower + 3 * ppm
        let top = upper - 1.5 * ppm
        let last = Tuning.columns - 1
        while y < top {
            let pattern = Int.random(in: 0..<3)
            let length = Int.random(in: 3...6)
            var c = Int.random(in: 0...last)
            let direction = c < Tuning.columns / 2 ? 1 : -1
            for i in 0..<length {
                let cy = y + CGFloat(i) * Tuning.coinSpacing * ppm
                guard cy < top else { break }
                spawnCoin(column: c, y: cy)
                switch pattern {
                case 1: c = min(last, max(0, c + direction))                            // diagonal
                case 2: if i % 2 == 1 { c = min(last, max(0, c + (i % 4 == 1 ? 1 : -1))) } // zigzag
                default: break                                                          // straight line
                }
            }
            y += CGFloat(length) * Tuning.coinSpacing * ppm + CGFloat.random(in: Tuning.coinGap) * ppm
        }
    }

    private func spawnCoin(column c: Int, y: CGFloat) {
        let coin = Coin.makeNode()
        coin.position = CGPoint(x: columnX(c), y: y)
        coin.zPosition = 7
        worldLayer.addChild(coin)
        coins.append(coin)
    }

    private func collectCoins() {
        let body = CGRect(x: climber.position.x - 14, y: climber.position.y, width: 28, height: 50)
        coins.removeAll { coin in
            guard body.contains(coin.position) else { return false }
            runCoins += 1
            Progress.coins += 1
            coinLabel.text = "\(Progress.coins)"
            coinLabel.removeAllActions()
            coinLabel.run(.sequence([.scale(to: 1.3, duration: 0.06), .scale(to: 1, duration: 0.12)]))
            Synth.shared.coin()
            Haptics.tick(0.6)
            coin.removeAllActions()
            coin.run(.sequence([.group([.moveBy(x: 0, y: 30, duration: 0.25), .fadeOut(withDuration: 0.25),
                                        .scale(to: 1.6, duration: 0.25)]), .removeFromParent()]))
            return true
        }
    }

    private func shiftColumn(_ direction: Int) {
        let target = min(Tuning.columns - 1, max(0, column + direction))
        guard target != column else { return }
        column = target
        Synth.shared.swoosh()
        Haptics.tick(0.5)
    }

    // MARK: - Touch handling

    override func touchesBegan(_ touches: Set<UITouch>, with event: UIEvent?) {
        guard let view else { return }
        for touch in touches {
            if (phase == .title || phase == .gameOver), menuTouch == nil, button(at: touch) != nil {
                menuTouch = touch
                continue
            }
            let point = touch.location(in: view)
            activeTouches[touch] = point.y
            touchStarts[touch] = TouchStart(point: point, time: now)
        }
        // Grab again mid snap-back, just like catching a scroll view.
        if phase == .playing && isSnapping && !isExhausted && !awaitingRelease && !activeTouches.isEmpty {
            isSnapping = false
            rawPull = inverseRubberBand(stretch, dimension: stretchLimit)
        }
    }

    override func touchesMoved(_ touches: Set<UITouch>, with event: UIEvent?) {
        guard let view else { return }
        // Whichever finger moved the most drives the climb, so hand-offs feel seamless.
        var pull: CGFloat = 0
        for touch in touches {
            guard let lastY = activeTouches[touch] else { continue }
            let point = touch.location(in: view)
            let dy = point.y - lastY
            if abs(dy) > abs(pull) { pull = dy }
            activeTouches[touch] = point.y
            detectSwipe(touch, at: point)
        }
        guard pull != 0 else { return }
        if phase == .title && pull > 0 { startRun() }
        guard phase == .playing, !isExhausted, !awaitingRelease else { return }
        rawPull = max(0, rawPull + pull)
    }

    /// A quick, mostly sideways flick moves you one column. Each finger can swipe once per touch.
    private func detectSwipe(_ touch: UITouch, at point: CGPoint) {
        guard phase == .playing, var start = touchStarts[touch], !start.swiped,
              now - start.time < Tuning.swipeMaxTime else { return }
        let dx = point.x - start.point.x
        let dy = point.y - start.point.y
        guard abs(dx) > Tuning.swipeDistance, abs(dx) > 2 * abs(dy) else { return }
        start.swiped = true
        touchStarts[touch] = start
        shiftColumn(dx > 0 ? 1 : -1)
    }

    override func touchesEnded(_ touches: Set<UITouch>, with event: UIEvent?) {
        for touch in touches {
            if touch === menuTouch {
                menuTouch = nil
                if let button = button(at: touch) { handleButton(button) }
                continue
            }
            activeTouches.removeValue(forKey: touch)
            touchStarts.removeValue(forKey: touch)
            if phase == .gameOver && now - gameOverAt > 0.5 {
                Synth.shared.click()
                resetWorld()
                return
            }
        }
        if activeTouches.isEmpty { releaseAll() }
    }

    override func touchesCancelled(_ touches: Set<UITouch>, with event: UIEvent?) {
        for touch in touches {
            if touch === menuTouch { menuTouch = nil }
            activeTouches.removeValue(forKey: touch)
            touchStarts.removeValue(forKey: touch)
        }
        if activeTouches.isEmpty { releaseAll() }
    }

    private func pruneDeadTouches() {
        let dead = activeTouches.keys.filter { $0.phase == .ended || $0.phase == .cancelled }
        guard !dead.isEmpty else { return }
        dead.forEach {
            activeTouches.removeValue(forKey: $0)
            touchStarts.removeValue(forKey: $0)
        }
        if activeTouches.isEmpty { releaseAll() }
    }

    private func button(at touch: UITouch) -> SKShapeNode? {
        var candidates: [(button: SKShapeNode, parent: SKNode)] = []
        if menuNode.alpha > 0.5 {
            candidates += [(lifeButton, menuNode), (soundButton, menuNode), (test3DButton, menuNode)]
        }
        if phase == .gameOver && gameOverNode.alpha > 0.5 && !continueButton.isHidden {
            candidates.append((continueButton, gameOverNode))
        }
        return candidates.first { $0.button.contains(touch.location(in: $0.parent)) }?.button
    }

    private func handleButton(_ button: SKShapeNode) {
        button.run(.sequence([.scale(to: 0.92, duration: 0.05), .scale(to: 1, duration: 0.1)]))
        // Buying and continuing make their own success or "nope" sound; only plain toggles click.
        if button === lifeButton {
            buyExtraLife()
        } else if button === continueButton {
            continueFromCheckpoint()
        } else if button === soundButton {
            Synth.shared.isEnabled.toggle()
            setButton(soundButton, text: soundTitle)
            Synth.shared.click()
        } else if button === test3DButton {
            Synth.shared.click()
            NotificationCenter.default.post(name: .open3DTest, object: nil)
        }
    }

    // MARK: - Game flow

    private func startRun() {
        phase = .playing
        titleNode.run(.fadeOut(withDuration: 0.2))
        menuNode.run(.fadeOut(withDuration: 0.2))
        showZoneBanner(zone: 0)
        if !shownSwipeTip {
            shownSwipeTip = true
            run(.sequence([.wait(forDuration: 2.5), .run { [weak self] in
                self?.showTip("SWIPE LEFT or RIGHT with your other finger\nto move and grab coins!")
            }]))
        }
    }

    private func releaseAll(slipped: Bool = false) {
        guard phase == .playing, stretch > 0.5, !isSnapping else {
            rawPull = 0
            return
        }
        isSnapping = true
        snapFrom = stretch
        snapT = 0
        snapDuration = 0.28 + min(0.5, Double(stretch / size.height) * 0.15)
        rawPull = 0
        Synth.shared.snapBack(fromStep: toneStep)
        Haptics.snap()
        if !slipped && !shownSnapTip && stretch > ppm * 1.5 {
            shownSnapTip = true
            showTip("TIP: put your other finger down\nbefore you lift the first one!")
        }
    }

    /// Out of stamina: hands slip, you snap back and can't grab until you've rested a bit.
    private func slip() {
        isExhausted = true
        Synth.shared.slip()
        Haptics.slip()
        if !shownSlipTip {
            shownSlipTip = true
            showTip("OUT OF STAMINA!\nRest on a checkpoint to recover.")
        }
        releaseAll(slipped: true)
    }

    private func updateStamina(dt: CGFloat) {
        let hanging = stretch > 1 && !isSnapping
        if hanging {
            let tension = stretch / stretchLimit
            stamina -= (Tuning.staminaBaseDrain + Tuning.staminaTensionDrain * tension) * dt
            if stamina <= 0 {
                stamina = 0
                slip()
            } else if stamina < 0.25 && now >= nextWarningBeep {
                Synth.shared.staminaWarning()
                Haptics.heartbeat()
                nextWarningBeep = now + 0.6
            }
        } else if !isSnapping {
            stamina = min(1, stamina + Tuning.staminaRegen * dt)
            if isExhausted && stamina >= Tuning.staminaRecoverAt {
                isExhausted = false
                rawPull = 0
            }
        }
    }

    private func updateStaminaBar() {
        staminaBar.position = CGPoint(x: climberX, y: climber.position.y + 66)
        staminaFill.xScale = max(0.001, stamina)
        staminaFill.color = stamina > 0.5 ? UIColor(hex: 0x6CD94F)
            : stamina > 0.25 ? UIColor(hex: 0xFFC83D) : UIColor(hex: 0xFF4D4D)
        let visible = phase == .playing && (stamina < 0.995 || isExhausted)
        var target: CGFloat = visible ? 1 : 0
        if visible && (isExhausted || stamina < 0.25) {
            target = sin(CGFloat(now) * 18) > 0 ? 1 : 0.35   // blink when low
        }
        staminaBar.alpha += (target - staminaBar.alpha) * 0.3
    }

    private func latch(_ checkpoint: Checkpoint) {
        anchorY = checkpoint.y
        rawPull = 0
        stretch = 0
        isSnapping = false
        awaitingRelease = !activeTouches.isEmpty
        climber.position.y = anchorY
        climber.land()
        checkpoint.node.run(.sequence([.scale(to: 1.15, duration: 0.06), .scale(to: 1, duration: 0.12)]))
        if checkpoint.isLaunchPad {
            startLaunch(from: checkpoint)
            return
        }
        Synth.shared.zoneFanfare()
        Haptics.zone()
        showZoneBanner(zone: checkpoint.zone)
    }

    // MARK: - Shop: extra lives and continues

    private var lifeTitle: String {
        let lives = Progress.extraLives
        let max = Tuning.maxExtraLives
        return lives >= max ? "❤️ \(lives)/\(max) · FULL" : "❤️ \(lives)/\(max) · BUY LIFE · \(Tuning.extraLifePrice) COINS"
    }

    private var continuePrice: Int { Tuning.continueBasePrice << continuesUsed }

    private func refreshShop() {
        setButton(lifeButton, text: lifeTitle)
        livesLabel.text = Progress.extraLives > 0 ? "❤️ \(Progress.extraLives)" : ""
        coinLabel.text = "\(Progress.coins)"
    }

    private func buyExtraLife() {
        guard Progress.extraLives < Tuning.maxExtraLives else {
            Synth.shared.nope()
            return
        }
        guard Progress.spend(Tuning.extraLifePrice) else {
            Synth.shared.nope()
            showTip("Not enough coins yet!\nCollect \(Tuning.extraLifePrice - Progress.coins) more.")
            return
        }
        Progress.extraLives += 1
        Synth.shared.purchase()
        Haptics.zone()
        refreshShop()
    }

    /// A mob hit you but you had an extra life: slide back to the checkpoint instead of falling.
    private func useExtraLife() {
        Progress.extraLives -= 1
        refreshShop()
        Synth.shared.hit()
        Haptics.hit()
        shakeCamera(1)
        showBanner(top: "EXTRA LIFE USED", name: "SAVED!")
        if stretch > 0.5 {
            isSnapping = true
            snapFrom = stretch
            snapT = 0
            snapDuration = 0.28 + min(0.5, Double(stretch / size.height) * 0.15)
        }
        rawPull = 0
        awaitingRelease = !activeTouches.isEmpty
        makeInvulnerable()
    }

    private func makeInvulnerable() {
        invulnerableUntil = now + Tuning.invulnerableTime
        climber.run(.repeat(.sequence([.fadeAlpha(to: 0.3, duration: 0.1), .fadeAlpha(to: 1, duration: 0.1)]),
                            count: Int(Tuning.invulnerableTime / 0.2)), withKey: "blink")
    }

    private func placeGroundRocket(at y: CGFloat) {
        let rocket = RocketNode()
        rocket.position = CGPoint(x: size.width / 2 + 118, y: y)
        rocket.zPosition = 6
        worldLayer.addChild(rocket)
        groundRocket = rocket
    }

    private func hopAction(from start: CGPoint, to end: CGPoint, duration: TimeInterval = 0.4) -> SKAction {
        let path = CGMutablePath()
        path.move(to: start)
        path.addQuadCurve(to: end, control: CGPoint(x: (start.x + end.x) / 2, y: max(start.y, end.y) + 60))
        return .follow(path, asOffset: false, orientToPath: false, duration: duration)
    }

    /// Pay coins, hop into the parked rocket and fly back up to the last checkpoint.
    private func continueFromCheckpoint() {
        guard Progress.spend(continuePrice) else {
            Synth.shared.nope()
            showTip("Not enough coins to continue.")
            return
        }
        continuesUsed += 1
        refreshShop()
        gameOverNode.removeAllActions()
        gameOverNode.run(.fadeOut(withDuration: 0.2))
        menuNode.removeAllActions()
        menuNode.run(.fadeOut(withDuration: 0.2))
        dizzy?.removeFromParent()
        dizzy = nil

        if groundRocket == nil { placeGroundRocket(at: fallFloorY) }
        guard let rocket = groundRocket else { return }
        groundRocket = nil
        taxiRocket = rocket
        phase = .reviving
        Haptics.zone()

        // Get up, hop in.
        climber.resetPose()
        climber.position.y = fallFloorY
        let door = CGPoint(x: rocket.position.x, y: rocket.position.y + 30)
        climber.run(.sequence([
            .wait(forDuration: 0.25),
            .group([hopAction(from: climber.position, to: door),
                    .sequence([.wait(forDuration: 0.25), .fadeOut(withDuration: 0.15)])]),
        ]))

        let meters = Double((anchorY - rocket.position.y) / ppm)
        let flight = min(2.4, 0.8 + meters * 0.01)
        let up = SKAction.moveTo(y: anchorY + 3 * ppm, duration: flight)
        up.timingMode = .easeInEaseOut
        let down = SKAction.moveTo(y: anchorY, duration: 0.6)
        down.timingMode = .easeOut
        rocket.run(.sequence([
            .wait(forDuration: 0.9),
            .run { [weak self] in
                rocket.setThrust(true)
                rocket.rumble()
                Synth.shared.liftoff(duration: Float(flight + 0.6))
                Haptics.land()
                self?.spawnDust(at: rocket.position, color: UIColor(white: 0.95, alpha: 1))
            },
            up,
            .run { rocket.setThrust(true, strength: 0.6) },
            down,
            .run { [weak self] in self?.dropOff(from: rocket) },
        ]))
    }

    /// The taxi rocket set down at the checkpoint: hop out, resume the climb, and the rocket flies off.
    private func dropOff(from rocket: RocketNode) {
        rocket.setThrust(false)
        rocket.stopRumble()
        Synth.shared.thud()
        Haptics.land()
        column = Tuning.columns / 2
        climberX = columnX(column)
        let door = CGPoint(x: rocket.position.x, y: anchorY + 30)
        climber.position = door
        climber.run(.sequence([
            .wait(forDuration: 0.2),
            .group([.fadeIn(withDuration: 0.1), hopAction(from: door, to: CGPoint(x: climberX, y: anchorY))]),
            .run { [weak self] in self?.resumeAfterContinue() },
        ]))
        rocket.run(.sequence([
            .wait(forDuration: 0.9),
            .run { rocket.setThrust(true) },
            .group([.moveBy(x: 0, y: size.height * 1.2, duration: 1.2), .sequence([.wait(forDuration: 0.8), .fadeOut(withDuration: 0.4)])]),
            .removeFromParent(),
        ]))
        // A fresh rocket waits on the ground for next time.
        placeGroundRocket(at: fallFloorY)
    }

    private func resumeAfterContinue() {
        taxiRocket = nil
        climber.position = CGPoint(x: climberX, y: anchorY)
        rawPull = 0
        stretch = 0
        isSnapping = false
        stamina = 1
        isExhausted = false
        awaitingRelease = !activeTouches.isEmpty
        toneStep = 0
        lastBlipY = anchorY
        phase = .playing
        climber.land()
        makeInvulnerable()
        showBanner(top: "CONTINUE", name: "BACK IN!")
    }

    // MARK: - Rocket launch

    private func startLaunch(from checkpoint: Checkpoint) {
        guard let rocket = checkpoint.rocket else { return }
        phase = .launching
        launchRocket = rocket
        rocket.move(toParent: worldLayer)
        rocket.zPosition = 6   // stay in front of rows built later during the flight
        tether.path = nil
        staminaBar.alpha = 0
        let planet = planetIndex(at: checkpoint.y - 1)
        let landingY = planetBase(planet + 1)
        Synth.shared.zoneFanfare()
        Haptics.zone()
        showBanner(top: "PLANET \(planet + 1) COMPLETE", name: "TO THE ROCKET!")

        // Hop into the rocket.
        let door = CGPoint(x: rocket.position.x, y: rocket.position.y + 30)
        let hop = CGMutablePath()
        hop.move(to: climber.position)
        hop.addQuadCurve(to: door, control: CGPoint(x: (climber.position.x + door.x) / 2, y: door.y + 60))
        climber.run(.sequence([
            .wait(forDuration: 0.4),
            .group([.follow(hop, asOffset: false, orientToPath: false, duration: 0.4),
                    .sequence([.wait(forDuration: 0.25), .fadeOut(withDuration: 0.15)])]),
        ]))

        let beat = SKAction.wait(forDuration: 0.6)
        run(.sequence([
            .wait(forDuration: 1.1),
            .run { [weak self] in self?.countdown("3"); rocket.rumble() },
            beat,
            .run { [weak self] in self?.countdown("2") },
            beat,
            .run { [weak self] in self?.countdown("1") },
            beat,
            .run { [weak self] in self?.liftOff(rocket, landingY: landingY, planet: planet + 1) },
        ]), withKey: "launch")
    }

    private func countdown(_ text: String, final: Bool = false) {
        countdownLabel.text = text
        countdownLabel.removeAllActions()
        countdownLabel.alpha = 1
        countdownLabel.setScale(1.6)
        countdownLabel.run(.group([.scale(to: 1, duration: 0.2),
                                   .sequence([.wait(forDuration: 0.4), .fadeOut(withDuration: 0.15)])]))
        Synth.shared.countdownBeep(final: final)
        Haptics.rumble(final ? 1 : 0.5)
    }

    private func liftOff(_ rocket: RocketNode, landingY: CGFloat, planet: Int) {
        countdown("GO!", final: true)
        rocket.setThrust(true)
        Synth.shared.liftoff(duration: 3.4)
        Haptics.land()
        spawnDust(at: rocket.position, color: UIColor(white: 0.95, alpha: 1))
        run(.repeat(.sequence([.run { Haptics.rumble(0.45) }, .wait(forDuration: 0.1)]), count: 26), withKey: "rumble")

        let up = SKAction.moveTo(y: landingY + 6 * ppm, duration: 2.6)
        up.timingMode = .easeInEaseOut
        let down = SKAction.moveTo(y: landingY, duration: 1.0)
        down.timingMode = .easeOut
        rocket.run(.sequence([
            up,
            .run { rocket.setThrust(true, strength: 0.6) },
            down,
            .run { [weak self] in self?.landRocket(rocket, at: landingY, planet: planet) },
        ]))
    }

    private func landRocket(_ rocket: RocketNode, at landingY: CGFloat, planet: Int) {
        groundRocket?.removeFromParent()
        groundRocket = rocket
        rocket.setThrust(false)
        rocket.stopRumble()
        removeAction(forKey: "rumble")
        Synth.shared.thud()
        Haptics.land()
        spawnDust(at: CGPoint(x: rocket.position.x, y: landingY), color: UIColor(white: 0.95, alpha: 1))

        // Hop out onto the new planet.
        let door = CGPoint(x: rocket.position.x, y: landingY + 30)
        let spot = CGPoint(x: size.width / 2, y: landingY)
        climber.position = door
        let hop = CGMutablePath()
        hop.move(to: door)
        hop.addQuadCurve(to: spot, control: CGPoint(x: (door.x + spot.x) / 2, y: door.y + 60))
        climber.run(.sequence([
            .wait(forDuration: 0.3),
            .group([.fadeIn(withDuration: 0.1), .follow(hop, asOffset: false, orientToPath: false, duration: 0.4)]),
            .run { [weak self] in self?.arrive(onPlanet: planet, at: landingY) },
        ]))
    }

    private func arrive(onPlanet planet: Int, at y: CGFloat) {
        launchRocket = nil
        anchorY = y
        rawPull = 0
        stretch = 0
        isSnapping = false
        stamina = 1
        isExhausted = false
        awaitingRelease = !activeTouches.isEmpty
        toneStep = 0
        lastBlipY = y
        runBest = max(runBest, y)
        column = Tuning.columns / 2
        climberX = columnX(column)
        climber.position = CGPoint(x: climberX, y: y)
        climber.land()
        phase = .playing
        Synth.shared.zoneFanfare()
        Haptics.zone()
        showZoneBanner(zone: planet * zonesPerPlanet)
    }

    // MARK: - Getting hit and falling

    private func knockOff(by lane: Lane) {
        phase = .falling
        isSnapping = false
        fallSpin = lane.speed > 0 ? -1 : 1
        fallTime = 0
        fallLanded = false
        fallStartY = climber.position.y
        fallPeakY = fallStartY + 1.5 * ppm
        fallFloorY = planetBase(planetIndex(at: anchorY + 1))
        let meters = Double((fallPeakY - fallFloorY) / ppm)
        fallDuration = min(2.2, max(0.6, 0.5 + meters * 0.012))
        fallZone = zoneIndex(at: fallStartY)
        tether.path = nil
        staminaBar.alpha = 0
        Synth.shared.hit()
        Synth.shared.fallWhistle(duration: Float(0.3 + fallDuration), delay: Float(Tuning.hitStop))
        Haptics.hit()
        climber.run(.sequence([.fadeAlpha(to: 0.3, duration: 0.03), .fadeAlpha(to: 1, duration: 0.03),
                               .fadeAlpha(to: 0.3, duration: 0.03), .fadeAlpha(to: 1, duration: 0.03)]))
        shakeCamera(1)
    }

    /// Hit-stop, a little pop up, then the long tumble all the way down to the planet's surface.
    private func updateFall(dt: TimeInterval) {
        fallTime += dt
        if fallLanded {
            if fallTime > 0.9 { showGameOver() }
            return
        }
        let t = fallTime - Tuning.hitStop
        guard t > 0 else { return }
        let pop = 0.3
        if t < pop {
            let u = CGFloat(t / pop)
            climber.position.y = fallStartY + (fallPeakY - fallStartY) * sin(u * .pi / 2)
        } else {
            let u = CGFloat(min(1, (t - pop) / fallDuration))
            climber.position.y = fallPeakY + (fallFloorY + 13 - fallPeakY) * u * u
            if u >= 1 {
                landFall()
                return
            }
        }
        climber.position.x = climberX + fallSpin * CGFloat(min(1, t)) * 40
        climber.zRotation += fallSpin * 9 * CGFloat(dt)

        // A tick for every zone you tumble past.
        let zone = zoneIndex(at: climber.position.y)
        if zone < fallZone {
            fallZone = zone
            Haptics.tick(0.8)
            Synth.shared.climbBlip(step: 3, of: Tuning.toneSteps)
        }
    }

    private func landFall() {
        fallLanded = true
        fallTime = 0
        climber.position.y = fallFloorY + 13
        climber.position.x = min(size.width / 2 + 70, max(size.width / 2 - 110, climber.position.x))
        climber.zRotation = fallSpin > 0 ? .pi / 2 : -.pi / 2
        Synth.shared.thud()
        Haptics.land()
        shakeCamera(1.6)
        spawnDust(at: CGPoint(x: climber.position.x, y: fallFloorY), color: UIColor(hex: 0xD7C4A3))

        // Dizzy stars circling the head.
        let headX = climber.position.x + (fallSpin > 0 ? -37 : 37)
        let orbit = SKNode()
        orbit.yScale = 0.4
        orbit.position = CGPoint(x: headX, y: climber.position.y + 22)
        orbit.zPosition = 15
        let spinner = SKNode()
        orbit.addChild(spinner)
        for i in 0..<3 {
            let star = SKSpriteNode(color: UIColor(hex: 0xFFE14D), size: CGSize(width: 5, height: 5))
            let angle = CGFloat(i) * 2 * .pi / 3
            star.position = CGPoint(x: cos(angle) * 14, y: sin(angle) * 14)
            spinner.addChild(star)
        }
        spinner.run(.repeatForever(.rotate(byAngle: 2 * .pi, duration: 0.8)))
        worldLayer.addChild(orbit)
        dizzy = orbit
    }

    private func showGameOver() {
        phase = .gameOver
        gameOverAt = now
        let meters = Int(runBest / ppm)
        let isNewBest = meters > bestAtStart
        if isNewBest { Progress.best = meters }
        gameOverScore.text = "\(meters) m"
        gameOverBest.alpha = isNewBest ? 1 : 0
        gameOverCoins.text = runCoins == 1 ? "+1 COIN" : "+\(runCoins) COINS"
        continueButton.isHidden = anchorY <= 0
        setButton(continueButton, text: "CONTINUE FROM \(Int(anchorY / ppm)) m · \(continuePrice) COINS")
        continueButton.alpha = Progress.coins >= continuePrice ? 1 : 0.45
        gameOverNode.run(.fadeIn(withDuration: 0.25))
        menuNode.removeAllActions()
        menuNode.run(.fadeIn(withDuration: 0.25))
    }

    // MARK: - Effects

    private func shakeCamera(_ strength: CGFloat) {
        let s = strength
        cam.removeAction(forKey: "shake")
        cam.run(.sequence([
            .moveBy(x: 9 * s, y: -6 * s, duration: 0.04),
            .moveBy(x: -15 * s, y: 10 * s, duration: 0.05),
            .moveBy(x: 10 * s, y: -6 * s, duration: 0.05),
            .moveBy(x: -4 * s, y: 2 * s, duration: 0.04),
        ]), withKey: "shake")
    }

    private func spawnDust(at point: CGPoint, color: UIColor) {
        for _ in 0..<10 {
            let bit = SKSpriteNode(color: color, size: CGSize(width: 6, height: 6))
            bit.position = point
            bit.zPosition = 25
            worldLayer.addChild(bit)
            let move = SKAction.moveBy(x: CGFloat.random(in: -70...70), y: CGFloat.random(in: 15...60), duration: 0.45)
            move.timingMode = .easeOut
            bit.run(.sequence([.group([move, .fadeOut(withDuration: 0.45), .scale(to: 0.3, duration: 0.45)]),
                               .removeFromParent()]))
        }
    }

    private func showZoneBanner(zone: Int) {
        showBanner(top: ZoneTheme.label(forZone: zone), name: ZoneTheme.forZone(zone).name)
    }

    private func showBanner(top: String, name: String) {
        zoneBannerTop.text = top
        zoneBannerName.text = name
        zoneBanner.removeAllActions()
        zoneBanner.alpha = 0
        zoneBanner.setScale(0.6)
        zoneBanner.run(.sequence([
            .group([.fadeIn(withDuration: 0.15), .scale(to: 1, duration: 0.2)]),
            .wait(forDuration: 1.6),
            .fadeOut(withDuration: 0.4),
        ]))
    }

    private func showTip(_ text: String) {
        tipLabel.removeAllActions()
        tipLabel.children.compactMap { $0 as? SKLabelNode }.forEach {
            $0.numberOfLines = 0
            $0.preferredMaxLayoutWidth = size.width - 40
        }
        tipLabel.text = text
        tipLabel.alpha = 0
        tipLabel.run(.sequence([.fadeIn(withDuration: 0.2), .wait(forDuration: 3), .fadeOut(withDuration: 0.4)]))
    }

    // MARK: - Frame update

    override func update(_ currentTime: TimeInterval) {
        let dt = lastUpdate == 0 ? 1.0 / 60 : min(currentTime - lastUpdate, 1.0 / 20)
        lastUpdate = currentTime
        now = currentTime
        pruneDeadTouches()
        if activeTouches.isEmpty { awaitingRelease = false }

        #if DEBUG
        runAutopilot()
        #endif

        switch phase {
        case .title, .playing: updateClimb(dt: dt)
        case .falling: updateFall(dt: dt)
        case .launching, .reviving, .gameOver: break
        }
        updateLanes(dt: CGFloat(dt))
        if phase == .playing && !isInvincible { checkCollisions() }
        updateCamera()
        updateSky()
        generateContent()
        updateHUD()
    }

    private func updateClimb(dt: TimeInterval) {
        if isSnapping {
            snapT += dt
            let p = CGFloat(min(1, snapT / snapDuration))
            stretch = snapFrom * pow(1 - p, 3)
            if p >= 1 {
                isSnapping = false
                stretch = 0
            }
        } else {
            stretch = rubberBand(rawPull, dimension: stretchLimit)
        }
        climber.position.y = anchorY + stretch

        if phase == .playing { updateStamina(dt: CGFloat(dt)) }

        // Jitter when the band gets tight or you're running out of stamina.
        var shakeAmount: CGFloat = 0
        if !isSnapping && strain > 0.7 { shakeAmount = (strain - 0.7) * 8 }
        if phase == .playing && stamina < 0.25 && !isSnapping { shakeAmount = max(shakeAmount, 1.5) }
        let targetX = columnX(column)
        climberX += (targetX - climberX) * min(1, CGFloat(dt) * 18)
        climber.zRotation = (climberX - targetX) / 300   // lean into a sideways move
        climber.position.x = climberX + CGFloat.random(in: -1...1) * shakeAmount

        if let next = checkpoints.first(where: { $0.y > anchorY + 1 }), climber.position.y >= next.y {
            latch(next)
            if phase != .playing { return }
        }

        // Rising arcade tone: a note every bit you climb, pitched by how close the next checkpoint is.
        if climber.position.y >= lastBlipY + Tuning.blipDistance {
            if let next = checkpoints.first(where: { $0.y > anchorY + 1 }) {
                let progress = (climber.position.y - anchorY) / (next.y - anchorY)
                toneStep = min(Tuning.toneSteps, Int(progress * CGFloat(Tuning.toneSteps + 1)))
            }
            Synth.shared.climbBlip(step: toneStep, of: Tuning.toneSteps)
            Haptics.tick(0.35 + 0.65 * CGFloat(toneStep) / CGFloat(Tuning.toneSteps))
            climber.reach()
            lastBlipY = climber.position.y
        } else if climber.position.y < lastBlipY {
            lastBlipY = climber.position.y
        }

        if phase == .playing {
            runBest = max(runBest, climber.position.y)
            collectCoins()
        }
        updateStaminaBar()
        drawTether()
    }

    private func updateLanes(dt: CGFloat) {
        let span = size.width + 2 * Lane.margin
        for lane in lanes {
            for mob in lane.mobs {
                mob.position.x += lane.speed * dt
                if lane.speed > 0 && mob.position.x > size.width + Lane.margin { mob.position.x -= span }
                if lane.speed < 0 && mob.position.x < -Lane.margin { mob.position.x += span }
            }
        }
    }

    private func checkCollisions() {
        guard now >= invulnerableUntil else { return }
        let body = CGRect(x: climber.position.x - 11, y: climber.position.y + 4, width: 22, height: 40)
        for lane in lanes where abs(lane.y - body.midY) < 90 {
            let hit = lane.kind.hitSize
            for mob in lane.mobs {
                let rect = CGRect(x: mob.position.x - hit.width / 2, y: lane.y - hit.height / 2,
                                  width: hit.width, height: hit.height)
                if body.intersects(rect) {
                    if Progress.extraLives > 0 {
                        useExtraLife()
                    } else {
                        knockOff(by: lane)
                    }
                    return
                }
            }
        }
    }

    private func updateCamera() {
        guard !cam.hasActions() else { return }
        let focusY: CGFloat
        switch phase {
        case .title, .playing: focusY = anchorY + stretch
        case .launching: focusY = launchRocket?.position.y ?? climber.position.y
        case .falling: focusY = max(fallFloorY, climber.position.y)
        case .reviving: focusY = taxiRocket?.position.y ?? climber.position.y
        case .gameOver: return
        }
        cam.position = CGPoint(x: size.width / 2, y: focusY + size.height * (0.5 - Tuning.climberScreenFraction))
    }

    private func updateSky() {
        backgroundColor = skyColor(at: cam.position.y)
    }

    private func skyColor(at y: CGFloat) -> UIColor {
        let planet = planetIndex(at: y)
        let local = max(0, y) - planetBase(planet)
        let last = (planet + 1) * zonesPerPlanet - 1
        if local >= planetClimb {
            // Flying between planets: space fades into the next planet's sky as you come in to land.
            let t = smoothstep(planetClimb + (planetSpan - planetClimb) * 0.35, planetSpan - 2 * ppm, local)
            return UIColor.lerp(ZoneTheme.forZone(last).sky, ZoneTheme.forZone(last + 1).sky, t)
        }
        let zone = zoneIndex(at: y)
        if zone == last { return ZoneTheme.forZone(zone).sky }
        let frac = (local - CGFloat(zone % zonesPerPlanet) * zoneHeight) / zoneHeight
        return UIColor.lerp(ZoneTheme.forZone(zone).sky, ZoneTheme.forZone(zone + 1).sky, smoothstep(0.75, 1.0, frac))
    }

    private func drawTether() {
        guard stretch > 2 else {
            tether.path = nil
            return
        }
        let x = climberX + 13
        let path = CGMutablePath()
        path.move(to: CGPoint(x: x, y: climber.position.y + 18))
        path.addLine(to: CGPoint(x: x, y: anchorY))
        tether.path = path
        tether.strokeColor = UIColor.lerp(UIColor(white: 1, alpha: 0.9), UIColor(hex: 0xFF4D4D), strain)
    }

    private func updateHUD() {
        switch phase {
        case .title, .playing, .falling:
            heightLabel.text = "\(max(0, Int(climber.position.y / ppm))) m"
        case .launching:
            heightLabel.text = "\(max(0, Int((launchRocket?.position.y ?? climber.position.y) / ppm))) m"
        case .reviving:
            heightLabel.text = "\(max(0, Int((taxiRocket?.position.y ?? climber.position.y) / ppm))) m"
        case .gameOver:
            break
        }
        if phase == .title || phase == .playing, let next = checkpoints.first(where: { $0.y > anchorY + 1 }) {
            let meters = Int(ceil((next.y - climber.position.y) / ppm))
            nextLabel.text = next.isLaunchPad ? "LAUNCH PAD IN \(meters) m" : "NEXT ZONE IN \(meters) m"
        } else if phase != .title && phase != .playing {
            nextLabel.text = ""
        }

        let runMeters = Int(runBest / ppm)
        bestLabel.text = "BEST \(max(bestAtStart, runMeters)) m"
        if !celebratedBest && bestAtStart > 0 && runMeters > bestAtStart {
            celebratedBest = true
            Synth.shared.newBest()
            bestLabel.setColor(UIColor(hex: 0xFFD54F))
            bestLabel.run(.sequence([.scale(to: 1.4, duration: 0.1), .scale(to: 1, duration: 0.2)]))
        } else if !celebratedBest {
            bestLabel.setColor(.white)
        }
    }
}
