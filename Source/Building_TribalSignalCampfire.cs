using RimWorld;
using Verse;

namespace TradingSignalsHSK;

public class Building_TribalSignalCampfire : Building
{
	public override AcceptanceReport DeconstructibleBy(Faction faction)
	{
		var comp = GetComp<CompTribalSignalFire>();
		if (comp != null && comp.IsBurning)
		{
			return AcceptanceReport.WasRejected;
		}

		return base.DeconstructibleBy(faction);
	}
}
