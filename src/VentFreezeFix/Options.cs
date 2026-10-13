using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace VentFreezeFix
{
	// Titles, tooltips and the category are string keys (see ModStrings), resolved by PLib when the options screen is built.
	[JsonObject(MemberSerialization.OptIn)]
	[ConfigFile(SharedConfigLocation: true)]
	public sealed class Options
	{
		[Option("STRINGS.VENTFREEZEFIX.OPTIONS.INSULATE_VENTS.NAME", "STRINGS.VENTFREEZEFIX.OPTIONS.INSULATE_VENTS.TOOLTIP")]
		[RestartRequired]
		[JsonProperty]
		public bool InsulateVents { get; set; } = true;

		[Option("STRINGS.VENTFREEZEFIX.OPTIONS.REPAIR_ON_LOAD.NAME", "STRINGS.VENTFREEZEFIX.OPTIONS.REPAIR_ON_LOAD.TOOLTIP")]
		[JsonProperty]
		public bool RepairOnLoad { get; set; } = true;

		// Static categories are shown sorted by name, after the uncategorised options above.
		[Option("STRINGS.VENTFREEZEFIX.OPTIONS.DEBUG_MODE.NAME", "STRINGS.VENTFREEZEFIX.OPTIONS.DEBUG_MODE.TOOLTIP", "STRINGS.VENTFREEZEFIX.OPTIONS.CATEGORY.DEVELOPER")]
		[JsonProperty]
		public bool DebugMode { get; set; } = false;

		[Option("STRINGS.VENTFREEZEFIX.OPTIONS.REPRO_BUTTON.NAME", "STRINGS.VENTFREEZEFIX.OPTIONS.REPRO_BUTTON.TOOLTIP", "STRINGS.VENTFREEZEFIX.OPTIONS.CATEGORY.DEVELOPER")]
		[JsonProperty]
		public bool ReproButton { get; set; } = false;

		public static Options Load()
		{
			return POptions.ReadSettings<Options>() ?? new Options();
		}
	}
}
