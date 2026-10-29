using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NomadDrive.Features.Restoration;

namespace RestorationToolsTweaks
{
    // Work drain: a tool drains only for the surface that it changes. Each read's work
    // becomes a debt in the tool's own resource. The drain prefixes ask the game to take it within PaySeconds,
    // and the finalizers take off only what the game actually took, so each m² is paid once: a tool that is off
    // pays its debt at the next use, and a tool with no debt drains nothing.
    internal static class WorkDrain
    {
        // The game's drain for each m² of work, measured in the game: its drain a second at 1x
        // divided by the work a second at 1x on fresh surface. The sponge is not measured yet (no dirty vehicle),
        // so it keeps area drain.
        private const float GrinderCost = 10.76f;
        private const float PolisherCost = 1.59f;
        private const float SprayCost = 8.00f;
        private const float PaySeconds = 0.25f;
        // A debt below this is paid: paying a part each frame would never reach exactly 0.
        private const float PaidOff = 0.00001f;

        private sealed class Debt
        {
            public float Owed;
        }

        private static readonly ConditionalWeakTable<object, Debt> Debts = new ConditionalWeakTable<object, Debt>();
        private static readonly HashSet<System.Type> WarnedUnreadable = new HashSet<System.Type>();

        // The factor on the game's drain for this Update. `scale` is the area-drain fallback (1 for the spray,
        // whose game drain already grows with its brush area). `gameRate` is the game's drain a second now.
        internal static float Factor(object tool, float scale, float gameRate)
        {
            float rate = Plugin.DrainRate.Value;
            float? cost = CostOf(tool);
            if (!cost.HasValue)
            {
                return BrushScaleLogic.DrainFactor(DrainMode.Area, scale, rate);
            }
            float? work = WorkTracker.TakeWork(tool);
            if (!work.HasValue)
            {
                if (WarnedUnreadable.Add(tool.GetType()))
                {
                    Plugin.Log.LogWarning($"RestorationToolsTweaks: a panel cannot be read, so {tool.GetType().Name} uses area drain there");
                }
                return BrushScaleLogic.DrainFactor(DrainMode.Area, scale, rate);
            }
            Debt debt = Debts.GetOrCreateValue(tool);
            debt.Owed += BrushScaleLogic.OwedFromWork(work.Value, cost.Value, rate);
            return BrushScaleLogic.DebtFactor(debt.Owed, PaySeconds, gameRate);
        }

        // Takes off the debt what the game's Update took from the resource.
        internal static void Settle(object tool, float resourceBefore, float resourceAfter)
        {
            if (!Debts.TryGetValue(tool, out Debt debt))
            {
                return;
            }
            debt.Owed = BrushScaleLogic.SettleDebt(debt.Owed, resourceBefore, resourceAfter);
            if (debt.Owed < PaidOff)
            {
                debt.Owed = 0f;
            }
        }

        private static float? CostOf(object tool)
        {
            switch (tool)
            {
                case GrinderTool _: return GrinderCost;
                case PolishTool _: return PolisherCost;
                case PaintSprayTool _: return SprayCost;
                default: return null;
            }
        }
    }
}
