using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using NomadDrive.Features.Restoration;
using NomadDrive.Features.Tools;
using UnityEngine;

namespace RestorationToolsTweaks
{
    [BepInPlugin(PluginGuid, "Restoration Tools Tweaks", "1.0.1")]
    [BepInProcess("Nomad Drive.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.ivmakk.nomaddrive.restorationtoolstweaks";

        internal static ManualLogSource Log;
        internal static bool BrushOn;
        internal static bool DrainOn;
        internal static ConfigEntry<bool> Verbose;
        internal static ConfigEntry<bool> InvertWheel;
        internal static ConfigEntry<float> StepSize;
        internal static ConfigEntry<float> MaxScale;
        internal static ConfigEntry<DrainMode> Drain;
        internal static ConfigEntry<bool> ShowPreview;
        internal static ConfigEntry<float> DrainRate;

        private static readonly HashSet<string> WarnedPatches = new HashSet<string>();

        private void Awake()
        {
            Log = Logger;
            Verbose = Config.Bind("General", "Verbose", false, "Log extra detail at Debug level. Keep off in normal play.");
            InvertWheel = Config.Bind("General", "InvertWheel", false, "Turn the wheel down, not up, to make the brush bigger.");
            StepSize = Config.Bind("Brush", "StepSize", 0.25f, new ConfigDescription(
                "How much one wheel notch changes the brush size. Does not change the spray can's steps.",
                new AcceptableValueRange<float>(0.05f, 1f)));
            MaxScale = Config.Bind("Brush", "MaxScale", 5f, new ConfigDescription(
                "The largest brush size, as a multiple of the game's size. Also sets how far the spray can goes above its own largest size. " +
                "A brush radius never goes above 2 m, the game's limit.",
                new AcceptableValueRange<float>(1f, 10f)));
            Drain = Config.Bind("Brush", "DrainMode", DrainMode.Work,
                "Work: a tool drains only for surface it changes, not for surface that is already done or for a tool in the air. " +
                "Area: a bigger brush drains faster, by its area. " +
                "Time: the drain each second stays as in the game at every brush size. Only the host's setting counts.");
            DrainRate = Config.Bind("Brush", "DrainRate", 0.5f, new ConfigDescription(
                "Multiplies the drain of the battery, the sponge, and the spray paint in every mode. 1 is the game's drain, 0.5 is half, 0 is no drain. " +
                "Only the host's setting counts.",
                new AcceptableValueRange<float>(0f, 2f)));
            ShowPreview = Config.Bind("Brush", "ShowPreview", true,
                "Shows a ring on the surface at the brush size while a polisher, grinder, or sponge is snapped.");

            // The drain hooks read the brush state, so the drain part runs only when the brush part is on.
            BrushOn = TryPatchPart("brush", GameMembers.ResolveBrush, BrushPatches,
                "the brush size, the brush ring, and the bigger spray brush are off, the tools work as in the game");
            DrainOn = BrushOn && TryPatchPart("drain", GameMembers.ResolveDrain, DrainPatches,
                "the drain rules are off, the tools drain as in the game");
            if (!BrushOn)
            {
                return;
            }
            Log.LogInfo("Restoration Tools Tweaks loaded.");
        }

        private static readonly Type[] BrushPatches =
        {
            typeof(CaptureWheelOnHeldItem),
            typeof(StepBrushOnRestorationToolUpdate),
            typeof(ResetBrushOnRestorationToolUnequip),
            typeof(StepBrushOnCleanToolUpdate),
            typeof(ResetBrushOnCleanToolUnequip),
            typeof(AddStepsOnPaintSprayToolAwake),
        };

        private static readonly Type[] DrainPatches =
        {
            typeof(ReadScaleOnRelayedHit),
            typeof(DrainOnChargableUpdate),
            typeof(DrainOnCleanToolUpdate),
            typeof(TouchPanelOnRelayedSprayHit),
            typeof(DrainOnPaintSprayToolUpdate),
        };

        // Resolves the members of one part and patches its classes with a Harmony instance of its own, so a
        // missing game member turns off only that part.
        private static bool TryPatchPart(string part, Action resolve, Type[] patches, string offText)
        {
            var harmony = new Harmony(PluginGuid + "." + part);
            try
            {
                resolve();
                foreach (Type patch in patches)
                {
                    harmony.CreateClassProcessor(patch).Patch();
                }
            }
            catch (Exception e)
            {
                harmony.UnpatchSelf();
                Log.LogWarning($"Restoration Tools Tweaks: {offText}. A game member it needs is missing: {e.Message}");
                return false;
            }
            if (Verbose.Value)
            {
                Log.LogDebug($"RestorationToolsTweaks: {part} part on, {patches.Length} patch classes");
            }
            return true;
        }

        private void Update()
        {
            if (!DrainOn)
            {
                return;
            }
            try
            {
                WorkTracker.Tick();
            }
            catch (Exception e)
            {
                WarnOnce(nameof(WorkTracker), e);
            }
        }

        // Logs the first error of each patch once. Many patches run each frame, so a fault must not flood the log.
        internal static void WarnOnce(string patch, Exception e)
        {
            if (WarnedPatches.Add(patch))
            {
                Log.LogWarning($"{patch} failed, later errors of it are not logged: {e}");
            }
        }
    }

    // Private game members, resolved in Awake. FieldRefAccess throws when a field is missing,
    // so a lookup in a static initializer would throw on each frame of a patched Update.
    internal static class GameMembers
    {
        internal static AccessTools.FieldRef<RestorationTool, float> RestorationActiveRadius;
        internal static AccessTools.FieldRef<RestorationTool, float> RestorationFastMovementMultiplier;
        internal static Action<RestorationTool> RestorationOnRadiusMultiplierChanged;
        internal static AccessTools.FieldRef<CleanTool, float> CleanActiveRadius;
        internal static Action<CleanTool, float, float> CleanApplyToolStateToSpheres;
        internal static AccessTools.FieldRef<RestorationTool, Transform> RestorationToolTip;
        internal static AccessTools.FieldRef<RestorationTool, ToolSnapHelper> RestorationSnapHelper;
        internal static AccessTools.FieldRef<CleanTool, Transform> CleanToolTip;
        internal static AccessTools.FieldRef<CleanTool, ToolSnapHelper> CleanSnapHelper;
        internal static AccessTools.FieldRef<ChargableDirectHeldItem, float> BatteryConsumptionSpeed;
        internal static AccessTools.FieldRef<ChargableDirectHeldItem, float> BatteryServerCharge;
        internal static AccessTools.FieldRef<CleanTool, float> CleanDegradationSpeed;
        internal static AccessTools.FieldRef<PaintSprayTool, float> SprayPaintConsumptionRate;
        internal static Func<PaintSprayTool, float> SprayConsumptionMultiplier;
        internal static AccessTools.FieldRef<PaintSprayTool, float> SprayBrushMinScale;
        internal static AccessTools.FieldRef<PaintSprayTool, float> SprayBrushMaxScale;
        internal static AccessTools.FieldRef<PaintSprayTool, int> SprayBrushStepCount;
        internal static AccessTools.FieldRef<PaintSprayTool, float> SpraySphereRadius;

        internal static void ResolveBrush()
        {
            RestorationActiveRadius = AccessTools.FieldRefAccess<RestorationTool, float>("_activeRadius");
            RestorationFastMovementMultiplier = AccessTools.FieldRefAccess<RestorationTool, float>("_fastMovementRadiusMultiplier");
            RestorationOnRadiusMultiplierChanged = AccessTools.MethodDelegate<Action<RestorationTool>>(
                Require(AccessTools.Method(typeof(RestorationTool), "OnRadiusMultiplierChanged")));
            CleanActiveRadius = AccessTools.FieldRefAccess<CleanTool, float>("_activeRadius");
            CleanApplyToolStateToSpheres = AccessTools.MethodDelegate<Action<CleanTool, float, float>>(
                Require(AccessTools.Method(typeof(CleanTool), "ApplyToolStateToSpheres", new[] { typeof(float), typeof(float) })));
            RestorationToolTip = AccessTools.FieldRefAccess<RestorationTool, Transform>("_toolTip");
            RestorationSnapHelper = AccessTools.FieldRefAccess<RestorationTool, ToolSnapHelper>("_snapHelper");
            CleanToolTip = AccessTools.FieldRefAccess<CleanTool, Transform>("_toolTip");
            CleanSnapHelper = AccessTools.FieldRefAccess<CleanTool, ToolSnapHelper>("_snapHelper");
            SprayBrushMinScale = AccessTools.FieldRefAccess<PaintSprayTool, float>("brushMinScale");
            SprayBrushMaxScale = AccessTools.FieldRefAccess<PaintSprayTool, float>("brushMaxScale");
            SprayBrushStepCount = AccessTools.FieldRefAccess<PaintSprayTool, int>("brushStepCount");
            SpraySphereRadius = AccessTools.FieldRefAccess<PaintSprayTool, float>("_sphereRadius");
        }

        internal static void ResolveDrain()
        {
            BatteryConsumptionSpeed = AccessTools.FieldRefAccess<ChargableDirectHeldItem, float>("batteryConsumptionSpeed");
            BatteryServerCharge = AccessTools.FieldRefAccess<ChargableDirectHeldItem, float>("_serverCharge");
            CleanDegradationSpeed = AccessTools.FieldRefAccess<CleanTool, float>("_degradationSpeed");
            SprayPaintConsumptionRate = AccessTools.FieldRefAccess<PaintSprayTool, float>("_paintConsumptionRate");
            SprayConsumptionMultiplier = AccessTools.MethodDelegate<Func<PaintSprayTool, float>>(
                Require(AccessTools.Method(typeof(PaintSprayTool), "GetConsumptionMultiplier")));
        }

        private static System.Reflection.MethodInfo Require(System.Reflection.MethodInfo method)
        {
            if (method == null)
            {
                throw new MissingMethodException("a private method of a restoration tool");
            }
            return method;
        }
    }
}
