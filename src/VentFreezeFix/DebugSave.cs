using System.IO;
using HarmonyLib;
using PeterHan.PLib.UI;
using UnityEngine;

namespace VentFreezeFix
{
	/// <summary>
	/// Debug mode: a button on the game screen that arms a save. The next time any liquid vent
	/// emits a packet, the save is written at the end of that frame. The vent's Exhaust runs after
	/// the sim frame of its 200 ms tick, and the packet's sim message is only applied on the next
	/// tick's frame, so the end of the emitting frame sits squarely in the window where the bug is
	/// captured. All liquid piping ticks in step, so one vent's emit stands for all of them.
	/// </summary>
	public static class DebugSave
	{
		public const string SaveName = "VentFreezeRepro.sav";

		private static bool enabled;
		private static bool armed;
		private static bool pending;
		private static string pendingNote;
		private static GameObject buttonObject;

		[HarmonyPatch(typeof(Game), "OnSpawn")]
		public static class Game_OnSpawn_Patch
		{
			public static void Postfix()
			{
				enabled = Options.Load().DebugMode;
				armed = false;
				pending = false;
				if (!enabled || GameScreenManager.Instance?.ssOverlayCanvas == null)
					return;
				var button = new PButton("VentFreezeFixDebugSave")
				{
					Text = "Save on next vent emit",
					ToolTip = "Vent Freeze Fix debug: arms a save that is written on the tick a liquid vent emits a packet, to " + SaveName + " next to the current save.",
					OnClick = OnClick,
					Margin = new RectOffset(10, 10, 6, 6),
				}.SetKleiBlueStyle();
				buttonObject = button.AddTo(GameScreenManager.Instance.ssOverlayCanvas.gameObject);
				RectTransform rect = buttonObject.GetComponent<RectTransform>();
				rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
				rect.anchoredPosition = new Vector2(12f, -140f);
			}
		}

		private static void OnClick(GameObject source)
		{
			armed = !armed;
			SetText(armed ? "Armed: waiting for a vent to emit..." : "Save on next vent emit");
		}

		private static void SetText(string text)
		{
			if (buttonObject != null)
				PUIElements.SetText(buttonObject, text);
		}

		/// <summary>Fires right after a vent has pushed a packet into the world (or a falling particle).</summary>
		[HarmonyPatch(typeof(Exhaust), "EmitCommon")]
		public static class Exhaust_EmitCommon_Patch
		{
			public static void Postfix(Exhaust __instance, bool __result, int cell, PrimaryElement primary_element)
			{
				if (!enabled || !armed || !__result || __instance.GetComponent<VentFreezeWatcher>() == null)
					return;
				armed = false;
				pending = true;
				pendingNote = (primary_element != null ? primary_element.ElementID.ToString() : "?") + " from the liquid vent at cell " + cell;
			}
		}

		/// <summary>End of the frame the emit happened in: write the save before the next sim frame can apply the packet.</summary>
		[HarmonyPatch(typeof(Game), "LateUpdate")]
		public static class Game_LateUpdate_Patch
		{
			public static void Postfix()
			{
				if (!pending)
					return;
				pending = false;
				string current = SaveLoader.GetActiveSaveFilePath();
				string folder = !string.IsNullOrEmpty(current) ? Path.GetDirectoryName(current) : SaveLoader.GetSavePrefixAndCreateFolder();
				string path = Path.Combine(folder, SaveName);
				SaveLoader.Instance.Save(path, isAutoSave: false, updateSavePointer: false);
				Debug.Log("[VentFreezeFix] Debug save written to " + path + " on the tick " + pendingNote + " was emitted");
				SetText("Saved " + SaveName);
			}
		}
	}
}
