using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NomadDrive.Features.Restoration;
using PaintCore;
using UnityEngine;

namespace RestorationToolsTweaks
{
    // The work-drain readers on the host: one WorkCounter for each mask of each panel that a tool touched in
    // the last few seconds. Each read adds its work to the panel, and one tool takes it, so each measured m² is
    // charged once. A counter that no tool touched for a while is destroyed: PaintCore releases its readers only
    // then, and its manager walks every live reader each frame.
    internal static class WorkTracker
    {
        private const float TouchInterval = 0.25f;
        private const float ReadInterval = 0.25f;
        private const float ActiveSeconds = 2f;
        private const float FindRadius = 0.05f;
        private const int MaskCount = 4;

        private sealed class Panel
        {
            public RestorableVehicleSurface Surface;
            public float Area;
            public bool Active;
            public readonly WorkCounter[] Counters = new WorkCounter[MaskCount];
            public readonly float[] LastMean = new float[MaskCount];
            public readonly bool[] HasLast = new bool[MaskCount];
            public readonly float[] LastTouched = new float[MaskCount];
            public readonly bool[] Unreadable = new bool[MaskCount];
            public readonly float[] PendingWork = new float[MaskCount];
        }

        private sealed class ToolTouch
        {
            public float NextTouch;
            public Panel Panel;
            public WorkMask Mask;
        }

        private static readonly Dictionary<RestorableVehicleSurface, Panel> Panels = new Dictionary<RestorableVehicleSurface, Panel>();
        private static readonly List<Panel> ActivePanels = new List<Panel>();
        private static readonly ConditionalWeakTable<object, ToolTouch> Touches = new ConditionalWeakTable<object, ToolTouch>();
        private static readonly Collider[] Hits = new Collider[8];
        private static readonly HashSet<System.Type> LoggedRates = new HashSet<System.Type>();
        private static float _nextTick;

        // Called on the host for each relayed hit. Finds the panel under the hit at most every 0.25 s for each tool.
        // Only work drain reads masks; Verbose also reads them, for the calibration of a tool's cost.
        internal static void Touch(object tool, WorkMask mask, Vector3 position)
        {
            if (Plugin.Drain.Value != DrainMode.Work && !Plugin.Verbose.Value)
            {
                return;
            }
            ToolTouch touch = Touches.GetOrCreateValue(tool);
            if (Time.time < touch.NextTouch)
            {
                return;
            }
            touch.NextTouch = Time.time + TouchInterval;
            LogGameRate(tool);
            RestorableVehicleSurface surface = FindSurface(position);
            if (surface == null)
            {
                return;
            }
            if (!Panels.TryGetValue(surface, out Panel panel))
            {
                panel = new Panel { Surface = surface, Area = float.NaN };
                Panels.Add(surface, panel);
            }
            int i = (int)mask;
            panel.LastTouched[i] = Time.time;
            touch.Panel = panel;
            touch.Mask = mask;
            if (panel.Counters[i] == null && !panel.Unreadable[i])
            {
                panel.Counters[i] = CreateCounter(panel, mask);
                panel.Unreadable[i] = panel.Counters[i] == null;
            }
            if (panel.Counters[i] != null && !panel.Active)
            {
                panel.Active = true;
                ActivePanels.Add(panel);
            }
        }

        // Hands the work read on the tool's panel since the last call to this tool, once. 0 when the tool touched
        // nothing yet. null when that panel's mask cannot be read, so the caller falls back to area drain.
        internal static float? TakeWork(object tool)
        {
            if (!Touches.TryGetValue(tool, out ToolTouch touch) || touch.Panel == null)
            {
                return 0f;
            }
            int i = (int)touch.Mask;
            Panel panel = touch.Panel;
            WorkCounter counter = panel.Counters[i];
            if (counter != null && counter.Unreadable)
            {
                panel.Unreadable[i] = true;
            }
            if (panel.Unreadable[i])
            {
                return null;
            }
            float work = panel.PendingWork[i];
            panel.PendingWork[i] = 0f;
            return work;
        }

        // Called each frame from the plugin. Destroys the counters that no tool touched recently.
        internal static void Tick()
        {
            if (Time.time < _nextTick || ActivePanels.Count == 0)
            {
                return;
            }
            _nextTick = Time.time + ReadInterval;
            for (int p = ActivePanels.Count - 1; p >= 0; p--)
            {
                Panel panel = ActivePanels[p];
                if (panel.Surface == null)
                {
                    Panels.Remove(panel.Surface);
                    ActivePanels.RemoveAt(p);
                    continue;
                }
                bool any = false;
                for (int i = 0; i < MaskCount; i++)
                {
                    WorkCounter counter = panel.Counters[i];
                    if (counter == null)
                    {
                        continue;
                    }
                    if (Time.time - panel.LastTouched[i] > ActiveSeconds)
                    {
                        Object.Destroy(counter.gameObject);
                        panel.Counters[i] = null;
                        panel.HasLast[i] = false;
                        panel.PendingWork[i] = 0f;
                    }
                    else
                    {
                        any = true;
                    }
                }
                if (!any)
                {
                    panel.Active = false;
                    ActivePanels.RemoveAt(p);
                }
            }
        }

        // The game's drain for each second of a tool type, once, for the work-drain calibration.
        private static void LogGameRate(object tool)
        {
            if (!Plugin.Verbose.Value || !LoggedRates.Add(tool.GetType()))
            {
                return;
            }
            float rate = tool is RestorationTool restoration ? GameMembers.BatteryConsumptionSpeed(restoration)
                : tool is CleanTool clean ? GameMembers.CleanDegradationSpeed(clean)
                : tool is PaintSprayTool spray ? GameMembers.SprayPaintConsumptionRate(spray)
                : float.NaN;
            Plugin.Log.LogDebug($"RestorationToolsTweaks: game drain {tool.GetType().Name} {rate:0.#####} per s at 1x");
        }

        private static RestorableVehicleSurface FindSurface(Vector3 position)
        {
            int count = Physics.OverlapSphereNonAlloc(position, FindRadius, Hits, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                RestorableVehicleSurface surface = Hits[i].GetComponentInParent<RestorableVehicleSurface>();
                if (surface != null)
                {
                    return surface;
                }
            }
            return null;
        }

        private static CwPaintableTexture MaskTexture(RestorableVehicleSurface surface, WorkMask mask)
        {
            switch (mask)
            {
                case WorkMask.Dirt: return surface.DirtMaskTexture;
                case WorkMask.Rust: return surface.RustMaskTexture;
                case WorkMask.Polish: return surface.PolishMaskTexture;
                default: return surface.PaintMaskTexture;
            }
        }

        private static WorkCounter CreateCounter(Panel panel, WorkMask mask)
        {
            CwPaintableTexture texture = MaskTexture(panel.Surface, mask);
            if (texture == null || !panel.Surface.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
            {
                Plugin.Log.LogWarning($"RestorationToolsTweaks: no {mask} mask on panel {panel.Surface.name}, work drain cannot read it");
                return null;
            }
            if (float.IsNaN(panel.Area))
            {
                panel.Area = MeshArea(filter, texture.Slot.Index);
                if (float.IsNaN(panel.Area))
                {
                    return null;
                }
            }
            // Made inactive, so OnEnable runs after the reader is set up.
            var host = new GameObject($"RestorationToolsTweaks.WorkCounter.{mask}");
            host.SetActive(false);
            host.transform.SetParent(panel.Surface.transform, false);
            WorkCounter counter = host.AddComponent<WorkCounter>();
            counter.MaskMesh = filter.sharedMesh;
            counter.MaskSubmesh = texture.Slot.Index;
            counter.PaintableTexture = texture;
            counter.Interval = ReadInterval;
            counter.Async = true;
            counter.DownsampleSteps = 3;
            counter.ReadAtStart = true;
            counter.OnUpdated += () => OnRead(panel, mask, counter);
            host.SetActive(true);
            return counter;
        }

        // The first read of a counter is the baseline. Each later read adds the change to the panel's pending work.
        private static void OnRead(Panel panel, WorkMask mask, WorkCounter counter)
        {
            int i = (int)mask;
            float mean = counter.Mean;
            float before = panel.LastMean[i];
            bool hadLast = panel.HasLast[i];
            panel.LastMean[i] = mean;
            panel.HasLast[i] = true;
            float work = hadLast ? BrushScaleLogic.WorkArea(mask, before, mean, panel.Area) : 0f;
            panel.PendingWork[i] += work;
            if (!Plugin.Verbose.Value)
            {
                return;
            }
            string name = panel.Surface != null ? panel.Surface.name : "?";
            if (!hadLast)
            {
                Plugin.Log.LogDebug($"RestorationToolsTweaks: work {mask} panel {name} area {panel.Area:0.###} mean {mean:0.####} baseline t {Time.time:0.00}");
            }
            else
            {
                Plugin.Log.LogDebug($"RestorationToolsTweaks: work {mask} panel {name} area {panel.Area:0.###} mean {mean:0.####} delta {work:0.#####} t {Time.time:0.00}");
            }
            Plugin.Log.LogDebug($"RestorationToolsTweaks: read {counter.PassMs:0.###} ms pass, {counter.RequestMs:0.###} ms request");
        }

        // The world-space area of one submesh, once for each panel. NaN when the mesh cannot be read on the CPU.
        private static float MeshArea(MeshFilter filter, int submesh)
        {
            Mesh mesh = filter.sharedMesh;
            if (!mesh.isReadable || submesh < 0 || submesh >= mesh.subMeshCount)
            {
                Plugin.Log.LogWarning($"RestorationToolsTweaks: mesh of panel {filter.name} is not readable, its area is unknown");
                return float.NaN;
            }
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.GetTriangles(submesh);
            Matrix4x4 world = filter.transform.localToWorldMatrix;
            double area = 0;
            for (int t = 0; t + 2 < triangles.Length; t += 3)
            {
                Vector3 a = world.MultiplyPoint3x4(vertices[triangles[t]]);
                Vector3 b = world.MultiplyPoint3x4(vertices[triangles[t + 1]]);
                Vector3 c = world.MultiplyPoint3x4(vertices[triangles[t + 2]]);
                area += Vector3.Cross(b - a, c - a).magnitude * 0.5;
            }
            return (float)area;
        }
    }
}
