using System.Collections.ObjectModel;
using Easymakemoney.Models;
using Easymakemoney.Models.Session;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.UseCase.LisCommandeUseCase;
using Easymakemoney.UseCase.ListCollectionUsecase;
using Easymakemoney.UseCase.ListFournisseurUseCase;
using Easymakemoney.UseCase.ListFraisDePortUseCase;
using Easymakemoney.UseCase.ListNoteDeFraisUseCase;
using Easymakemoney.UseCase.ListTransporteurUseCase;
using Easymakemoney.ViewModels.FormModels;
using Xunit;

namespace Easymakemoney.Tests.UseCases;

public class SaveUseCasesTests
{
    private class FakeCollectionService : IListCollectionService
    {
        public ListCollection? CapturedCollection { get; private set; }

        public Task<ObservableCollection<ListCollection>> GetCollectionList() =>
            Task.FromResult(new ObservableCollection<ListCollection>());

        public Task<bool> PostCollection(ListCollection newCollection)
        {
            CapturedCollection = newCollection;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteCollection(int id) => Task.FromResult(true);
    }

    private class FakeCommandService : IListCommandService
    {
        public ListCommand? CapturedCommand { get; private set; }
        public int CapturedId { get; private set; }

        public Task<ObservableCollection<ListCommand>> GetCommandesList(int id) =>
            Task.FromResult(new ObservableCollection<ListCommand>());

        public Task<bool> PostCommandes(ListCommand newCommandes, int id)
        {
            CapturedCommand = newCommandes;
            CapturedId = id;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteCommandes(int id) => Task.FromResult(true);
    }

    private class FakeFournisseursService : IListFourniseursServices
    {
        public Task<ObservableCollection<ListFournisseur>> GetFourniseursList() =>
            Task.FromResult(new ObservableCollection<ListFournisseur>());

        public Task<ObservableCollection<TypeFournisseur>> GetTypeFournisseurs() =>
            Task.FromResult(new ObservableCollection<TypeFournisseur>());

        public Task<bool> PostFourniseurs(ListFournisseur newFourniseurs) => Task.FromResult(true);
        public Task<bool> DeleteFourniseurs(int id) => Task.FromResult(true);
    }

    private class FakeTransporteurService : IListTransporteurService
    {
        public Task<ObservableCollection<Transporteur>> GetListTransporteur() =>
            Task.FromResult(new ObservableCollection<Transporteur>());
    }

    private class FakeFraisDePortService : IListFraisDePortService
    {
        public FraisDePort? CapturedFraisDePort { get; private set; }
        public int CapturedCommandId { get; private set; }

        public Task<FraisDePort?> GetListFraisDePort(int commandId) =>
            Task.FromResult<FraisDePort?>(null);

        public Task<bool> PostFraisDePort(FraisDePort newFraisDePort, int commandId)
        {
            CapturedFraisDePort = newFraisDePort;
            CapturedCommandId = commandId;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteFraisDePort(int commandId) => Task.FromResult(true);
    }

    private class FakeNoteDeFraisService : IListNoteDeFraisServices
    {
        public NoteDeFrais? CapturedNoteDeFrais { get; private set; }
        public int CapturedCollectionId { get; private set; }

        public Task<ObservableCollection<NoteDeFrais>> GetNoteDeFraisList(int id) =>
            Task.FromResult(new ObservableCollection<NoteDeFrais>());

        public Task<bool> PostNoteDeFrais(NoteDeFrais newNoteDeFrais, int id)
        {
            CapturedNoteDeFrais = newNoteDeFrais;
            CapturedCollectionId = id;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteNoteDeFrais(int id) => Task.FromResult(true);

        public Task<ObservableCollection<TypeNoteDeFrais>> GetTypeNoteDeFrais() =>
            Task.FromResult(new ObservableCollection<TypeNoteDeFrais>());
    }

    [Fact]
    public async Task SaveCollectionUseCase_RoundsBudgetToCents()
    {
        var fakeService = new FakeCollectionService();
        var createUseCase = new CreateCollectionUseCase(fakeService);
        var sessionService = new FakeSessionService
        {
            CurrentUser = new UserInfo { Id = 42, Email = "test@exemple.test" }
        };

        var saveUseCase = new SaveCollectionUseCase(createUseCase, sessionService);

        var form = new CollectionFormModel
        {
            NomCollection = "Collection Hiver",
            BudgetCollection = 12.345,
            StartDateCollection = new DateTime(2026, 1, 1),
            EndDateCollection = new DateTime(2026, 2, 1),
            Del = false
        };

        var result = await saveUseCase.ExecuteAsync(form);

        Assert.True(result);
        Assert.NotNull(fakeService.CapturedCollection);
        Assert.Equal(1235L, fakeService.CapturedCollection.budgetCollection);
        Assert.Equal(42, fakeService.CapturedCollection.userId);
    }

    [Fact]
    public async Task SaveCommandUseCase_RoundsBudgetToCents()
    {
        var fakeCommandService = new FakeCommandService();
        var fakeFournisseurService = new FakeFournisseursService();
        var createUseCase = new CreateCommandUseCase(fakeCommandService);
        var saveUseCase = new SaveCommandUseCase(createUseCase);
        var getFournisseursUseCase = new GetListFournisseursUseCase(fakeFournisseurService);

        var form = new CommandFormModel(getFournisseursUseCase)
        {
            Name = "Commande Fournisseur A",
            Budget = 1.005,
            Date = new DateTime(2026, 3, 15)
        };

        var result = await saveUseCase.ExecuteAsync(form, 10);

        Assert.True(result);
        Assert.NotNull(fakeCommandService.CapturedCommand);
        Assert.Equal(101L, fakeCommandService.CapturedCommand.budget);
        Assert.Equal(10, fakeCommandService.CapturedId);
    }

    [Fact]
    public async Task SaveFraisDePortUseCase_RoundsPriceToCents()
    {
        var fakeFraisService = new FakeFraisDePortService();
        var fakeTransporteurService = new FakeTransporteurService();
        var createUseCase = new CreateFraisDePortUseCase(fakeFraisService);
        var saveUseCase = new SaveFraisDePortUseCase(createUseCase);
        var getTransporteurUseCase = new GetListTransporteurUseCase(fakeTransporteurService);

        var form = new FraisDePortFormModel(getTransporteurUseCase)
        {
            Name = "Livraison Express",
            Prix = 2.675
        };

        var result = await saveUseCase.ExecuteAsync(form, 99);

        Assert.True(result);
        Assert.NotNull(fakeFraisService.CapturedFraisDePort);
        Assert.Equal(268L, fakeFraisService.CapturedFraisDePort.price);
        Assert.Equal(99, fakeFraisService.CapturedCommandId);
    }

    [Fact]
    public async Task SaveNoteDeFraisUseCase_RoundsMontantToCents()
    {
        var fakeNoteService = new FakeNoteDeFraisService();
        var createUseCase = new CreateNoteDeFraisUseCase(fakeNoteService);
        var saveUseCase = new SaveNoteDeFraisUseCase(createUseCase);
        var getTypeUseCase = new GestListTypeNoteDeFraisUseCase(fakeNoteService);

        var form = new NoteDeFraisForModel(getTypeUseCase)
        {
            Name = "Restaurant Client",
            Montant = 19.99,
            Date = new DateTime(2026, 4, 1),
            SelectedTypeNoteDeFrais = new TypeNoteDeFrais { id = 5, name = "Repas" }
        };

        var result = await saveUseCase.ExecuteAsync(form, 20);

        Assert.True(result);
        Assert.NotNull(fakeNoteService.CapturedNoteDeFrais);
        Assert.Equal(1999L, fakeNoteService.CapturedNoteDeFrais.montant);
        Assert.Equal(20, fakeNoteService.CapturedCollectionId);
    }
}
