# Changelog

## Unreleased

- First version.
- Insulate vent storage (option, default on, needs a restart): the packet a liquid vent holds is kept sealed and insulated, so the game cannot solidify it into the vent's cell after a load. This is the cause of the tile inside the vent; the storage modifiers are Sgt_Imalas's fix.
- Repair tiles after loading (option, default on): for the first second after a load, a natural tile that formed at or under a liquid vent out of less liquid than a tile needs is dug up through the game's own dig path and dropped as debris at its full mass. This covers the tile the sim's own load makes out of a super-cooled puddle under the vent, which a mod cannot prevent.
- Developer options: "Enable debug mode" logs in detail what the watcher sees; "Add button to reproduce bug conditions" adds a button that writes a save on the tick a liquid vent emits.
