// Notes when a run starts (server time, in a value the player can read but not change), so SubmitScore can
// tell whether a height was possible in the time since.
const { DataApi } = require("@unity-services/cloud-save-1.4");

module.exports = async ({ context }) => {
  const cloudSave = new DataApi(context);
  await cloudSave.setProtectedItem(context.projectId, context.playerId, { key: "runStartedAt", value: Date.now() });
  return { ok: true };
};
