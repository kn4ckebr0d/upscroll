using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Exceptions;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

/// The online part: Unity Gaming Services. Every phone signs in anonymously (no account screen), gets a
/// display name, and posts its heights to the leaderboards: this week, all time, and your league (about 40
/// players, a weekly board split into buckets). A server script checks each height first. Each score carries
/// the climber's look (character, hat, backpack, charm, tag) so others see your banner. Everything here fails
/// softly: offline or not set up, the game plays on and the leaderboard screen says why.
public static class Online
{
    public const string Weekly = "weekly", AllTime = "alltime", League = "league";

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
            ? le.Reason is LeaderboardsExceptionReason.ProjectIdMissing
            : e.Message.IndexOf("project id", StringComparison.OrdinalIgnoreCase) >= 0;

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

    /// What a run's height did: your rank this week, and in your league (0 when there's none).
    public class RunResult
    {
        public int WeeklyRank, LeagueRank;
    }

    /// Boards that don't exist in the cloud project (the league is optional): asked once, then skipped.
    static readonly HashSet<string> missing = new();
    public static bool HasBoard(string board) => !missing.Contains(board);

    /// Tells the server a run starts now, so it can tell later whether a height was possible in the time.
    public static async void StartRun()
    {
        if (Status != State.Ready) return;
        try { await CloudCodeService.Instance.CallEndpointAsync("StartRun", new Dictionary<string, object>()); }
        catch (Exception e) { Debug.LogWarning($"Online: start run: {e.Message}"); }
    }

    /// Posts a run's height. The server (Cloud Code, "SubmitScore") checks it's possible in the time since
    /// StartRun and posts it to every board, each keeping your best. Until that script is deployed, the
    /// game posts directly.
    public static async Task<RunResult> Submit(int meters)
    {
        if (meters <= 0 || !await Connect()) return null;
        var look = new Dictionary<string, string>
        {
            ["c"] = Progress.Character, ["h"] = Progress.Hat, ["b"] = Progress.Backpack, ["ch"] = Progress.Charm, ["t"] = Progress.Tag,
        };
        try
        {
            var ranks = await CloudCodeService.Instance.CallEndpointAsync<Dictionary<string, int>>("SubmitScore",
                new Dictionary<string, object> { ["meters"] = meters, ["look"] = look });
            foreach (var board in new[] { Weekly, AllTime, League })
                if (!ranks.ContainsKey(board)) missing.Add(board);
            return new RunResult { WeeklyRank = ranks.GetValueOrDefault(Weekly), LeagueRank = ranks.GetValueOrDefault(League) };
        }
        catch (CloudCodeException e) when (e.Reason == CloudCodeExceptionReason.NotFound)
        {
            return await SubmitDirectly(meters, look);   // the check isn't deployed yet
        }
        catch (CloudCodeException e) when (e.Reason == CloudCodeExceptionReason.ScriptError)
        {
            Debug.LogWarning($"Online: height refused: {e.Message}");
            return null;
        }
        catch (Exception e)
        {
            Fail(e);
            return null;
        }
    }

    static async Task<RunResult> SubmitDirectly(int meters, Dictionary<string, string> look)
    {
        var options = new AddPlayerScoreOptions { Metadata = look };
        var result = new RunResult();
        foreach (var board in new[] { AllTime, League, Weekly })
        {
            if (missing.Contains(board)) continue;
            try
            {
                var entry = await LeaderboardsService.Instance.AddPlayerScoreAsync(board, meters, options);
                if (board == Weekly) result.WeeklyRank = entry.Rank + 1;
                if (board == League) result.LeagueRank = entry.Rank + 1;
            }
            catch (Exception e)
            {
                Fail(e, board);
                if (Status != State.Ready) return null;
            }
        }
        return result;
    }

    /// The top of a board (for the league: your league). Empty for a league you haven't joined yet
    /// (you join with your first height of the week); null when it can't be read.
    public static async Task<List<Row>> Top(string board, int count = 50)
    {
        var rows = new List<Row>();
        if (missing.Contains(board) || !await Connect()) return null;
        try
        {
            var page = await LeaderboardsService.Instance.GetScoresAsync(board, new GetScoresOptions { Limit = count, IncludeMetadata = true });
            foreach (var entry in page.Results) rows.Add(ToRow(entry));
            return rows;
        }
        catch (LeaderboardsException e) when (e.Reason is LeaderboardsExceptionReason.ScoreSubmissionRequired or LeaderboardsExceptionReason.EntryNotFound)
        {
            return rows;
        }
        catch (Exception e)
        {
            Fail(e, board);
            return null;
        }
    }

    /// Your own row on a board, or null if you haven't posted there (yet, or this week).
    public static async Task<Row> Mine(string board)
    {
        if (missing.Contains(board) || !await Connect()) return null;
        try
        {
            return ToRow(await LeaderboardsService.Instance.GetPlayerScoreAsync(board, new GetPlayerScoreOptions { IncludeMetadata = true }));
        }
        catch (LeaderboardsException e) when (e.Reason is LeaderboardsExceptionReason.EntryNotFound or LeaderboardsExceptionReason.ScoreSubmissionRequired)
        {
            return null;
        }
        catch (Exception e)
        {
            Fail(e, board);
            return null;
        }
    }

    /// Up to `count` players just above you on a board, closest first: the ones to beat next. If you're not on
    /// the board yet, the lowest ones on it.
    public static async Task<List<Row>> Rivals(string board, int count)
    {
        if (missing.Contains(board) || !await Connect()) return null;
        var rivals = new List<Row>();
        try
        {
            var range = await LeaderboardsService.Instance.GetPlayerRangeAsync(board, new GetPlayerRangeOptions { RangeLimit = count, IncludeMetadata = true });
            int myRank = int.MaxValue;
            foreach (var entry in range.Results)
                if (entry.PlayerId == PlayerId) myRank = entry.Rank;
            foreach (var entry in range.Results)
                if (entry.Rank < myRank) rivals.Add(ToRow(entry));
        }
        catch (LeaderboardsException e) when (e.Reason is LeaderboardsExceptionReason.EntryNotFound or LeaderboardsExceptionReason.ScoreSubmissionRequired)
        {
            var top = await Top(board == League ? Weekly : board, 50);
            if (top == null) return null;
            rivals.AddRange(top.GetRange(Math.Max(0, top.Count - count), Math.Min(count, top.Count)));
        }
        catch (Exception e)
        {
            Fail(e, board);
            return null;
        }
        rivals.RemoveAll(r => r.PlayerId == PlayerId);
        rivals.Sort((a, b) => a.Meters.CompareTo(b.Meters));
        if (rivals.Count > count) rivals.RemoveRange(count, rivals.Count - count);
        return rivals;
    }

    static Row ToRow(LeaderboardEntry entry) => entry == null ? null : new Row
    {
        Rank = entry.Rank + 1,
        Name = Display(entry.PlayerName),
        PlayerId = entry.PlayerId,
        Meters = (int)entry.Score,
        Look = Look.From(entry.Metadata),
    };

    static void Fail(Exception e, string board = null)
    {
        if (e is LeaderboardsException { Reason: LeaderboardsExceptionReason.LeaderboardNotFound } && board != null)
        {
            missing.Add(board);   // e.g. no league board in this project: carry on without it
            return;
        }
        if (IsSetupProblem(e)) Status = State.NotSetUp;
        else if (e is LeaderboardsException { Reason: LeaderboardsExceptionReason.NoInternetConnection }
                 || e is CloudCodeException { Reason: CloudCodeExceptionReason.NoInternetConnection }) Status = State.Offline;
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
