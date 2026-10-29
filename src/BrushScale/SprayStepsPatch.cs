using System;
using HarmonyLib;
using NomadDrive.Features.Restoration;

namespace RestorationToolsTweaks
{
    // The spray can keeps its own wheel code. The owner sends the step number, and each machine turns it into a
    // radius with its own copy of these fields, so the extra steps work only where the mod runs (host and viewers).
    [HarmonyPatch(typeof(PaintSprayTool), "Awake")]
    internal static class AddStepsOnPaintSprayToolAwake
    {
        private static void Postfix(PaintSprayTool __instance)
        {
            try
            {
                ref float maxScale = ref GameMembers.SprayBrushMaxScale(__instance);
                ref int stepCount = ref GameMembers.SprayBrushStepCount(__instance);
                float sphere = GameMembers.SpraySphereRadius(__instance);
                BrushScaleLogic.SpraySteps(GameMembers.SprayBrushMinScale(__instance), maxScale, stepCount,
                    Plugin.MaxScale.Value, sphere, out int newCount, out float newMax);
                stepCount = newCount;
                maxScale = newMax;
                if (Plugin.Verbose.Value)
                {
                    Plugin.Log.LogDebug($"RestorationToolsTweaks: PaintSprayTool steps {newCount} max {newMax:0.##} radius {sphere * newMax:0.###}");
                }
            }
            catch (Exception e)
            {
                Plugin.WarnOnce(nameof(AddStepsOnPaintSprayToolAwake), e);
            }
        }
    }
}
