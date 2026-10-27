# The item sheet

All characters, hats, backpacks, charms and tags live in one spreadsheet: `ScrollyRoadUnity/Assets/Resources/Items.csv`.
Open it in Numbers, Excel or Google Sheets, edit, and save it as CSV again. The game reads it at startup.
One row is one item. The order of the rows is the order in the closet.

| Column | What to fill in |
|---|---|
| `id` | A short name without spaces, like `blackcat`, unique across the whole sheet (only `none` may repeat). Never change it once players own the item. |
| `type` | `character`, `hat`, `backpack`, `charm` or `tag` |
| `name` | What players see, like `Black Cat` |
| `rarity` | `common`, `rare`, `mega`, `ultra` or `legendary` |
| `unlock` | How you get it: `free`, `coins`, `achievement` or `gift` (gift = only from the daily gift) |
| `price` | Coins, when `unlock` is `coins` |
| `achievement` | When `unlock` is `achievement`: `stamps10`, `height500`, `height1000`, `page` or `boss` |
| `in_gift` | `yes` if it can come out of the daily gift |
| `base` | Which 3D model to use: the id of any model in `Cosmetics.cs` or the `Cosmetics.Candidates*.cs` files (usually the same as the row's `id`). The candidates you didn't pick are still there as models, so you can add one later with just a row. |
| `legs` `body` `arms` `hands` `head` | Optional colors (hex like `2B2B33`) to recolor a character. Leave empty to keep the model's colors. For a **tag**: `legs` = top color, `body` = bottom color, `arms` = pattern color. |
| `notes` | Anything for yourself; the game ignores it |

## The daily gift and rarity
- Common gift: a heart (extra life), or coins when the hearts are full.
- Rare gift: a common item. Mega gift: a rare item. Ultra gift: a mega item. Legendary gift: an ultra or legendary item.
- Only items with `in_gift` = `yes` that the player doesn't own yet can drop. If none are left at that rarity, it falls back to a lower rarity, then to coins.
- Keep achievement items at `in_gift` = `no` so they stay exclusive.

## Adding things
- **One of the unpicked candidates:** add a row with its id as `base`. The overview page (Closet Candidates) shows them all.
- **A new character in other colors:** add a row with an existing `base` and your colors. No code needed.
- **A new tag in other colors:** same idea, e.g. `Night Sakura` uses the `sakura` pattern with dark colors. No code needed.
- Tags are the background behind your character in the closet, and later your banner on the leaderboard.
- **A new shape** (a new hat, backpack or charm, or a character with new ears or a helmet): ask for the model to be built, then add its row.
- Charms hang on the backpack, so they only show when a backpack is worn.
- **Legal:** everything for sale (`coins`) is a fixed item you see first. Only the free gift is random.
