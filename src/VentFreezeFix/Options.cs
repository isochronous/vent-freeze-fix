using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace VentFreezeFix
{
	[JsonObject(MemberSerialization.OptIn)]
	[ConfigFile(SharedConfigLocation: true)]
	public sealed class Options
	{
		[Option("Insulate vent storage", "Prevents the tile inside the vent. The packet a vent is holding is kept sealed and insulated, as it is in other buildings that hold packets, so the game cannot solidify it into the vent's cell after a load. Turn off if a game update fixes this on its own and this mod has not been updated yet. Needs a restart.")]
		[RestartRequired]
		[JsonProperty]
		public bool InsulateVents { get; set; } = true;

		[Option("Repair tiles after loading", "For the first second after a load, a natural tile that formed at or under a vent out of less liquid than a tile needs is dug up and dropped as debris at its full mass. Takes effect on the next load.")]
		[JsonProperty]
		public bool RepairOnLoad { get; set; } = true;

		[Option("Debug mode", "Adds a button to the game screen that writes a save file (VentFreezeRepro.sav, next to the current save) on the tick a liquid vent emits, which is the only moment the bug can be captured. Takes effect when a game is loaded.")]
		[JsonProperty]
		public bool DebugMode { get; set; } = false;

		public static Options Load()
		{
			return POptions.ReadSettings<Options>() ?? new Options();
		}
	}
}
