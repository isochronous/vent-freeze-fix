namespace VentFreezeFix
{
	/// <summary>
	/// The mod's text, registered under the game's STRINGS tree by Isochronous.Translations
	/// (ModStrings.VENTFREEZEFIX.X is the key STRINGS.VENTFREEZEFIX.X) and replaced from
	/// translations/&lt;locale&gt;.po. Code reads the fields when it needs the text, so a translation
	/// loaded later is what the player sees. The option attributes name these keys, which PLib
	/// resolves through the string table when it builds the options screen.
	/// </summary>
	public static class ModStrings
	{
		public static class VENTFREEZEFIX
		{
			public static class OPTIONS
			{
				public static class CATEGORY
				{
					public static LocString DEVELOPER = "Developer options";
				}

				public static class INSULATE_VENTS
				{
					public static LocString NAME = "Insulate vent storage";
					public static LocString TOOLTIP = "Prevents the tile inside the vent. The packet a vent is holding is kept sealed and insulated, as it is in other buildings that hold packets, so the game cannot solidify it into the vent's cell after a load. Turn off if a game update fixes this on its own and this mod has not been updated yet. Needs a restart.";
				}

				public static class REPAIR_ON_LOAD
				{
					public static LocString NAME = "Repair tiles after loading";
					public static LocString TOOLTIP = "For the first second after a load, a natural tile that formed at or under a vent out of less liquid than a tile needs is dug up and dropped as debris at its full mass. Takes effect on the next load.";
				}

				public static class DEBUG_MODE
				{
					public static LocString NAME = "Enable debug mode";
					public static LocString TOOLTIP = "Logs in detail what the mod sees: the snapshots restored from the save, every cell the watcher checks on every tick with its verdict, and the packets the vents emit with their temperatures. Takes effect when a game is loaded.";
				}

				public static class REPRO_BUTTON
				{
					public static LocString NAME = "Add button to reproduce bug conditions";
					public static LocString TOOLTIP = "Adds a button to the game screen that writes a save file (VentFreezeRepro.sav, next to the current save) on the tick a liquid vent emits, which is the only moment the bug can be captured. Takes effect when a game is loaded.";
				}
			}

			/// <summary>The button the "Add button to reproduce bug conditions" option puts on the game screen.</summary>
			public static class DEBUG_BUTTON
			{
				public static LocString TEXT = "Save on next vent emit";
				// Format arg: {0} the save file name.
				public static LocString TOOLTIP = "Vent Freeze Fix debug: arms a save that is written on the tick a liquid vent emits a packet, to {0} next to the current save.";
				public static LocString ARMED = "Armed: waiting for a vent to emit...";
				// Format arg: {0} the save file name.
				public static LocString SAVED = "Saved {0}";
			}
		}
	}
}
