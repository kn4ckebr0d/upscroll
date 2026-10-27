import AVFoundation
import os

/// Tiny chiptune synth: square, triangle and noise voices generated live, no sound files.
final class Synth {
    static let shared = Synth()

    enum Wave { case square, triangle, noise }

    struct Voice {
        var wave: Wave
        var duty: Float = 0.5
        var f0: Float
        var f1: Float
        var duration: Float
        var volume: Float
        var delay: Float = 0
        var sustain: Float = 0      // share of the duration held at full volume before fading
        var t: Float = 0
        var phase: Float = 0
        var noise: Float = 0
    }

    var isEnabled: Bool {
        didSet { UserDefaults.standard.set(isEnabled, forKey: "soundEnabled") }
    }

    private let engine = AVAudioEngine()
    private var source: AVAudioSourceNode?
    private let pending = OSAllocatedUnfairLock<[Voice]>(initialState: [])
    private var voices: [Voice] = []          // audio thread only
    private var sampleRate: Float = 44_100
    private var rng: UInt32 = 0x1234_5678
    private var lastNope: CFTimeInterval = 0

    private init() {
        isEnabled = UserDefaults.standard.object(forKey: "soundEnabled") as? Bool ?? true
        voices.reserveCapacity(32)
        setUp()
    }

    private func setUp() {
        let session = AVAudioSession.sharedInstance()
        // Alpha: .playback so you hear it even with the silent switch on. Switch to .ambient before release.
        try? session.setCategory(.playback, options: [.mixWithOthers])
        try? session.setActive(true)

        let hwRate = engine.outputNode.outputFormat(forBus: 0).sampleRate
        sampleRate = Float(hwRate > 0 ? hwRate : 44_100)
        guard let format = AVAudioFormat(standardFormatWithSampleRate: Double(sampleRate), channels: 1) else { return }

        let node = AVAudioSourceNode(format: format) { [unowned self] _, _, frameCount, bufferList -> OSStatus in
            self.render(frameCount: Int(frameCount), bufferList: bufferList)
            return noErr
        }
        source = node
        engine.attach(node)
        engine.connect(node, to: engine.mainMixerNode, format: format)
        engine.mainMixerNode.outputVolume = 0.4
        engine.prepare()
        try? engine.start()

        NotificationCenter.default.addObserver(forName: AVAudioSession.interruptionNotification,
                                               object: nil, queue: .main) { [weak self] _ in
            self?.restartIfNeeded()
        }
        // Headphones plugged in / AirPods connected: the engine stops and must be restarted.
        NotificationCenter.default.addObserver(forName: .AVAudioEngineConfigurationChange,
                                               object: engine, queue: .main) { [weak self] _ in
            self?.restartIfNeeded()
        }
    }

    private func restartIfNeeded() {
        guard !engine.isRunning else { return }
        try? AVAudioSession.sharedInstance().setActive(true)
        try? engine.start()
    }

    private func play(_ voice: Voice) {
        guard isEnabled else { return }
        restartIfNeeded()
        pending.withLock { $0.append(voice) }
    }

    // MARK: - Rendering (audio thread)

    private func render(frameCount: Int, bufferList: UnsafeMutablePointer<AudioBufferList>) {
        if let incoming = pending.withLockIfAvailable({ state -> [Voice] in
            let new = state
            state.removeAll(keepingCapacity: true)
            return new
        }), !incoming.isEmpty {
            voices.append(contentsOf: incoming)
        }

        let buffers = UnsafeMutableAudioBufferListPointer(bufferList)
        guard let out = buffers.first?.mData?.assumingMemoryBound(to: Float.self) else { return }
        let dt = 1 / sampleRate
        for frame in 0..<frameCount {
            var mix: Float = 0
            for i in voices.indices {
                mix += nextSample(&voices[i], dt: dt)
            }
            out[frame] = max(-1, min(1, mix))
        }
        voices.removeAll { $0.t >= $0.delay + $0.duration }
    }

    private func nextSample(_ v: inout Voice, dt: Float) -> Float {
        defer { v.t += dt }
        let local = v.t - v.delay
        guard local >= 0, local < v.duration else { return 0 }
        let p = local / v.duration
        let freq = v.f0 * powf(v.f1 / v.f0, p)   // exponential slide f0 -> f1
        v.phase += freq * dt
        if v.phase >= 1 {
            v.phase -= 1
            if v.wave == .noise { v.noise = nextNoise() }
        }
        let s: Float
        switch v.wave {
        case .square: s = v.phase < v.duty ? 1 : -1
        case .triangle: s = 4 * abs(v.phase - 0.5) - 1
        case .noise: s = v.noise
        }
        let attack: Float = 0.004
        let env: Float
        if local < attack {
            env = local / attack
        } else if p < v.sustain {
            env = 1
        } else {
            let r = (1 - p) / max(0.0001, 1 - v.sustain)
            env = r * r
        }
        return s * env * v.volume
    }

    private func nextNoise() -> Float {
        rng ^= rng << 13
        rng ^= rng >> 17
        rng ^= rng << 5
        return Float(rng) / Float(UInt32.max) * 2 - 1
    }

    // MARK: - Sound effects

    private static let pentatonic: [Float] = [0, 2, 4, 7, 9]

    static func note(_ step: Int, base: Float = 261.63) -> Float {
        let octave = step / 5
        let semitones = Float(octave * 12) + pentatonic[step % 5]
        return base * powf(2, semitones / 12)
    }

    /// One step of the rising climb tone. Higher notes get softer so the top isn't shrill.
    func climbBlip(step: Int, of maxStep: Int) {
        let f = Synth.note(step)
        let height = Float(step) / Float(max(1, maxStep))
        play(Voice(wave: .square, duty: 0.25 + 0.25 * height, f0: f, f1: f * 1.02,
                   duration: 0.09, volume: 0.2 - 0.08 * height))
    }

    func staminaWarning() {
        play(Voice(wave: .triangle, f0: 220, f1: 196, duration: 0.12, volume: 0.5))
    }

    func slip() {
        play(Voice(wave: .noise, f0: 3000, f1: 1200, duration: 0.15, volume: 0.15))
        play(Voice(wave: .square, duty: 0.125, f0: 900, f1: 300, duration: 0.15, volume: 0.15))
    }

    func snapBack(fromStep step: Int) {
        let f = Synth.note(min(max(step, 4), 12))
        play(Voice(wave: .square, duty: 0.5, f0: f, f1: 90, duration: 0.32, volume: 0.2))
    }

    func hit() {
        play(Voice(wave: .noise, f0: 6000, f1: 700, duration: 0.3, volume: 0.35))
        play(Voice(wave: .square, duty: 0.5, f0: 700, f1: 60, duration: 0.9, volume: 0.2, delay: 0.05))
    }

    func zoneFanfare() {
        for (i, step) in [5, 7, 9, 10].enumerated() {
            let f = Synth.note(step)
            play(Voice(wave: .square, duty: 0.25, f0: f, f1: f, duration: 0.12, volume: 0.2, delay: Float(i) * 0.09))
        }
        let top = Synth.note(12)
        play(Voice(wave: .square, duty: 0.25, f0: top, f1: top, duration: 0.45, volume: 0.2, delay: 0.36))
        play(Voice(wave: .triangle, f0: Synth.note(0), f1: Synth.note(0), duration: 0.8, volume: 0.45, delay: 0.36))
    }

    func newBest() {
        for (i, step) in [10, 11, 12, 13, 15].enumerated() {
            let f = Synth.note(step)
            play(Voice(wave: .triangle, f0: f, f1: f, duration: 0.1, volume: 0.45, delay: Float(i) * 0.06))
        }
    }

    /// Falling "peeeoooow" plus wind.
    func fallWhistle(duration: Float, delay: Float) {
        play(Voice(wave: .square, duty: 0.5, f0: 1400, f1: 160, duration: duration, volume: 0.1, delay: delay, sustain: 0.85))
        play(Voice(wave: .noise, f0: 2500, f1: 700, duration: duration, volume: 0.07, delay: delay, sustain: 0.85))
    }

    func thud() {
        play(Voice(wave: .noise, f0: 600, f1: 60, duration: 0.3, volume: 0.5))
        play(Voice(wave: .triangle, f0: 120, f1: 40, duration: 0.35, volume: 0.8))
    }

    func countdownBeep(final: Bool) {
        let f: Float = final ? 1046.5 : 523.25
        play(Voice(wave: .square, duty: 0.5, f0: f, f1: f, duration: final ? 0.45 : 0.15, volume: 0.18, sustain: 0.6))
    }

    func liftoff(duration: Float) {
        play(Voice(wave: .noise, f0: 250, f1: 1800, duration: duration, volume: 0.32, sustain: 0.7))
        play(Voice(wave: .square, duty: 0.125, f0: 70, f1: 320, duration: duration, volume: 0.1, sustain: 0.7))
    }

    /// Classic two-note coin pickup.
    func coin() {
        play(Voice(wave: .square, duty: 0.5, f0: 988, f1: 988, duration: 0.06, volume: 0.13))
        play(Voice(wave: .square, duty: 0.5, f0: 1319, f1: 1319, duration: 0.22, volume: 0.13, delay: 0.06))
    }

    /// Rising whistle while flying back up to the checkpoint.
    func revive(duration: Float) {
        play(Voice(wave: .square, duty: 0.5, f0: 160, f1: 1400, duration: duration, volume: 0.1, sustain: 0.8))
        play(Voice(wave: .triangle, f0: 80, f1: 700, duration: duration, volume: 0.35, sustain: 0.8))
    }

    func purchase() {
        for (i, step) in [7, 9, 12, 14].enumerated() {
            let f = Synth.note(step)
            play(Voice(wave: .triangle, f0: f, f1: f, duration: 0.1, volume: 0.45, delay: Float(i) * 0.06))
        }
        coin()
    }

    /// One soft downward "bwomp". Ignores rapid repeats so mashing a button can't stack it up.
    func nope() {
        let now = CACurrentMediaTime()
        guard now - lastNope > 0.3 else { return }
        lastNope = now
        play(Voice(wave: .triangle, f0: 330, f1: 196, duration: 0.22, volume: 0.5, sustain: 0.3))
    }

    func swoosh() {
        play(Voice(wave: .noise, f0: 4000, f1: 1500, duration: 0.12, volume: 0.12))
    }

    func click() {
        play(Voice(wave: .triangle, f0: 880, f1: 1320, duration: 0.05, volume: 0.4))
    }
}
