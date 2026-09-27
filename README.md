# Oblitercycle

The Obliterator recycles instead of just burning.

Trophies gate a lot of recipes, especially modded ones, and farming them is a chore. With Oblitercycle, pulling the lever on a crafted item hands you back the trophies its recipe used, and only the rest of the item goes up as coal. That is the default. If you want more, a second mode breaks items down into every material in their recipe.

<!-- Screenshot goes here once docs/screenshot.png exists:
![Trophies recovered from an obliterated weapon](https://raw.githubusercontent.com/dsoltyka/ValheimOblitercycle/main/docs/screenshot.png)
-->

## Modes

| `Mode` | Crafted items (anything with a recipe) | Everything else |
| --- | --- | --- |
| `Trophies` (default) | Trophies from the recipe come back; the item burns to coal | See `BaseItems` |
| `Materials` | Every recipe material comes back, scaled by `MaterialYieldPercent` | See `BaseItems` |
| `Vanilla` | Coal | Coal |

* **Upgrades count.** An item at quality 3 returns what its upgrades cost too (`RecoverUpgrades`).
* **Loose trophies are safe.** A trophy dropped straight into the Obliterator is left alone (`KeepLooseTrophies`).
* **Base items** (raw materials, cooked food, creature drops, anything with no recipe) burn to coal as in vanilla, or are left untouched with `BaseItems = Keep`.
* The game's own item-to-item conversions still run first, so anything the base game or another mod converts keeps working.
* Whatever does not fit back into the Obliterator is dropped next to it. Nothing is lost.

Items made by recipes that accept "any one of" several ingredients cannot say which ingredient was used, so they are treated as base items.

## Installation

Install with r2modman / Thunderstore Mod Manager, or drop `Oblitercycle.dll` into `BepInEx/plugins`.

Requires [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) and [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/).

**Server and client.** The Obliterator does its work on whichever player or server owns it at the time, so everyone must have the mod, and the server enforces that. The configuration is admin-only and synced from the server, so every lever pull behaves the same for everyone. In single player your own config applies.

## Configuration

`BepInEx/config/dsoltyka.Oblitercycle.cfg`. On a server, edit the server's copy; clients receive its values on connect. Admins can also change values in-game with [Configuration Manager](https://thunderstore.io/c/valheim/p/Azumatt/Official_BepInEx_ConfigurationManager/).

| Setting | Default | Meaning |
| --- | --- | --- |
| `Mode` | Trophies | `Trophies`, `Materials` or `Vanilla` (see above) |
| `BaseItems` | Coal | What happens to items without a recipe: `Coal` or `Keep` |
| `KeepLooseTrophies` | true | Never burn trophies placed in the Obliterator directly |
| `RecoverUpgrades` | true | Also return what upgrading the item cost |
| `MaterialYieldPercent` | 100 | Materials mode: share of non-trophy materials returned, rounded down. Trophies always return in full |
| `VanillaConversions` | true | Run the Obliterator's built-in conversions first |

## Source

Code is on GitHub at [dsoltyka/ValheimOblitercycle](https://github.com/dsoltyka/ValheimOblitercycle).

<!-- github-only -->
## Building

See [BUILDING.md](BUILDING.md). This section is stripped from the copy of the README that ships to Thunderstore.
<!-- /github-only -->

## License

MIT. See [LICENSE](https://github.com/dsoltyka/ValheimOblitercycle/blob/main/LICENSE).
