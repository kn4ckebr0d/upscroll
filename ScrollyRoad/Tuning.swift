import CoreGraphics
import Foundation

/// All the feel knobs in one place. Tweak these while playtesting.
enum Tuning {
    static let pointsPerMeter: CGFloat = 40          // one voxel tile = one meter
    static let zoneLength: CGFloat = 40              // meters per zone

    static let rubberBandCoefficient: CGFloat = 0.55 // the constant UIScrollView uses
    static let fullStrainRatio: CGFloat = 0.6        // stretch share where the tether turns fully red

    // How much of the band's max stretch one zone uses.
    // Higher = the last meters before a checkpoint get heavier. Grows each zone.
    static let zoneReachStart: CGFloat = 0.45
    static let zoneReachPerZone: CGFloat = 0.05
    static let zoneReachMax: CGFloat = 0.7

    // Stamina, 0...1
    static let staminaBaseDrain: CGFloat = 0.015     // per second while hanging between checkpoints
    static let staminaTensionDrain: CGFloat = 0.12   // extra per second, scaled by how stretched you are
    static let staminaRegen: CGFloat = 0.5           // per second while resting on a checkpoint
    static let staminaRecoverAt: CGFloat = 0.35      // after slipping, you can grab again from here

    static let safeStartMeters: CGFloat = 8          // no mobs below this
    static let zoneSafeMeters: CGFloat = 5           // no mobs this close above a zone checkpoint

    // Planets: after the last zone a rocket flies you through empty space to the next planet.
    static let rocketFlightMeters: CGFloat = 30      // empty space between planets
    static let planetGroundRows = 8                  // ground thickness under a new planet's surface
    static let planetHueShift: CGFloat = 0.18        // each planet recolors the zone palettes by this hue

    static let hitStop: TimeInterval = 0.12          // freeze frame when a mob hits you

    // Sideways movement: swipe left/right to change column.
    static let columns = 5
    static let columnSpacing: CGFloat = 40           // one tile per column
    static let swipeDistance: CGFloat = 30           // sideways points a finger must travel to count as a swipe
    static let swipeMaxTime: TimeInterval = 0.4      // ...within this many seconds of touching down

    // Coins
    static let coinSpacing: CGFloat = 0.8            // meters between coins in a pattern
    static let coinGap: ClosedRange<CGFloat> = 4...9 // meters between patterns

    // Shop
    static let extraLifePrice = 60
    static let maxExtraLives = 3
    static let continueBasePrice = 30                // doubles with each continue in the same run
    static let invulnerableTime: TimeInterval = 1.5  // after using a life or continuing

    static let climberScreenFraction: CGFloat = 0.3  // where the climber sits on screen, from the bottom
    static let blipDistance: CGFloat = 20            // points climbed per note
    static let toneSteps = 12                        // top note, reached just before the next checkpoint
}

/// iOS-style rubber band: easy at first, approaching `dimension` but never reaching it.
func rubberBand(_ offset: CGFloat, dimension: CGFloat) -> CGFloat {
    let c = Tuning.rubberBandCoefficient
    return (1 - 1 / (offset * c / dimension + 1)) * dimension
}

func inverseRubberBand(_ stretched: CGFloat, dimension: CGFloat) -> CGFloat {
    let c = Tuning.rubberBandCoefficient
    let ratio = min(stretched / dimension, 0.999)
    return dimension / c * (1 / (1 - ratio) - 1)
}

func smoothstep(_ edge0: CGFloat, _ edge1: CGFloat, _ x: CGFloat) -> CGFloat {
    let t = min(1, max(0, (x - edge0) / (edge1 - edge0)))
    return t * t * (3 - 2 * t)
}
