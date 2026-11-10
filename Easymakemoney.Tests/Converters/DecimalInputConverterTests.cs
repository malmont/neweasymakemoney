using Easymakemoney.Converters;
using Xunit;

namespace Easymakemoney.Tests.Converters;

public class DecimalInputConverterTests
{
    [Theory]
    [InlineData("12,50", 12.5)]
    [InlineData("12.50", 12.5)]
    [InlineData("1,36", 1.36)]
    [InlineData("100", 100d)]
    [InlineData(" 7,5 ", 7.5)]
    [InlineData("1 250,50", 1250.5)]
    [InlineData("1,250.50", 1250.5)]
    public void Parse_ReadsCommaAndPoint(string text, double expected) =>
        Assert.Equal(expected, DecimalInputConverter.Parse(text));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData(null)]
    public void Parse_ReturnsNull_WhenNotANumber(string? text) =>
        Assert.Null(DecimalInputConverter.Parse(text));
}
