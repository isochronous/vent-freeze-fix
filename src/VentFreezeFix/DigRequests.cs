using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace VentFreezeFix
{
	/// <summary>
	/// The dig requests the watchers have made, by cell. Digging is the game's own way of turning a
	/// natural tile into debris: the sim removes the tile and reports the mass it took out, which the
	/// game then drops as ore. For a requested cell that report is answered here instead, spawning
	/// the full mass (a dug tile normally drops half). A request the sim ignores produces no report
	/// and therefore no debris, so asking again is always safe.
	/// </summary>
	public static class DigRequests
	{
		/// <summary>Requested cell -> the watcher that asked.</summary>
		private static readonly Dictionary<int, VentFreezeWatcher> pending = new Dictionary<int, VentFreezeWatcher>();

		public static void Add(int cell, VentFreezeWatcher watcher)
		{
			pending[cell] = watcher;
		}

		public static bool IsPending(int cell)
		{
			return pending.ContainsKey(cell);
		}

		/// <summary>Cell indices mean nothing across games.</summary>
		[HarmonyPatch(typeof(Game), "OnSpawn")]
		public static class Game_OnSpawn_Patch
		{
			public static void Postfix()
			{
				pending.Clear();
			}
		}

		[HarmonyPatch(typeof(WorldDamage), nameof(WorldDamage.OnDigComplete))]
		public static class WorldDamage_OnDigComplete_Patch
		{
			public static bool Prefix(int cell, float mass, float temperature, ushort element_idx, byte disease_idx, int disease_count)
			{
				if (!pending.TryGetValue(cell, out VentFreezeWatcher watcher))
					return true;
				pending.Remove(cell);
				Grid.Damage[cell] = 0f;
				Element element = ElementLoader.elements[element_idx];
				if (mass > 0f && element.substance != null)
					element.substance.SpawnResource(Grid.CellToPosCCC(cell, Grid.SceneLayer.Ore), mass, temperature, disease_idx, disease_count);
				Debug.Log("[VentFreezeFix] " + element.name + " tile (" + mass.ToString("F1") + " kg) at cell " + cell + " near the liquid vent at cell " + (watcher != null ? watcher.VentCell.ToString() : "?") + " turned back into debris");
				if (watcher != null)
					watcher.OnDug(cell);
				return false;
			}
		}
	}
}
