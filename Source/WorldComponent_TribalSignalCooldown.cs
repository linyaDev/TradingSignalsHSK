using RimWorld.Planet;
using Verse;

namespace TribalSignalCampfire;

/// <summary>
/// Single per-world cooldown for all tribal signal fires.
/// Automatically discovered and registered by <see cref="World.FillComponents"/> —
/// no manual injection or reflection needed.
/// </summary>
public class WorldComponent_TribalSignalCooldown : WorldComponent
{
	private int lastSignalTick = -1;

	public WorldComponent_TribalSignalCooldown(World world) : base(world)
	{
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref lastSignalTick, "lastSignalTick", -1);
	}

	public bool CooldownComplete(int cooldownTicks, out int ticksLeft)
	{
		ticksLeft = 0;
		if (lastSignalTick < 0)
		{
			return true;
		}

		int elapsed = Find.TickManager.TicksGame - lastSignalTick;
		if (elapsed >= cooldownTicks)
		{
			return true;
		}

		ticksLeft = cooldownTicks - elapsed;
		return false;
	}

	public void NotifySignalUsed()
	{
		lastSignalTick = Find.TickManager.TicksGame;
	}
}
