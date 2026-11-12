using Easymakemoney.Services.Offline;
using Easymakemoney.Utilities;
using Xunit;

namespace Easymakemoney.Tests.Models;

public class FrenchDateTests
{
    [Fact]
    public void PremierDuMois_EcritPremier()
    {
        Assert.Equal("1er décembre 2026", FrenchDate.Format(new DateTime(2026, 12, 1), "d MMMM yyyy"));
        Assert.Equal("2 décembre 2026", FrenchDate.Format(new DateTime(2026, 12, 2), "d MMMM yyyy"));
        Assert.Equal("1er novembre à 9 h 05", FrenchDate.Format(new DateTime(2026, 11, 1, 9, 5, 0), "d MMMM 'à' H' h 'mm"));
    }

    [Fact]
    public void BandeauHorsConnexion_PremierDuMois()
    {
        var now = new DateTime(2026, 11, 12, 18, 0, 0);
        Assert.Equal("Hors connexion : données du 1er novembre à 9 h 30", OfflineState.Text(new DateTime(2026, 11, 1, 9, 30, 0), now));
    }
}
