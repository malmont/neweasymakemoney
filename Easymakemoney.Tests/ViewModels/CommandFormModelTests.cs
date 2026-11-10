using System.Collections.ObjectModel;
using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.UseCase.ListFournisseurUseCase;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.FormModels;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

[Collection("SiteCurrency")]
public class CommandFormModelTests : IDisposable
{
    public CommandFormModelTests()
    {
        SiteCurrency.Reset();
    }

    public void Dispose()
    {
        SiteCurrency.Reset();
    }

    private class ImmediateEmptyFournisseursService : IListFourniseursServices
    {
        public Task<ObservableCollection<ListFournisseur>> GetFourniseursList() =>
            Task.FromResult(new ObservableCollection<ListFournisseur>());

        public Task<ObservableCollection<TypeFournisseur>> GetTypeFournisseurs() =>
            Task.FromResult(new ObservableCollection<TypeFournisseur>());

        public Task<bool> PostFourniseurs(ListFournisseur newFourniseurs) =>
            Task.FromResult(true);

        public Task<bool> DeleteFourniseurs(int id) =>
            Task.FromResult(true);
    }

    private static CommandFormModel CreateFormModel()
    {
        var fakeService = new ImmediateEmptyFournisseursService();
        var useCase = new GetListFournisseursUseCase(fakeService);
        return new CommandFormModel(useCase);
    }

    private static ListFournisseur MakeFournisseur(string name = "Fournisseur Test", string? purchaseCurrency = null) => new()
    {
        name = name,
        purchaseCurrency = purchaseCurrency
    };

    [Fact]
    public void NeedsExchangeRate_WhenNoFournisseurSelected_ReturnsFalseAndDefaultLabels()
    {
        var model = CreateFormModel();
        model.SelectedFournisseurs = null!;

        Assert.False(model.NeedsExchangeRate);
        Assert.Equal("Budget", model.BudgetLabel);
        Assert.Equal(string.Empty, model.ExchangeRateLabel);
    }

    [Fact]
    public void NeedsExchangeRate_WhenFournisseurHasNoPurchaseCurrency_ReturnsFalse()
    {
        SiteCurrency.SetCurrency("EUR");
        var model = CreateFormModel();

        model.SelectedFournisseurs = MakeFournisseur(purchaseCurrency: null);
        Assert.False(model.NeedsExchangeRate);
        Assert.Equal("Budget", model.BudgetLabel);
        Assert.Equal(string.Empty, model.ExchangeRateLabel);

        model.SelectedFournisseurs = MakeFournisseur(purchaseCurrency: "");
        Assert.False(model.NeedsExchangeRate);
        Assert.Equal("Budget", model.BudgetLabel);
        Assert.Equal(string.Empty, model.ExchangeRateLabel);
    }

    [Fact]
    public void NeedsExchangeRate_WhenFournisseurCurrencyMatchesSiteCurrency_ReturnsFalse()
    {
        SiteCurrency.SetCurrency("EUR");
        var model = CreateFormModel();

        // Exact match
        model.SelectedFournisseurs = MakeFournisseur(purchaseCurrency: "EUR");
        Assert.False(model.NeedsExchangeRate);
        Assert.Equal("Budget", model.BudgetLabel);
        Assert.Equal(string.Empty, model.ExchangeRateLabel);

        // Case-insensitive match
        model.SelectedFournisseurs = MakeFournisseur(purchaseCurrency: "eur");
        Assert.False(model.NeedsExchangeRate);
        Assert.Equal("Budget", model.BudgetLabel);
        Assert.Equal(string.Empty, model.ExchangeRateLabel);
    }

    [Fact]
    public void NeedsExchangeRate_WhenFournisseurCurrencyDiffersFromSite_ReturnsTrueAndLabels()
    {
        SiteCurrency.SetCurrency("CAD");
        var model = CreateFormModel();

        model.SelectedFournisseurs = MakeFournisseur(purchaseCurrency: "USD");

        Assert.True(model.NeedsExchangeRate);
        Assert.Equal("Budget (USD)", model.BudgetLabel);
        Assert.Equal("Taux : 1 USD en CAD", model.ExchangeRateLabel);
    }

    [Fact]
    public void NeedsExchangeRate_WhenSiteCurrencyNullAndFournisseurHasCurrency_ReturnsTrueAndFallbackLabel()
    {
        SiteCurrency.Reset(); // SiteCurrency.CurrentCurrency is null
        var model = CreateFormModel();

        model.SelectedFournisseurs = MakeFournisseur(purchaseCurrency: "USD");

        Assert.True(model.NeedsExchangeRate);
        Assert.Equal("Budget (USD)", model.BudgetLabel);
        Assert.Equal("Taux : 1 USD en devise du site", model.ExchangeRateLabel);
    }

    [Fact]
    public void SelectedFournisseurs_TriggersPropertyChangedForDependentProperties()
    {
        SiteCurrency.SetCurrency("EUR");
        var model = CreateFormModel();

        var changedProperties = new List<string>();
        model.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName != null)
            {
                changedProperties.Add(args.PropertyName);
            }
        };

        model.SelectedFournisseurs = MakeFournisseur(purchaseCurrency: "USD");

        Assert.Contains(nameof(model.SelectedFournisseurs), changedProperties);
        Assert.Contains(nameof(model.NeedsExchangeRate), changedProperties);
        Assert.Contains(nameof(model.BudgetLabel), changedProperties);
        Assert.Contains(nameof(model.ExchangeRateLabel), changedProperties);
    }
}
