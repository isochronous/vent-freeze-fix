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
	/// the tile could not have formed in normal play, so the sim is asked to dig it and the mass it
	/// removes is dropped as debris in full (see DigRequests). A tile formed from a real pool is left alone.
	/// The problem only happens on load, so the watcher runs for the first second after it spawns
	/// (five ticks of running time) and then removes itself from the scheduler. The tile forms in the first sim frame
	/// after a load, before any tick, so the "previous tick" for that first comparison is a snapshot
	/// of the two cells taken when the game was saved and carried in the save.
	/// </summary>
	// Opt-in: the game's serializer defaults to opt-out, which saves public fields only and ignores [Serialize] on private ones.
	[SerializationConfig(KSerialization.MemberSerialization.OptIn)]
	public sealed class VentFreezeWatcher : KMonoBehaviour, ISim200ms
	{
		/// <summary>The game forms a tile only when the freezing liquid has at least this share of its default mass.</summary>
		private const float TileMassShare = 0.8f;

		private struct Snapshot
		{
			public Element element;
			public float mass;
		}

		/// <summary>
		/// Ticks of 200 ms the watcher stays active after spawning. Only running time counts, and in
		/// every test the tiles were there by the first or second tick after the game started running,
		/// so five ticks (one second) leave a wide margin.
		/// </summary>
		private const int ActiveTicks = 5;

		private readonly Dictionary<int, Snapshot> previous = new Dictionary<int, Snapshot>();
		/// <summary>Debug mode: log what every watched cell holds on every tick.</summary>
		private static bool Verbose => DebugSave.Enabled;
		private int ventCell = Grid.InvalidCell;
		private int ticks;

		/// <summary>The "Repair tiles after loading" option, read once per game by DebugSave's Game.OnSpawn patch.</summary>
		public static bool RepairEnabled = true;

		public int VentCell => ventCell;

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
			if (Verbose)
				Debug.Log("[VentFreezeFix] Vent " + savedVentCell + " restored snapshot: vent cell " + savedVentElement + " " + savedVentMass.ToString("F1")
					+ " kg, landing cell " + savedLandingCell + " " + savedLandingElement + " " + savedLandingMass.ToString("F1") + " kg");
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
			if (!RepairEnabled)
			{
				SimAndRenderScheduler.instance.Remove(this);
				previous.Clear();
				return;
			}
			if (++ticks > ActiveTicks)
			{
				SimAndRenderScheduler.instance.Remove(this);
				previous.Clear();
				if (Verbose && Grid.IsValidCell(ventCell))
				{
					int last = LandingCell(ventCell);
					Debug.Log("[VentFreezeFix] Vent " + ventCell + " done: vent cell holds " + Describe(ventCell, Grid.Element[ventCell], Grid.Mass[ventCell], true)
						+ (last != ventCell ? ", landing cell " + last + " holds " + Describe(last, Grid.Element[last], Grid.Mass[last], true) : ""));
				}
				return;
			}
			if (!Grid.IsValidCell(ventCell))
				return;
			int landing = LandingCell(ventCell);
			if (Verbose && ticks == 1)
			{
				Storage storage = GetComponent<Storage>();
				Debug.Log("[VentFreezeFix] Vent " + ventCell + " tick 1: " + (storage != null ? storage.MassStored().ToString("F1") : "?") + " kg stored in the vent");
			}
			// Cells still in the snapshot are checked too: when the landing cell itself turned solid,
			// the walk down now stops one cell above it, and only the snapshot still names it.
			var cells = new List<int>(previous.Keys);
			if (!cells.Contains(ventCell))
				cells.Add(ventCell);
			if (landing != ventCell && !cells.Contains(landing))
				cells.Add(landing);
			foreach (int cell in cells)
				Check(cell);
			// Forget cells no longer watched (the column changes as tiles come and go).
			foreach (int cell in cells)
				if (cell != ventCell && cell != landing)
					previous.Remove(cell);
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
			if (DigRequests.IsPending(cell))
			{
				// Asked already and the sim has not answered. Still solid a tick later: ask again.
				if (element != null && element.IsSolid)
				{
					SimMessages.Dig(cell);
					if (Verbose)
						Debug.Log("[VentFreezeFix] Vent " + ventCell + " tick " + ticks + ": cell " + cell + " still holds " + Describe(cell, element, mass, true) + ", dig not applied yet, asked again");
				}
				return;
			}
			string verdict = Verdict(cell, element, had, was);
			if (Verbose)
				Debug.Log("[VentFreezeFix] Vent " + ventCell + " tick " + ticks + ": cell " + cell + " holds " + Describe(cell, element, mass, true)
					+ (had ? ", was " + Describe(cell, was.element, was.mass, false) : ", no earlier state") + ": " + (verdict ?? "turning into debris"));
			if (verdict == null)
				RequestDig(cell, element, mass);
		}

		/// <summary>
		/// The sim has dug the cell: it is empty now, so whatever is there on the next tick is new and
		/// is judged on its own. Without this, a tile that re-forms before that tick (a vent dumping a
		/// backlog into the cleared cell) would pass as an old one.
		/// </summary>
		public void OnDug(int cell)
		{
			if (ticks <= ActiveTicks)
				previous[cell] = new Snapshot { element = ElementLoader.FindElementByHash(SimHashes.Vacuum), mass = 0f };
		}

		/// <summary>Why the cell is left alone, or null when its tile is the bug and gets converted.</summary>
		private static string Verdict(int cell, Element element, bool had, Snapshot was)
		{
			if (element == null || !element.IsSolid)
				return "not solid";
			if (Grid.Foundation[cell])
				return "built tile";
			if (!had)
				return "no earlier state";
			// A real tile needs a pool of at least the tile share the tick before; anything less is the bug.
			if (was.element != null && was.element.IsLiquid && was.mass >= TileMassShare * was.element.defaultValues.mass)
				return "formed from a real pool";
			if (was.element != null && was.element.IsSolid)
				return "was already solid";
			return null;
		}

		/// <summary>Element and mass; for the cell's current content also its temperature and whether the game's solid mask agrees with a solid element.</summary>
		private static string Describe(int cell, Element element, float mass, bool current)
		{
			string text = (element != null ? element.id.ToString() : "?") + " " + mass.ToString("F1") + " kg";
			if (!current)
				return text;
			text += " at " + (Grid.Temperature[cell] - 273.15f).ToString("F0") + " C";
			if (element != null && element.IsSolid != Grid.Solid[cell])
				text += Grid.Solid[cell] ? " (in solid mask)" : " (NOT in solid mask)";
			return text;
		}

		/// <summary>Asks the sim to dig the tile; DigRequests drops what the sim reports removed.</summary>
		private void RequestDig(int cell, Element element, float mass)
		{
			DigRequests.Add(cell, this);
			SimMessages.Dig(cell);
			Debug.Log("[VentFreezeFix] " + element.name + " tile (" + mass.ToString("F1") + " kg) at cell " + cell + " near the liquid vent at cell " + ventCell + ": asked the sim to dig it");
		}
	}
}
