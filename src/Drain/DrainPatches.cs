using System;
using System.Collections.Generic;
using HarmonyLib;
using NomadDrive.Features.Restoration;
using NomadDrive.Features.Tools;

namespace RestorationToolsTweaks
{
    // The [Command] bodies of the hit relay run on the host, also for the host's own tool, with the
    // radius the owner sent. The host reads the brush scale of each tool from them.
    [HarmonyPatch]
    internal static class ReadScaleOnRelayedHit
    {
        private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            const string point = "UserCode_CmdRelayHitPoint__Vector3__Quaternion__Single__Int32__Int32__Single__Single";
            const string line = "UserCode_CmdRelayHitLine__Vector3__Vector3__Quaternion__Single__Int32__Boolean__Int32__Single__Single";
            foreach (Type type in new[] { typeof(RestorationTool), typeof(CleanTool) })
            {
                foreach (string name in new[] { point, line })
                {
                    System.Reflection.MethodInfo method = AccessTools.Method(type, name);
                    if (method == null)
                    {
                        throw new MissingMethodException(type.Name, name);
                    }
                    yield return method;
                }
            }
        }

        private static void Postfix(object __instance, UnityEngine.Vector3 position, float radius)
        {
            try
            {
                BrushState state = __instance is RestorationTool restoration ? Brushes.Get(restoration)
                    : __instance is CleanTool clean ? Brushes.Get(clean)
                    : null;
                if (state == null)
                {
                    return;
                }
                if (__instance is CleanTool)
                {
                    WorkTracker.Touch(__instance, WorkMask.Dirt, position);
                }
                else if (__instance is GrinderTool)
                {
                    WorkTracker.Touch(__instance, WorkMask.Rust, position);
                }
                else if (__instance is PolishTool)
                {
                    WorkTracker.Touch(__instance, WorkMask.Polish, position);
                }
                float scale = BrushScaleLogic.ScaleFromRadius(radius, state.VanillaBrushRadius);
                if (scale != state.HostScale)
                {
                    state.HostScale = scale;
                    if (Plugin.Verbose.Value)
                    {
                        Plugin.Log.LogDebug($"RestorationToolsTweaks: host {__instance.GetType().Name} scale {scale:0.##}");
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.WarnOnce(nameof(ReadScaleOnRelayedHit), e);
            }
        }
    }

    // Area drain: the battery drains in ChargableDirectHeldItem.Update on the host. The prefix scales the
    // drain rate for this one call, and the finalizer puts it back, also when Update throws, so the game's
    // own empty-battery path runs unchanged.
    [HarmonyPatch(typeof(ChargableDirectHeldItem), "Update")]
    internal static class DrainOnChargableUpdate
    {
        private static void Prefix(ChargableDirectHeldItem __instance, out DrainState __state)
        {
            __state = DrainState.Untouched;
            try
            {
                if (!__instance.isServer || !(__instance is RestorationTool))
                {
                    return;
                }
                __state.Before = GameMembers.BatteryServerCharge(__instance);
                __state.Settle = Plugin.Drain.Value == DrainMode.Work;
                float factor = Brushes.HostDrainFactor(__instance, GameMembers.BatteryConsumptionSpeed(__instance));
                if (factor == 1f)
                {
                    return;
                }
                ref float speed = ref GameMembers.BatteryConsumptionSpeed(__instance);
                __state.Speed = speed;
                speed *= factor;
            }
            catch (Exception e)
            {
                Plugin.WarnOnce(nameof(DrainOnChargableUpdate), e);
            }
        }

        private static void Finalizer(ChargableDirectHeldItem __instance, DrainState __state)
        {
            if (!float.IsNaN(__state.Speed))
            {
                GameMembers.BatteryConsumptionSpeed(__instance) = __state.Speed;
            }
            if (__state.Settle)
            {
                DrainState.SettleSafely(__instance, __state.Before, GameMembers.BatteryServerCharge(__instance));
            }
        }
    }

    // Area drain for the sponge: its condition drains in CleanTool.Update on the host.
    [HarmonyPatch(typeof(CleanTool), "Update")]
    internal static class DrainOnCleanToolUpdate
    {
        private static void Prefix(CleanTool __instance, out DrainState __state)
        {
            __state = DrainState.Untouched;
            try
            {
                if (!__instance.isServer)
                {
                    return;
                }
                __state.Before = __instance.Condition;
                __state.Settle = Plugin.Drain.Value == DrainMode.Work;
                float factor = Brushes.HostDrainFactor(__instance, GameMembers.CleanDegradationSpeed(__instance));
                if (factor == 1f)
                {
                    return;
                }
                ref float speed = ref GameMembers.CleanDegradationSpeed(__instance);
                __state.Speed = speed;
                speed *= factor;
            }
            catch (Exception e)
            {
                Plugin.WarnOnce(nameof(DrainOnCleanToolUpdate), e);
            }
        }

        private static void Finalizer(CleanTool __instance, DrainState __state)
        {
            if (!float.IsNaN(__state.Speed))
            {
                GameMembers.CleanDegradationSpeed(__instance) = __state.Speed;
            }
            if (__state.Settle)
            {
                DrainState.SettleSafely(__instance, __state.Before, __instance.Condition);
            }
        }
    }

    // The spray's relayed hits carry a step, not a radius. The host only marks the panel for work drain.
    [HarmonyPatch]
    internal static class TouchPanelOnRelayedSprayHit
    {
        private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            foreach (string name in new[]
            {
                "UserCode_CmdRelayHitPoint__Vector3__Quaternion__Single__Int32__Int32__Byte",
                "UserCode_CmdRelayHitLine__Vector3__Vector3__Quaternion__Single__Int32__Boolean__Int32__Byte",
            })
            {
                System.Reflection.MethodInfo method = AccessTools.Method(typeof(PaintSprayTool), name);
                if (method == null)
                {
                    throw new MissingMethodException(nameof(PaintSprayTool), name);
                }
                yield return method;
            }
        }

        private static void Postfix(PaintSprayTool __instance, UnityEngine.Vector3 position)
        {
            try
            {
                WorkTracker.Touch(__instance, WorkMask.Paint, position);
            }
            catch (Exception e)
            {
                Plugin.WarnOnce(nameof(TouchPanelOnRelayedSprayHit), e);
            }
        }
    }

    // The drain rate for the spray paint: it drains in PaintSprayTool.Update on the host, and the game's
    // own consumption multiplier already grows with the brush area, so only the rate applies here.
    [HarmonyPatch(typeof(PaintSprayTool), "Update")]
    internal static class DrainOnPaintSprayToolUpdate
    {
        private static float _loggedFactor = 1f;

        private static void Prefix(PaintSprayTool __instance, out DrainState __state)
        {
            __state = DrainState.Untouched;
            try
            {
                if (!__instance.isServer)
                {
                    return;
                }
                __state.Before = __instance.PaintCapacity;
                __state.Settle = Plugin.Drain.Value == DrainMode.Work;
                // The game's paint drain a second is the rate times its area multiplier; work drain replaces it.
                float factor = Plugin.Drain.Value == DrainMode.Work
                    ? WorkDrain.Factor(__instance, 1f,
                        GameMembers.SprayPaintConsumptionRate(__instance) * GameMembers.SprayConsumptionMultiplier(__instance))
                    : Plugin.DrainRate.Value;
                if (factor == 1f)
                {
                    return;
                }
                if (Math.Abs(factor - _loggedFactor) >= 0.1f)
                {
                    _loggedFactor = factor;
                    if (Plugin.Verbose.Value)
                    {
                        Plugin.Log.LogDebug($"RestorationToolsTweaks: host PaintSprayTool drain x{factor:0.##}");
                    }
                }
                ref float rate = ref GameMembers.SprayPaintConsumptionRate(__instance);
                __state.Speed = rate;
                rate *= factor;
            }
            catch (Exception e)
            {
                Plugin.WarnOnce(nameof(DrainOnPaintSprayToolUpdate), e);
            }
        }

        private static void Finalizer(PaintSprayTool __instance, DrainState __state)
        {
            if (!float.IsNaN(__state.Speed))
            {
                GameMembers.SprayPaintConsumptionRate(__instance) = __state.Speed;
            }
            if (__state.Settle)
            {
                DrainState.SettleSafely(__instance, __state.Before, __instance.PaintCapacity);
            }
        }
    }

    // What a drain prefix hands its finalizer: the game's rate to put back (NaN when the prefix did not change it),
    // and in work drain the resource before the game's Update, to settle the tool's debt with what the game took.
    internal struct DrainState
    {
        public float Speed;
        public float Before;
        public bool Settle;

        public static DrainState Untouched => new DrainState { Speed = float.NaN };

        // A finalizer must not throw: it would replace the game's own exception, or add one.
        public static void SettleSafely(object tool, float before, float after)
        {
            try
            {
                WorkDrain.Settle(tool, before, after);
            }
            catch (Exception e)
            {
                Plugin.WarnOnce(nameof(WorkDrain.Settle), e);
            }
        }
    }
}
