using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.UseCase.DocumentUseCase;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Lists;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

[Collection("SiteCurrency")]
public class DocumentReadViewModelTests : IDisposable
{
    private readonly CultureInfo _originalCulture;
    private readonly FakeDocumentReaderService _documentReaderService = new();
    private readonly FakeListNoteDeFraisServices _notesService = new();
    private readonly FakeListTransporteurService _transporteursService = new();
    private readonly FakeDialogService _dialogService = new();
    private readonly List<string> _tempFiles = new();

    public DocumentReadViewModelTests()
    {
        _originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        SiteCurrency.Reset();
        SiteCurrency.SetCurrency("CAD");
    }

    public void Dispose()
    {
        SiteCurrency.Reset();
        CultureInfo.CurrentCulture = _originalCulture;

        foreach (var path in _tempFiles)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Ignoré lors du nettoyage
            }
        }
    }

    private DocumentReadViewModel CreateViewModel()
    {
        var documentUseCase = new DocumentReaderUseCase(_documentReaderService);
        var vm = new DocumentReadViewModel(documentUseCase, _notesService, _transporteursService, _dialogService)
        {
            PollDelay = TimeSpan.Zero, // Zéro attente dans les tests
            // La version de bureau de MAUI ne sait pas ouvrir un FileResult : lecture directe du fichier temporaire.
            OpenFile = file => Task.FromResult<Stream>(File.OpenRead(file.FullPath)),
        };
        return vm;
    }

    private FileResult CreateTempFile(string fileName)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}_{fileName}");
        File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4 });
        _tempFiles.Add(path);
        return new FileResult(path);
    }



    // =========================================================================
    // 1. Configuration InitAsync
    // =========================================================================

    [Fact]
    public async Task InitAsync_Note_ConfiguresPageTitleAndLoadsTypes()
    {
        var vm = CreateViewModel();
        _notesService.TypesResult = new ObservableCollection<TypeNoteDeFrais>
        {
            new() { id = 1, name = "Repas" },
            new() { id = 2, name = "Hôtel" }
        };

        await vm.InitAsync("note", 0);

        Assert.True(vm.IsNote);
        Assert.False(vm.IsTransport);
        Assert.False(vm.IsInvoice);
        Assert.False(vm.IsCampaign);
        Assert.True(vm.CanEnterManually);
        Assert.Equal("Lire un reçu", vm.PageTitle);
        Assert.Equal("Montant toutes taxes", vm.AmountLabel);
        Assert.Equal(2, vm.Types.Count);
        Assert.Equal("Repas", vm.Types[0].name);
    }

    [Fact]
    public async Task InitAsync_Transport_ConfiguresPageTitleAndLoadsCarriers()
    {
        var vm = CreateViewModel();
        _transporteursService.TransporteursResult = new ObservableCollection<Transporteur>
        {
            new() { id = 10, name = "DHL" },
            new() { id = 20, name = "FedEx" }
        };

        await vm.InitAsync("transport", 42);

        Assert.False(vm.IsNote);
        Assert.True(vm.IsTransport);
        Assert.False(vm.IsInvoice);
        Assert.False(vm.IsCampaign);
        Assert.True(vm.CanEnterManually);
        Assert.Equal("Lire une facture de transport", vm.PageTitle);
        Assert.Equal("Montant hors taxes", vm.AmountLabel);
        Assert.Equal(2, vm.Transporteurs.Count);
        Assert.Equal("DHL", vm.Transporteurs[0].name);
    }

    [Fact]
    public async Task InitAsync_Campaign_ConfiguresPageTitleAndDisablesManualEntry()
    {
        var vm = CreateViewModel();

        await vm.InitAsync("note", 0, purpose: "campaign");

        Assert.True(vm.IsNote);
        Assert.True(vm.IsCampaign);
        Assert.False(vm.CanEnterManually);
        Assert.Equal("Lire la facture de la régie", vm.PageTitle);
    }

    [Fact]
    public async Task InitAsync_Invoice_LoadsPendingInvoices()
    {
        var vm = CreateViewModel();
        _documentReaderService.ListResult = new List<GestionDocument>
        {
            new() { id = 5, status = "extracted", createdAt = "2026-11-10T14:30:00Z" },
            new() { id = 6, status = "extracted", createdAt = "2026-11-11T09:15:00Z" }
        };

        await vm.InitAsync("facture", 0);

        Assert.True(vm.IsInvoice);
        Assert.False(vm.CanEnterManually);
        Assert.Equal("Lire une facture fournisseur", vm.PageTitle);
        Assert.Equal(2, vm.PendingInvoices.Count);
        Assert.True(vm.HasPendingInvoices);
        Assert.Equal(5, vm.PendingInvoices[0].Id);
    }

    // =========================================================================
    // 2. Gestion des pages et sélection de fichiers
    // =========================================================================

    [Fact]
    public void PagesText_FormatsCountsCorrectly()
    {
        var vm = CreateViewModel();
        Assert.Equal("Aucune page", vm.PagesText);
        Assert.False(vm.HasPages);

        var photo1 = CreateTempFile("recu1.jpg");
        vm.AddFiles(new[] { photo1 });
        Assert.Equal("1 page", vm.PagesText);
        Assert.True(vm.HasPages);

        var photo2 = CreateTempFile("recu2.jpg");
        var photo3 = CreateTempFile("recu3.jpg");
        vm.AddFiles(new[] { photo2, photo3 });
        Assert.Equal("3 pages", vm.PagesText);

        vm.ClearPagesCommand.Execute(null);
        var pdf = CreateTempFile("facture.pdf");
        vm.AddFiles(new[] { pdf }); // Un PDF se lit seul
        Assert.Equal("1 document PDF", vm.PagesText);

        vm.ClearPagesCommand.Execute(null);
        Assert.Equal("Aucune page", vm.PagesText);
        Assert.False(vm.HasPages);
    }

    [Fact]
    public void AddFiles_LimitsToMaxPages()
    {
        var vm = CreateViewModel();
        var files = Enumerable.Range(1, 10).Select(i => CreateTempFile($"page{i}.jpg")).ToList();

        vm.AddFiles(files);

        Assert.Equal("6 pages", vm.PagesText); // Max 6 pages
    }

    // =========================================================================
    // 3. ManualEntry
    // =========================================================================

    [Fact]
    public async Task ManualEntry_Note_SetsVerifyStepWithDefaultDate()
    {
        var vm = CreateViewModel();
        await vm.InitAsync("note", 0);
        Assert.True(vm.IsChoosing);

        vm.ManualEntryCommand.Execute(null);

        Assert.True(vm.IsVerifying);
        Assert.Equal(DateTime.Today, vm.Date);
        Assert.Null(vm.AmountText);
        Assert.Null(vm.Merchant);
    }

    [Fact]
    public async Task ManualEntry_InvoiceOrCampaign_IsIgnored()
    {
        var vmInvoice = CreateViewModel();
        await vmInvoice.InitAsync("facture", 0);
        vmInvoice.ManualEntryCommand.Execute(null);
        Assert.True(vmInvoice.IsChoosing);

        var vmCampaign = CreateViewModel();
        await vmCampaign.InitAsync("note", 0, purpose: "campaign");
        vmCampaign.ManualEntryCommand.Execute(null);
        Assert.True(vmCampaign.IsChoosing);
    }

    // =========================================================================
    // 4. Envoi et suivi de lecture
    // =========================================================================

    [Fact]
    public async Task Read_WithoutFiles_DoesNotCallUpload()
    {
        var vm = CreateViewModel();
        await vm.InitAsync("note", 0);

        await vm.ReadCommand.ExecuteAsync(null);

        Assert.Empty(_documentReaderService.UploadCalls);
    }

    [Fact]
    public async Task Read_WithFile_UploadsAndEntersVerifyStepOnExtracted()
    {
        var vm = CreateViewModel();
        _notesService.TypesResult = new ObservableCollection<TypeNoteDeFrais>
        {
            new() { id = 3, name = "Taxi" }
        };
        await vm.InitAsync("note", 0);

        var file = CreateTempFile("recu.png");
        vm.AddFiles(new[] { file });

        _documentReaderService.UploadResult = new GestionDocument { id = 10, status = "reading" };
        _documentReaderService.DocumentResult = new GestionDocument
        {
            id = 10,
            status = "extracted",
            extraction = JObject.Parse("""
            {
                "amount": 4550,
                "date": "2026-11-05",
                "merchant": "Uber",
                "description": "Course client",
                "confidence": { "overall": 0.9 },
                "typeNoteDeFrais": { "id": 3 }
            }
            """)
        };

        await vm.ReadCommand.ExecuteAsync(null);

        var uploadCall = Assert.Single(_documentReaderService.UploadCalls);
        Assert.Equal("note", uploadCall.Type);
        Assert.Single(uploadCall.Files);
        Assert.Equal("image/png", uploadCall.Files[0].ContentType);

        Assert.True(vm.IsVerifying);
        Assert.Equal("45,50", vm.AmountText);
        Assert.Equal(new DateTime(2026, 11, 5), vm.Date);
        Assert.Equal("Uber", vm.Merchant);
        Assert.Equal("Course client", vm.Description);
        Assert.NotNull(vm.SelectedType);
        Assert.Equal(3, vm.SelectedType!.id);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task Read_WhenFailed_SetsFailureTextAndFailedStep()
    {
        var vm = CreateViewModel();
        await vm.InitAsync("note", 0);

        var file = CreateTempFile("recu_flou.jpg");
        vm.AddFiles(new[] { file });

        _documentReaderService.UploadResult = new GestionDocument { id = 12, status = "reading" };
        _documentReaderService.DocumentResult = new GestionDocument
        {
            id = 12,
            status = "failed",
            error = "DOCUMENT_UNREADABLE"
        };

        await vm.ReadCommand.ExecuteAsync(null);

        Assert.True(vm.IsFailed);
        Assert.Equal("Le document est illisible. Reprenez une photo plus nette, ou saisissez les informations à la main.", vm.FailureText);
    }

    [Fact]
    public async Task Read_WhenStopped_AbortsReadingLoop()
    {
        var vm = CreateViewModel();
        await vm.InitAsync("note", 0);

        var file = CreateTempFile("recu.jpg");
        vm.AddFiles(new[] { file });

        _documentReaderService.UploadResult = new GestionDocument { id = 15, status = "reading" };
        vm.Stop(); // Arrête avant le poll

        await vm.ReadCommand.ExecuteAsync(null);

        Assert.Empty(_documentReaderService.GetCalls);
    }

    [Fact]
    public async Task Read_CommandsWithoutShell_ExecuteSafely()
    {
        // Facture : extraction déclenche GoToAsync vers InvoiceReviewPage
        var vmInvoice = CreateViewModel();
        await vmInvoice.InitAsync("facture", 0);
        vmInvoice.AddFiles(new[] { CreateTempFile("facture.pdf") });
        _documentReaderService.UploadResult = new GestionDocument { id = 20, status = "reading" };
        _documentReaderService.DocumentResult = new GestionDocument { id = 20, status = "extracted" };

        var exInvoice = await Record.ExceptionAsync(() => vmInvoice.ReadCommand.ExecuteAsync(null));
        Assert.Null(exInvoice);

        // Campagne : extraction déclenche GoToAsync vers ../DocumentReadPage
        var vmCampaign = CreateViewModel();
        await vmCampaign.InitAsync("note", 0, purpose: "campaign");
        vmCampaign.AddFiles(new[] { CreateTempFile("campagne.jpg") });
        _documentReaderService.UploadResult = new GestionDocument { id = 21, status = "reading" };
        _documentReaderService.DocumentResult = new GestionDocument { id = 21, status = "extracted" };

        var exCampaign = await Record.ExceptionAsync(() => vmCampaign.ReadCommand.ExecuteAsync(null));
        Assert.Null(exCampaign);
    }

    [Fact]
    public async Task Read_Invoice_OpensConversationWithoutPolling()
    {
        var routes = new List<string>();
        var vm = CreateViewModel();
        vm.NavigateAsync = route => { routes.Add(route); return Task.CompletedTask; };
        await vm.InitAsync("facture", 0);
        vm.AddFiles(new[] { CreateTempFile("facture.jpg") });
        _documentReaderService.UploadResult = new GestionDocument { id = 25, status = "reading" };

        await vm.ReadCommand.ExecuteAsync(null);

        Assert.Equal("../InvoiceConversationPage?DocumentId=25", Assert.Single(routes));
        Assert.Empty(_documentReaderService.GetCalls);
    }

    [Fact]
    public async Task ResumeInvoice_OpensConversation()
    {
        var routes = new List<string>();
        var vm = CreateViewModel();
        vm.NavigateAsync = route => { routes.Add(route); return Task.CompletedTask; };
        _documentReaderService.ListResult = new List<GestionDocument> { new() { id = 5, status = "extracted", createdAt = "2026-11-10T14:30:00Z" } };
        await vm.InitAsync("facture", 0);

        await vm.ResumeInvoiceCommand.ExecuteAsync(vm.PendingInvoices[0]);

        Assert.Equal("../InvoiceConversationPage?DocumentId=5", Assert.Single(routes));
    }

    // =========================================================================
    // 5. Relecture (Reread) avec confirmation
    // =========================================================================

    [Fact]
    public async Task Reread_ConfirmationRefused_DoesNotCallService()
    {
        var vm = CreateViewModel();
        await vm.InitAsync("note", 0);

        // Lit un document
        vm.AddFiles(new[] { CreateTempFile("recu.jpg") });
        _documentReaderService.UploadResult = new GestionDocument { id = 30, status = "reading" };
        _documentReaderService.DocumentResult = new GestionDocument { id = 30, status = "extracted" };
        await vm.ReadCommand.ExecuteAsync(null);

        _dialogService.ConfirmResult = false; // Gérant refuse la relecture

        await vm.RereadCommand.ExecuteAsync(null);

        Assert.Single(_dialogService.ConfirmCalls);
        Assert.Contains("Relire le document", _dialogService.ConfirmCalls[0].Title);
        Assert.Empty(_documentReaderService.RereadCalls);
    }

    [Fact]
    public async Task Reread_Confirmed_CallsRereadAsync()
    {
        var vm = CreateViewModel();
        await vm.InitAsync("note", 0);

        vm.AddFiles(new[] { CreateTempFile("recu.jpg") });
        _documentReaderService.UploadResult = new GestionDocument { id = 35, status = "reading" };
        _documentReaderService.DocumentResult = new GestionDocument { id = 35, status = "extracted" };
        await vm.ReadCommand.ExecuteAsync(null);

        _dialogService.ConfirmResult = true; // Gérant confirme
        _documentReaderService.RereadResult = new GestionDocument { id = 35, status = "reading" };

        await vm.RereadCommand.ExecuteAsync(null);

        var rereadCall = Assert.Single(_documentReaderService.RereadCalls);
        Assert.Equal(35, rereadCall);
    }

    // =========================================================================
    // 6. Préremplissage (alertes confiance et doublons)
    // =========================================================================

    [Fact]
    public async Task Read_LowConfidenceAndDuplicate_PopulatesWarningRemarks()
    {
        var vm = CreateViewModel();
        await vm.InitAsync("note", 0);

        vm.AddFiles(new[] { CreateTempFile("recu.jpg") });
        _documentReaderService.UploadResult = new GestionDocument { id = 40, status = "reading" };
        _documentReaderService.DocumentResult = new GestionDocument
        {
            id = 40,
            status = "extracted",
            extraction = JObject.Parse("""
            {
                "amount": 2000,
                "confidence": { "overall": 0.5 },
                "duplicate": { "id": 12 },
                "notes": ["TVA non trouvée"]
            }
            """)
        };

        await vm.ReadCommand.ExecuteAsync(null);

        Assert.True(vm.HasRemarks);
        Assert.Contains("Lecture incertaine : vérifiez chaque champ.", vm.RemarksText);
        Assert.Contains("Ce document semble déjà enregistré.", vm.RemarksText);
        Assert.Contains("TVA non trouvée", vm.RemarksText);
    }

    // =========================================================================
    // 7. Validation ConfirmCommand
    // =========================================================================

    [Theory]
    [InlineData("", "Montant invalide.")]
    [InlineData("-10", "Montant invalide.")]
    [InlineData("abc", "Montant invalide.")]
    public async Task Confirm_InvalidAmount_ShowsInfoDialog(string amount, string expectedError)
    {
        var vm = CreateViewModel();
        await vm.InitAsync("note", 0);
        vm.AmountText = amount;

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Contains(expectedError, Assert.Single(_dialogService.ShownInfos));
        Assert.Empty(_documentReaderService.ConfirmCalls);
    }

    [Fact]
    public async Task Confirm_NoteWithoutType_ShowsTypeRequiredDialog()
    {
        var vm = CreateViewModel();
        await vm.InitAsync("note", 0);
        vm.AmountText = "25,00";
        vm.SelectedType = null; // Pas de type

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Contains("Choisissez le type de la note de frais.", Assert.Single(_dialogService.ShownInfos));
        Assert.Empty(_documentReaderService.ConfirmCalls);
    }

    [Fact]
    public async Task Confirm_TransportWithoutCarrier_ShowsCarrierRequiredDialog()
    {
        var vm = CreateViewModel();
        await vm.InitAsync("transport", 100);
        vm.AmountText = "80,00";
        vm.SelectedTransporteur = null; // Pas de transporteur

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Contains("Choisissez le transporteur.", Assert.Single(_dialogService.ShownInfos));
        Assert.Empty(_documentReaderService.ConfirmCalls);
    }

    [Fact]
    public async Task Confirm_WithoutDocumentId_ShowsReadDocumentFirstDialog()
    {
        var vm = CreateViewModel();
        _notesService.TypesResult = new ObservableCollection<TypeNoteDeFrais> { new() { id = 1, name = "Repas" } };
        await vm.InitAsync("note", 0);
        vm.AmountText = "25,00";
        vm.SelectedType = vm.Types[0];
        // Pas de lecture effectuée : _documentId == 0

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Contains("Lisez d'abord un document : il servira de justificatif.", Assert.Single(_dialogService.ShownInfos));
        Assert.Empty(_documentReaderService.ConfirmCalls);
    }

    // =========================================================================
    // 8. Envoi ConfirmAsync réussi et cas d'erreur
    // =========================================================================

    [Fact]
    public async Task Confirm_Note_SendsExpectedBodyAndShowsSuccess()
    {
        var vm = CreateViewModel();
        _notesService.TypesResult = new ObservableCollection<TypeNoteDeFrais> { new() { id = 5, name = "Carburant" } };
        await vm.InitAsync("note", 0);

        vm.AddFiles(new[] { CreateTempFile("recu.jpg") });
        _documentReaderService.UploadResult = new GestionDocument { id = 50, status = "reading" };
        _documentReaderService.DocumentResult = new GestionDocument { id = 50, status = "extracted" };
        await vm.ReadCommand.ExecuteAsync(null);

        vm.AmountText = "75,50";
        vm.Date = new DateTime(2026, 11, 10);
        vm.Merchant = "Shell";
        vm.Description = "Plein essence";
        vm.SelectedType = vm.Types[0];

        await vm.ConfirmCommand.ExecuteAsync(null);

        var (id, body) = Assert.Single(_documentReaderService.ConfirmCalls);
        Assert.Equal(50, id);
        var dict = Assert.IsAssignableFrom<IDictionary<string, object?>>(body);
        Assert.Equal(7550L, dict["montant"]);
        Assert.Equal("2026-11-10", dict["date"]);
        Assert.Equal(5, dict["typeNoteDeFraisId"]);
        Assert.Equal("Plein essence", dict["description"]);
        Assert.Equal("Shell", dict["name"]);

        Assert.Contains("Note de frais enregistrée.", _dialogService.ShownInfos);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task Confirm_Transport_SendsExpectedBodyAndShowsSuccess()
    {
        var vm = CreateViewModel();
        _transporteursService.TransporteursResult = new ObservableCollection<Transporteur> { new() { id = 8, name = "Chronopost" } };
        await vm.InitAsync("transport", 123);

        vm.AddFiles(new[] { CreateTempFile("facture.pdf") });
        _documentReaderService.UploadResult = new GestionDocument { id = 60, status = "reading" };
        _documentReaderService.DocumentResult = new GestionDocument { id = 60, status = "extracted" };
        await vm.ReadCommand.ExecuteAsync(null);

        vm.AmountText = "150,00";
        vm.TrackingNumber = "CHR-999";
        vm.InvoiceNumber = "INV-2026";
        vm.SelectedTransporteur = vm.Transporteurs[0];

        await vm.ConfirmCommand.ExecuteAsync(null);

        var (id, body) = Assert.Single(_documentReaderService.ConfirmCalls);
        Assert.Equal(60, id);
        var dict = Assert.IsAssignableFrom<IDictionary<string, object?>>(body);
        Assert.Equal(123, dict["commandeId"]);
        Assert.Equal(8, dict["transporteurId"]);
        Assert.Equal(15000L, dict["price"]);
        Assert.Equal("CHR-999", dict["tracknumber"]);
        Assert.Equal("INV-2026", dict["facture"]);

        Assert.Contains("Frais de port enregistré.", _dialogService.ShownInfos);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task Confirm_Note_409PeriodClosed_Refused_DoesNotRetry()
    {
        var vm = CreateViewModel();
        _notesService.TypesResult = new ObservableCollection<TypeNoteDeFrais> { new() { id = 1, name = "Repas" } };
        await vm.InitAsync("note", 0);

        vm.AddFiles(new[] { CreateTempFile("recu.jpg") });
        _documentReaderService.UploadResult = new GestionDocument { id = 70, status = "reading" };
        _documentReaderService.DocumentResult = new GestionDocument { id = 70, status = "extracted" };
        await vm.ReadCommand.ExecuteAsync(null);

        vm.AmountText = "20,00";
        vm.SelectedType = vm.Types[0];

        var apiError = ApiError.FromHttpResponse("""{ "error": "PERIOD_CLOSED" }""", HttpStatusCode.Conflict, null);
        _documentReaderService.ConfirmException = new ApiException(apiError, HttpStatusCode.Conflict);
        _dialogService.ConfirmResult = false; // Gérant refuse la bascule sur la période courante

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Single(_dialogService.ConfirmCalls);
        Assert.Equal("Période clôturée", _dialogService.ConfirmCalls[0].Title);
        Assert.Single(_documentReaderService.ConfirmCalls); // Pas de retry
    }

    [Fact]
    public async Task Confirm_Note_409PeriodClosed_Accepted_RetriesWithCurrentPeriodClassificationDate()
    {
        var vm = CreateViewModel();
        _notesService.TypesResult = new ObservableCollection<TypeNoteDeFrais> { new() { id = 1, name = "Repas" } };
        await vm.InitAsync("note", 0);

        vm.AddFiles(new[] { CreateTempFile("recu.jpg") });
        _documentReaderService.UploadResult = new GestionDocument { id = 71, status = "reading" };
        _documentReaderService.DocumentResult = new GestionDocument { id = 71, status = "extracted" };
        await vm.ReadCommand.ExecuteAsync(null);

        vm.AmountText = "20,00";
        vm.SelectedType = vm.Types[0];

        var apiError = ApiError.FromHttpResponse("""{ "error": "PERIOD_CLOSED" }""", HttpStatusCode.Conflict, null);
        _documentReaderService.ConfirmException = new ApiException(apiError, HttpStatusCode.Conflict);
        _documentReaderService.ConfirmExceptionOnlyInitial = true; // Échoue au premier envoi, réussit au retry
        _dialogService.ConfirmResult = true; // Gérant accepte la période en cours

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Single(_dialogService.ConfirmCalls);
        Assert.Equal("Période clôturée", _dialogService.ConfirmCalls[0].Title);

        // Deux envois ConfirmAsync : le premier rejeté, le second avec classificationDate
        Assert.Equal(2, _documentReaderService.ConfirmCalls.Count);
        var secondBody = Assert.IsAssignableFrom<IDictionary<string, object?>>(_documentReaderService.ConfirmCalls[1].Body);
        Assert.Equal(DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), secondBody["classificationDate"]);
        Assert.Contains("Note de frais enregistrée.", _dialogService.ShownInfos);
    }

    [Fact]
    public async Task Confirm_Transport_409InvoiceAlreadyConfirmed_Refused_DoesNotRetry()
    {
        var vm = CreateViewModel();
        _transporteursService.TransporteursResult = new ObservableCollection<Transporteur> { new() { id = 8, name = "UPS" } };
        await vm.InitAsync("transport", 200);

        vm.AddFiles(new[] { CreateTempFile("facture.pdf") });
        _documentReaderService.UploadResult = new GestionDocument { id = 80, status = "reading" };
        _documentReaderService.DocumentResult = new GestionDocument { id = 80, status = "extracted" };
        await vm.ReadCommand.ExecuteAsync(null);

        vm.AmountText = "95,00";
        vm.SelectedTransporteur = vm.Transporteurs[0];

        var apiError = ApiError.FromHttpResponse("""{ "error": "INVOICE_ALREADY_CONFIRMED" }""", HttpStatusCode.Conflict, null);
        _documentReaderService.ConfirmException = new ApiException(apiError, HttpStatusCode.Conflict);
        _dialogService.ConfirmResult = false; // Gérant refuse

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Single(_dialogService.ConfirmCalls);
        Assert.Equal("Facture déjà enregistrée", _dialogService.ConfirmCalls[0].Title);
        Assert.Single(_documentReaderService.ConfirmCalls); // Pas de retry
    }

    [Fact]
    public async Task Confirm_Transport_409InvoiceAlreadyConfirmed_Accepted_RetriesWithForceTrue()
    {
        var vm = CreateViewModel();
        _transporteursService.TransporteursResult = new ObservableCollection<Transporteur> { new() { id = 8, name = "UPS" } };
        await vm.InitAsync("transport", 200);

        vm.AddFiles(new[] { CreateTempFile("facture.pdf") });
        _documentReaderService.UploadResult = new GestionDocument { id = 85, status = "reading" };
        _documentReaderService.DocumentResult = new GestionDocument { id = 85, status = "extracted" };
        await vm.ReadCommand.ExecuteAsync(null);

        vm.AmountText = "95,00";
        vm.SelectedTransporteur = vm.Transporteurs[0];

        var apiError = ApiError.FromHttpResponse("""{ "error": "INVOICE_ALREADY_CONFIRMED" }""", HttpStatusCode.Conflict, null);
        _documentReaderService.ConfirmException = new ApiException(apiError, HttpStatusCode.Conflict);
        _documentReaderService.ConfirmExceptionOnlyInitial = true; // Échoue au premier envoi, réussit au retry
        _dialogService.ConfirmResult = true; // Gérant accepte de forcer

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Single(_dialogService.ConfirmCalls);
        Assert.Equal("Facture déjà enregistrée", _dialogService.ConfirmCalls[0].Title);

        // Deux envois ConfirmAsync : le premier rejeté, le second avec force: true
        Assert.Equal(2, _documentReaderService.ConfirmCalls.Count);
        var secondBody = Assert.IsAssignableFrom<IDictionary<string, object?>>(_documentReaderService.ConfirmCalls[1].Body);
        Assert.True((bool?)secondBody["force"]);
        Assert.Contains("Frais de port enregistré.", _dialogService.ShownInfos);
    }
}
