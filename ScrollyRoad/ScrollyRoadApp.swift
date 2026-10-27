import SwiftUI
import SpriteKit

@main
struct ScrollyRoadApp: App {
    var body: some Scene {
        WindowGroup {
            RootView()
                .ignoresSafeArea()
                .statusBarHidden()
                .persistentSystemOverlays(.hidden)
                // Dragging down from the top edge would open Notification Center mid-climb.
                .defersSystemGestures(on: .all)
        }
    }
}

/// The 2D game, with the 3D look test shown on top when opened from the title screen.
struct RootView: View {
    @State private var show3DTest = ProcessInfo.processInfo.environment["SCROLLY_OPEN_3D"] != nil

    var body: some View {
        ZStack {
            GameView()
            if show3DTest {
                VoxelTestView { show3DTest = false }
                    .transition(.opacity)
            }
        }
        .animation(.easeInOut(duration: 0.25), value: show3DTest)
        .onReceive(NotificationCenter.default.publisher(for: .open3DTest)) { _ in show3DTest = true }
    }
}

struct GameView: UIViewRepresentable {
    func makeUIView(context: Context) -> GameSKView {
        let view = GameSKView()
        view.isMultipleTouchEnabled = true
        view.ignoresSiblingOrder = false
        return view
    }

    func updateUIView(_ uiView: GameSKView, context: Context) {}
}

/// Presents the scene once the view has a real size, so the scene and safe area are correct.
final class GameSKView: SKView {
    override func layoutSubviews() {
        super.layoutSubviews()
        guard scene == nil, bounds.width > 0, bounds.height > 0 else { return }
        let scene = GameScene(size: bounds.size)
        scene.scaleMode = .resizeFill
        presentScene(scene)
    }
}
