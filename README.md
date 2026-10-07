# Vent Freeze Fix

An [Oxygen Not Included](https://www.klei.com/games/oxygen-not-included) mod for a save/load quirk with liquid vents.

Status: **work in progress**, not yet tested in-game.

## The problem

A packet of 1 kg or so leaving a liquid vent normally freezes into debris, because the game only forms a natural tile when the freezing liquid holds at least 80% of its element's default mass. A packet that leaves the vent on the very tick a save is written is different: on load it exists as a plain liquid cell, and the sim then freezes it into a tile over the vent. When that happens in the cell where the vent's debris pile sits, the pile goes with it and automation built on the pile breaks.

## What the mod does

Every liquid vent gets a small watcher that runs on the game's 200 ms tick for the first two seconds after the game loads and looks at two cells: the vent cell, where a packet is injected when the cell below is solid, and the landing cell, the last open cell above the first solid below the vent, where a falling packet enters the sim. The watcher remembers what each cell held the previous tick. When a cell turns into a natural solid tile although the liquid in it the tick before was under the 80% rule, that tile could not have formed in normal play, so it is replaced with debris of the same element at the tile's full mass, pile included. A tile formed from a real pool, or a cell that was already solid, is left alone. Each conversion is logged. After those two seconds the watcher removes itself from the scheduler, since the problem only occurs on load. Because the tile forms in the first sim frame after a load, before any tick, the watcher stores a snapshot of its two cells in the save when the game is saved and uses that as the previous tick for its first comparison.

The mod never touches the sim's internals or the save: it reads public grid data and uses the game's own replace-element and spawn-resource calls.

## Debug mode

The bug needs a save written in a 200 ms window, so it is hard to reproduce by hand. Turn on **Debug mode** in the mod's options and load a game: a button "Save on next vent emit" appears at the top left of the game screen. Click it to arm it; the next time any liquid vent emits a packet, the game writes `VentFreezeRepro.sav` next to the current save at the end of that frame, which is after the vent pushed the packet out and before the next sim frame can process it. All liquid piping ticks in step, so one vent stands for all of them. Load that file and unpause to see the tile form, and the watcher convert it; the log line "[VentFreezeFix] Debug save written" names the element and vent.

## Installing

As a local mod:

1. Download `VentFreezeFix-<version>.zip` from the [latest release](https://github.com/isochronous/vent-freeze-fix/releases/latest).
2. Extract it into a new folder named `VentFreezeFix` inside the game's local mods folder, so that `mod.yaml` ends up directly inside it (create `local` if it does not exist):
   - Windows: `Documents\Klei\OxygenNotIncluded\mods\local\VentFreezeFix`
   - Linux: `~/.config/unity3d/Klei/Oxygen Not Included/mods/local/VentFreezeFix`
3. Enable it in the game's Mods menu and restart.

## Building

```
git clone --recurse-submodules https://github.com/isochronous/vent-freeze-fix.git
dotnet build vent-freeze-fix/src/VentFreezeFix -c Release
```
