using ShiftSoftware.ShiftFrameworkTestingTools;

namespace StockPlusPlus.Test.Tests;

[TestCaseOrderer(typeof(PriorityOrderer))]
public class TestPriority
{
    private static int COUNT = 0;

    [Fact]
    [TestPriority(2)]
    public void One()
    {
        COUNT++;

        Assert.Equal(2, COUNT);
    }

    [Fact]
    [TestPriority(1)]
    public async Task Two()
    {
        COUNT++;

        await Task.Delay(2000, TestContext.Current.CancellationToken);

        Assert.Equal(1, COUNT);
    }
}
