// Posts a run's height to every leaderboard, after checking it's possible: no faster than about 8 m per
// second since StartRun (a quick human climbs 6 to 8 while climbing, and rests, waits and rocket flights
// only add time), plus some headroom. Players can't post scores themselves (see the access policy), so
// this is the only way onto the boards.
const { LeaderboardsApi } = require("@unity-services/leaderboards-1.1");
const { DataApi } = require("@unity-services/cloud-save-1.4");

const METERS_PER_SECOND = 8;
const HEADROOM_METERS = 60;
const MAX_METERS = 100000;
const BOARDS = ["weekly", "alltime", "league"];
const LOOK_KEYS = ["c", "h", "b", "ch", "t"];

module.exports = async ({ params, context, logger }) => {
  const meters = Math.floor(Number(params.meters));
  if (!Number.isFinite(meters) || meters <= 0 || meters > MAX_METERS) throw new Error(`Not a height: ${JSON.stringify(params.meters)}`);

  const cloudSave = new DataApi(context);
  const saved = await cloudSave.getProtectedItems(context.projectId, context.playerId, ["runStartedAt"]);
  const started = saved.data.results.find((item) => item.key === "runStartedAt");
  if (!started) throw new Error("No run started");
  const seconds = (Date.now() - Number(started.value)) / 1000;
  if (meters > seconds * METERS_PER_SECOND + HEADROOM_METERS) {
    logger.warning(`Refused ${meters} m after ${Math.round(seconds)} s`);
    throw new Error("Too high for the time climbed");
  }

  // Only known look fields, as short strings.
  const look = {};
  for (const key of LOOK_KEYS) {
    const value = params.look && params.look[key];
    if (typeof value === "string") look[key] = value.slice(0, 32);
  }

  const leaderboards = new LeaderboardsApi(context);
  const ranks = {};
  for (const board of BOARDS) {
    try {
      const result = await leaderboards.addLeaderboardPlayerScore(context.projectId, board, context.playerId, { score: meters, metadata: look });
      ranks[board] = result.data.rank + 1;
    } catch (error) {
      logger.info(`Skipped ${board}: ${error.message}`);   // e.g. no league board in this project
    }
  }
  return ranks;
};

module.exports.params = {
  meters: "NUMERIC",
  look: "JSON",
};
