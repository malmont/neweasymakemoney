using System.Collections.ObjectModel;
using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

/// <summary>Faux service de tailles pour initialiser ProductVariantFormModel sans réseau.</summary>
public class FakeListSizesService : IListSizesServices
{
    public List<SizeProduct> Sizes { get; } = new();

    public Task<ObservableCollection<SizeProduct>> GetSizesList() =>
        Task.FromResult(new ObservableCollection<SizeProduct>(Sizes));
}

