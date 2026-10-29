using System.Numerics;
using RestorationToolsTweaks;
using Xunit;

// The surface is the plane z = 0, facing +z. The tool tip sits 0.3 m in front of it.
public class PreviewContactTests
{
    private static readonly Vector3 SnapPoint = Vector3.Zero;
    private static readonly Vector3 SnapNormal = Vector3.UnitZ;

    [Fact]
    public void A_tip_that_points_at_the_surface_hits_the_point_in_front_of_it()
    {
        Assert.True(BrushScaleLogic.TryGetContact(new Vector3(0f, 0f, 0.3f), -Vector3.UnitZ, SnapPoint, SnapNormal, out Vector3 center));
        AssertNear(Vector3.Zero, center);
    }

    [Fact]
    public void A_tip_at_45_degrees_hits_the_surface_along_its_axis()
    {
        Vector3 forward = Vector3.Normalize(new Vector3(1f, 0f, -1f));
        Assert.True(BrushScaleLogic.TryGetContact(new Vector3(0f, 0f, 0.3f), forward, SnapPoint, SnapNormal, out Vector3 center));
        AssertNear(new Vector3(0.3f, 0f, 0f), center);
    }

    [Fact]
    public void A_tip_at_a_flat_angle_falls_back_to_the_point_under_it()
    {
        Vector3 forward = Vector3.Normalize(new Vector3(1f, 0f, -0.1f));
        Assert.True(BrushScaleLogic.TryGetContact(new Vector3(0.5f, 0f, 0.3f), forward, SnapPoint, SnapNormal, out Vector3 center));
        AssertNear(new Vector3(0.5f, 0f, 0f), center);
    }

    [Fact]
    public void A_tip_that_points_away_falls_back_to_the_point_under_it()
    {
        Assert.True(BrushScaleLogic.TryGetContact(new Vector3(0.5f, 0f, 0.3f), Vector3.UnitZ, SnapPoint, SnapNormal, out Vector3 center));
        AssertNear(new Vector3(0.5f, 0f, 0f), center);
    }

    [Fact]
    public void A_tip_on_the_surface_pointing_away_has_no_contact()
    {
        Assert.False(BrushScaleLogic.TryGetContact(new Vector3(0.5f, 0f, 0f), Vector3.UnitZ, SnapPoint, SnapNormal, out _));
    }

    private static void AssertNear(Vector3 expected, Vector3 actual)
    {
        Assert.True(Vector3.Distance(expected, actual) < 0.0001f, $"expected {expected}, got {actual}");
    }
}
