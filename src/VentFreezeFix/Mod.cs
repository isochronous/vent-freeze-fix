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
