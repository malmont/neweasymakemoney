using System.Collections.ObjectModel;
using System.Net;
using System.Text;
using CommunityToolkit.Mvvm.Input;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Models.Session;
using Easymakemoney.Services.Api;
using Easymakemoney.Services.Dialog;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.UseCase.LisCommandeUseCase;
using Easymakemoney.UseCase.ListCategoryUseCase;
using Easymakemoney.UseCase.ListCollectionUsecase;
using Easymakemoney.UseCase.ListColorsUseCase;
using Easymakemoney.UseCase.ListFournisseurUseCase;
using Easymakemoney.UseCase.ListFraisDePortUseCase;
using Easymakemoney.UseCase.ListNoteDeFraisUseCase;
using Easymakemoney.UseCase.ListProductUseCase;
using Easymakemoney.UseCase.ListProductVariantUseCase;
using Easymakemoney.UseCase.ListSizesUseCase;
using Easymakemoney.UseCase.ListTransporteurUseCase;
using Easymakemoney.UseCase.PeriodResultUseCase;
using Easymakemoney.UseCase.ProductAttachmentUseCase;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Components;
using Easymakemoney.ViewModels.FormModels;
using Easymakemoney.ViewModels.Lists;
using EEasymakemoney.UseCase.ListStyleUsesCase;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

[Collection("SiteCurrency")]
public sealed class BottomSheetPopupAndFormsTests : IDisposable
{
    public BottomSheetPopupAndFormsTests()
    {
        SiteCurrency.Reset();
    }

    public void Dispose()
    {
        SiteCurrency.Reset();
    }

    private sealed class Harness
    {
        public FakeListCollectionService CollectionService { get; } = new();
        public FakeListCommandService CommandService { get; } = new();
        public FakeListProductsService ProductService { get; } = new();
        public FakeListProductVariantService VariantService { get; } = new();
        public FakeListNoteDeFraisServices NoteService { get; } = new();
        public FakeListFournisseursServices FournisseurService { get; } = new();
        public FakeListFraisDePortService FraisDePortService { get; } = new();
        public FakeListCategoryServices CategoryService { get; } = new();
        public FakeListStyleService StyleService { get; } = new();
        public FakeListColorsService ColorsService { get; } = new();
        public FakeListSizesService SizesService { get; } = new();
        public FakeListTransporteurService TransporteurService { get; } = new();
        public FakePeriodService PeriodService { get; } = new();
        public FakeTripService TripService { get; } = new();
        public FakeSessionService SessionService { get; } = new()
        {
            CurrentUser = new UserInfo { Id = 42, Email = "gerant@exemple.com" }
        };
        public FakeDialogService Dialog { get; } = new();
        public int CloseCalls { get; set; }

        public SaveCollectionUseCase SaveCollectionUseCase =>
            new(new CreateCollectionUseCase(CollectionService), SessionService);

        public SaveCommandUseCase SaveCommandUseCase =>
            new(new CreateCommandUseCase(CommandService));

        public SaveProductUseCase SaveProductUseCase =>
            new(new CreateProductsUseCase(ProductService));

        public SaveProductvariantUseCase SaveVariantUseCase =>
            new(new CreateProductsVariantUseCase(VariantService));

        public SaveNoteDeFraisUseCase SaveNoteUseCase =>
            new(new CreateNoteDeFraisUseCase(NoteService));

        public SaveFournisseurUseCase SaveFournisseurUseCase =>
            new(new CreateFournisseurUseCase(FournisseurService));

        public SaveFraisDePortUseCase SaveFraisDePortUseCase =>
            new(new CreateFraisDePortUseCase(FraisDePortService));

        public ListNewCollectionViewModel CreateCollectionListVm(CollectionFormModel form) => new(
            new GetListCollectionsUseCase(CollectionService),
            SaveCollectionUseCase,
            new DeleteCollectionUseCase(CollectionService),
            form,
            Dialog);

        public ListNewCommandViewModel CreateCommandListVm(CommandFormModel form, int collectionId = 7) => new(
            new GetListCommandUseCase(CommandService),
            SaveCommandUseCase,
            new DeleteCommandUseCase(CommandService),
            form,
            Dialog,
            TripService)
        {
            CollectionId = collectionId
        };

        public ListNewProductViewModel CreateProductListVm(ProductFormModel form, int commandId = 15) => new(
            new GetListProductsUseCase(ProductService),
            form,
            SaveProductUseCase,
            new DeleteProductsUseCase(ProductService),
            new DetachProductUseCase(new FakeProductAttachmentService()),
            Dialog)
        {
            CommandId = commandId
        };

        public ListNewProductsVariantViewModel CreateVariantListVm(ProductVariantFormModel form, int productId = 22) => new(
            new GetListProductsVariantUseCase(VariantService),
            new DeleteProductsVariantUseCase(VariantService),
            SaveVariantUseCase,
            form,
            Dialog)
        {
            ProductId = productId
        };

        public ListNewNoteDeFraisViewModel CreateNoteListVm(NoteDeFraisForModel form, int collectionId = 9) => new(
            new GetListCollectionsUseCase(CollectionService),
            new GetListNoteDeFraisUseCase(NoteService),
            new DeleteNoteDeFraisUseCase(NoteService),
            SaveNoteUseCase,
            form,
            SaveCollectionUseCase,
            CreateCollectionListVm(new CollectionFormModel()),
            new CollectionFormModel(),
            new PeriodUseCase(PeriodService),
            Dialog,
            TripService)
        {
            CollectionId = collectionId
        };

        public ListNewFournisseurViewModel CreateFournisseurListVm(FournisseurFormModel form) => new(
            new GetListFournisseursUseCase(FournisseurService),
            new DeleteFournisseurUseCase(FournisseurService),
            SaveFournisseurUseCase,
            form,
            Dialog);

        public ListNewFraisDePortViewModel CreateFraisDePortListVm(int collectionId = 10, int commandId = 55) => new(
            new GetListCollectionsUseCase(CollectionService),
            new GetListCommandUseCase(CommandService),
            new GetListFraisDePortUseCase(FraisDePortService),
            new DeleteFraisDePortUseCase(FraisDePortService),
            SaveFraisDePortUseCase,
            new PeriodUseCase(PeriodService),
            null!,
            Dialog)
        {
            CollectionId = collectionId,
            CommandId = commandId
        };

        public CollectionFormModel CreateCollectionForm() => new();
        public CommandFormModel CreateCommandForm() => new(new GetListFournisseursUseCase(FournisseurService));
        public ProductFormModel CreateProductForm() => new(new GetListCategoryUseCase(CategoryService), new GetListStyleUsesCases(StyleService));
        public ProductVariantFormModel CreateVariantForm() => new(new GetListColorsUseCase(ColorsService), new GetListSizesUseCase(SizesService));
        public NoteDeFraisForModel CreateNoteForm() => new(new GestListTypeNoteDeFraisUseCase(NoteService));
        public FournisseurFormModel CreateFournisseurForm() => new(new GetListTypeFournisseurUseCase(FournisseurService));
        public FraisDePortFormModel CreateFraisDePortForm() => new(new GetListTransporteurUseCase(TransporteurService));

        public BottomSheetPopupViewModel CreateCollectionPopup(CollectionFormModel form, ListNewCollectionViewModel listVm) => new(
            isCollectionForm: true,
            collectionFormModel: form,
            saveCollectionUseCase: SaveCollectionUseCase,
            collectionViewModel: listVm,
            dialogService: Dialog)
        {
            ClosePopupAsync = TrackCloseAsync
        };

        public BottomSheetPopupViewModel CreateCommandPopup(CommandFormModel form, ListNewCommandViewModel listVm) => new(
            isCommandForm: true,
            commandFormModel: form,
            saveCommandUseCase: SaveCommandUseCase,
            commandViewModel: listVm,
            dialogService: Dialog)
        {
            ClosePopupAsync = TrackCloseAsync
        };

        public BottomSheetPopupViewModel CreateProductPopup(ProductFormModel form, ListNewProductViewModel listVm) => new(
            isProductForm: true,
            productFormModel: form,
            saveProductUseCase: SaveProductUseCase,
            productViewModel: listVm,
            dialogService: Dialog)
        {
            ClosePopupAsync = TrackCloseAsync
        };

        public BottomSheetPopupViewModel CreateVariantPopup(ProductVariantFormModel form, ListNewProductsVariantViewModel listVm) => new(
            isProductVariantForm: true,
            productVariantForm: form,
            saveProductvariantUseCase: SaveVariantUseCase,
            productVariantViewModel: listVm,
            dialogService: Dialog)
        {
            ClosePopupAsync = TrackCloseAsync
        };

        public BottomSheetPopupViewModel CreateNotePopup(NoteDeFraisForModel form, ListNewNoteDeFraisViewModel listVm) => new(
            isNoteDeFraisForm: true,
            noteDeFraisForm: form,
            saveNoteDeFraisUseCase: SaveNoteUseCase,
            noteDeFraisViewModel: listVm,
            dialogService: Dialog)
        {
            ClosePopupAsync = TrackCloseAsync
        };

        public BottomSheetPopupViewModel CreateFournisseurPopup(FournisseurFormModel form, ListNewFournisseurViewModel listVm) => new(
            isFournisseurForm: true,
            fournisseurForm: form,
            saveFournisseurUseCase: SaveFournisseurUseCase,
            fournisseurViewModel: listVm,
            dialogService: Dialog)
        {
            ClosePopupAsync = TrackCloseAsync
        };

        public BottomSheetPopupViewModel CreateFraisDePortPopup(FraisDePortFormModel form, ListNewFraisDePortViewModel listVm) => new(
            isFraisDePortForm: true,
            fraisDePortForm: form,
            saveFraisDePortUseCase: SaveFraisDePortUseCase,
            fraisDePortViewModel: listVm,
            dialogService: Dialog)
        {
            ClosePopupAsync = TrackCloseAsync
        };

        private Task TrackCloseAsync()
        {
            CloseCalls++;
            return Task.CompletedTask;
        }
    }

    private static ApiException ServerError(string code, string message, HttpStatusCode status = HttpStatusCode.Conflict) =>
        new(new ApiError { Code = code, Message = message }, status);

    private static async Task ExecuteSaveAsync(BottomSheetPopupViewModel vm) =>
        await ((IAsyncRelayCommand)vm.SaveCommand).ExecuteAsync(null);

    private static async Task ExecuteCancelAsync(BottomSheetPopupViewModel vm) =>
        await ((IAsyncRelayCommand)vm.CancelCommand).ExecuteAsync(null);

    [Fact]
    public async Task CollectionForm_Save_SendsCentsDatesAndUserId_ReloadsCollections_AndClosesPopup()
    {
        var h = new Harness();
        var form = h.CreateCollectionForm();
        form.NomCollection = "Hiver 2026";
        form.PhotoCollection = "hiver.png";
        form.BudgetCollection = 1234.56;
        form.StartDateCollection = new DateTime(2026, 11, 1, 9, 30, 0, DateTimeKind.Utc);
        form.EndDateCollection = new DateTime(2026, 12, 15, 18, 0, 0, DateTimeKind.Utc);
        form.Del = true;
        var listVm = h.CreateCollectionListVm(form);
        var vm = h.CreateCollectionPopup(form, listVm);

        await ExecuteSaveAsync(vm);

        var sent = Assert.Single(h.CollectionService.PostCalls);
        Assert.Equal("Hiver 2026", sent.nomCollection);
        Assert.Equal("hiver.png", sent.photoCollection);
        Assert.Equal(123456L, sent.budgetCollection);
        Assert.Equal("2026-11-01T09:30:00Z", sent.startDateCollection);
        Assert.Equal("2026-12-15T18:00:00Z", sent.endDateCollection);
        Assert.True(sent.del);
        Assert.Equal(42, sent.userId);
        Assert.Equal(1, h.CollectionService.GetCalls);
        Assert.Equal(1, h.CloseCalls);
        Assert.Empty(h.Dialog.ShownErrors);
    }

    [Fact]
    public async Task CommandForm_Save_SendsCentsExchangeRateDateAndCollectionId_ReloadsOrInforms_AndCloses()
    {
        SiteCurrency.SetCurrency("EUR");
        var h = new Harness();
        var fournisseur = new ListFournisseur { id = 3, name = "Milan", purchaseCurrency = "USD" };
        var form = h.CreateCommandForm();
        form.Name = "Lot automne";
        form.Photo = "lot.png";
        form.Budget = 250.75;
        form.Date = new DateTime(2026, 11, 10, 14, 0, 0, DateTimeKind.Utc);
        form.SelectedFournisseurs = fournisseur;
        form.ExchangeRate = 1.12;
        var listVm = h.CreateCommandListVm(form, collectionId: 7);
        var vm = h.CreateCommandPopup(form, listVm);

        await ExecuteSaveAsync(vm);

        var (sent, sentCollectionId) = Assert.Single(h.CommandService.PostCalls);
        Assert.Equal(7, sentCollectionId);
        Assert.Equal(7, sent.collectionId);
        Assert.Equal("Lot automne", sent.name);
        Assert.Equal("lot.png", sent.photo);
        Assert.Equal(25075L, sent.budget);
        Assert.Equal("2026-11-10T14:00:00Z", sent.date);
        Assert.Same(fournisseur, sent.fournisseur);
        Assert.Equal(1.12, sent.exchangeRate);
        Assert.Equal(new[] { 7 }, h.CommandService.GetCalls);
        Assert.Equal(1, h.CloseCalls);
        Assert.Empty(h.Dialog.ShownErrors);

        listVm.CollectionId = 0;
        await ExecuteSaveAsync(vm);

        Assert.Equal(2, h.CommandService.PostCalls.Count);
        Assert.Single(h.CommandService.GetCalls);
        Assert.Contains("Commande enregistrée dans la période de sa date.", h.Dialog.ShownInfos);
        Assert.Equal(2, h.CloseCalls);
    }

    [Fact]
    public async Task ProductForm_Save_SendsProductFieldsImageAndCommandId_ReloadsProducts_AndCloses()
    {
        var h = new Harness();
        var category = new Category { id = 5, name = "Robes", description = "Soirée", image = "cat.png" };
        var style = new Styles { id = 8, name = "Chic" };
        var imageBytes = new byte[] { 10, 20, 30, 40 };

        var form = h.CreateProductForm();
        form.Name = "Robe velours";
        form.Description = "Noire";
        form.PurchasePrice = 42.5;
        form.CoefficientMultiplier = 2.4;
        form.SelectedCategory = category;
        form.SelectedStyles = style;
        form.ImageStream = new MemoryStream(imageBytes);
        form.ImageFileName = "robe.jpg";

        var listVm = h.CreateProductListVm(form, commandId: 15);
        var vm = h.CreateProductPopup(form, listVm);

        await ExecuteSaveAsync(vm);

        var (sent, sentBytes, fileName, commandId) = Assert.Single(h.ProductService.PostCalls);
        Assert.Equal(15, commandId);
        Assert.Equal("Robe velours", sent.name);
        Assert.Equal("Noire", sent.description);
        Assert.Equal(42.5, sent.purchasePrice);
        Assert.Equal(2.4, sent.coefficientMultiplier);
        Assert.Single(sent.category!);
        Assert.Same(category, sent.category![0]);
        Assert.Same(style, sent.style);
        Assert.Equal("robe.jpg", fileName);
        Assert.Equal(imageBytes, sentBytes);
        Assert.Equal(new[] { 15 }, h.ProductService.GetCalls);
        Assert.Equal(1, h.CloseCalls);
        Assert.Empty(h.Dialog.ShownErrors);
    }

    [Fact]
    public async Task ProductVariantForm_Save_SendsVariantFieldsAndProductId_ReloadsVariants_AndCloses()
    {
        var h = new Harness();
        var color = new ColorsProduct { id = 2, name = "Rouge", codeHexa = "#FF0000" };
        var size = new SizeProduct { id = 4, name = "M" };
        var form = h.CreateVariantForm();
        form.SelectedColor = color;
        form.SelectedSize = size;
        form.StockQuantity = 12;

        var listVm = h.CreateVariantListVm(form, productId: 22);
        var vm = h.CreateVariantPopup(form, listVm);

        await ExecuteSaveAsync(vm);

        var (sent, productId) = Assert.Single(h.VariantService.PostCalls);
        Assert.Equal(22, productId);
        Assert.Same(color, sent.color);
        Assert.Same(size, sent.size);
        Assert.Equal(12, sent.stockQuantity);
        Assert.Equal(new[] { 22 }, h.VariantService.GetCalls);
        Assert.Equal(1, h.CloseCalls);
        Assert.Empty(h.Dialog.ShownErrors);
    }

    [Fact]
    public async Task NoteDeFraisForm_Save_SendsCentsDateAndTypeId_ReloadsNotes_AndCloses()
    {
        var h = new Harness();
        var form = h.CreateNoteForm();
        form.Name = "Taxi salon";
        form.Description = "Gare -> hôtel";
        form.Montant = 89.95;
        form.Date = new DateTime(2026, 11, 11, 8, 15, 0, DateTimeKind.Utc);
        form.SelectedTypeNoteDeFrais = new TypeNoteDeFrais { id = 4, name = "Transport" };

        var listVm = h.CreateNoteListVm(form, collectionId: 9);
        var vm = h.CreateNotePopup(form, listVm);

        await ExecuteSaveAsync(vm);

        var (sent, collectionId) = Assert.Single(h.NoteService.PostCalls);
        Assert.Equal(9, collectionId);
        Assert.Equal("Taxi salon", sent.name);
        Assert.Equal("Gare -> hôtel", sent.description);
        Assert.Equal(8995L, sent.montant);
        Assert.Equal("2026-11-11T08:15:00Z", sent.date);
        Assert.Equal(4, sent.typeNoteDeFraisId);
        Assert.Equal(new[] { 9 }, h.NoteService.GetCalls);
        Assert.Equal(1, h.CloseCalls);
        Assert.Empty(h.Dialog.ShownErrors);
    }

    [Fact]
    public async Task FournisseurForm_Save_SendsFieldsCurrencyAndTypeId_ReloadsFournisseurs_AndCloses()
    {
        var h = new Harness();
        var form = h.CreateFournisseurForm();
        form.Name = "Atelier Soho";
        form.Adresse = "12 Mercer St";
        form.Ville = "New York";
        form.Pays = "USA";
        form.Tel = "+1 555 0199";
        form.SelectedCurrency = "USD";
        form.SelectedTypeFournisseur = new TypeFournisseur { id = 3, name = "Grossiste" };

        var listVm = h.CreateFournisseurListVm(form);
        var vm = h.CreateFournisseurPopup(form, listVm);

        await ExecuteSaveAsync(vm);

        var sent = Assert.Single(h.FournisseurService.PostCalls);
        Assert.Equal("Atelier Soho", sent.name);
        Assert.Equal("12 Mercer St", sent.adresse);
        Assert.Equal("New York", sent.ville);
        Assert.Equal("USA", sent.pays);
        Assert.Equal("+1 555 0199", sent.tel);
        Assert.Equal("USD", sent.currency);
        Assert.Equal(3, sent.typeFournisseur);
        Assert.Equal(1, h.FournisseurService.GetFournisseursCalls);
        Assert.Equal(1, h.CloseCalls);
        Assert.Empty(h.Dialog.ShownErrors);
    }

    [Fact]
    public async Task FraisDePortForm_Save_SendsCentsTransporteurAndCommandId_ReloadsFrais_AndCloses()
    {
        var h = new Harness();
        var transporteur = new Transporteur { id = 6, name = "DHL" };
        var form = h.CreateFraisDePortForm();
        form.Name = "Colis express";
        form.Facture = "FAC-99";
        form.Tracknumber = "TRK-123";
        form.Image = "bordereau.png";
        form.Prix = 19.99;
        form.SelectedTransporteur = transporteur;

        var listVm = h.CreateFraisDePortListVm(collectionId: 10, commandId: 55);
        var vm = h.CreateFraisDePortPopup(form, listVm);

        await ExecuteSaveAsync(vm);

        var (sent, commandId) = Assert.Single(h.FraisDePortService.PostCalls);
        Assert.Equal(55, commandId);
        Assert.Equal("Colis express", sent.name);
        Assert.Equal("FAC-99", sent.facture);
        Assert.Equal("TRK-123", sent.tracknumber);
        Assert.Equal("bordereau.png", sent.image);
        Assert.Equal(1999L, sent.price);
        Assert.Same(transporteur, sent.transporteur);
        Assert.Equal(new[] { 10 }, h.CommandService.GetCalls);
        Assert.Equal(1, h.CloseCalls);
        Assert.Empty(h.Dialog.ShownErrors);
    }

    [Fact]
    public async Task CollectionForm_Save_ServerRefusal_ShowsExactApiError_DoesNotReloadOrClose()
    {
        var h = new Harness();
        h.CollectionService.PostException = ServerError("INVALID_PERIOD", "La date de fin précède le début.", HttpStatusCode.BadRequest);
        var form = h.CreateCollectionForm();
        var listVm = h.CreateCollectionListVm(form);
        var vm = h.CreateCollectionPopup(form, listVm);

        await ExecuteSaveAsync(vm);

        var err = Assert.IsType<ApiException>(Assert.Single(h.Dialog.ShownErrors));
        Assert.Equal("La date de fin précède le début.", err.ApiError.Message);
        Assert.Equal(0, h.CollectionService.GetCalls);
        Assert.Equal(0, h.CloseCalls);
    }

    [Fact]
    public async Task CommandAndProduct_Save_ServerRefusal_ShowsExactApiError_DoesNotReloadOrClose()
    {
        var h = new Harness();
        h.CommandService.PostException = ServerError("PERIOD_CLOSED", "Cette période est clôturée.");
        h.ProductService.PostException = ServerError("COMMAND_LOCKED", "Commande verrouillée.");

        var cmdVm = h.CreateCommandPopup(h.CreateCommandForm(), h.CreateCommandListVm(h.CreateCommandForm(), 7));
        await ExecuteSaveAsync(cmdVm);

        var prodForm = h.CreateProductForm();
        prodForm.ImageStream = new MemoryStream(Encoding.UTF8.GetBytes("img"));
        prodForm.ImageFileName = "img.png";
        var prodVm = h.CreateProductPopup(prodForm, h.CreateProductListVm(prodForm, 15));
        await ExecuteSaveAsync(prodVm);

        Assert.Equal(2, h.Dialog.ShownErrors.Count);
        Assert.Equal("Cette période est clôturée.", Assert.IsType<ApiException>(h.Dialog.ShownErrors[0]).ApiError.Message);
        Assert.Equal("Commande verrouillée.", Assert.IsType<ApiException>(h.Dialog.ShownErrors[1]).ApiError.Message);
        Assert.Empty(h.CommandService.GetCalls);
        Assert.Empty(h.ProductService.GetCalls);
        Assert.Equal(0, h.CloseCalls);
    }

    [Fact]
    public async Task NoteDeFraisAndFraisDePort_Save_ServerRefusal_ShowsExactApiError_DoesNotReloadOrClose()
    {
        var h = new Harness();
        h.NoteService.PostException = ServerError("PERIOD_CLOSED", "Période clôturée : note refusée.");
        h.FraisDePortService.PostException = ServerError("SHIPPING_EXISTS", "Des frais de port existent déjà.");

        var noteForm = h.CreateNoteForm();
        noteForm.SelectedTypeNoteDeFrais = new TypeNoteDeFrais { id = 1, name = "Repas" };
        var noteVm = h.CreateNotePopup(noteForm, h.CreateNoteListVm(noteForm, 9));
        await ExecuteSaveAsync(noteVm);

        var fraisVm = h.CreateFraisDePortPopup(h.CreateFraisDePortForm(), h.CreateFraisDePortListVm(10, 55));
        await ExecuteSaveAsync(fraisVm);

        Assert.Equal(2, h.Dialog.ShownErrors.Count);
        Assert.Equal("Période clôturée : note refusée.", Assert.IsType<ApiException>(h.Dialog.ShownErrors[0]).ApiError.Message);
        Assert.Equal("Des frais de port existent déjà.", Assert.IsType<ApiException>(h.Dialog.ShownErrors[1]).ApiError.Message);
        Assert.Empty(h.NoteService.GetCalls);
        Assert.Empty(h.CommandService.GetCalls);
        Assert.Equal(0, h.CloseCalls);
    }

    [Fact]
    public async Task Save_AllForms_WhenUseCaseReturnsFalse_ShowsFrenchUserMessageException_DoesNotReloadOrClose()
    {
        var h = new Harness();
        h.CollectionService.PostResult = false;
        h.CommandService.PostResult = false;
        h.ProductService.PostResult = false;
        h.VariantService.PostResult = false;
        h.NoteService.PostResult = false;
        h.FournisseurService.PostResult = false;
        h.FraisDePortService.PostResult = false;

        var prodForm = h.CreateProductForm();
        prodForm.ImageStream = new MemoryStream(new byte[] { 1 });
        prodForm.ImageFileName = "a.png";

        var noteForm = h.CreateNoteForm();
        noteForm.SelectedTypeNoteDeFrais = new TypeNoteDeFrais { id = 1, name = "Repas" };

        var fournForm = h.CreateFournisseurForm();
        fournForm.SelectedTypeFournisseur = new TypeFournisseur { id = 2, name = "Usine" };

        var popups = new[]
        {
            h.CreateCollectionPopup(h.CreateCollectionForm(), h.CreateCollectionListVm(h.CreateCollectionForm())),
            h.CreateCommandPopup(h.CreateCommandForm(), h.CreateCommandListVm(h.CreateCommandForm())),
            h.CreateProductPopup(prodForm, h.CreateProductListVm(prodForm)),
            h.CreateVariantPopup(h.CreateVariantForm(), h.CreateVariantListVm(h.CreateVariantForm())),
            h.CreateNotePopup(noteForm, h.CreateNoteListVm(noteForm)),
            h.CreateFournisseurPopup(fournForm, h.CreateFournisseurListVm(fournForm)),
            h.CreateFraisDePortPopup(h.CreateFraisDePortForm(), h.CreateFraisDePortListVm())
        };

        var initialFournisseursReads = h.FournisseurService.GetFournisseursCalls;

        foreach (var vm in popups)
            await ExecuteSaveAsync(vm);

        var expected = new[]
        {
            "L'enregistrement de la période a échoué : réessayez.",
            "L'enregistrement de la commande a échoué : réessayez.",
            "L'enregistrement de l'article a échoué : réessayez.",
            "L'enregistrement de la variante a échoué : réessayez.",
            "L'enregistrement de la note de frais a échoué : réessayez.",
            "L'enregistrement du fournisseur a échoué : réessayez.",
            "L'enregistrement du frais de port a échoué : réessayez."
        };

        Assert.Equal(expected.Length, h.Dialog.ShownErrors.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            var ex = Assert.IsType<UserMessageException>(h.Dialog.ShownErrors[i]);
            Assert.Equal(expected[i], ex.Message);
        }

        Assert.Equal(0, h.CollectionService.GetCalls);
        Assert.Empty(h.CommandService.GetCalls);
        Assert.Empty(h.ProductService.GetCalls);
        Assert.Empty(h.VariantService.GetCalls);
        Assert.Empty(h.NoteService.GetCalls);
        Assert.Equal(initialFournisseursReads, h.FournisseurService.GetFournisseursCalls);
        Assert.Equal(0, h.CloseCalls);
    }

    [Fact]
    public async Task CancelCommand_ClosesPopup_WithoutPostingOrReloading()
    {
        var h = new Harness();
        var form = h.CreateCollectionForm();
        var vm = h.CreateCollectionPopup(form, h.CreateCollectionListVm(form));

        await ExecuteCancelAsync(vm);

        Assert.Equal(1, h.CloseCalls);
        Assert.Empty(h.CollectionService.PostCalls);
        Assert.Equal(0, h.CollectionService.GetCalls);
        Assert.Empty(h.Dialog.ShownErrors);
    }

    [Fact]
    public async Task CollectionForm_EmptyOrNullBudget_StillPostsZeroCents()
    {
        var h = new Harness();
        var form = h.CreateCollectionForm();
        form.NomCollection = null!;
        form.BudgetCollection = null;
        var vm = h.CreateCollectionPopup(form, h.CreateCollectionListVm(form));

        await ExecuteSaveAsync(vm);

        var sent = Assert.Single(h.CollectionService.PostCalls);
        Assert.Null(sent.nomCollection);
        Assert.Equal(0L, sent.budgetCollection);
        Assert.Equal(1, h.CloseCalls);
    }

    [Fact]
    public async Task CommandForm_SameCurrencyDiscardsExchangeRate_WhileEmptyNameStillPosts()
    {
        SiteCurrency.SetCurrency("EUR");
        var h = new Harness();
        var form = h.CreateCommandForm();
        form.Name = "";
        form.Budget = null;
        form.SelectedFournisseurs = new ListFournisseur { id = 1, name = "Paris", purchaseCurrency = "EUR" };
        form.ExchangeRate = 1.35;

        var vm = h.CreateCommandPopup(form, h.CreateCommandListVm(form, collectionId: 7));
        await ExecuteSaveAsync(vm);

        var (sent, _) = Assert.Single(h.CommandService.PostCalls);
        Assert.Equal("", sent.name);
        Assert.Equal(0L, sent.budget);
        Assert.Null(sent.exchangeRate);
        Assert.Equal(1, h.CloseCalls);
    }

    [Fact]
    public async Task ProductForm_MissingImage_DoesNotPost_ShowsFrenchUserMessageException_DoesNotClose()
    {
        var h = new Harness();
        var form = h.CreateProductForm();
        form.Name = "Veste";
        form.ImageStream = null!;
        form.ImageFileName = "";
        var vm = h.CreateProductPopup(form, h.CreateProductListVm(form, commandId: 15));

        await ExecuteSaveAsync(vm);

        form.ImageStream = new MemoryStream(new byte[] { 1, 2 });
        form.ImageFileName = "";
        await ExecuteSaveAsync(vm);

        Assert.Empty(h.ProductService.PostCalls);
        Assert.Empty(h.ProductService.GetCalls);
        Assert.Equal(0, h.CloseCalls);
        Assert.Equal(2, h.Dialog.ShownErrors.Count);
        foreach (var err in h.Dialog.ShownErrors)
        {
            var userErr = Assert.IsType<UserMessageException>(err);
            Assert.Equal("Choisissez une image pour l'article.", userErr.Message);
        }
    }

    [Fact]
    public async Task ProductForm_WithImageAndEmptyOptionalFields_StillPosts()
    {
        var h = new Harness();
        var form = h.CreateProductForm();
        form.Name = null!;
        form.PurchasePrice = 0;
        form.SelectedCategory = null!;
        form.SelectedStyles = null!;
        form.ImageStream = new MemoryStream(new byte[] { 7, 8 });
        form.ImageFileName = "photo.png";
        var vm = h.CreateProductPopup(form, h.CreateProductListVm(form, commandId: 15));

        await ExecuteSaveAsync(vm);

        var (sent, _, _, _) = Assert.Single(h.ProductService.PostCalls);
        Assert.Null(sent.name);
        Assert.Equal(0, sent.purchasePrice);
        Assert.Empty(sent.category!);
        Assert.NotNull(sent.style);
        Assert.Equal(1, h.CloseCalls);
    }

    [Fact]
    public async Task NoteDeFraisForm_MissingType_DoesNotPost_ShowsUserMessage_WhileNullAmountPostsZero()
    {
        var h = new Harness();
        var form = h.CreateNoteForm();
        form.Name = "Hôtel";
        form.SelectedTypeNoteDeFrais = null!;
        var vm = h.CreateNotePopup(form, h.CreateNoteListVm(form, collectionId: 9));

        await ExecuteSaveAsync(vm);

        Assert.Empty(h.NoteService.PostCalls);
        Assert.Empty(h.NoteService.GetCalls);
        Assert.Equal(0, h.CloseCalls);
        var userErr = Assert.IsType<UserMessageException>(Assert.Single(h.Dialog.ShownErrors));
        Assert.Equal("Choisissez le type de la note de frais.", userErr.Message);

        form.SelectedTypeNoteDeFrais = new TypeNoteDeFrais { id = 2, name = "Hébergement" };
        form.Montant = null;
        await ExecuteSaveAsync(vm);

        var (sent, _) = Assert.Single(h.NoteService.PostCalls);
        Assert.Equal(0L, sent.montant);
        Assert.Equal(2, sent.typeNoteDeFraisId);
        Assert.Equal(1, h.CloseCalls);
    }

    [Fact]
    public async Task FournisseurForm_MissingType_DoesNotPost_ShowsUserMessage_AndSiteCurrencySendsNullCurrency()
    {
        var h = new Harness();
        var form = h.CreateFournisseurForm();
        form.Name = "Lyon Textile";
        form.SelectedTypeFournisseur = null!;
        var vm = h.CreateFournisseurPopup(form, h.CreateFournisseurListVm(form));

        await ExecuteSaveAsync(vm);

        Assert.Empty(h.FournisseurService.PostCalls);
        Assert.Equal(0, h.FournisseurService.GetFournisseursCalls);
        Assert.Equal(0, h.CloseCalls);
        var userErr = Assert.IsType<UserMessageException>(Assert.Single(h.Dialog.ShownErrors));
        Assert.Equal("Choisissez le type du fournisseur.", userErr.Message);

        form.SelectedTypeFournisseur = new TypeFournisseur { id = 5, name = "Fabricant" };
        form.SelectedCurrency = "Devise du site";
        await ExecuteSaveAsync(vm);

        var sent = Assert.Single(h.FournisseurService.PostCalls);
        Assert.Null(sent.currency);
        Assert.Equal(5, sent.typeFournisseur);
        Assert.Equal(1, h.CloseCalls);
    }
}
