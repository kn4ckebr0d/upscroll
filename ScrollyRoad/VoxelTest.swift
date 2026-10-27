import SwiftUI
import SceneKit

// A standalone look test: a short slice of the climb rendered in 3D voxels
// with a tilted orthographic camera and soft shadows, Crossy Road style.
// It doesn't touch the 2D game; open it from the title screen.

extension Notification.Name {
    static let open3DTest = Notification.Name("ScrollyOpen3DTest")
}

struct VoxelTestView: View {
    let onClose: () -> Void
    @State private var auto = true

    var body: some View {
        ZStack(alignment: .top) {
            VoxelSceneView(auto: auto)
                .ignoresSafeArea()
            HStack {
                Button("◀ BACK", action: onClose)
                Spacer()
                Text("3D LOOK TEST")
                Spacer()
                Button(auto ? "AUTO: ON" : "AUTO: OFF") { auto.toggle() }
            }
            .font(.system(size: 15, weight: .heavy, design: .rounded))
            .foregroundStyle(.white)
            .shadow(color: .black.opacity(0.4), radius: 0, x: 0, y: 2)
            .padding(.horizontal, 18)
            .padding(.top, 8)
        }
    }
}

struct VoxelSceneView: UIViewRepresentable {
    var auto: Bool

    func makeCoordinator() -> VoxelTestController { VoxelTestController() }

    func makeUIView(context: Context) -> SCNView { context.coordinator.makeView() }

    func updateUIView(_ uiView: SCNView, context: Context) {
        context.coordinator.auto = auto
    }

    static func dismantleUIView(_ uiView: SCNView, coordinator: VoxelTestController) {
        coordinator.stop()
    }
}

/// Builds voxel boxes with shared flat (lambert) materials.
private enum Vox {
    private static var materials: [UInt32: SCNMaterial] = [:]

    static func material(_ hex: UInt32) -> SCNMaterial {
        if let cached = materials[hex] { return cached }
        let material = SCNMaterial()
        material.diffuse.contents = UIColor(hex: hex)
        material.lightingModel = .lambert
        materials[hex] = material
        return material
    }

    static func box(_ w: Float, _ h: Float, _ d: Float, _ hex: UInt32, at position: SCNVector3 = SCNVector3Zero) -> SCNNode {
        let geometry = SCNBox(width: CGFloat(w), height: CGFloat(h), length: CGFloat(d), chamferRadius: 0)
        geometry.firstMaterial = material(hex)
        let node = SCNNode(geometry: geometry)
        node.position = position
        return node
    }
}

/// The climber as a little voxel figure. Origin at the feet, facing the camera (+z).
private final class VoxelClimber: SCNNode {
    private let leftArm = SCNNode()
    private let rightArm = SCNNode()

    override init() {
        super.init()
        let shirt: UInt32 = 0xE8553E, skin: UInt32 = 0xF5C99B, cap: UInt32 = 0x2F80ED
        for x: Float in [-0.15, 0.15] {
            addChildNode(Vox.box(0.22, 0.3, 0.24, 0x3B5BA5, at: SCNVector3(x, 0.15, 0)))
        }
        addChildNode(Vox.box(0.6, 0.45, 0.4, shirt, at: SCNVector3(0, 0.52, 0)))
        addChildNode(Vox.box(0.55, 0.45, 0.5, skin, at: SCNVector3(0, 0.97, 0)))
        for x: Float in [-0.12, 0.12] {
            addChildNode(Vox.box(0.08, 0.1, 0.02, 0x111111, at: SCNVector3(x, 1.0, 0.26)))
        }
        addChildNode(Vox.box(0.6, 0.17, 0.55, cap, at: SCNVector3(0, 1.27, 0)))
        addChildNode(Vox.box(0.6, 0.06, 0.25, 0x2569C9, at: SCNVector3(0, 1.2, 0.36)))
        for (arm, x) in [(leftArm, Float(-0.38)), (rightArm, Float(0.38))] {
            arm.position = SCNVector3(x, 0.7, 0)
            arm.addChildNode(Vox.box(0.15, 0.5, 0.15, shirt, at: SCNVector3(0, 0.25, 0)))
            arm.addChildNode(Vox.box(0.17, 0.15, 0.17, skin, at: SCNVector3(0, 0.55, 0)))
            addChildNode(arm)
        }
    }

    required init?(coder: NSCoder) { fatalError("init(coder:) has not been implemented") }

    /// Hand over hand: arms alternate as you climb.
    func setClimbPhase(_ phase: Float) {
        leftArm.scale = SCNVector3(1, 0.85 + 0.15 * sin(phase), 1)
        rightArm.scale = SCNVector3(1, 0.85 - 0.15 * sin(phase), 1)
    }
}

final class VoxelTestController: NSObject {
    var auto = true

    private let scene = SCNScene()
    private let cameraNode = SCNNode()
    private let sun = SCNNode()
    private let climber = VoxelClimber()
    private var birds: [(node: SCNNode, speed: Float)] = []
    private var coins: [(node: SCNNode, column: Int, y: Float, collected: Bool)] = []
    private var displayLink: CADisplayLink?
    private weak var view: SCNView?

    private var lastTime: CFTimeInterval = 0
    private var lastTouch: CFTimeInterval = 0
    private var nextAutoSwipe: CFTimeInterval = 0
    private var climbY: Float = 0
    private var targetY: Float = 0
    private var column = 0
    private var x: Float = 0
    private var dragAccumX: CGFloat = 0

    private let viewHeight: Float = 18      // world units (meters) visible vertically
    private let topY: Float = 58
    private let checkpointY: Float = 30     // meadow below, snow above

    func makeView() -> SCNView {
        let view = SCNView()
        view.scene = scene
        view.antialiasingMode = .multisampling4X
        view.preferredFramesPerSecond = 60
        view.isPlaying = true
        build()
        view.pointOfView = cameraNode
        view.addGestureRecognizer(UIPanGestureRecognizer(target: self, action: #selector(handlePan(_:))))
        self.view = view

        let link = CADisplayLink(target: self, selector: #selector(tick(_:)))
        link.add(to: .main, forMode: .common)
        displayLink = link
        return view
    }

    func stop() {
        displayLink?.invalidate()
        displayLink = nil
    }

    // MARK: - Scene

    private func build() {
        scene.background.contents = skyGradient()

        let camera = SCNCamera()
        camera.usesOrthographicProjection = true
        camera.orthographicScale = Double(viewHeight / 2)
        camera.zNear = 1
        camera.zFar = 150
        cameraNode.camera = camera
        scene.rootNode.addChildNode(cameraNode)

        let ambient = SCNNode()
        ambient.light = SCNLight()
        ambient.light?.type = .ambient
        ambient.light?.intensity = 550
        scene.rootNode.addChildNode(ambient)

        let light = SCNLight()
        light.type = .directional
        light.intensity = 950
        light.castsShadow = true
        light.shadowMode = .forward
        light.shadowColor = UIColor(white: 0, alpha: 0.45)
        light.shadowRadius = 3
        light.shadowSampleCount = 8
        light.shadowMapSize = CGSize(width: 2048, height: 2048)
        light.automaticallyAdjustsShadowProjection = true
        sun.light = light
        sun.eulerAngles = SCNVector3(-0.85, -0.55, 0)   // from the upper left, slightly in front
        scene.rootNode.addChildNode(sun)

        buildGround()
        buildWall()
        buildCheckpoint()
        buildBirds()
        buildCoins()
        buildClouds()

        climber.position = SCNVector3(0, 0, 0.62)
        scene.rootNode.addChildNode(climber)
    }

    private func halfWidth(_ r: Int) -> Int {
        let v = sin(Double(r / 3) * 12.9898 + 78.233) * 43758.5453
        return v - floor(v) < 0.5 ? 2 : 3
    }

    private func buildGround() {
        scene.rootNode.addChildNode(Vox.box(30, 0.4, 16, 0x6CBF4B, at: SCNVector3(0, -0.2, 3)))
        scene.rootNode.addChildNode(Vox.box(30, 3, 16, 0x7A5236, at: SCNVector3(0, -1.9, 3)))
        for (x, z) in [(Float(-4.6), Float(2.6)), (5.4, 1.2), (-6.8, 0.4), (3.4, 4.5)] {
            let tree = SCNNode()
            tree.addChildNode(Vox.box(0.4, 0.8, 0.4, 0x7A5236, at: SCNVector3(0, 0.4, 0)))
            tree.addChildNode(Vox.box(1.2, 1.2, 1.2, 0x4E9A34, at: SCNVector3(0, 1.4, 0)))
            tree.addChildNode(Vox.box(0.8, 0.6, 0.8, 0x5DB043, at: SCNVector3(0, 2.3, 0)))
            tree.position = SCNVector3(x, 0, z)
            scene.rootNode.addChildNode(tree)
        }
        scene.rootNode.addChildNode(Vox.box(0.6, 0.4, 0.6, 0x9E9E9E, at: SCNVector3(-2.8, 0.2, 3.2)))

        // The rocket parked on the ground.
        let rocket = SCNNode()
        for x: Float in [-0.4, 0.4] {
            rocket.addChildNode(Vox.box(0.25, 0.5, 0.25, 0xE53935, at: SCNVector3(x, 0.25, 0)))
        }
        rocket.addChildNode(Vox.box(0.65, 1.6, 0.65, 0xF4F4F4, at: SCNVector3(0, 0.95, 0)))
        rocket.addChildNode(Vox.box(0.67, 0.15, 0.67, 0xE53935, at: SCNVector3(0, 1.45, 0)))
        rocket.addChildNode(Vox.box(0.3, 0.3, 0.05, 0x7FE7FF, at: SCNVector3(0, 1.0, 0.33)))
        rocket.addChildNode(Vox.box(0.5, 0.2, 0.5, 0xE53935, at: SCNVector3(0, 1.85, 0)))
        rocket.addChildNode(Vox.box(0.34, 0.18, 0.34, 0xE53935, at: SCNVector3(0, 2.04, 0)))
        rocket.addChildNode(Vox.box(0.18, 0.15, 0.18, 0xE53935, at: SCNVector3(0, 2.2, 0)))
        rocket.position = SCNVector3(3.9, 0, 2.0)
        scene.rootNode.addChildNode(rocket)
    }

    private func buildWall() {
        let wall = SCNNode()
        for r in 0..<64 {
            let snowy = Float(r) >= checkpointY
            let half = halfWidth(r)
            let above = halfWidth(r + 1)
            let rocks: [UInt32] = snowy ? [0xA7B0B8, 0x98A2AB, 0xB7BFC6] : [0x9C7B5B, 0x8A6B4E, 0xA88866]
            for c in -half...half {
                let edge = abs(c) == half
                var hex = rocks.randomElement()!
                if edge {
                    hex = snowy ? 0x7D8790 : 0x6E5238
                } else if Double.random(in: 0..<1) < (snowy ? 0.15 : 0.1) {
                    hex = snowy ? 0xF4F8FB : 0x6CBF4B
                }
                // Edge blocks sometimes stick out a bit for relief.
                let depth: Float = edge && Bool.random() ? 1.4 : 1
                let z = -1 + depth / 2
                wall.addChildNode(Vox.box(1, 1, depth, hex, at: SCNVector3(Float(c), Float(r) + 0.5, z)))
                // Where the wall steps in above, the exposed top gets grass or snow.
                if abs(c) > above {
                    wall.addChildNode(Vox.box(1.02, 0.18, depth + 0.02, snowy ? 0xFFFFFF : 0x6CBF4B,
                                              at: SCNVector3(Float(c), Float(r) + 1.09, z)))
                }
            }
            if !snowy && Int.random(in: 0..<6) == 0 {
                let side: Float = Bool.random() ? 1 : -1
                wall.addChildNode(Vox.box(0.5, 0.45, 0.5, 0x5DB043,
                                          at: SCNVector3(side * (Float(half) + 0.65), Float(r) + 0.4, -0.3)))
            }
        }
        scene.rootNode.addChildNode(wall.flattenedClone())
    }

    private func buildCheckpoint() {
        let y = checkpointY
        scene.rootNode.addChildNode(Vox.box(6, 0.25, 1.6, 0x6B4423, at: SCNVector3(0, y - 0.125, 0.6)))
        let pole = SCNNode(geometry: SCNCylinder(radius: 0.05, height: 1.4))
        pole.geometry?.firstMaterial = Vox.material(0xF2F2F2)
        pole.position = SCNVector3(-2.6, y + 0.7, 1.0)
        scene.rootNode.addChildNode(pole)
        let flag = Vox.box(0.6, 0.35, 0.06, 0xFFD54F, at: SCNVector3(-2.27, y + 1.2, 1.0))
        flag.runAction(.repeatForever(.sequence([.scale(to: 0.85, duration: 0.3), .scale(to: 1, duration: 0.3)])))
        scene.rootNode.addChildNode(flag)
    }

    private func makeBird(eagle: Bool) -> SCNNode {
        let s: Float = eagle ? 1.35 : 1
        let body: UInt32 = eagle ? 0x6D4C2F : 0xE5533D
        let wing: UInt32 = eagle ? 0x5A3D24 : 0xB83A28
        let bird = SCNNode()
        bird.addChildNode(Vox.box(0.55 * s, 0.33 * s, 0.38 * s, body))
        bird.addChildNode(Vox.box(0.28 * s, 0.28 * s, 0.3 * s, eagle ? 0xFFFFFF : body, at: SCNVector3(0.32 * s, 0.1 * s, 0)))
        bird.addChildNode(Vox.box(0.15 * s, 0.1 * s, 0.12 * s, 0xF5B731, at: SCNVector3(0.52 * s, 0.07 * s, 0)))
        for z: Float in [-0.16, 0.16] {
            bird.addChildNode(Vox.box(0.06, 0.06, 0.02, 0x111111, at: SCNVector3(0.38 * s, 0.16 * s, z * s)))
        }
        bird.addChildNode(Vox.box(0.18 * s, 0.15 * s, 0.22 * s, wing, at: SCNVector3(-0.34 * s, 0.05 * s, 0)))
        for side: Float in [-1, 1] {
            let hinge = SCNNode()
            hinge.position = SCNVector3(0, 0.1 * s, side * 0.19 * s)
            hinge.addChildNode(Vox.box(0.32 * s, 0.06 * s, 0.34 * s, wing, at: SCNVector3(0, 0, side * 0.17 * s)))
            let flap = SCNAction.sequence([.rotateTo(x: CGFloat(side * 0.7), y: 0, z: 0, duration: 0.14),
                                           .rotateTo(x: CGFloat(-side * 0.5), y: 0, z: 0, duration: 0.14)])
            hinge.runAction(.repeatForever(flap))
            bird.addChildNode(hinge)
        }
        return bird
    }

    private func buildBirds() {
        for y: Float in [8, 14.5, 21, 26, 37, 44, 51] {
            let eagle = y > checkpointY
            let speed = Float.random(in: 2.2...3.4) * (Bool.random() ? 1 : -1) * (eagle ? 1.3 : 1)
            let bird = makeBird(eagle: eagle)
            bird.position = SCNVector3(Float.random(in: -8...8), y, 0.9)
            if speed < 0 { bird.eulerAngles.y = .pi }
            scene.rootNode.addChildNode(bird)
            birds.append((bird, speed))
        }
    }

    private func buildCoins() {
        // A few patterns: lines, diagonals and zigzags across the 5 columns.
        let patterns: [(start: Float, columns: [Int])] = [
            (3, [0, 0, 0, 0]), (10, [-2, -1, 0, 1, 2]), (17, [1, 2, 1, 2, 1]),
            (24, [-1, -1, -1]), (33, [2, 1, 0, -1]), (40, [0, 0, 0, 0, 0]), (47, [-2, -1, -2, -1]), (54, [1, 1, 1]),
        ]
        for pattern in patterns {
            for (i, c) in pattern.columns.enumerated() {
                let y = pattern.start + Float(i) * 0.8
                let holder = SCNNode()
                holder.position = SCNVector3(Float(c), y + 0.6, 0.85)
                let coin = SCNNode(geometry: SCNCylinder(radius: 0.2, height: 0.07))
                let gold = SCNMaterial()
                gold.diffuse.contents = UIColor(hex: 0xFFC83D)
                gold.emission.contents = UIColor(hex: 0x3A2A00)
                gold.lightingModel = .lambert
                coin.geometry?.firstMaterial = gold
                coin.eulerAngles.x = .pi / 2
                holder.addChildNode(coin)
                holder.runAction(.repeatForever(.rotateBy(x: 0, y: .pi * 2, z: 0, duration: 1.2)))
                scene.rootNode.addChildNode(holder)
                coins.append((holder, c, y, false))
            }
        }
    }

    private func buildClouds() {
        for _ in 0..<10 {
            let cloud = SCNNode()
            for (dx, dy, w) in [(Float(0), Float(0), Float(1.6)), (0.7, 0.35, 1.0), (-0.6, 0.25, 0.9)] {
                cloud.addChildNode(Vox.box(w, 0.6, 0.8, 0xFFFFFF, at: SCNVector3(dx, dy, 0)))
            }
            let side: Float = Bool.random() ? 1 : -1
            cloud.position = SCNVector3(side * Float.random(in: 4.5...8), Float.random(in: 4...60), -3)
            cloud.castsShadow = false
            scene.rootNode.addChildNode(cloud)
        }
    }

    private func resetCoins() {
        for i in coins.indices {
            coins[i].collected = false
            let node = coins[i].node
            node.removeAllActions()
            node.opacity = 1
            node.scale = SCNVector3(1, 1, 1)
            node.position = SCNVector3(Float(coins[i].column), coins[i].y + 0.6, 0.85)
            node.runAction(.repeatForever(.rotateBy(x: 0, y: .pi * 2, z: 0, duration: 1.2)))
        }
    }

    private func skyGradient() -> UIImage {
        let size = CGSize(width: 4, height: 256)
        return UIGraphicsImageRenderer(size: size).image { context in
            let colors = [UIColor(hex: 0x6FC3F0).cgColor, UIColor(hex: 0xC4ECFC).cgColor] as CFArray
            if let gradient = CGGradient(colorsSpace: CGColorSpaceCreateDeviceRGB(), colors: colors, locations: [0, 1]) {
                context.cgContext.drawLinearGradient(gradient, start: .zero, end: CGPoint(x: 0, y: size.height), options: [])
            }
        }
    }

    // MARK: - Input

    @objc private func handlePan(_ gesture: UIPanGestureRecognizer) {
        guard let view else { return }
        lastTouch = CACurrentMediaTime()
        let t = gesture.translation(in: view)
        // Drag down to climb; the world follows your finger.
        let unitsPerPoint = viewHeight / Float(max(1, view.bounds.height))
        targetY = min(topY, max(0, targetY + Float(t.y) * unitsPerPoint))
        // Sideways drags change column.
        if abs(t.x) > abs(t.y) { dragAccumX += t.x }
        if abs(dragAccumX) > 45 {
            shiftColumn(dragAccumX > 0 ? 1 : -1)
            dragAccumX = 0
        }
        if gesture.state == .ended || gesture.state == .cancelled { dragAccumX = 0 }
        gesture.setTranslation(.zero, in: view)
    }

    private func shiftColumn(_ direction: Int) {
        let target = min(2, max(-2, column + direction))
        guard target != column else { return }
        column = target
        Synth.shared.swoosh()
    }

    // MARK: - Frame update

    @objc private func tick(_ link: CADisplayLink) {
        let now = link.timestamp
        let dt = Float(lastTime == 0 ? 1.0 / 60 : min(now - lastTime, 1.0 / 20))
        lastTime = now

        if auto && now - lastTouch > 1.5 {
            targetY += dt * 2.4
            if targetY > topY {
                targetY = 0
                resetCoins()
            }
            // Chase the next coin a little way up.
            if now >= nextAutoSwipe,
               let next = coins.filter({ !$0.collected && $0.y > climbY + 0.3 && $0.y < climbY + 4 }).min(by: { $0.y < $1.y }),
               next.column != column {
                shiftColumn(next.column > column ? 1 : -1)
                nextAutoSwipe = now + 0.3
            }
        }

        climbY += (targetY - climbY) * min(1, dt * 12)
        let targetX = Float(column)
        x += (targetX - x) * min(1, dt * 14)
        climber.position = SCNVector3(x, climbY, 0.5)
        climber.eulerAngles.z = (x - targetX) * 0.25   // lean into a sideways move
        climber.setClimbPhase(climbY * 4)

        for bird in birds {
            bird.node.position.x += bird.speed * dt
            if bird.speed > 0 && bird.node.position.x > 9 { bird.node.position.x = -9 }
            if bird.speed < 0 && bird.node.position.x < -9 { bird.node.position.x = 9 }
        }

        for i in coins.indices where !coins[i].collected {
            guard coins[i].column == column, abs(coins[i].y - climbY) < 0.6 else { continue }
            coins[i].collected = true
            Synth.shared.coin()
            Haptics.tick(0.6)
            coins[i].node.runAction(.group([.moveBy(x: 0, y: 0.8, z: 0, duration: 0.25),
                                            .fadeOut(duration: 0.25),
                                            .scale(to: 1.6, duration: 0.25)]))
        }

        // Tilted orthographic camera, Crossy Road style: a bit from the right and from above.
        let target = SCNVector3(0, climbY + viewHeight * 0.2, 0)
        let yaw: Float = 0.38, pitch: Float = 0.32, distance: Float = 40
        cameraNode.position = SCNVector3(target.x + sin(yaw) * cos(pitch) * distance,
                                         target.y + sin(pitch) * distance,
                                         target.z + cos(yaw) * cos(pitch) * distance)
        cameraNode.look(at: target)
        sun.position = SCNVector3(target.x, target.y + 6, 6)
    }
}
