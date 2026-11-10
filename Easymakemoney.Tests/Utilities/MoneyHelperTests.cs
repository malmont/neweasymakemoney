using Easymakemoney.Utilities;
using Xunit;

namespace Easymakemoney.Tests.Utilities;

public class MoneyHelperTests
{
    [Theory]
    [InlineData(12.345, 1235L)]
    [InlineData(1.005, 101L)]
    [InlineData(2.675, 268L)]
    [InlineData(-12.345, -1235L)]
    [InlineData(19.99, 1999L)]
    [InlineData(100.50, 10050L)]
    [InlineData(0.0, 0L)]
    public void ToCents_ValidValues_RoundsAwayFromZero(double input, long expectedCents)
    {
        var result = MoneyHelper.ToCents(input);
        Assert.Equal(expectedCents, result);
    }

    [Fact]
    public void ToCents_NullValue_ReturnsZero()
    {
        var result = MoneyHelper.ToCents(null);
        Assert.Equal(0L, result);
    }
}

