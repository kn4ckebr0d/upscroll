# Online setup (Unity Gaming Services)

Project: "ScrollyRoad 3D" on cloud.unity.com, environment **production**.

## Leaderboards (done)
- `weekly`: highest first, keep best, resets every week
- `alltime`: highest first, keep best, never resets
- `league` (optional, not created yet): like `weekly`, with **buckets of 40**. The League tab appears once it exists.

## Cloud Code scripts (done)
The scripts live in `ScrollyRoadUnity/Assets/CloudCode/`. The website keeps its own copy, so after changing a file,
paste it into the website (Cloud Code → Scripts), save and **publish**.
- `StartRun`: no parameters. Notes the run's start time on the server.
- `SubmitScore`: parameters **meters** (NUMERIC, required) and **look** (JSON, optional). These must be added in the
  website's Parameters section, or the script receives nothing. Refuses heights faster than 8 m/s + 60 m, then posts
  to every board.

## Last step: stop players posting scores directly (to do)
Until this is on, someone technical could still post to the leaderboards without going through `SubmitScore`.
1. On cloud.unity.com: Administration → Service accounts → create one (e.g. "upscroll-cli") with the roles
   **Project Resource Policy Editor**, **Project Resource Policy Reader** and **Unity Environments Viewer**
   for this project. Create a key and keep the key ID and secret somewhere safe.
2. Install the Unity Gaming Services CLI (needs Node.js): `npm install -g ugs`
3. In this folder, run:
   ```
   ugs login
   ugs config set project-id dfd8a89c-2654-417e-8427-25e432e33608
   ugs config set environment-name production
   ugs access upsert-project-policy project-access-policy.json
   ```
4. Play a run on your phone: your rank should still show on the results (it goes through `SubmitScore`, which
   is allowed). Posting directly is now refused.
