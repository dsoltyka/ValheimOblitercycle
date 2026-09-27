# Building

The project imports shared MSBuild props from the [ValheimModBuild](https://github.com/dsoltyka/ValheimModBuild) repo, which must be cloned next to this one:

```
<parent>/
  ValheimModBuild/
  ValheimOblitercycle/
```

Then:

```
dotnet build                                          # build + copy DLL into your Thunderstore profile
dotnet build -c Release -p:ThunderstorePack=true      # also write thunderstore/manifest.json and the upload zip
```

Paths to the game and the BepInEx profile are configured once in `ValheimModBuild/Valheim.Local.props` (see that repo's README).

## How it works

The Obliterator is the game's `Incinerator`. Pulling the lever sends an RPC to the peer that owns the object, which runs the `Incinerate` coroutine: lever animation, a random delay, then it runs its conversion list, turns whatever is left into coal, and reports back to the puller.

`IncineratorPatch` prefixes `Incinerate` and, unless the mode is Vanilla, substitutes its own coroutine. That coroutine repeats the animation, delay and validity checks verbatim, runs the vanilla conversions if enabled, then hands the inventory to `Recycler`.

`Recycler.Build` makes a plan without touching anything: for each stack it finds the first enabled recipe that produces the item (skipping "only one ingredient" recipes, whose inputs are ambiguous). Per requirement it computes `amount * stack / recipe output`, adds the per-level upgrade costs from `Piece.Requirement.GetAmount` when enabled, filters to trophies in Trophies mode or applies the yield percentage to non-trophies in Materials mode, and records the totals by prefab name. Items with no usable recipe follow the `BaseItems` rule. `Recycler.Apply` removes the consumed stacks, adds the outputs and coal back through `Inventory.AddItem`, and drops anything that does not fit beside the Obliterator with `ItemDrop.DropItem`.

All settings are `IsAdminOnly`, so Jötunn's synchronization pushes the server's values to clients, and the plugin declares `NetworkCompatibility(EveryoneMustHaveMod)`. Only a dedicated server watches its config file for live edits; clients would just be overwritten by the sync.

## Releasing

1. Bump `<Version>` in `Oblitercycle.csproj` and add a `CHANGELOG.md` entry.
2. Run the pack command above. It regenerates `thunderstore/manifest.json` from the csproj and writes `thunderstore/Oblitercycle-<version>.zip`.
3. Commit, tag `v<version>`, push, and upload the zip through the normal Thunderstore upload form under the `dsoltyka` team.

Thunderstore renders `README.md` from inside the zip as the mod page, so keep that file user-facing. Anything between the `github-only` markers is stripped from the packaged copy, and images must use absolute URLs.
