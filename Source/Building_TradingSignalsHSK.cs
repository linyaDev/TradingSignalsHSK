using RimWorld;
using Verse;

namespace TradingSignalsHSK;

public class Building_TradingSignalsHSK : Building
{
	public override AcceptanceReport DeconstructibleBy(Faction faction)
	{
		var comp = GetComp<CompTradeSignal>();
		if (comp != null && comp.IsActive)
		{
			return AcceptanceReport.WasRejected;
		}

		return base.DeconstructibleBy(faction);
	}
}
