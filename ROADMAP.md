# ScrollyRoad roadmap

Ideas collected so far, grouped into phases. Each phase builds on the one before it.

## Done
- Climbing feel: rubber band, two-hand hand-off, checkpoints per zone, stamina
- 5 columns with sideways swipes, coins, mobs, fall animation
- Rocket to new planets, ground rocket for continues
- Shop basics: extra lives and continues (coins)
- Sound (chiptune synth) and haptics on iOS and Android
- First Animal Crossing-style UI prototype (cards, pills, zone stamps)
- Stamp book with SVG stamps, fall damage, two-step game over
- UI plan (all 22 items, 2026-10-25): slim HUD, best-height flag, message queue, ghost-hand onboarding, coin flights, counting results, title with one main action, patterned menus, stamp book pages, settings (sound, vibration, screen shake)
- Interactive first-launch tutorial (hold, swipe down, hand-off x3, then sideways), replayable from Settings; stamp book pages show planet effects with icons

## Phase 1: Gameplay depth (no servers needed) — first version built
Each planet gets its own status effect, and the columns get obstacles.
Earth is always normal. The planets after it get a random effect (never the same twice in a row), each with its own colors, and the launch pad shows which one is next.
- **Low gravity:** the pull is lighter, but you also slide back further when you let go.
- **Slippery ice:** while you hang, you slowly slip downward.
- **Heat:** stamina drains faster. Collect water drops (instead of coins) or rest in shade spots to cool down.
- **Waterfalls:** a column with falling water of a certain length that pushes you down. Swipe out of it.
- **Blocking blocks:** a column is blocked for a stretch, so you have to swipe around it.
- **Random free heart:** a rare pickup, not every run.

## Boss planets: first version built (2026-10-25)
- Every 4 to 5 planets. A 40 m "Meteor Gate" warm-up, then the 120 m "Meteor Storm" straight to the rocket: no checkpoints, no mobs, two rest ledges.
- Meteors: a red mark with closing brackets shows where one lands (1.3 s warning). They come faster higher up, sometimes two at once.
- Reward: +50 coins for reaching the rocket, plus a Boss page in the stamp book (shield stamps).
- Settings has two alpha test controls, "Alpha: boss next" and "Alpha: new daily gift" (remove both before release).
- Still open: more boss mechanics, its own music, a bigger reward (gems).

## Phase 2: Customization and progression: first version built (2026-10-26)
- Closet: 7 characters (Climber, Cat, Frog, Penguin for coins; Robot, Astronaut, Ninja through achievements) and 8 hats (6 for coins; Viking helmet and Crown through achievements), with a live 3D preview.
- Achievements: collect 10 stamps, reach 500 m, fill a stamp book page, beat a boss planet, reach 1000 m.
- Daily gift: a 3D gift you tap 3 times; each tap may upgrade it (Rare, Mega, Ultra, Legendary) for a better prize, and opening it is always a confetti party. Free, so legal. Before launch: add real items to the higher tiers.
- Legal line (BE/NL/EU): anything random is free; anything paid is a fixed, visible item. No paid spins or boxes, and no ad-for-spin.
- Calm planets (no effect) now mix in between the effect planets.

## Phase 2 (rest): still to do
- Unlockables: hats, characters, rockets, profile tag designs
- Achievements, some of which unlock skins you can only get through that achievement
- A second currency: gems 💎 (rare, also purchasable later)
- A customization screen ("closet")

## Phase 3: Wheel of fortune
- One free spin per day. Watch an ad for an extra spin.
- Prizes: hats, tag designs, characters, rockets, a respin, 100 coins, a few gems
- ⚠️ Spins paid for with real money (or with purchased gems) count as loot boxes. Belgium bans them, the Netherlands looks at them critically, and Apple and Google require the odds to be shown. Safer: keep the wheel free or ad-based only, and sell items directly in a shop.

## Phase 4: Online
- Player accounts (Sign in with Apple and Google)
- A global leaderboard with a profile banner for each player: tag design, a profile picture showing off rare skins, and their high score. Tags already exist in the closet (the background behind your character); the banner can reuse TagArt
- Progress saved in the cloud, so it carries across devices
- Likely technology: Unity Gaming Services (Authentication, Leaderboards, Cloud Save), which fits Unity and has a free tier

## Phase 5: Monetization
- Rewarded ads only (an extra spin, a free continue). No forced ads.
- In-app purchases: gem packs
- Supporter pack: removes ads, plus bonus gems and an exclusive supporter hat and tag

## Later / open
- Daily challenge: everyone gets the same planets that day (fair for the leaderboard)
- A nicer transition for the rocket flight between planets
- Mobs that move up and down within a column
- Final game name (ScrollyRoad is a working title)
