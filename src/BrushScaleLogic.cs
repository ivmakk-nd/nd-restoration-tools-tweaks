using System;
using System.Numerics;

namespace RestorationToolsTweaks
{
    internal enum DrainMode
    {
        Area,
        Time,
        Work,
    }

    // The mask that a tool changes: the sponge takes dirt off, the grinder takes rust off, the polisher adds
    // polish, the spray can adds paint coverage.
    internal enum WorkMask
    {
        Dirt,
        Rust,
        Polish,
        Paint,
    }

    internal static class BrushScaleLogic
    {
        // The host clamps the radius of each relayed hit to this (RestorationRelayGuard.MaxToolRadius).
        public const float HostMaxRadius = 2f;

        // The largest brush scale of a tool: the configured maximum, but never a brush wider than
        // the host allows. A bad radius (0, negative, NaN) gives 1x, the vanilla brush.
        public static float CapScale(float maxScale, float vanillaRadius)
        {
            if (!float.IsFinite(vanillaRadius) || vanillaRadius <= 0f)
            {
                return 1f;
            }
            return Math.Max(1f, Math.Min(maxScale, HostMaxRadius / vanillaRadius));
        }

        // Step 0 is 1x. A cap between two steps adds one last step that lands on the cap.
        public static int StepBy(int step, int direction, float stepSize, float cap)
        {
            return Math.Max(0, Math.Min(MaxStep(stepSize, cap), step + direction));
        }

        public static float ScaleForStep(int step, float stepSize, float cap)
        {
            return Math.Min(cap, 1f + step * stepSize);
        }

        // Extra spray steps above the game's largest one, with the ratio of the game's geometric steps, so the
        // game's steps keep their scales. The last step stays within maxScale and within a 2 m brush radius.
        // Bad game values give no extra steps.
        public static void SpraySteps(float minScale, float maxScale, int stepCount, float configMax, float sphereRadius,
            out int newStepCount, out float newMaxScale)
        {
            newStepCount = stepCount;
            newMaxScale = maxScale;
            if (stepCount < 2 || !(minScale > 0f) || !(maxScale > minScale) || !(sphereRadius > 0f))
            {
                return;
            }
            double ratio = Math.Pow(maxScale / minScale, 1.0 / (stepCount - 1));
            double cap = Math.Min(configMax, HostMaxRadius / sphereRadius) * 1.0001;
            double top = maxScale;
            while (top * ratio <= cap && newStepCount < 255)
            {
                top *= ratio;
                newStepCount++;
            }
            newMaxScale = (float)top;
        }

        // The game's spray scale of a step (PaintSprayTool.GetBrushScale).
        public static float GameStepScale(int step, float minScale, float maxScale, int stepCount)
        {
            float p = Math.Max(0f, Math.Min(1f, (float)step / Math.Max(1, stepCount - 1)));
            return minScale * (float)Math.Pow(maxScale / minScale, p);
        }

        // Area drain keeps the drain for each unit of surface: the brush area grows with scale².
        // The drain rate of the config multiplies the drain in each mode.
        public static float DrainFactor(DrainMode mode, float scale, float rate)
        {
            return (mode == DrainMode.Area ? scale * scale : 1f) * rate;
        }

        // The surface (m²) that a tool changed between two reads of a panel mask: the sponge and the grinder
        // take dirt and rust off (the mean drops), the polisher and the spray add (the mean rises). A change
        // the other way, or a bad area, is no work.
        public static float WorkArea(WorkMask mask, float meanBefore, float meanNow, float panelArea)
        {
            if (!float.IsFinite(panelArea) || panelArea <= 0f)
            {
                return 0f;
            }
            float change = mask == WorkMask.Dirt || mask == WorkMask.Rust ? meanBefore - meanNow : meanNow - meanBefore;
            return Math.Max(0f, change) * panelArea;
        }

        // Work drain charges each measured m² once, as a debt in the tool's own resource (battery charge,
        // sponge condition, paint).
        public static float OwedFromWork(float workArea, float costPerM2, float rate)
        {
            return Math.Max(0f, workArea * costPerM2 * rate);
        }

        // The factor on the game's drain rate that pays the debt within `paySeconds`.
        public static float DebtFactor(float owed, float paySeconds, float gameRatePerSecond)
        {
            if (!(owed > 0f) || !(paySeconds > 0f) || !(gameRatePerSecond > 0f))
            {
                return 0f;
            }
            return owed / paySeconds / gameRatePerSecond;
        }

        // The debt after the game's Update: only what the game took counts. Nothing taken (the tool was off) or a
        // rise (a refill) keeps the debt for the next use.
        public static float SettleDebt(float owed, float resourceBefore, float resourceAfter)
        {
            return Math.Max(0f, owed - Math.Max(0f, resourceBefore - resourceAfter));
        }

        // The brush scale that the host reads from a relayed hit. A bad input gives 1x, the vanilla drain.
        public static float ScaleFromRadius(float radius, float vanillaRadius)
        {
            if (!float.IsFinite(radius) || !float.IsFinite(vanillaRadius) || vanillaRadius <= 0f)
            {
                return 1f;
            }
            return Math.Max(1f, Math.Min(radius / vanillaRadius, HostMaxRadius / vanillaRadius));
        }

        // The center of the brush preview on the snap plane, the same math as the spray can's
        // PaintSprayTool.TryGetBrushSurfaceContact: along the tool axis, or straight under the tip
        // when the axis meets the plane at a flat angle or points away from it.
        public static bool TryGetContact(Vector3 tip, Vector3 forward, Vector3 snapPoint, Vector3 snapNormal, out Vector3 center)
        {
            float facing = Vector3.Dot(forward, snapNormal);
            if (Math.Abs(facing) >= 0.2f)
            {
                float along = Vector3.Dot(snapPoint - tip, snapNormal) / facing;
                if (along > 0f)
                {
                    center = tip + forward * along;
                    return true;
                }
            }
            float offset = Vector3.Dot(snapPoint - tip, snapNormal);
            center = tip + snapNormal * offset;
            return Math.Abs(offset) > 0.001f;
        }

        private static int MaxStep(float stepSize, float cap)
        {
            return (int)Math.Ceiling((cap - 1f) / stepSize - 0.0001f);
        }
    }

    // Turns the wheel axis into steps the way the game's spray can does (PaintSprayTool.TickBrushInput):
    // a smooth-scrolling wheel sends several pulses for one notch, and they give one step.
    internal sealed class WheelStepFilter
    {
        private const float DeadZone = 0.05f;
        private const float MinStepInterval = 0.04f;

        private int _engagedDirection;
        private float _lastStepTime = float.NegativeInfinity;

        // Returns +1, -1, or 0. `now` is an unscaled time in seconds.
        public int Step(float axis, float now, bool invert)
        {
            if (invert)
            {
                axis = -axis;
            }
            if (Math.Abs(axis) < DeadZone)
            {
                _engagedDirection = 0;
                return 0;
            }
            int direction = axis > 0f ? 1 : -1;
            if (direction == _engagedDirection || now - _lastStepTime < MinStepInterval)
            {
                return 0;
            }
            _engagedDirection = direction;
            _lastStepTime = now;
            return direction;
        }
    }
}
