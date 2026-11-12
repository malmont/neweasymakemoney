using System.Collections.ObjectModel;
using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

/// <summary>Faux service de couleurs pour initialiser ProductVariantFormModel sans réseau.</summary>
public class FakeListColorsService : IListColorsServices
{
    public List<ColorsProduct> Colors { get; } = new();

    public Task<ObservableCollection<ColorsProduct>> GetColorsList() =>
        Task.FromResult(new ObservableCollection<ColorsProduct>(Colors));
}

