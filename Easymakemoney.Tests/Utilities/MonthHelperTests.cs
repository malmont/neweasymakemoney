using Easymakemoney.Utilities;
using Xunit;

namespace Easymakemoney.Tests.Utilities;

public class MonthHelperTests
{
    [Theory]
    [InlineData("1", "janvier")]
    [InlineData("2", "février")]
    [InlineData("3", "mars")]
    [InlineData("4", "avril")]
    [InlineData("5", "mai")]
    [InlineData("6", "juin")]
    [InlineData("7", "juillet")]
    [InlineData("8", "août")]
    [InlineData("9", "septembre")]
    [InlineData("10", "octobre")]
    [InlineData("11", "novembre")]
    [InlineData("12", "décembre")]
    public void FormatMonthName_ValidMonths_ReturnsFrenchMonthName(string input, string expected)
    {
        var result = MonthHelper.FormatMonthName(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("0", "0")]
    [InlineData("13", "13")]
    [InlineData("-5", "-5")]
    [InlineData("99", "99")]
    public void FormatMonthName_OutOfRangeNumbers_ReturnsOriginalValue(string input, string expected)
    {
        var result = MonthHelper.FormatMonthName(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("mars", "mars")]
    [InlineData("T1", "T1")]
    [InlineData("March", "March")]
    [InlineData("abc", "abc")]
    public void FormatMonthName_NonNumericText_ReturnsOriginalValue(string input, string expected)
    {
        var result = MonthHelper.FormatMonthName(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatMonthName_NullOrEmpty_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, MonthHelper.FormatMonthName(null));
        Assert.Equal(string.Empty, MonthHelper.FormatMonthName(""));
    }

    [Theory]
    [InlineData("   ", "")]
    [InlineData(" \t\n ", "")]
    public void FormatMonthName_WhitespaceOnly_ReturnsEmptyString(string input, string expected)
    {
        var result = MonthHelper.FormatMonthName(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(" 03 ", "mars")]
    [InlineData(" 10 ", "octobre")]
    [InlineData("  1  ", "janvier")]
    [InlineData("08", "août")]
    public void FormatMonthName_PaddedWithSpacesOrZeros_ReturnsCorrectMonth(string input, string expected)
    {
        var result = MonthHelper.FormatMonthName(input);
        Assert.Equal(expected, result);
    }
}

