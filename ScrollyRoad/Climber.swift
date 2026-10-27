import SpriteKit

/// The blocky little climber. The node's origin is at the feet.
final class Climber: SKNode {
    private let rig = SKNode()
    private let leftArm: SKSpriteNode
    private let rightArm: SKSpriteNode
    /// Future unlockable hats go here.
    let hatAnchor = SKNode()
    private var armToggle = false

    override init() {
        let shirt = UIColor(hex: 0xE8553E)
        let skin = UIColor(hex: 0xF5C99B)
        let bottom = CGPoint(x: 0.5, y: 0)
        leftArm = Voxel.block(shirt, CGSize(width: 6, height: 20), anchor: bottom)
        rightArm = Voxel.block(shirt, CGSize(width: 6, height: 20), anchor: bottom)
        super.init()
        addChild(rig)

        for x: CGFloat in [-6, 6] {
            let leg = Voxel.block(UIColor(hex: 0x3B5BA5), CGSize(width: 9, height: 12), anchor: bottom)
            leg.position = CGPoint(x: x, y: 0)
            rig.addChild(leg)
        }

        leftArm.position = CGPoint(x: -15, y: 22)
        rightArm.position = CGPoint(x: 15, y: 22)
        for arm in [leftArm, rightArm] {
            let hand = Voxel.block(skin, CGSize(width: 7, height: 6), anchor: bottom)
            hand.position = CGPoint(x: 0, y: 20)
            arm.addChild(hand)
            rig.addChild(arm)
        }

        let body = Voxel.block(shirt, CGSize(width: 24, height: 18), anchor: bottom)
        body.position = CGPoint(x: 0, y: 11)
        rig.addChild(body)

        let head = Voxel.block(skin, CGSize(width: 22, height: 18), anchor: bottom)
        head.position = CGPoint(x: 0, y: 28)
        rig.addChild(head)

        for x: CGFloat in [-5, 5] {
            let eye = SKSpriteNode(color: .black, size: CGSize(width: 3, height: 4))
            eye.position = CGPoint(x: x, y: 38)
            rig.addChild(eye)
        }

        hatAnchor.position = CGPoint(x: 0, y: 46)
        rig.addChild(hatAnchor)
        let capColor = UIColor(hex: 0x2F80ED)
        hatAnchor.addChild(Voxel.block(capColor, CGSize(width: 24, height: 7), anchor: bottom))
        let brim = Voxel.block(capColor.adjusted(brightness: -0.12), CGSize(width: 12, height: 3), anchor: .zero)
        brim.position = CGPoint(x: 8, y: 0)
        hatAnchor.addChild(brim)
    }

    required init?(coder aDecoder: NSCoder) { fatalError("init(coder:) has not been implemented") }

    /// Alternate hands, hand over hand.
    func reach() {
        armToggle.toggle()
        leftArm.yScale = armToggle ? 1 : 0.7
        rightArm.yScale = armToggle ? 0.7 : 1
    }

    /// Little squash when grabbing a ledge.
    func land() {
        rig.removeAction(forKey: "squash")
        rig.run(.sequence([.scaleY(to: 0.82, duration: 0.05), .scaleY(to: 1, duration: 0.14)]), withKey: "squash")
    }

    func resetPose() {
        removeAllActions()
        rig.removeAllActions()
        rig.yScale = 1
        leftArm.yScale = 1
        rightArm.yScale = 1
        zRotation = 0
        alpha = 1
        setScale(1)
    }
}
