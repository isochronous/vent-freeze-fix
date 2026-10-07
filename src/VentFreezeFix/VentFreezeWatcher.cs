using System.Collections.Generic;
using System.Runtime.Serialization;
using KSerialization;
using UnityEngine;

namespace VentFreezeFix
{
	/// <summary>
	/// Watches the cells a liquid vent's output passes through: the vent cell itself (the packet is
	/// injected there when the cell below is solid) and the landing cell, the last open cell above
	/// the first solid below the vent (where falling packets enter the sim). Each tick it remembers
	/// what those cells held. When one of them turns into a natural solid tile although the liquid it
	/// held the tick before was under the game's own tile rule (80% of the element's default mass),
	/// the tile could not have formed in normal play, so it is turned back into debris at its full
	/// current mass. A tile formed from a real pool is left alone.
	/// The problem only happens on load, so the watcher runs for the first two seconds after it spawns
	/// (ten ticks) and then removes itself from the scheduler. The tile forms in the first sim frame
	/// after a load, before any tick, so the "previous tick" for that first comparison is a snapshot
	/// of the two cells taken when the game was saved and carried in the save.
	/// </summary>
	public sealed class VentFreezeWatcher : KMonoBehaviour, ISim200ms
	{
		/// <summary>The game forms a tile only when the freezing liquid has at least this share of its default mass.</summary>
		private const float TileMassShare = 0.8f;

		private struct Snapshot
		{
			public Element element;
			public float mass;
		}

		/// <summary>Ticks of 200 ms the watcher stays active after spawning.</summary>
		private const int ActiveTicks = 10;

		private readonly Dictionary<int, Snapshot> previous = new Dictionary<int, Snapshot>();
		private int ventCell = Grid.InvalidCell;
		private int ticks;

		// State of the watched cells at save time, so the first tick after a load has a "previous tick".
		[Serialize] private int savedVentCell = Grid.InvalidCell;
		[Serialize] private SimHashes savedVentElement = SimHashes.Vacuum;
		[Serialize] private float savedVentMass;
		[Serialize] private int savedLandingCell = Grid.InvalidCell;
		[Serialize] private SimHashes savedLandingElement = SimHashes.Vacuum;
		[Serialize] private float savedLandingMass;

		protected override void OnSpawn()
		{
			base.OnSpawn();
			ventCell = Grid.PosToCell(this);
		}

		[OnSerializing]
		private void OnSerializing()
		{
			savedVentCell = Grid.IsValidCell(ventCell) ? ventCell : Grid.InvalidCell;
			savedLandingCell = Grid.IsValidCell(ventCell) ? LandingCell(ventCell) : Grid.InvalidCell;
			Capture(savedVentCell, out savedVentElement, out savedVentMass);
			Capture(savedLandingCell, out savedLandingElement, out savedLandingMass);
		}

		[OnDeserialized]
		private void OnDeserialized()
		{
			Restore(savedVentCell, savedVentElement, savedVentMass);
			Restore(savedLandingCell, savedLandingElement, savedLandingMass);
		}

		private static void Capture(int cell, out SimHashes element, out float mass)
		{
			element = SimHashes.Vacuum;
			mass = 0f;
			if (!Grid.IsValidCell(cell))
				return;
			Element e = Grid.Element[cell];
			if (e != null)
				element = e.id;
			mass = Grid.Mass[cell];
		}

		private void Restore(int cell, SimHashes element, float mass)
		{
			if (!Grid.IsValidCell(cell))
				return;
			Element e = ElementLoader.FindElementByHash(element);
			if (e != null)
				previous[cell] = new Snapshot { element = e, mass = mass };
		}

		public void Sim200ms(float dt)
		{
			if (++ticks > ActiveTicks)
			{
				SimAndRenderScheduler.instance.Remove(this);
				previous.Clear();
				return;
			}
			if (!Grid.IsValidCell(ventCell))
				return;
			int landing = LandingCell(ventCell);
			Check(ventCell);
			if (landing != ventCell)
				Check(landing);
			// Forget cells no longer watched (the column changes as tiles come and go).
			if (previous.Count > 2)
			{
				var stale = new List<int>();
				foreach (int cell in previous.Keys)
					if (cell != ventCell && cell != landing)
						stale.Add(cell);
				foreach (int cell in stale)
					previous.Remove(cell);
			}
		}

		/// <summary>The last open cell going down from the vent, where a falling packet is added to the sim.</summary>
		private static int LandingCell(int from)
		{
			int cell = from;
			while (true)
			{
				int below = Grid.CellBelow(cell);
				if (!Grid.IsValidCell(below) || Grid.Solid[below])
					return cell;
				cell = below;
			}
		}

		private void Check(int cell)
		{
			Element element = Grid.Element[cell];
			float mass = Grid.Mass[cell];
			bool had = previous.TryGetValue(cell, out Snapshot was);
			previous[cell] = new Snapshot { element = element, mass = mass };
			if (!had || element == null || !element.IsSolid || Grid.Foundation[cell])
				return;
			// A real tile needs a pool of at least the tile share the tick before; anything less is the bug.
			if (was.element != null && was.element.IsLiquid && was.mass >= TileMassShare * was.element.defaultValues.mass)
				return;
			if (was.element != null && was.element.IsSolid)
				return;
			ConvertToDebris(cell, element, mass);
		}

		private void ConvertToDebris(int cell, Element element, float mass)
		{
			float temperature = Grid.Temperature[cell];
			byte diseaseIdx = Grid.DiseaseIdx[cell];
			int diseaseCount = Grid.DiseaseCount[cell];
			SimMessages.ReplaceElement(cell, SimHashes.Vacuum, CellEventLogger.Instance.SandBoxTool, 0f);
			if (mass > 0f && element.substance != null)
				element.substance.SpawnResource(Grid.CellToPosCCC(cell, Grid.SceneLayer.Ore), mass, temperature, diseaseIdx, diseaseCount);
			previous[cell] = new Snapshot { element = ElementLoader.FindElementByHash(SimHashes.Vacuum), mass = 0f };
			Debug.Log("[VentFreezeFix] " + element.name + " tile (" + mass.ToString("F1") + " kg) at cell " + cell + " near the liquid vent at cell " + ventCell + " turned back into debris");
		}
	}
}
