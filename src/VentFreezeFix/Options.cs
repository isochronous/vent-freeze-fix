using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace VentFreezeFix
{
	[JsonObject(MemberSerialization.OptIn)]
	[ConfigFile(SharedConfigLocation: true)]
	public sealed class Options
	{
		[Option("Debug mode", "Adds a button to the game screen that writes a save file (VentFreezeRepro.sav, next to the current save) on the tick a liquid vent emits, which is the only moment the bug can be captured. Takes effect when a game is loaded.")]
		[JsonProperty]
		public bool DebugMode { get; set; } = false;

		public static Options Load()
		{
			return POptions.ReadSettings<Options>() ?? new Options();
		}
	}
}
