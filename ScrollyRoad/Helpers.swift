import SpriteKit
import UIKit

extension UIColor {
    convenience init(hex: UInt32, alpha: CGFloat = 1) {
        self.init(red: CGFloat((hex >> 16) & 0xFF) / 255,
                  green: CGFloat((hex >> 8) & 0xFF) / 255,
                  blue: CGFloat(hex & 0xFF) / 255,
                  alpha: alpha)
    }

    func adjusted(brightness delta: CGFloat) -> UIColor {
        var h: CGFloat = 0, s: CGFloat = 0, b: CGFloat = 0, a: CGFloat = 0
        guard getHue(&h, saturation: &s, brightness: &b, alpha: &a) else { return self }
        return UIColor(hue: h, saturation: s, brightness: min(1, max(0, b + delta)), alpha: a)
    }

    func hueShifted(by delta: CGFloat) -> UIColor {
        var h: CGFloat = 0, s: CGFloat = 0, b: CGFloat = 0, a: CGFloat = 0
        guard getHue(&h, saturation: &s, brightness: &b, alpha: &a) else { return self }
        var hue = (h + delta).truncatingRemainder(dividingBy: 1)
        if hue < 0 { hue += 1 }
        return UIColor(hue: hue, saturation: s, brightness: b, alpha: a)
    }

    static func lerp(_ from: UIColor, _ to: UIColor, _ t: CGFloat) -> UIColor {
        var r1: CGFloat = 0, g1: CGFloat = 0, b1: CGFloat = 0, a1: CGFloat = 0
        var r2: CGFloat = 0, g2: CGFloat = 0, b2: CGFloat = 0, a2: CGFloat = 0
        from.getRed(&r1, green: &g1, blue: &b1, alpha: &a1)
        to.getRed(&r2, green: &g2, blue: &b2, alpha: &a2)
        let t = min(1, max(0, t))
        return UIColor(red: r1 + (r2 - r1) * t,
                       green: g1 + (g2 - g1) * t,
                       blue: b1 + (b2 - b1) * t,
                       alpha: a1 + (a2 - a1) * t)
    }
}

enum Voxel {
    /// A flat block with a lighter top edge and darker right edge, faking a chunky 3D voxel.
    static func block(_ color: UIColor, _ size: CGSize, anchor: CGPoint = CGPoint(x: 0.5, y: 0.5)) -> SKSpriteNode {
        let node = SKSpriteNode(color: color, size: size)
        node.anchorPoint = anchor

        let side = SKSpriteNode(color: color.adjusted(brightness: -0.16),
                                size: CGSize(width: max(1, size.width * 0.18), height: size.height))
        side.anchorPoint = CGPoint(x: 1, y: 0)
        side.position = CGPoint(x: size.width * (1 - anchor.x), y: -size.height * anchor.y)
        node.addChild(side)

        let top = SKSpriteNode(color: color.adjusted(brightness: 0.12),
                               size: CGSize(width: size.width, height: max(1, size.height * 0.16)))
        top.anchorPoint = CGPoint(x: 0, y: 1)
        top.position = CGPoint(x: -size.width * anchor.x, y: size.height * (1 - anchor.y))
        node.addChild(top)
        return node
    }
}

/// Chunky arcade label with a drop shadow.
final class ShadowLabel: SKNode {
    private let main: SKLabelNode
    private let shadow: SKLabelNode

    var text: String {
        get { main.text ?? "" }
        set {
            guard newValue != main.text else { return }
            main.text = newValue
            shadow.text = newValue
        }
    }

    init(text: String = "", fontSize: CGFloat, color: UIColor = .white, font: String = "AvenirNext-Heavy") {
        main = SKLabelNode(fontNamed: font)
        shadow = SKLabelNode(fontNamed: font)
        super.init()
        for label in [shadow, main] {
            label.fontSize = fontSize
            label.text = text
            label.verticalAlignmentMode = .center
            label.horizontalAlignmentMode = .center
            addChild(label)
        }
        main.fontColor = color
        shadow.fontColor = UIColor(white: 0, alpha: 0.35)
        shadow.position = CGPoint(x: 0, y: -max(2, fontSize * 0.07))
    }

    required init?(coder aDecoder: NSCoder) { fatalError("init(coder:) has not been implemented") }

    func setColor(_ color: UIColor) { main.fontColor = color }

    func setAlignment(_ mode: SKLabelHorizontalAlignmentMode) {
        main.horizontalAlignmentMode = mode
        shadow.horizontalAlignmentMode = mode
    }
}

enum Haptics {
    private static let light = UIImpactFeedbackGenerator(style: .light)
    private static let soft = UIImpactFeedbackGenerator(style: .soft)
    private static let rigid = UIImpactFeedbackGenerator(style: .rigid)
    private static let heavy = UIImpactFeedbackGenerator(style: .heavy)
    private static let notification = UINotificationFeedbackGenerator()

    /// Climb tick; pass a higher intensity as the next checkpoint gets closer.
    static func tick(_ intensity: CGFloat) { light.impactOccurred(intensity: min(1, max(0.1, intensity))) }
    static func snap() { soft.impactOccurred() }
    static func slip() { rigid.impactOccurred(intensity: 0.8) }
    static func hit() { heavy.impactOccurred(intensity: 1) }
    static func land() { heavy.impactOccurred(intensity: 1) }
    static func rumble(_ intensity: CGFloat) { rigid.impactOccurred(intensity: intensity) }
    static func zone() { notification.notificationOccurred(.success) }

    /// Double thump, like a heartbeat, for low stamina.
    static func heartbeat() {
        soft.impactOccurred(intensity: 1)
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.14) { soft.impactOccurred(intensity: 0.6) }
    }
}
