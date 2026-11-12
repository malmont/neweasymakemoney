using System.Collections.ObjectModel;
using Easymakemoney.Models;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.UseCase.LisCommandeUseCase;
using Easymakemoney.UseCase.ListCollectionUsecase;
using Easymakemoney.UseCase.ListFraisDePortUseCase;
using Easymakemoney.UseCase.ListNoteDeFraisUseCase;
using Easymakemoney.UseCase.PeriodResultUseCase;
using Easymakemoney.ViewModels.Lists;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

public class ManagementListLoadingTests
{
    private static ListCollection SamplePeriod(int id, string name = "Période", bool isClosed = false) => new()
    {
        id = id,
        nomCollection = name,
        startDateCollection = "2026-06-01",
        endDateCollection = "2026-08-31",
        photoCollection = "photo.png",
        status = isClosed ? "closed" : "active"
    };

    private static ListCommand SampleCommand(int id, string name = "Commande") => new()
    {
        id = id,
        name = name,
        photo = "photo.png",
        date = "2026-11-10"
    };

    private static FraisDePort SampleFrais(int id = 1, long price = 1500) => new()
    {
        id = id,
        name = "Transporteur",
        image = "transport.png",
        price = price
    };

    [Fact]
    public async Task ListNewCollection_GetList_Success_PopulatesList()
    {
        var fakeService = new FakeListCollectionService
        {
            CollectionsToReturn = new ObservableCollection<ListCollection>
            {
                SamplePeriod(1, "Printemps 2026"),
                SamplePeriod(2, "Été 2026")
            }
        };
        var fakeDialog = new FakeDialogService();
        var vm = new ListNewCollectionViewModel(
            new GetListCollectionsUseCase(fakeService),
            null!,
            new DeleteCollectionUseCase(fakeService),
            null!,
            fakeDialog);

        await vm.GetListCollectionAsync();

        Assert.Equal(2, vm.ListCollections.Count);
        Assert.False(vm.IsBusy);
        Assert.Equal("Printemps 2026", vm.ListCollections[0].nomCollection);
    }

    [Fact]
    public async Task ListNewCollection_GetList_Error_DisplaysError()
    {
        var fakeService = new FakeListCollectionService
        {
            GetException = new InvalidOperationException("Connexion impossible")
        };
        var fakeDialog = new FakeDialogService();
        var vm = new ListNewCollectionViewModel(
            new GetListCollectionsUseCase(fakeService),
            null!,
            new DeleteCollectionUseCase(fakeService),
            null!,
            fakeDialog);

        await vm.GetListCollectionAsync();

        Assert.Single(fakeDialog.ShownErrors);
        Assert.False(vm.IsBusy);
        Assert.Empty(vm.ListCollections);
    }

    [Fact]
    public async Task ListNewFraisDePort_SelectCurrentPeriodIfNone_LoadsCurrentPeriodAndRows()
    {
        var current = SamplePeriod(3, "Été 2026");
        var fakePeriodService = new FakePeriodService { CurrentPeriodResult = current };
        var periodUseCase = new PeriodUseCase(fakePeriodService);

        var fakeCommandService = new FakeListCommandService();
        fakeCommandService.Commands.Add(SampleCommand(42));

        var fakeFraisService = new FakeListFraisDePortService();
        fakeFraisService.FraisByCommandId[42] = SampleFrais(1, 2500);

        var vm = new ListNewFraisDePortViewModel(
            null!,
            new GetListCommandUseCase(fakeCommandService),
            new GetListFraisDePortUseCase(fakeFraisService),
            new DeleteFraisDePortUseCase(fakeFraisService),
            null!,
            periodUseCase,
            null!,
            new FakeDialogService())
        {
            CollectionId = 0
        };

        await vm.SelectCurrentPeriodIfNoneAsync();

        Assert.Equal(3, vm.CollectionId);
        Assert.Equal("Été 2026", vm.CollectionName);
        Assert.Single(vm.Rows);
        Assert.Equal(42, vm.Rows[0].Commande.id);
        Assert.NotNull(vm.Rows[0].Frais);
        Assert.Equal(2500, vm.Rows[0].Frais!.price);
        Assert.False(vm.ShowEmptyState);
    }

    [Fact]
    public async Task ListNewFraisDePort_GetList_EmptyCollection_ShowsEmptyState()
    {
        var fakeCommandService = new FakeListCommandService(); // aucune commande
        var fakeFraisService = new FakeListFraisDePortService();
        var periodUseCase = new PeriodUseCase(new FakePeriodService());

        var vm = new ListNewFraisDePortViewModel(
            null!,
            new GetListCommandUseCase(fakeCommandService),
            new GetListFraisDePortUseCase(fakeFraisService),
            new DeleteFraisDePortUseCase(fakeFraisService),
            null!,
            periodUseCase,
            null!,
            new FakeDialogService())
        {
            CollectionId = 7
        };

        await vm.GetListFraisDePort();

        Assert.Empty(vm.Rows);
        Assert.True(vm.ShowEmptyState);
        Assert.Equal("Aucune commande", vm.EmptyTitle);
    }

    [Fact]
    public async Task ListNewNoteDeFrais_GetList_Success_PopulatesList()
    {
        var fakeNoteService = new FakeListNoteDeFraisServices();
        fakeNoteService.NotesResult.Add(new NoteDeFrais { id = 1, name = "Hôtel", montant = 12000 });
        fakeNoteService.NotesResult.Add(new NoteDeFrais { id = 2, name = "Train", montant = 8500 });

        var fakeDialog = new FakeDialogService();
        var periodUseCase = new PeriodUseCase(new FakePeriodService());

        var vm = new ListNewNoteDeFraisViewModel(
            null!,
            new GetListNoteDeFraisUseCase(fakeNoteService),
            new DeleteNoteDeFraisUseCase(fakeNoteService),
            null!,
            null!,
            null!,
            null!,
            null!,
            periodUseCase,
            fakeDialog,
            new FakeTripService())
        {
            CollectionId = 5
        };

        await vm.GetListNoteDeFraisAsync();

        Assert.Equal(2, vm.ListNoteDeFrais.Count);
        Assert.False(vm.IsBusy);
        Assert.Equal("Hôtel", vm.ListNoteDeFrais[0].name);
        Assert.Equal(12000, vm.ListNoteDeFrais[0].montant);
    }
}
