using RestorationToolsTweaks;
using Xunit;

public class WheelStepFilterTests
{
    [Fact]
    public void Several_pulses_of_one_notch_give_one_step()
    {
        var filter = new WheelStepFilter();
        Assert.Equal(+1, filter.Step(0.5f, 0.00f, invert: false));
        Assert.Equal(0, filter.Step(0.5f, 0.01f, invert: false));
        Assert.Equal(0, filter.Step(0.5f, 0.10f, invert: false));
    }

    [Fact]
    public void A_value_in_the_dead_zone_re_arms_the_filter()
    {
        var filter = new WheelStepFilter();
        Assert.Equal(+1, filter.Step(0.5f, 0.00f, invert: false));
        Assert.Equal(0, filter.Step(0.01f, 0.02f, invert: false));
        Assert.Equal(+1, filter.Step(0.5f, 0.05f, invert: false));
    }

    [Fact]
    public void A_direction_change_gives_a_new_step()
    {
        var filter = new WheelStepFilter();
        Assert.Equal(+1, filter.Step(0.5f, 0.00f, invert: false));
        Assert.Equal(-1, filter.Step(-0.5f, 0.05f, invert: false));
    }

    [Fact]
    public void A_new_step_waits_for_the_minimum_interval()
    {
        var filter = new WheelStepFilter();
        Assert.Equal(+1, filter.Step(0.5f, 0.00f, invert: false));
        Assert.Equal(0, filter.Step(-0.5f, 0.02f, invert: false));
        Assert.Equal(-1, filter.Step(-0.5f, 0.05f, invert: false));
    }

    [Fact]
    public void Inversion_turns_wheel_up_into_a_step_down()
    {
        var filter = new WheelStepFilter();
        Assert.Equal(-1, filter.Step(0.5f, 0.00f, invert: true));
    }

    [Fact]
    public void No_wheel_gives_no_step()
    {
        var filter = new WheelStepFilter();
        Assert.Equal(0, filter.Step(0f, 0.00f, invert: false));
    }
}
