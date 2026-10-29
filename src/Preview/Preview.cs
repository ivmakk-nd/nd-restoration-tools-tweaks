using NomadDrive.Features.Restoration;
using UnityEngine;

namespace RestorationToolsTweaks
{
    // The brush preview of the polisher, the grinder, and the sponge, drawn by the game's spray preview helper.
    internal static class Preview
    {
        // The look of the spray can's preview (PaintSprayTool: brushGhostOpacity, brushGhostRimWidth,
        // brushGhostEmission, _hardness), in white.
        private const float Hardness = 6f;
        private const float FillOpacity = 0.55f;
        private const float RimWidth = 0.004f;
        private const float Emission = 0.3f;

        internal static void Show(object tool, BrushState state, Transform tip, ToolSnapHelper snap, float radius)
        {
            if (!Plugin.ShowPreview.Value || tip == null || snap == null ||
                !BrushScaleLogic.TryGetContact(ToNumerics(tip.position), ToNumerics(tip.forward),
                    ToNumerics(snap.SnapPoint), ToNumerics(snap.SnapNormal), out System.Numerics.Vector3 center))
            {
                Hide(tool, state);
                return;
            }
            SurfaceBrushGhost.Set(new Vector3(center.X, center.Y, center.Z), radius, Hardness, Color.white, FillOpacity, RimWidth, Emission);
            SurfaceBrushGhost.SetAssist(0f, 0f, 0f, 0f);
            if (!state.PreviewVisible)
            {
                state.PreviewVisible = true;
                if (Plugin.Verbose.Value)
                {
                    Plugin.Log.LogDebug($"RestorationToolsTweaks: {tool.GetType().Name} preview on");
                }
            }
        }

        internal static void Hide(object tool, BrushState state)
        {
            if (!state.PreviewVisible)
            {
                return;
            }
            state.PreviewVisible = false;
            SurfaceBrushGhost.Clear();
            if (Plugin.Verbose.Value)
            {
                Plugin.Log.LogDebug($"RestorationToolsTweaks: {tool.GetType().Name} preview off");
            }
        }

        private static System.Numerics.Vector3 ToNumerics(Vector3 v)
        {
            return new System.Numerics.Vector3(v.x, v.y, v.z);
        }
    }
}
