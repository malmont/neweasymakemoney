using Easymakemoney.Models;
using Xunit;

namespace Easymakemoney.Tests.Models;

public class ListCollectionTests
{
    [Theory]
    [InlineData("period", true, false)]
    [InlineData("manual", false, true)]
    [InlineData(null, false, true)]
    [InlineData("", false, true)]
    public void KindProperties_IsPeriodAndCanDelete_ReflectKind(string? kind, bool expectedIsPeriod, bool expectedCanDelete)
    {
        var collection = new ListCollection
        {
            nomCollection = "Automne 2026",
            photoCollection = "photo.png",
            startDateCollection = "2026-09-01T00:00:00Z",
            endDateCollection = "2026-11-30T00:00:00Z",
            kind = kind
        };

        Assert.Equal(expectedIsPeriod, collection.IsPeriod);
        Assert.Equal(expectedCanDelete, collection.CanDelete);
    }

    [Fact]
    public void StatusText_WhenClosed_ReturnsCloturee()
    {
        var collection = new ListCollection
        {
            nomCollection = "Hiver 2025",
            photoCollection = "photo.png",
            startDateCollection = "2025-12-01T00:00:00Z",
            endDateCollection = "2026-02-28T00:00:00Z",
            IsClosed = true
        };

        Assert.Equal("Clôturée", collection.StatusText);
    }

    [Fact]
    public void StatusText_WhenFutureDate_ReturnsAVenir()
    {
        var futureDate = DateTimeOffset.UtcNow.AddDays(30).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var collection = new ListCollection
        {
            nomCollection = "Période Future",
            photoCollection = "photo.png",
            startDateCollection = futureDate,
            endDateCollection = DateTimeOffset.UtcNow.AddDays(120).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            IsClosed = false
        };

        Assert.Equal("À venir", collection.StatusText);
    }

    [Fact]
    public void StatusText_WhenPastDate_ReturnsEnCours()
    {
        var pastDate = DateTimeOffset.UtcNow.AddDays(-30).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var collection = new ListCollection
        {
            nomCollection = "Période En Cours",
            photoCollection = "photo.png",
            startDateCollection = pastDate,
            endDateCollection = DateTimeOffset.UtcNow.AddDays(60).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            IsClosed = false
        };

        Assert.Equal("En cours", collection.StatusText);
    }
}

