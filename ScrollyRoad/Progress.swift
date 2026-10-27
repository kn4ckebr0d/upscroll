import Foundation

/// The player's saved progress. Stored on this device for now; one place to move to iCloud later.
enum Progress {
    private static let defaults = UserDefaults.standard

    static var coins: Int {
        get { defaults.integer(forKey: "coins.total") }
        set { defaults.set(max(0, newValue), forKey: "coins.total") }
    }

    static var extraLives: Int {
        get { defaults.integer(forKey: "lives.extra") }
        set { defaults.set(max(0, newValue), forKey: "lives.extra") }
    }

    static var best: Int {
        get { defaults.integer(forKey: "best.zonesOnly") }
        set { defaults.set(newValue, forKey: "best.zonesOnly") }
    }

    /// Spends coins if there are enough. Returns whether it worked.
    static func spend(_ amount: Int) -> Bool {
        guard coins >= amount else { return false }
        coins -= amount
        return true
    }
}
