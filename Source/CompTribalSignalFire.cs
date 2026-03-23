using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TribalSignalCampfire;

public class CompProperties_TribalSignalFire : CompProperties
{
	public int cooldownTicks = 900000;

	public CompProperties_TribalSignalFire()
	{
		compClass = typeof(CompTribalSignalFire);
	}
}

public class CompTribalSignalFire : ThingComp
{
	/// <summary>Delay before the caravan incident fires (2 days).</summary>
	private const int ArrivalDelayTicks = 120000;

	private CompProperties_TribalSignalFire Props => (CompProperties_TribalSignalFire)props;

	/// <summary>
	/// Returns the shared cooldown tracker from <see cref="Find.World"/>.
	/// The component is auto-registered by vanilla <c>World.FillComponents()</c>.
	/// </summary>
	private static WorldComponent_TribalSignalCooldown? Tracker =>
		Find.World?.GetComponent<WorldComponent_TribalSignalCooldown>();

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		if (parent is not Building || !parent.Spawned || parent.Map == null)
		{
			yield break;
		}

		var cmd = new Command_Action
		{
			defaultLabel = "TribalSignal_CommandLabel".Translate(),
			defaultDesc = "TribalSignal_CommandDesc".Translate(),
			icon = parent.def.uiIcon,
			action = TryCallTribalTrader
		};

		if (!CooldownComplete(out int ticksLeft))
		{
			cmd.Disable("TribalSignal_OnCooldown".Translate(ticksLeft.ToStringTicksToPeriod()));
		}
		else if (!AnyNeolithicTradeFaction(parent.Map))
		{
			cmd.Disable("TribalSignal_NoFaction".Translate());
		}

		yield return cmd;
	}

	public override string CompInspectStringExtra()
	{
		if (!CooldownComplete(out int ticksLeft))
		{
			return "TribalSignal_OnCooldown".Translate(ticksLeft.ToStringTicksToPeriod());
		}

		return "TribalSignal_CooldownShared".Translate();
	}

	private bool CooldownComplete(out int ticksLeft)
	{
		ticksLeft = 0;
		var tracker = Tracker;
		if (tracker == null)
		{
			return true;
		}

		return tracker.CooldownComplete(Props.cooldownTicks, out ticksLeft);
	}

	private void TryCallTribalTrader()
	{
		Map map = parent.Map;
		if (map == null)
		{
			return;
		}

		if (!CooldownComplete(out _))
		{
			return;
		}

		List<Faction> candidates = FindNeolithicTradeFactions(map).ToList();
		if (candidates.Count == 0)
		{
			Messages.Message("TribalSignal_NoFaction".Translate(), MessageTypeDefOf.RejectInput);
			return;
		}

		Faction faction = candidates.RandomElement();
		IncidentParms parms = new IncidentParms
		{
			target = map,
			faction = faction,
			forced = true
		};

		IncidentDef? incident = DefDatabase<IncidentDef>.GetNamedSilentFail("TraderCaravanArrival");
		if (incident == null)
		{
			Log.Error("TribalSignalCampfire: TraderCaravanArrival incident not found.");
			return;
		}

		int fireTick = Find.TickManager.TicksGame + ArrivalDelayTicks;
		Find.Storyteller.incidentQueue.Add(incident, fireTick, parms);
		string delayDaysStr = ((float)ArrivalDelayTicks / GenDate.TicksPerDay).ToString("F1");
		Messages.Message(
			"TribalSignal_Scheduled".Translate(faction.Name, delayDaysStr),
			MessageTypeDefOf.PositiveEvent);

		Tracker?.NotifySignalUsed();
	}

	private static bool AnyNeolithicTradeFaction(Map map)
	{
		return FindNeolithicTradeFactions(map).Any();
	}

	/// <summary>
	/// Mirrors vanilla IncidentWorker_TraderCaravanArrival + IncidentWorker_NeutralGroup filters, plus Neolithic only.
	/// </summary>
	private static IEnumerable<Faction> FindNeolithicTradeFactions(Map map)
	{
		foreach (Faction f in Find.FactionManager.AllFactions)
		{
			if (IsValidNeolithicTradeSource(f, map))
			{
				yield return f;
			}
		}
	}

	private static bool IsValidNeolithicTradeSource(Faction f, Map map)
	{
		if (f.IsPlayer || f.defeated)
		{
			return false;
		}

		if (f.def.hidden)
		{
			return false;
		}

		if (f.def.techLevel != TechLevel.Neolithic)
		{
			return false;
		}

		if (f.def.caravanTraderKinds.NullOrEmpty())
		{
			return false;
		}

		if (f.HostileTo(Faction.OfPlayer))
		{
			return false;
		}

		if (!f.def.allowedArrivalTemperatureRange.Includes(map.mapTemperature.OutdoorTemp))
		{
			return false;
		}

		if (!f.def.allowedArrivalTemperatureRange.Includes(map.mapTemperature.SeasonalTemp))
		{
			return false;
		}

		if (NeutralGroupIncidentUtility.AnyBlockingHostileLord(map, f))
		{
			return false;
		}

		return true;
	}
}
