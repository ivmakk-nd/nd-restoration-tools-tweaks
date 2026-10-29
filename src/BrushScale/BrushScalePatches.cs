using System;
using HarmonyLib;
using NomadDrive.Features.Interaction;
using NomadDrive.Features.Restoration;

namespace RestorationToolsTweaks
{
    // The hotbar leaves the wheel alone while the equipped item captures it (HotbarManager.IsScrollCapturedByEquippedItem).
    // PaintSprayTool overrides this getter, so the spray can keeps its own rule.
    [HarmonyPatch(typeof(HeldItem), nameof(HeldItem.IsCapturingScrollInput), MethodType.Getter)]
    internal static class CaptureWheelOnHeldItem
    {
        private static void Postfix(HeldItem __instance, ref bool __result)
        {
            try
            {
                if (__result || !__instance.isOwned || !__instance.IsEquipped)
                {
                    return;
                }
                if ((__instance is RestorationTool restoration && restoration.IsSnapped) ||
                    (__instance is CleanTool clean && clean.IsSnapped))
                {
                    __result = true;
                }
            }
            catch (Exception e)
            {
                Plugin.WarnOnce(nameof(CaptureWheelOnHeldItem), e);
            }
        }
    }

    [HarmonyPatch(typeof(RestorationTool), "Update")]
    internal static class StepBrushOnRestorationToolUpdate
    {
        private static void Postfix(RestorationTool __instance)
        {
            try
            {
                if (!__instance.isOwned || !__instance.IsEquipped)
                {
                    return;
                }
                BrushState state = Brushes.Get(__instance);
                if (!__instance.IsSnapped)
                {
                    Preview.Hide(__instance, state);
                    return;
                }
                float scale = Brushes.TickWheel(__instance, state);
                if (scale != 0f)
                {
                    GameMembers.RestorationActiveRadius(__instance) = state.VanillaActiveRadius * scale;
                    GameMembers.RestorationOnRadiusMultiplierChanged(__instance);
                }
                float radius = GameMembers.RestorationActiveRadius(__instance) * GameMembers.RestorationFastMovementMultiplier(__instance);
                Preview.Show(__instance, state, GameMembers.RestorationToolTip(__instance), GameMembers.RestorationSnapHelper(__instance), radius);
            }
            catch (Exception e)
            {
                Plugin.WarnOnce(nameof(StepBrushOnRestorationToolUpdate), e);
            }
        }
    }

    [HarmonyPatch(typeof(RestorationTool), nameof(RestorationTool.OnUnequip))]
    internal static class ResetBrushOnRestorationToolUnequip
    {
        private static void Postfix(RestorationTool __instance)
        {
            try
            {
                BrushState state = Brushes.Get(__instance);
                Preview.Hide(__instance, state);
                if (Brushes.Reset(__instance))
                {
                    GameMembers.RestorationActiveRadius(__instance) = state.VanillaActiveRadius;
                }
            }
            catch (Exception e)
            {
                Plugin.WarnOnce(nameof(ResetBrushOnRestorationToolUnequip), e);
            }
        }
    }

    [HarmonyPatch(typeof(CleanTool), "Update")]
    internal static class StepBrushOnCleanToolUpdate
    {
        private static void Postfix(CleanTool __instance)
        {
            try
            {
                if (!__instance.isOwned || !__instance.IsEquipped)
                {
                    return;
                }
                BrushState state = Brushes.Get(__instance);
                if (!__instance.IsSnapped)
                {
                    Preview.Hide(__instance, state);
                    return;
                }
                float scale = Brushes.TickWheel(__instance, state);
                if (scale != 0f)
                {
                    float scaled = state.VanillaActiveRadius * scale;
                    GameMembers.CleanActiveRadius(__instance) = scaled;
                    // EnableTool reads the field. A running sponge also needs the new radius on its spheres;
                    // opacity 0 keeps the current opacity.
                    if (__instance.IsToolActive)
                    {
                        GameMembers.CleanApplyToolStateToSpheres(__instance, scaled, 0f);
                    }
                }
                Preview.Show(__instance, state, GameMembers.CleanToolTip(__instance), GameMembers.CleanSnapHelper(__instance),
                    GameMembers.CleanActiveRadius(__instance));
            }
            catch (Exception e)
            {
                Plugin.WarnOnce(nameof(StepBrushOnCleanToolUpdate), e);
            }
        }
    }

    [HarmonyPatch(typeof(CleanTool), nameof(CleanTool.OnUnequip))]
    internal static class ResetBrushOnCleanToolUnequip
    {
        private static void Postfix(CleanTool __instance)
        {
            try
            {
                BrushState state = Brushes.Get(__instance);
                Preview.Hide(__instance, state);
                if (Brushes.Reset(__instance))
                {
                    GameMembers.CleanActiveRadius(__instance) = state.VanillaActiveRadius;
                }
            }
            catch (Exception e)
            {
                Plugin.WarnOnce(nameof(ResetBrushOnCleanToolUnequip), e);
            }
        }
    }
}
