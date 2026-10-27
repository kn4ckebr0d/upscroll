import SpriteKit

struct ZoneTheme {
    enum Decoration { case bushes, snow, clouds, stars }

    let name: String
    let sky: UIColor
    let rocks: [UIColor]
    let edge: UIColor
    let accents: [UIColor]
    let accentChance: Double
    let ledge: UIColor
    let mob: MobKind
    let decoration: Decoration

    static let all: [ZoneTheme] = [
        ZoneTheme(name: "MEADOW CLIFFS",
                  sky: UIColor(hex: 0x8ED6F5),
                  rocks: [UIColor(hex: 0x9C7B5B), UIColor(hex: 0x8A6B4E), UIColor(hex: 0xA88866)],
                  edge: UIColor(hex: 0x6E5238),
                  accents: [UIColor(hex: 0x6CBF4B), UIColor(hex: 0x58A83A)],
                  accentChance: 0.12,
                  ledge: UIColor(hex: 0x6B4423),
                  mob: .bird,
                  decoration: .bushes),
        ZoneTheme(name: "SNOWY PEAKS",
                  sky: UIColor(hex: 0xC9E6F7),
                  rocks: [UIColor(hex: 0xA7B0B8), UIColor(hex: 0x98A2AB), UIColor(hex: 0xB7BFC6)],
                  edge: UIColor(hex: 0x7D8790),
                  accents: [UIColor(hex: 0xF4F8FB), UIColor(hex: 0xFFFFFF)],
                  accentChance: 0.18,
                  ledge: UIColor(hex: 0x5D4037),
                  mob: .eagle,
                  decoration: .snow),
        ZoneTheme(name: "CLOUD STALK",
                  sky: UIColor(hex: 0x5B8FE0),
                  rocks: [UIColor(hex: 0x4E9E3A), UIColor(hex: 0x5BAE45), UIColor(hex: 0x458C33)],
                  edge: UIColor(hex: 0x33702A),
                  accents: [UIColor(hex: 0x7BD35A)],
                  accentChance: 0.1,
                  ledge: UIColor(hex: 0x2E5E22),
                  mob: .plane,
                  decoration: .clouds),
        ZoneTheme(name: "OUTER SPACE",
                  sky: UIColor(hex: 0x12122A),
                  rocks: [UIColor(hex: 0x6B5A7E), UIColor(hex: 0x5D4E70), UIColor(hex: 0x7A6A8C)],
                  edge: UIColor(hex: 0x43375A),
                  accents: [UIColor(hex: 0x9CF2FF)],
                  accentChance: 0.06,
                  ledge: UIColor(hex: 0x8E8E9E),
                  mob: .ufo,
                  decoration: .stars),
    ]

    static var zonesPerPlanet: Int { all.count }

    private static var cache: [Int: ZoneTheme] = [:]

    /// Zones repeat on every planet, recolored with a hue shift so each planet looks different.
    static func forZone(_ zone: Int) -> ZoneTheme {
        let z = max(0, zone)
        if let cached = cache[z] { return cached }
        let base = all[z % all.count]
        let planet = z / all.count
        let theme = planet == 0 ? base : base.hueShifted(by: CGFloat(planet) * Tuning.planetHueShift)
        cache[z] = theme
        return theme
    }

    func hueShifted(by delta: CGFloat) -> ZoneTheme {
        ZoneTheme(name: name,
                  sky: sky.hueShifted(by: delta),
                  rocks: rocks.map { $0.hueShifted(by: delta) },
                  edge: edge.hueShifted(by: delta),
                  accents: accents.map { $0.hueShifted(by: delta) },
                  accentChance: accentChance,
                  ledge: ledge.hueShifted(by: delta),
                  mob: mob,
                  decoration: decoration)
    }

    /// After the last zone you "arrive on a new planet" and the cycle restarts, faster.
    static func label(forZone zone: Int) -> String {
        let planet = zone / all.count + 1
        return "PLANET \(planet) · ZONE \(zone % all.count + 1)"
    }
}

enum MobKind {
    case bird, eagle, plane, ufo

    var baseSpeed: CGFloat {
        switch self {
        case .bird: return 105
        case .eagle: return 150
        case .plane: return 175
        case .ufo: return 125
        }
    }

    var hitSize: CGSize {
        switch self {
        case .bird: return CGSize(width: 30, height: 18)
        case .eagle: return CGSize(width: 42, height: 20)
        case .plane: return CGSize(width: 44, height: 18)
        case .ufo: return CGSize(width: 42, height: 20)
        }
    }

    /// Mob art faces right; flip with xScale = -1.
    func makeNode() -> SKNode {
        switch self {
        case .bird:
            return MobKind.bird(body: UIColor(hex: 0xE5533D), wing: UIColor(hex: 0xB83A28), head: UIColor(hex: 0xE5533D), scale: 1)
        case .eagle:
            return MobKind.bird(body: UIColor(hex: 0x6D4C2F), wing: UIColor(hex: 0x5A3D24), head: .white, scale: 1.4)
        case .plane:
            return MobKind.plane()
        case .ufo:
            return MobKind.ufo()
        }
    }

    private static func bird(body: UIColor, wing: UIColor, head: UIColor, scale s: CGFloat) -> SKNode {
        let node = SKNode()
        node.addChild(Voxel.block(body, CGSize(width: 22 * s, height: 13 * s)))
        let headNode = Voxel.block(head, CGSize(width: 11 * s, height: 11 * s))
        headNode.position = CGPoint(x: 13 * s, y: 4 * s)
        node.addChild(headNode)
        let beak = Voxel.block(UIColor(hex: 0xF5B731), CGSize(width: 6 * s, height: 4 * s))
        beak.position = CGPoint(x: 21 * s, y: 3 * s)
        node.addChild(beak)
        let eye = SKSpriteNode(color: .black, size: CGSize(width: 3, height: 3))
        eye.position = CGPoint(x: 15 * s, y: 6 * s)
        node.addChild(eye)
        let tail = Voxel.block(wing, CGSize(width: 7 * s, height: 6 * s))
        tail.position = CGPoint(x: -14 * s, y: 3 * s)
        node.addChild(tail)
        let wingNode = Voxel.block(wing, CGSize(width: 12 * s, height: 10 * s), anchor: CGPoint(x: 0.5, y: 0))
        wingNode.position = CGPoint(x: -1 * s, y: 3 * s)
        node.addChild(wingNode)
        wingNode.run(.repeatForever(.sequence([.scaleY(to: -0.8, duration: 0.12), .scaleY(to: 1, duration: 0.12)])))
        return node
    }

    private static func plane() -> SKNode {
        let node = SKNode()
        let red = UIColor(hex: 0xE53935)
        let tail = Voxel.block(red, CGSize(width: 6, height: 12), anchor: CGPoint(x: 0.5, y: 0))
        tail.position = CGPoint(x: -15, y: 2)
        node.addChild(tail)
        node.addChild(Voxel.block(UIColor(hex: 0xF4F4F4), CGSize(width: 34, height: 10)))
        let window = Voxel.block(UIColor(hex: 0x81D4FA), CGSize(width: 8, height: 4))
        window.position = CGPoint(x: 7, y: 2)
        node.addChild(window)
        let wing = Voxel.block(UIColor(hex: 0x1E88E5), CGSize(width: 14, height: 5))
        wing.position = CGPoint(x: 1, y: -3)
        node.addChild(wing)
        let nose = Voxel.block(red, CGSize(width: 6, height: 10))
        nose.position = CGPoint(x: 19, y: 0)
        node.addChild(nose)
        let prop = SKSpriteNode(color: UIColor(white: 0.2, alpha: 1), size: CGSize(width: 2, height: 16))
        prop.position = CGPoint(x: 23, y: 0)
        node.addChild(prop)
        prop.run(.repeatForever(.sequence([.scaleY(to: 0.2, duration: 0.04), .scaleY(to: 1, duration: 0.04)])))
        return node
    }

    private static func ufo() -> SKNode {
        let node = SKNode()
        let saucer = SKNode()
        node.addChild(saucer)
        let dome = Voxel.block(UIColor(hex: 0x7FE7FF), CGSize(width: 18, height: 10), anchor: CGPoint(x: 0.5, y: 0))
        dome.position = CGPoint(x: 0, y: 3)
        saucer.addChild(dome)
        saucer.addChild(Voxel.block(UIColor(hex: 0xB0BEC5), CGSize(width: 42, height: 10)))
        for (i, x) in [CGFloat(-13), 0, 13].enumerated() {
            let light = SKSpriteNode(color: UIColor(hex: 0xFFE14D), size: CGSize(width: 4, height: 3))
            light.position = CGPoint(x: x, y: -2)
            saucer.addChild(light)
            light.run(.sequence([.wait(forDuration: Double(i) * 0.15),
                                 .repeatForever(.sequence([.fadeAlpha(to: 0.2, duration: 0.2), .fadeAlpha(to: 1, duration: 0.25)]))]))
        }
        saucer.run(.repeatForever(.sequence([.moveBy(x: 0, y: 4, duration: 0.4), .moveBy(x: 0, y: -4, duration: 0.4)])))
        return node
    }
}

/// A horizontal row where mobs fly across, like a road in a lane-crossing game.
final class Lane {
    static let margin: CGFloat = 70

    let y: CGFloat
    let speed: CGFloat
    let kind: MobKind
    var mobs: [SKNode] = []
    var decor: [SKNode] = []

    init(y: CGFloat, speed: CGFloat, kind: MobKind) {
        self.y = y
        self.speed = speed
        self.kind = kind
    }

    func removeNodes() {
        mobs.forEach { $0.removeFromParent() }
        decor.forEach { $0.removeFromParent() }
    }
}

/// Voxel rocket that carries you to the next planet. Origin is at the bottom of the fins.
final class RocketNode: SKNode {
    private let body = SKNode()
    private let flame = SKNode()

    override init() {
        super.init()
        let red = UIColor(hex: 0xE53935)
        let bottom = CGPoint(x: 0.5, y: 0)

        let outer = SKSpriteNode(color: UIColor(hex: 0xFF8A00), size: CGSize(width: 16, height: 26))
        outer.anchorPoint = CGPoint(x: 0.5, y: 1)
        let inner = SKSpriteNode(color: UIColor(hex: 0xFFE14D), size: CGSize(width: 8, height: 16))
        inner.anchorPoint = CGPoint(x: 0.5, y: 1)
        flame.addChild(outer)
        flame.addChild(inner)
        flame.position = CGPoint(x: 0, y: 4)
        flame.isHidden = true
        addChild(flame)
        addChild(body)

        let nozzle = Voxel.block(UIColor(hex: 0x546E7A), CGSize(width: 12, height: 6), anchor: bottom)
        body.addChild(nozzle)
        for x: CGFloat in [-16, 16] {
            let fin = Voxel.block(red, CGSize(width: 10, height: 20), anchor: bottom)
            fin.position = CGPoint(x: x, y: 0)
            body.addChild(fin)
        }
        let hull = Voxel.block(UIColor(hex: 0xF4F4F4), CGSize(width: 26, height: 64), anchor: bottom)
        hull.position = CGPoint(x: 0, y: 6)
        body.addChild(hull)
        let stripe = Voxel.block(red, CGSize(width: 26, height: 6), anchor: bottom)
        stripe.position = CGPoint(x: 0, y: 54)
        body.addChild(stripe)
        let frame = SKSpriteNode(color: UIColor(hex: 0x37474F), size: CGSize(width: 14, height: 14))
        frame.position = CGPoint(x: 0, y: 38)
        body.addChild(frame)
        let glass = Voxel.block(UIColor(hex: 0x7FE7FF), CGSize(width: 10, height: 10))
        glass.position = CGPoint(x: 0, y: 38)
        body.addChild(glass)
        for (w, h, y) in [(CGFloat(20), CGFloat(8), CGFloat(70)), (14, 7, 78), (8, 6, 85), (3, 4, 91)] {
            let cone = Voxel.block(red, CGSize(width: w, height: h), anchor: bottom)
            cone.position = CGPoint(x: 0, y: y)
            body.addChild(cone)
        }
    }

    required init?(coder aDecoder: NSCoder) { fatalError("init(coder:) has not been implemented") }

    func setThrust(_ on: Bool, strength: CGFloat = 1) {
        flame.removeAllActions()
        flame.isHidden = !on
        guard on else { return }
        flame.run(.repeatForever(.sequence([.scaleY(to: 1.3 * strength, duration: 0.05),
                                            .scaleY(to: 0.8 * strength, duration: 0.05)])))
    }

    func rumble() {
        body.run(.repeatForever(.sequence([.moveBy(x: 1.5, y: 0, duration: 0.03),
                                           .moveBy(x: -3, y: 0, duration: 0.06),
                                           .moveBy(x: 1.5, y: 0, duration: 0.03)])), withKey: "rumble")
    }

    func stopRumble() {
        body.removeAction(forKey: "rumble")
        body.position = .zero
    }
}

enum Coin {
    /// A spinning voxel coin.
    static func makeNode() -> SKNode {
        let node = SKNode()
        let face = Voxel.block(UIColor(hex: 0xFFC83D), CGSize(width: 14, height: 14))
        let shine = SKSpriteNode(color: UIColor(hex: 0xFFF3B0), size: CGSize(width: 3, height: 8))
        shine.position = CGPoint(x: -2, y: 1)
        face.addChild(shine)
        node.addChild(face)
        face.run(.repeatForever(.sequence([.scaleX(to: 0.25, duration: 0.35), .scaleX(to: 1, duration: 0.35)])))
        return node
    }

    /// A still coin for the HUD.
    static func makeIcon() -> SKNode {
        let face = Voxel.block(UIColor(hex: 0xFFC83D), CGSize(width: 16, height: 16))
        let shine = SKSpriteNode(color: UIColor(hex: 0xFFF3B0), size: CGSize(width: 3, height: 9))
        shine.position = CGPoint(x: -3, y: 1)
        face.addChild(shine)
        return face
    }
}
