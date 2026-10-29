using RestorationToolsTweaks;
using Xunit;

public class BrushScaleLogicTests
{
    // A polisher: 0.1 m active radius x 1.5 fast-movement multiplier.
    private const float Polisher = 0.15f;

    [Fact]
    public void Wheel_up_from_1x_gives_1_25x()
    {
        float cap = BrushScaleLogic.CapScale(5f, Polisher);
        int step = BrushScaleLogic.StepBy(0, +1, 0.25f, cap);
        Assert.Equal(1.25f, BrushScaleLogic.ScaleForStep(step, 0.25f, cap), 4);
    }

    [Fact]
    public void Wheel_down_from_2x_gives_1_75x()
    {
        float cap = BrushScaleLogic.CapScale(5f, Polisher);
        int step = BrushScaleLogic.StepBy(4, -1, 0.25f, cap);
        Assert.Equal(1.75f, BrushScaleLogic.ScaleForStep(step, 0.25f, cap), 4);
    }

    [Fact]
    public void Wheel_up_at_5x_stays_5x()
    {
        float cap = BrushScaleLogic.CapScale(5f, Polisher);
        int step = BrushScaleLogic.StepBy(16, +1, 0.25f, cap);
        Assert.Equal(16, step);
        Assert.Equal(5f, BrushScaleLogic.ScaleForStep(step, 0.25f, cap), 4);
    }

    [Fact]
    public void Wheel_down_at_1x_stays_1x()
    {
        float cap = BrushScaleLogic.CapScale(5f, Polisher);
        int step = BrushScaleLogic.StepBy(0, -1, 0.25f, cap);
        Assert.Equal(0, step);
        Assert.Equal(1f, BrushScaleLogic.ScaleForStep(step, 0.25f, cap), 4);
    }

    [Fact]
    public void A_half_metre_tool_stops_at_4x_for_the_2m_host_limit()
    {
        float cap = BrushScaleLogic.CapScale(5f, 0.5f);
        Assert.Equal(4f, cap, 4);
        Assert.Equal(12, BrushScaleLogic.StepBy(12, +1, 0.25f, cap));
    }

    [Fact]
    public void A_cap_between_steps_gives_one_last_step_at_the_cap()
    {
        // A sponge of 0.45 m: 2 / 0.45 = 4.44x, between the steps 4.25x and 4.5x.
        float cap = BrushScaleLogic.CapScale(5f, 0.45f);
        int step = BrushScaleLogic.StepBy(13, +1, 0.25f, cap);
        Assert.Equal(14, step);
        Assert.Equal(2f / 0.45f, BrushScaleLogic.ScaleForStep(step, 0.25f, cap), 4);
        Assert.Equal(14, BrushScaleLogic.StepBy(step, +1, 0.25f, cap));
    }

    [Fact]
    public void A_tool_above_2m_stays_at_1x()
    {
        float cap = BrushScaleLogic.CapScale(5f, 2.5f);
        Assert.Equal(1f, cap);
        Assert.Equal(0, BrushScaleLogic.StepBy(0, +1, 0.25f, cap));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-0.1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void A_bad_vanilla_radius_gives_1x(float radius)
    {
        float cap = BrushScaleLogic.CapScale(5f, radius);
        Assert.Equal(1f, cap);
        Assert.Equal(0, BrushScaleLogic.StepBy(0, +1, 0.25f, cap));
    }
}
