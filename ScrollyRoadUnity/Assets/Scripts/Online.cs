using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Exceptions;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

/// The online part: Unity Gaming Services. Every phone signs in anonymously (no account screen), gets a
/// display name, and posts its heights to two leaderboards: this week and all time. Each score carries the
/// climber's look (character, hat, backpack, charm, tag) so others see your banner. Everything here fails
/// softly: offline or not set up, the game plays on and the leaderboard screen says why.
public static class Online
{
    public const string Weekly = "weekly", AllTime = "alltime";

    public enum State { Off, Connecting, Ready, NotSetUp, Offline }
    public static State Status { get; private set; }
    public static string PlayerId => Status == State.Ready ? AuthenticationService.Instance.PlayerId : null;

    static Task<bool> connecting;

    /// The look a score was posted with (stored with the score as JSON).
    [Serializable]
    public class Look
    {
        public string c, h, b, ch, t;

        public static Look Mine() => new()
            { c = Progress.Character, h = Progress.Hat, b = Progress.Backpack, ch = Progress.Charm, t = Progress.Tag };

        public static Look From(string json)
        {
            if (string.IsNullOrEmpty(json)) return new Look();
            try { return JsonUtility.FromJson<Look>(json) ?? new Look(); }
            catch (Exception) { return new Look(); }
        }
    }

    /// One row on a leaderboard.
    public class Row
    {
        public int Rank;          // 1 = first
        public string Name, PlayerId;
        public int Meters;
        public Look Look;
    }

    // MARK: - Connecting

    /// Signs in (once) and makes sure the name is set. True when the leaderboards can be used.
    public static Task<bool> Connect()
    {
        if (Status == State.Ready) return Task.FromResult(true);
        if (Status == State.Connecting && connecting != null) return connecting;
        return connecting = ConnectNow();
    }

    static async Task<bool> ConnectNow()
    {
        Status = State.Connecting;
        try
        {
            if (string.IsNullOrEmpty(Application.cloudProjectId))
            {
                Status = State.NotSetUp;   // the game isn't linked to a Unity Cloud project yet
                return false;
            }
            if (UnityServices.State == ServicesInitializationState.Uninitialized) await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Status = State.Ready;
            await SyncName();
            return true;
        }
        catch (Exception e)
        {
            Status = IsSetupProblem(e) ? State.NotSetUp : State.Offline;
            Debug.LogWarning($"Online: {Status}: {e.Message}");
            return false;
        }
    }

    static bool IsSetupProblem(Exception e) =>
        e is LeaderboardsException le
            ? le.Reason is LeaderboardsExceptionReason.ProjectIdMissing or LeaderboardsExceptionReason.LeaderboardNotFound
            : e.Message.IndexOf("project", StringComparison.OrdinalIgnoreCase) >= 0;

    // MARK: - Names

    /// The name to show: the saved one, or a fresh cute one the first time.
    public static string MyName
    {
        get
        {
            if (string.IsNullOrEmpty(Progress.PlayerName))
            {
                Progress.PlayerName = Names.Random();
                Progress.Save();
            }
            return Progress.PlayerName;
        }
    }

    /// Saves a new name (already checked with Names.Problem) and sends it.
    public static async Task Rename(string name)
    {
        Progress.PlayerName = name;
        Progress.Save();
        await SyncName();
    }

    /// The services keep names without spaces (and add a "#1234" tag), so spaces travel as underscores.
    static async Task SyncName()
    {
        if (Status != State.Ready) return;
        string wanted = MyName.Replace(' ', '_');
        try
        {
            string current = await AuthenticationService.Instance.GetPlayerNameAsync();
            if (Display(current) != MyName) await AuthenticationService.Instance.UpdatePlayerNameAsync(wanted);
        }
        catch (Exception e) { Debug.LogWarning($"Online: name: {e.Message}"); }
    }

    /// "Fluffy_Otter#4821" → "Fluffy Otter".
    static string Display(string serviceName)
    {
        if (string.IsNullOrEmpty(serviceName)) return "";
        int hash = serviceName.LastIndexOf('#');
        return (hash > 0 ? serviceName[..hash] : serviceName).Replace('_', ' ');
    }

    // MARK: - Scores

    /// Posts a run's height to both boards. Each board keeps your best. Returns your weekly row (or null).
    public static async Task<Row> Submit(int meters)
    {
        if (meters <= 0 || !await Connect()) return null;
        var options = new AddPlayerScoreOptions
        {
            Metadata = new Dictionary<string, string>
            {
                ["c"] = Progress.Character, ["h"] = Progress.Hat, ["b"] = Progress.Backpack, ["ch"] = Progress.Charm, ["t"] = Progress.Tag,
            },
        };
        try
        {
            await LeaderboardsService.Instance.AddPlayerScoreAsync(AllTime, meters, options);
            return ToRow(await LeaderboardsService.Instance.AddPlayerScoreAsync(Weekly, meters, options));
        }
        catch (Exception e)
        {
            Fail(e);
            return null;
        }
    }

    /// The top of a board.
    public static async Task<List<Row>> Top(string board, int count = 50)
    {
        var rows = new List<Row>();
        if (!await Connect()) return null;
        try
        {
            var page = await LeaderboardsService.Instance.GetScoresAsync(board, new GetScoresOptions { Limit = count, IncludeMetadata = true });
            foreach (var entry in page.Results) rows.Add(ToRow(entry));
            return rows;
        }
        catch (Exception e)
        {
            Fail(e);
            return null;
        }
    }

    /// Your own row on a board, or null if you haven't posted there (yet, or this week).
    public static async Task<Row> Mine(string board)
    {
        if (!await Connect()) return null;
        try
        {
            return ToRow(await LeaderboardsService.Instance.GetPlayerScoreAsync(board, new GetPlayerScoreOptions { IncludeMetadata = true }));
        }
        catch (LeaderboardsException e) when (e.Reason == LeaderboardsExceptionReason.EntryNotFound)
        {
            return null;
        }
        catch (Exception e)
        {
            Fail(e);
            return null;
        }
    }

    static Row ToRow(LeaderboardEntry entry) => entry == null ? null : new Row
    {
        Rank = entry.Rank + 1,
        Name = Display(entry.PlayerName),
        PlayerId = entry.PlayerId,
        Meters = (int)entry.Score,
        Look = Look.From(entry.Metadata),
    };

    static void Fail(Exception e)
    {
        if (IsSetupProblem(e)) Status = State.NotSetUp;
        else if (e is LeaderboardsException { Reason: LeaderboardsExceptionReason.NoInternetConnection }) Status = State.Offline;
        Debug.LogWarning($"Online: {e.Message}");
    }
}

/// Player names: a cute random one to start, and simple rules for typed ones.
public static class Names
{
    static readonly string[] Adjectives =
    {
        "Brave", "Cozy", "Fluffy", "Sleepy", "Speedy", "Tiny", "Mighty", "Sunny", "Lucky", "Jolly", "Bouncy", "Clever",
        "Happy", "Sneaky", "Sparkly", "Snowy", "Cosmic", "Mossy", "Fuzzy", "Zippy", "Peppy", "Giggly", "Daring", "Comfy",
    };

    static readonly string[] Animals =
    {
        "Penguin", "Fox", "Panda", "Frog", "Bunny", "Koala", "Otter", "Hamster", "Owl", "Duck", "Shiba", "Axolotl",
        "Kitten", "Puppy", "Raccoon", "Tanuki", "Yeti", "Dragon", "Mochi", "Climber", "Goat", "Gecko", "Moth", "Badger",
    };

    public static string Random() =>
        Adjectives[UnityEngine.Random.Range(0, Adjectives.Length)] + " " + Animals[UnityEngine.Random.Range(0, Animals.Length)];

    public const int MinLength = 3, MaxLength = 16;

    // Not a complete filter, just the obvious ones (English and Dutch). Matched anywhere in the name.
    static readonly string[] Blocked =
    {
        "fuck", "shit", "cunt", "dick", "cock", "pussy", "bitch", "whore", "slut", "nigg", "fag", "rape", "nazi", "hitler",
        "kanker", "kut", "lul", "hoer", "tering", "tyfus", "mongool", "neuk", "porn", "sex", "penis", "vagina", "anus",
    };

    /// Cleans up a typed name: trims it and squeezes repeated spaces.
    public static string Clean(string name) =>
        string.Join(" ", (name ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

    /// What's wrong with a name, or null when it's fine.
    public static string Problem(string name)
    {
        if (name.Length < MinLength) return $"At least {MinLength} letters";
        if (name.Length > MaxLength) return $"At most {MaxLength} letters";
        foreach (char ch in name)
            if (!char.IsLetterOrDigit(ch) && ch != ' ') return "Only letters, numbers and spaces";
        string squashed = name.ToLowerInvariant().Replace(" ", "");
        foreach (var word in Blocked)
            if (squashed.Contains(word)) return "Please pick another name";
        return null;
    }
}
