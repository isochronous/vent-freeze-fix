# Vent Freeze Fix

An [Oxygen Not Included](https://www.klei.com/games/oxygen-not-included) mod for a save/load bug with liquid vents.

## The problem

Liquid pushed through pipes in small packets can be far below its freezing point without the pipes breaking, and a liquid vent dripping it into a cold room makes debris: the game only forms a natural tile when freezing liquid holds at least 80% of its element's default mass, and a packet of a kilogram or two is nowhere near that. Loading a save that was written while such a packet was in flight breaks the rule in two places:

- **Inside the vent.** The packet a vent is about to emit is an item in the vent's storage, and the game's default storage leaves its items temperature simulated. In running play the vent emits the packet before the sim gets to it. After a load the packet sits in the vent through the whole load, the sim reports that the item is past its phase transition, and the game's handler for that turns the item into cell mass of the new phase at the vent's cell, with no mass rule at all: a natural tile of the packet's mass entombs the vent, and the packet is gone. Whether it happens depends on a race with the vent's next emit, so it varies from load to load and rarely happens at 3x speed.
- **Under the vent.** A packet that has landed but not yet frozen is a super-cooled liquid cell when the save is written. The sim's own load restores such a cell as its solid form in place, before the game gets a turn. If the vent's debris pile sits in that cell, the pile is absorbed into the tile and automation built on the pile breaks.

## What the mod does

Both fixes are options, on by default.

**Insulate vent storage** gives each liquid vent's storage the game's insulated storage modifiers (hide, seal, insulate), as other buildings that hold element packets have. A sealed item is skipped by the phase-transition handler and an insulated one is not temperature simulated, so the packet stays in the vent and is emitted as usual, where the sim's normal rules turn it into debris. This prevents the tile inside the vent. The storage modifiers are Sgt_Imalas's fix, which several players confirmed; the option exists so the mod can be switched off if a game update fixes this on its own before the mod is updated. Needs a restart.

**Repair tiles after loading** gives each liquid vent a watcher that runs on the game's 200 ms tick for the first second of running time after a load and then removes itself. It looks at the vent cell and the landing cell, the last open cell above the first solid below the vent, remembers what each held the tick before, and stores that snapshot in the save so the first comparison after a load has a previous tick. When a cell holds a natural tile although the liquid in it the tick before was under the 80% rule, the tile could not have formed in normal play, so the watcher asks the sim to dig it, the game's own way of turning a tile into debris, and drops the mass the sim reports as debris in full (a dug tile normally yields half). A tile formed from a real pool, or a cell that was already solid, is left alone. Each conversion is logged. This covers the tile under the vent, which a mod cannot prevent, and would also catch the tile inside the vent if the first option were off.

The mod does not touch the sim or the save beyond the watcher's own snapshot; it reads public grid data and uses the game's own dig path.

## Developer options

The bug needs a save written in a 200 ms window, so it is hard to reproduce by hand. Turn on **Add button to reproduce bug conditions** and load a game: a button "Save on next vent emit" appears at the top left of the game screen. Click it to arm it; the next time any liquid vent emits a packet, the game writes `VentFreezeRepro.sav` next to the current save at the end of that frame. Load that file and unpause to see the tiles form and the watcher dig them.

**Enable debug mode** logs in detail what the mod sees: the restored snapshots, every cell the watcher looks at on every tick with its verdict, and the packets the vents emit with their temperatures. Without it only the repairs themselves are logged.

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
