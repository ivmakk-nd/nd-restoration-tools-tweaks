using RestorationToolsTweaks;
using Xunit;

public class DrainTests
{
    [Fact]
    public void Area_drain_at_2x_drains_4_times_as_fast()
    {
        Assert.Equal(4f, BrushScaleLogic.DrainFactor(DrainMode.Area, 2f, 1f), 4);
    }

    [Fact]
    public void Time_drain_at_2x_keeps_the_drain_of_the_game()
    {
        Assert.Equal(1f, BrushScaleLogic.DrainFactor(DrainMode.Time, 2f, 1f), 4);
    }

    [Fact]
    public void At_1x_both_modes_keep_the_drain_of_the_game()
    {
        Assert.Equal(1f, BrushScaleLogic.DrainFactor(DrainMode.Area, 1f, 1f), 4);
        Assert.Equal(1f, BrushScaleLogic.DrainFactor(DrainMode.Time, 1f, 1f), 4);
    }

    [Fact]
    public void Half_rate_in_area_drain_at_2x_drains_2_times_as_fast()
    {
        Assert.Equal(2f, BrushScaleLogic.DrainFactor(DrainMode.Area, 2f, 0.5f), 4);
    }

    [Fact]
    public void Half_rate_in_time_drain_halves_the_drain()
    {
        Assert.Equal(0.5f, BrushScaleLogic.DrainFactor(DrainMode.Time, 3f, 0.5f), 4);
    }

    [Fact]
    public void Rate_0_stops_the_drain_in_both_modes()
    {
        Assert.Equal(0f, BrushScaleLogic.DrainFactor(DrainMode.Area, 2f, 0f), 4);
        Assert.Equal(0f, BrushScaleLogic.DrainFactor(DrainMode.Time, 2f, 0f), 4);
    }

    [Fact]
    public void A_relayed_radius_twice_the_vanilla_one_is_2x()
    {
        Assert.Equal(2f, BrushScaleLogic.ScaleFromRadius(0.45f, 0.225f), 4);
    }

    [Fact]
    public void A_relayed_radius_above_the_host_limit_is_capped()
    {
        // The host clamps a relayed radius to 2 m, so a 0.5 m tool is at most 4x.
        Assert.Equal(4f, BrushScaleLogic.ScaleFromRadius(3f, 0.5f), 4);
    }

    [Fact]
    public void A_relayed_radius_below_the_vanilla_one_is_1x()
    {
        Assert.Equal(1f, BrushScaleLogic.ScaleFromRadius(0.1f, 0.225f), 4);
    }

    [Theory]
    [InlineData(0.45f, 0f)]
    [InlineData(0.45f, -0.2f)]
    [InlineData(0.45f, float.NaN)]
    [InlineData(float.NaN, 0.225f)]
    [InlineData(float.PositiveInfinity, 0.225f)]
    public void A_bad_input_is_1x(float radius, float vanillaRadius)
    {
        Assert.Equal(1f, BrushScaleLogic.ScaleFromRadius(radius, vanillaRadius), 4);
    }
}
