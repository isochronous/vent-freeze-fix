using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using UnityEngine;

namespace VentFreezeFix
{
	public sealed class VentFreezeFixMod : UserMod2
	{
		public override void OnLoad(Harmony harmony)
		{
			base.OnLoad(harmony);
			PUtil.InitLibrary(false);
			new POptions().RegisterOptions(this, typeof(Options));
			Debug.Log("[VentFreezeFix] Loaded version " + typeof(VentFreezeFixMod).Assembly.GetName().Version);
		}
	}

	/// <summary>
	/// The cause of the vent tiles, and the fix for it.
	///
	/// A vent keeps the packet it is about to emit as an item in its storage. The game's default
	/// storage only hides its items, so the packet is a live sim temperature chunk sitting at the
	/// vent's cell. When the sim reports that a chunk has crossed a phase transition, the game's
	/// handler (SimTemperatureTransfer.DoOreMeltTransition) converts the item into cell mass of the
	/// new phase at that cell, with no mass rule, and destroys the item. In running play a packet is
	/// emitted within a tick, before the sim evaluates it. After a load it sits in the vent through
	/// the whole load and any pause, so a packet already past its transition (super-cooled liquid
	/// pushed through pipes in packets small enough not to break them) is caught by the sim's first
	/// running step unless the vent happens to emit first: a natural tile of the packet's mass in
	/// the vent's cell, and the packet is gone.
	///
	/// The vent's storage is given the game's insulated storage modifiers (hide, seal, insulate), as
	/// other buildings holding element packets have. Sealed items are skipped by the transition
	/// handler, and insulated items are not temperature simulated, so the packet stays in the vent
	/// and is emitted as usual, where the sim's own rules turn a small freezing packet into debris.
	/// Credit for the storage modifiers goes to the forum user whose patch several players confirmed.
	/// </summary>
	[HarmonyPatch(typeof(LiquidVentConfig), nameof(LiquidVentConfig.ConfigureBuildingTemplate))]
	public static class LiquidVentConfig_Storage_Patch
	{
		public static void Postfix(GameObject go)
		{
			Storage storage = go.GetComponent<Storage>();
			if (storage != null && Options.Load().InsulateVents)
				storage.SetDefaultStoredItemModifiers(Storage.StandardInsulatedStorage);
		}
	}

	/// <summary>Every liquid vent gets a watcher component.</summary>
	[HarmonyPatch(typeof(LiquidVentConfig), nameof(LiquidVentConfig.DoPostConfigureComplete))]
	public static class LiquidVentConfig_Patch
	{
		public static void Postfix(GameObject go)
		{
			go.AddOrGet<VentFreezeWatcher>();
		}
	}
}
