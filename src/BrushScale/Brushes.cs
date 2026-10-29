using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NomadDrive.Features.Inputs;
using NomadDrive.Features.Restoration;
using UnityEngine;

namespace RestorationToolsTweaks
{
    internal static class Brushes
    {
        private static readonly ConditionalWeakTable<object, BrushState> States = new ConditionalWeakTable<object, BrushState>();
        private static readonly HashSet<Type> LoggedTypes = new HashSet<Type>();

        internal static readonly WheelStepFilter Wheel = new WheelStepFilter();

        internal static BrushState Get(RestorationTool tool)
        {
            if (States.TryGetValue(tool, out BrushState state))
            {
                return state;
            }
            float active = GameMembers.RestorationActiveRadius(tool);
            return Add(tool, active, active * GameMembers.RestorationFastMovementMultiplier(tool));
        }

        internal static BrushState Get(CleanTool tool)
        {
            if (States.TryGetValue(tool, out BrushState state))
            {
                return state;
            }
            float active = GameMembers.CleanActiveRadius(tool);
            return Add(tool, active, active);
        }

        private static BrushState Add(object tool, float vanillaActive, float vanillaBrush)
        {
            var state = new BrushState { VanillaActiveRadius = vanillaActive, VanillaBrushRadius = vanillaBrush };
            States.Add(tool, state);
            LogVanillaRadius(tool.GetType(), vanillaBrush);
            return state;
        }

        // The new brush scale of a held, snapped tool after this frame's wheel, or 0 when it did not change.
        internal static float TickWheel(object tool, BrushState state)
        {
            int direction = ReadWheel();
            if (direction == 0)
            {
                return 0f;
            }
            float scale = StepBy(state, direction);
            if (scale != 0f && Plugin.Verbose.Value)
            {
                Plugin.Log.LogDebug($"RestorationToolsTweaks: {tool.GetType().Name} step {state.Step} scale {scale:0.##} radius {state.VanillaBrushRadius * scale:0.###}");
            }
            return scale;
        }

        // The host's drain factor for a tool. A tool with no relayed hit yet counts as 1x, so the drain rate still applies.
        // `gameRate` is the game's drain a second, which work drain needs.
        internal static float HostDrainFactor(object tool, float gameRate)
        {
            States.TryGetValue(tool, out BrushState state);
            float scale = state != null ? state.HostScale : 1f;
            float factor = Plugin.Drain.Value == DrainMode.Work
                ? WorkDrain.Factor(tool, scale, gameRate)
                : BrushScaleLogic.DrainFactor(Plugin.Drain.Value, scale, Plugin.DrainRate.Value);
            // Work drain changes with each read, so the log line is only for a change of a tenth or more.
            if (state != null && System.Math.Abs(factor - state.LoggedDrainFactor) >= 0.1f)
            {
                state.LoggedDrainFactor = factor;
                if (Plugin.Verbose.Value)
                {
                    Plugin.Log.LogDebug($"RestorationToolsTweaks: host {tool.GetType().Name} drain x{factor:0.##}");
                }
            }
            return factor;
        }

        // Returns true when the tool had a changed brush, so the caller puts its vanilla radius back.
        internal static bool Reset(object tool)
        {
            if (!States.TryGetValue(tool, out BrushState state) || state.Step == 0)
            {
                return false;
            }
            state.Step = 0;
            if (Plugin.Verbose.Value)
            {
                Plugin.Log.LogDebug($"RestorationToolsTweaks: {tool.GetType().Name} reset");
            }
            return true;
        }

        // Applies one wheel step. Returns the new brush scale, or 0 when the step did not change.
        private static float StepBy(BrushState state, int direction)
        {
            float stepSize = Plugin.StepSize.Value;
            float cap = BrushScaleLogic.CapScale(Plugin.MaxScale.Value, state.VanillaBrushRadius);
            int step = BrushScaleLogic.StepBy(state.Step, direction, stepSize, cap);
            if (step == state.Step)
            {
                return 0f;
            }
            state.Step = step;
            return BrushScaleLogic.ScaleForStep(step, stepSize, cap);
        }

        // The wheel direction of this frame, or 0. Skipped while a menu has the cursor.
        private static int ReadWheel()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                return 0;
            }
            return Wheel.Step(BaseInputs.GetScrollAxis(), Time.unscaledTime, Plugin.InvertWheel.Value);
        }

        private static void LogVanillaRadius(Type type, float radius)
        {
            if (Plugin.Verbose.Value && LoggedTypes.Add(type))
            {
                Plugin.Log.LogDebug($"RestorationToolsTweaks: {type.Name} vanilla radius {radius:0.###}");
            }
        }
    }
}
