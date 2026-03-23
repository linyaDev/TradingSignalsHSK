using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TradingSignalsHSK;

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

	private bool isBurning;
	private int arrivalTick = -1;

	public bool IsBurning => isBurning;

	private CompProperties_TribalSignalFire Props => (CompProperties_TribalSignalFire)props;

	/// <summary>
	/// Returns the shared cooldown tracker from <see cref="Find.World"/>.
	/// The component is auto-registered by vanilla <c>World.FillComponents()</c>.
	/// </summary>
	private static WorldComponent_TribalSignalCooldown? Tracker =>
		Find.World?.GetComponent<WorldComponent_TribalSignalCooldown>();

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref isBurning, "isBurning");
		Scribe_Values.Look(ref arrivalTick, "arrivalTick", -1);
	}

	public override void CompTick()
	{
		base.CompTick();
		if (!isBurning || parent.Map == null)
		{
			return;
		}

		Vector3 pos = parent.DrawPos;
		Map map = parent.Map;

		if (parent.IsHashIntervalTick(15))
		{
			FleckMaker.ThrowFireGlow(pos, map, 1.5f);
		}

		if (parent.IsHashIntervalTick(30))
		{
			FleckMaker.ThrowSmoke(pos + new Vector3(0f, 0f, 0.5f), map, 2f);
		}

		if (parent.IsHashIntervalTick(50))
		{
			FleckMaker.ThrowMicroSparks(pos, map);
		}

		if (arrivalTick > 0 && Find.TickManager.TicksGame >= arrivalTick)
		{
			isBurning = false;
			Messages.Message("TribalSignal_BurnedOut".Translate(), MessageTypeDefOf.NeutralEvent);
			parent.Destroy(DestroyMode.Vanish);
		}
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		if (parent is not Building || !parent.Spawned || parent.Map == null)
		{
			yield break;
		}

		if (isBurning)
		{
			yield break;
		}

		var cmd = new Command_Action
		{
			defaultLabel = "TribalSignal_CommandLabel".Translate(),
			defaultDesc = "TribalSignal_CommandDesc".Translate(SilverCost),
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
		if (isBurning && arrivalTick > 0)
		{
			int ticksLeft = arrivalTick - Find.TickManager.TicksGame;
			if (ticksLeft > 0)
			{
				return "TribalSignal_Burning".Translate(ticksLeft.ToStringTicksToPeriod());
			}
		}

		if (!CooldownComplete(out int cooldownLeft))
		{
			return "TribalSignal_OnCooldown".Translate(cooldownLeft.ToStringTicksToPeriod());
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

	private const int SilverCost = 200;

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

		int silverAvailable = CountSilverOnMap(map);
		if (silverAvailable < SilverCost)
		{
			Messages.Message("TribalSignal_NotEnoughSilver".Translate(SilverCost, silverAvailable), MessageTypeDefOf.RejectInput);
			return;
		}

		Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
			"TribalSignal_ConfirmPayment".Translate(SilverCost),
			delegate { ExecuteSignal(map, candidates); }));
	}

	private void ExecuteSignal(Map map, List<Faction> candidates)
	{
		if (!TakesilverFromMap(map, SilverCost))
		{
			Messages.Message("TribalSignal_NotEnoughSilver".Translate(SilverCost, CountSilverOnMap(map)), MessageTypeDefOf.RejectInput);
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
			Log.Error("TradingSignalsHSK: TraderCaravanArrival incident not found.");
			return;
		}

		int fireTick = Find.TickManager.TicksGame + ArrivalDelayTicks;
		Find.Storyteller.incidentQueue.Add(incident, fireTick, parms);

		isBurning = true;
		arrivalTick = fireTick;

		string delayDaysStr = ((float)ArrivalDelayTicks / GenDate.TicksPerDay).ToString("F1");
		Messages.Message(
			"TribalSignal_Scheduled".Translate(faction.Name, delayDaysStr),
			MessageTypeDefOf.PositiveEvent);

		Tracker?.NotifySignalUsed();
	}

	private static int CountSilverOnMap(Map map)
	{
		int total = 0;
		foreach (Thing t in map.listerThings.ThingsOfDef(ThingDefOf.Silver))
		{
			total += t.stackCount;
		}
		return total;
	}

	private static bool TakesilverFromMap(Map map, int amount)
	{
		int remaining = amount;
		List<Thing> silvers = map.listerThings.ThingsOfDef(ThingDefOf.Silver).ToList();
		foreach (Thing silver in silvers)
		{
			if (remaining <= 0) break;
			int take = Mathf.Min(silver.stackCount, remaining);
			silver.SplitOff(take).Destroy();
			remaining -= take;
		}
		return remaining <= 0;
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
