using RestorationToolsTweaks;
using Xunit;

// The game's spray steps: 7 geometric steps from 0.25x to 2x (x1.414 each), 1x at step 4.
public class SprayStepsTests
{
    private const float GameMin = 0.25f;
    private const float GameMax = 2f;
    private const int GameCount = 7;
    private const float Sphere = 0.08f;

    [Theory]
    [InlineData(5f, 2, 4f)]
    [InlineData(10f, 4, 8f)]
    [InlineData(2f, 0, 2f)]
    public void Max_scale_sets_the_extra_steps(float configMax, int extra, float newMax)
    {
        BrushScaleLogic.SpraySteps(GameMin, GameMax, GameCount, configMax, Sphere, out int count, out float max);
        Assert.Equal(GameCount + extra, count);
        Assert.Equal(newMax, max, 3);
    }

    [Fact]
    public void A_big_sphere_stops_at_the_2m_limit()
    {
        // 2 m / 0.3 m = 6.67x, so the last step is 5.66x.
        BrushScaleLogic.SpraySteps(GameMin, GameMax, GameCount, 10f, 0.3f, out int count, out float max);
        Assert.Equal(GameCount + 3, count);
        Assert.Equal(5.657f, max, 3);
    }

    [Theory]
    [InlineData(0, 0.25f)]
    [InlineData(4, 1f)]
    [InlineData(6, 2f)]
    public void The_game_steps_keep_their_scales(int step, float scale)
    {
        BrushScaleLogic.SpraySteps(GameMin, GameMax, GameCount, 10f, Sphere, out int count, out float max);
        Assert.Equal(scale, BrushScaleLogic.GameStepScale(step, GameMin, max, count), 3);
    }

    [Theory]
    [InlineData(0f, 2f, 7)]
    [InlineData(0.25f, 2f, 1)]
    [InlineData(0.25f, 0.25f, 7)]
    public void Bad_game_values_add_no_steps(float min, float max, int stepCount)
    {
        BrushScaleLogic.SpraySteps(min, max, stepCount, 10f, Sphere, out int count, out float newMax);
        Assert.Equal(stepCount, count);
        Assert.Equal(max, newMax);
    }
}
