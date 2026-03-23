using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace TradingSignalsHSK;

/// <summary>
/// Per-world cooldown tracker for all trade signal buildings.
/// Each building type uses its own cooldownKey (e.g. "tribal", "medieval").
/// </summary>
public class WorldComponent_TradeSignalCooldown : WorldComponent
{
	private Dictionary<string, int> lastSignalTicks = new Dictionary<string, int>();

	public WorldComponent_TradeSignalCooldown(World world) : base(world)
	{
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Collections.Look(ref lastSignalTicks, "lastSignalTicks", LookMode.Value, LookMode.Value);
		lastSignalTicks ??= new Dictionary<string, int>();
	}

	public bool CooldownComplete(string key, int cooldownTicks, out int ticksLeft)
	{
		ticksLeft = 0;
		if (!lastSignalTicks.TryGetValue(key, out int lastTick) || lastTick < 0)
		{
			return true;
		}

		int elapsed = Find.TickManager.TicksGame - lastTick;
		if (elapsed >= cooldownTicks)
		{
			return true;
		}

		ticksLeft = cooldownTicks - elapsed;
		return false;
	}

	public void NotifySignalUsed(string key)
	{
		lastSignalTicks[key] = Find.TickManager.TicksGame;
	}
}
