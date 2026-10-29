using RestorationToolsTweaks;
using Xunit;

public class WorkDrainTests
{
    [Fact]
    public void Rust_taken_off_is_work()
    {
        // A 1.6 m2 panel whose rust mean drops from 0.30 to 0.25: 0.08 m2 of rust taken off.
        Assert.Equal(0.08f, BrushScaleLogic.WorkArea(WorkMask.Rust, 0.30f, 0.25f, 1.6f), 4);
    }

    [Fact]
    public void Polish_added_is_work()
    {
        Assert.Equal(0.5f, BrushScaleLogic.WorkArea(WorkMask.Polish, 0.10f, 0.15f, 10f), 4);
    }

    [Fact]
    public void A_change_the_wrong_way_is_no_work()
    {
        // Rust that grows (for example from weathering) is not work of the grinder.
        Assert.Equal(0f, BrushScaleLogic.WorkArea(WorkMask.Rust, 0.25f, 0.30f, 1.6f));
        Assert.Equal(0f, BrushScaleLogic.WorkArea(WorkMask.Paint, 0.50f, 0.40f, 1.6f));
    }

    [Fact]
    public void A_bad_area_is_no_work()
    {
        Assert.Equal(0f, BrushScaleLogic.WorkArea(WorkMask.Dirt, 0.5f, 0.2f, float.NaN));
    }

    [Fact]
    public void One_second_of_calibrated_work_owes_one_second_of_the_game_drain()
    {
        // The grinder: 0.0604 m2 a second at 1x, cost 10.76 for each m2, game drain 0.65 a second.
        Assert.Equal(0.65f, BrushScaleLogic.OwedFromWork(0.0604f, 10.76f, 1f), 2);
    }

    [Fact]
    public void The_drain_rate_multiplies_what_is_owed()
    {
        Assert.Equal(0.5f, BrushScaleLogic.OwedFromWork(0.1f, 10f, 0.5f), 4);
    }

    [Fact]
    public void No_debt_is_no_drain()
    {
        Assert.Equal(0f, BrushScaleLogic.DebtFactor(0f, 0.25f, 0.65f));
    }

    [Fact]
    public void A_debt_is_paid_within_the_pay_time()
    {
        // 0.65 owed, paid in 0.25 s at a game rate of 0.65 a second: 4 times the game's rate.
        Assert.Equal(4f, BrushScaleLogic.DebtFactor(0.65f, 0.25f, 0.65f), 4);
    }

    [Fact]
    public void A_bad_pay_time_or_game_rate_is_no_drain()
    {
        Assert.Equal(0f, BrushScaleLogic.DebtFactor(0.5f, 0f, 0.65f));
        Assert.Equal(0f, BrushScaleLogic.DebtFactor(0.5f, 0.25f, 0f));
    }

    [Fact]
    public void What_the_game_took_comes_off_the_debt()
    {
        Assert.Equal(0.3f, BrushScaleLogic.SettleDebt(0.5f, 0.9f, 0.7f), 4);
    }

    [Fact]
    public void Taking_more_than_owed_leaves_no_debt()
    {
        Assert.Equal(0f, BrushScaleLogic.SettleDebt(0.1f, 0.9f, 0.5f));
    }

    [Fact]
    public void Nothing_taken_keeps_the_debt()
    {
        // The tool was off, so the game's Update took nothing; the debt is paid at the next use.
        Assert.Equal(0.5f, BrushScaleLogic.SettleDebt(0.5f, 0.9f, 0.9f), 4);
        // A refill (the bucket) between the reads is not a payment.
        Assert.Equal(0.5f, BrushScaleLogic.SettleDebt(0.5f, 0.4f, 0.9f), 4);
    }
}
