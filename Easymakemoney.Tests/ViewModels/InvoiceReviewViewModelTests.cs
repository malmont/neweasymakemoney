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
public class InvoiceReviewViewModelTests : IDisposable
{
    private readonly CultureInfo _originalCulture;

    public InvoiceReviewViewModelTests()
    {
        _originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        SiteCurrency.Reset();
        SiteCurrency.SetCurrency("CAD");
    }

    public void Dispose()
    {
        SiteCurrency.Reset();
        CultureInfo.CurrentCulture = _originalCulture;
    }

    private static (InvoiceReviewViewModel Vm, FakeDocumentReaderService Docs, FakeListFournisseursServices Suppliers, FakeCommandeReceptionService Reception, FakeDialogService Dialog)
        CreateViewModel(GestionDocument? document = null)
    {
        var docs = new FakeDocumentReaderService();
        if (document != null)
            docs.DocumentResult = document;

        var suppliers = new FakeListFournisseursServices();
        suppliers.Fournisseurs.Add(new ListFournisseur { id = 4, name = "Mode & Co", purchaseCurrency = "CAD" });
        suppliers.Fournisseurs.Add(new ListFournisseur { id = 5, name = "Fournisseur US", purchaseCurrency = "USD" });
        suppliers.Types.Add(new TypeFournisseur { id = 1, name = "Grossiste" });

        var reception = new FakeCommandeReceptionService();
        var dialog = new FakeDialogService();

        var useCase = new DocumentReaderUseCase(docs);
        var vm = new InvoiceReviewViewModel(useCase, suppliers, reception, dialog);

        return (vm, docs, suppliers, reception, dialog);
    }

    private static GestionDocument CreateSampleDocument(
        string status = "extracted",
        string invoiceNumber = "F-2026-118",
        string date = "2026-11-02",
        string currency = "CAD",
        JToken? draft = null)
    {
        var extraction = JObject.Parse($$"""
        {
          "supplier": { "name": "Mode & Co", "id": 4, "candidates": [{ "id": 4, "name": "Mode & Co", "score": 0.9 }] },
          "invoice": { "number": "{{invoiceNumber}}", "date": "{{date}}", "currency": "{{currency}}" },
          "currency": "{{currency}}",
          "amountsMode": "ht",
          "lines": [
            { "n": 1, "label": "Robe lin M", "quantity": 3, "unitCost": 1250, "groupKey": "robe-lin",
              "product": { "id": 10, "variantId": 20, "name": "Robe lin" } },
            { "n": 2, "label": "Ceinture cuir", "quantity": 5, "unitCost": 800, "groupKey": "ceinture-cuir",
              "product": { "id": 51, "variantId": 77, "name": "Ceinture cuir" } }
          ],
          "totals": { "total": 7750 },
          "computed": { "total": 7750, "difference": 0 }
        }
        """);

        return new GestionDocument
        {
            id = 42,
            type = "facture_fournisseur",
            status = status,
            extraction = extraction,
            draft = draft
        };
    }

    private static ApiException MakeApiException(string code, HttpStatusCode statusCode)
    {
        var apiError = ApiError.FromHttpResponse($$"""{ "error": "{{code}}", "code": "{{code}}" }""", statusCode, null);
        return new ApiException(apiError, statusCode);
    }

    // =========================================================================
    // Priorité 1 : Ce qui part au serveur à la validation (6 tests)
    // =========================================================================

    [Fact]
    public async Task Confirm_ExistingSupplier_ValidLines_SendsExpectedBody()
    {
        var doc = CreateSampleDocument();
        var (vm, docs, _, _, _) = CreateViewModel(doc);
        await vm.InitAsync(42);

        await vm.ValidateAllCommand.ExecuteAsync(null);
        Assert.True(vm.CanConfirm);

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Single(docs.ConfirmCalls);
        var call = docs.ConfirmCalls[0];
        Assert.Equal(42, call.Id);

        var body = Assert.IsType<JObject>(call.Body);
        Assert.Equal(4, body["supplier"]?["id"]?.Value<int>());

        var lines = Assert.IsAssignableFrom<JArray>(body["lines"]);
        Assert.Equal(2, lines.Count);
        Assert.Equal(1, lines[0]?["n"]?.Value<int>());
        Assert.Equal(3, lines[0]?["quantity"]?.Value<int>());
        Assert.Equal(1250, lines[0]?["unitCost"]?.Value<long>());
    }

    [Fact]
    public async Task Confirm_NewSupplier_ValidLines_SendsNewSupplierFields()
    {
        var doc = CreateSampleDocument();
        var (vm, docs, suppliers, _, _) = CreateViewModel(doc);
        await vm.InitAsync(42);

        vm.ChooseNewSupplierCommand.Execute(null);
        vm.NewSupplierName = "Nouveau Fournisseur SARL";
        vm.SelectedSupplierType = suppliers.Types[0];

        await vm.ValidateAllCommand.ExecuteAsync(null);
        Assert.True(vm.CanConfirm);

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Single(docs.ConfirmCalls);
        var body = Assert.IsType<JObject>(docs.ConfirmCalls[0].Body);
        Assert.Equal("Nouveau Fournisseur SARL", body["supplier"]?["create"]?["name"]?.ToString());
        Assert.Equal(1, body["supplier"]?["create"]?["typeFournisseurId"]?.Value<int>());
    }

    [Fact]
    public async Task Confirm_WithShipping_SendsShippingAmount()
    {
        var doc = CreateSampleDocument();
        var (vm, docs, _, _, _) = CreateViewModel(doc);
        await vm.InitAsync(42);

        vm.IncludeShipping = true;
        vm.ShippingAmountText = "15,00";

        await vm.ValidateAllCommand.ExecuteAsync(null);
        Assert.True(vm.CanConfirm);

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Single(docs.ConfirmCalls);
        var body = Assert.IsType<JObject>(docs.ConfirmCalls[0].Body);
        Assert.Equal(1500, body["shipping"]?["amount"]?.Value<long>());
    }

    [Fact]
    public async Task Confirm_ForeignCurrency_SendsExchangeRate()
    {
        var doc = CreateSampleDocument(currency: "USD");
        var (vm, docs, _, _, _) = CreateViewModel(doc);
        await vm.InitAsync(42);

        Assert.True(vm.NeedsExchangeRate);
        vm.ExchangeRateText = "1,35";

        await vm.ValidateAllCommand.ExecuteAsync(null);
        Assert.True(vm.CanConfirm);

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Single(docs.ConfirmCalls);
        var body = Assert.IsType<JObject>(docs.ConfirmCalls[0].Body);
        Assert.Equal(1.35, body["invoice"]?["exchangeRate"]?.Value<double>());
    }

    [Fact]
    public async Task Confirm_CannotConfirm_WhenLinesNotAllValidated()
    {
        var doc = CreateSampleDocument();
        var (vm, docs, _, _, _) = CreateViewModel(doc);
        await vm.InitAsync(42);

        // Lignes non validées
        Assert.False(vm.CanConfirm);
        Assert.False(vm.ConfirmCommand.CanExecute(null));
        Assert.Equal("Validez toutes les lignes pour continuer.", vm.ConfirmHint);

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Empty(docs.ConfirmCalls);
    }

    [Fact]
    public async Task Confirm_CannotConfirm_WhenSupplierMissing()
    {
        var doc = CreateSampleDocument();
        var (vm, docs, _, _, _) = CreateViewModel(doc);
        await vm.InitAsync(42);

        await vm.ValidateAllCommand.ExecuteAsync(null);
        // Bascule en nouveau fournisseur sans nom ni type
        vm.ChooseNewSupplierCommand.Execute(null);
        vm.NewSupplierName = "";
        vm.SelectedSupplierType = null;

        Assert.False(vm.CanConfirm);
        Assert.False(vm.ConfirmCommand.CanExecute(null));
        Assert.Equal("Choisissez le fournisseur pour continuer.", vm.ConfirmHint);

        await vm.ConfirmCommand.ExecuteAsync(null);
        Assert.Empty(docs.ConfirmCalls);
    }

    // =========================================================================
    // Priorité 2 : Les codes d'erreur traités à part (7 tests)
    // =========================================================================

    [Fact]
    public async Task Confirm_Error_InvoiceConfirmationPending_ShowsInformativeMessage()
    {
        var doc = CreateSampleDocument();
        var (vm, docs, _, _, dialog) = CreateViewModel(doc);
        await vm.InitAsync(42);
        await vm.ValidateAllCommand.ExecuteAsync(null);

        docs.ConfirmException = MakeApiException("INVOICE_CONFIRMATION_PENDING", HttpStatusCode.BadRequest);

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Single(docs.ConfirmCalls);
        Assert.Contains(dialog.ShownInfos, m => m.Contains("La validation des factures fournisseur arrive très bientôt."));
    }

    [Fact]
    public async Task Confirm_Error_PeriodClosed_Accepted_RetriesWithTodayClassificationDate()
    {
        var doc = CreateSampleDocument();
        var (vm, docs, _, _, dialog) = CreateViewModel(doc);
        await vm.InitAsync(42);
        await vm.ValidateAllCommand.ExecuteAsync(null);

        docs.ConfirmException = MakeApiException("PERIOD_CLOSED", HttpStatusCode.Conflict);
        docs.ConfirmExceptionOnlyInitial = true;
        dialog.ConfirmResult = true; // Gérant accepte "Enregistrer dans la période en cours"

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Equal(2, docs.ConfirmCalls.Count);
        Assert.Contains(dialog.ConfirmCalls, c => c.Title == "Période clôturée");

        var secondBody = Assert.IsType<JObject>(docs.ConfirmCalls[1].Body);
        var today = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        Assert.Equal(today, secondBody["classificationDate"]?.ToString());
    }

    [Fact]
    public async Task Confirm_Error_PeriodClosed_Declined_NoRetry()
    {
        var doc = CreateSampleDocument();
        var (vm, docs, _, _, dialog) = CreateViewModel(doc);
        await vm.InitAsync(42);
        await vm.ValidateAllCommand.ExecuteAsync(null);

        docs.ConfirmException = MakeApiException("PERIOD_CLOSED", HttpStatusCode.Conflict);
        dialog.ConfirmResult = false; // Gérant refuse

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Single(docs.ConfirmCalls);
        Assert.Contains(dialog.ConfirmCalls, c => c.Title == "Période clôturée");
    }

    [Fact]
    public async Task Confirm_Error_InvoiceAlreadyConfirmed_Accepted_RetriesWithForceTrue()
    {
        var doc = CreateSampleDocument();
        var (vm, docs, _, _, dialog) = CreateViewModel(doc);
        await vm.InitAsync(42);
        await vm.ValidateAllCommand.ExecuteAsync(null);

        docs.ConfirmException = MakeApiException("INVOICE_ALREADY_CONFIRMED", HttpStatusCode.Conflict);
        docs.ConfirmExceptionOnlyInitial = true;
        dialog.ConfirmResult = true; // Gérant accepte "Valider quand même"

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Equal(2, docs.ConfirmCalls.Count);
        Assert.Contains(dialog.ConfirmCalls, c => c.Title == "Facture déjà enregistrée");

        var secondBody = Assert.IsType<JObject>(docs.ConfirmCalls[1].Body);
        Assert.True(secondBody["force"]?.Value<bool>());
    }

    [Fact]
    public async Task Confirm_Error_InvoiceAlreadyConfirmed_Declined_NoRetry()
    {
        var doc = CreateSampleDocument();
        var (vm, docs, _, _, dialog) = CreateViewModel(doc);
        await vm.InitAsync(42);
        await vm.ValidateAllCommand.ExecuteAsync(null);

        docs.ConfirmException = MakeApiException("INVOICE_ALREADY_CONFIRMED", HttpStatusCode.Conflict);
        dialog.ConfirmResult = false; // Gérant refuse

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Single(docs.ConfirmCalls);
        Assert.Contains(dialog.ConfirmCalls, c => c.Title == "Facture déjà enregistrée");
    }

    [Fact]
    public async Task Confirm_Error_DocumentConfirmed_ShowsMessage()
    {
        var doc = CreateSampleDocument();
        var (vm, docs, _, _, dialog) = CreateViewModel(doc);
        await vm.InitAsync(42);
        await vm.ValidateAllCommand.ExecuteAsync(null);

        docs.ConfirmException = MakeApiException("DOCUMENT_CONFIRMED", HttpStatusCode.Conflict);

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Single(docs.ConfirmCalls);
        Assert.Contains(dialog.ShownInfos, m => m.Contains("Cette facture est déjà validée."));
    }

    [Fact]
    public async Task Confirm_Error_DocumentExpired_ShowsMessage()
    {
        var doc = CreateSampleDocument();
        var (vm, docs, _, _, dialog) = CreateViewModel(doc);
        await vm.InitAsync(42);
        await vm.ValidateAllCommand.ExecuteAsync(null);

        docs.ConfirmException = MakeApiException("DOCUMENT_EXPIRED", HttpStatusCode.Conflict);

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Single(docs.ConfirmCalls);
        Assert.Contains(dialog.ShownInfos, m => m.Contains("Cette facture n'a pas été validée dans les 30 jours"));
    }

    // =========================================================================
    // Priorité 3 : Le brouillon (5 tests)
    // =========================================================================

    [Fact]
    public async Task Draft_NotSavedOnOpen()
    {
        var doc = CreateSampleDocument();
        var (vm, docs, _, _, _) = CreateViewModel(doc);

        await vm.InitAsync(42);

        Assert.Empty(docs.SaveDraftCalls);
    }

    [Fact]
    public async Task Draft_SavedAfterModification_ViaFlushDraft()
    {
        var doc = CreateSampleDocument();
        var (vm, docs, _, _, _) = CreateViewModel(doc);
        await vm.InitAsync(42);

        vm.InvoiceNumber = "F-MODIFIEE";
        await vm.FlushDraftAsync();

        Assert.Single(docs.SaveDraftCalls);
        var call = docs.SaveDraftCalls[0];
        Assert.Equal(42, call.Id);

        var json = Assert.IsType<JObject>(call.Draft);
        Assert.Equal(1, json["version"]?.Value<int>());
        Assert.Equal("F-MODIFIEE", json["invoice"]?["number"]?.ToString());
        Assert.NotNull(vm.DraftStatusText);
        Assert.StartsWith("Brouillon enregistré à", vm.DraftStatusText);
    }

    [Fact]
    public async Task Draft_ResumeOnOpen_RestoresPreviousState()
    {
        var draftJson = JObject.Parse("""
        {
          "version": 1,
          "supplier": { "mode": "existing", "id": 4, "name": "Mode & Co" },
          "invoice": { "number": "F-REPRISE-2026", "date": "2026-11-05", "exchangeRate": null },
          "shipping": { "include": true, "amount": "12,00" },
          "lines": [
            { "n": 1, "validated": true, "mode": "existing", "productId": 10, "variantId": 20, "quantity": "4", "unitCost": "12,50" },
            { "n": 2, "validated": false, "mode": "existing", "productId": 51, "variantId": 77, "quantity": "5", "unitCost": "8,00" }
          ]
        }
        """);

        var doc = CreateSampleDocument(draft: draftJson);
        var (vm, _, _, _, _) = CreateViewModel(doc);

        await vm.InitAsync(42);

        Assert.Equal("F-REPRISE-2026", vm.InvoiceNumber);
        Assert.Equal(new DateTime(2026, 11, 5), vm.InvoiceDate);
        Assert.True(vm.IncludeShipping);
        Assert.Equal("12,00", vm.ShippingAmountText);
        Assert.Equal("Vérification reprise là où vous l'aviez laissée.", vm.DraftStatusText);

        Assert.Equal(2, vm.Lines.Count);
        Assert.True(vm.Lines[0].IsValidated);
        Assert.Equal("4", vm.Lines[0].QuantityText);
        Assert.False(vm.Lines[1].IsValidated);
    }

    [Fact]
    public async Task Draft_DuplicateContent_NotResentIfUnchanged()
    {
        var doc = CreateSampleDocument();
        var (vm, docs, _, _, _) = CreateViewModel(doc);
        await vm.InitAsync(42);

        vm.InvoiceNumber = "F-CHANGEMENT";
        await vm.FlushDraftAsync();
        Assert.Single(docs.SaveDraftCalls);

        // Second appel direct sans modification : rien ne doit repartir
        await vm.FlushDraftAsync();
        Assert.Single(docs.SaveDraftCalls);
    }

    [Fact]
    public async Task Draft_PreservesUnknownFields_OnResave()
    {
        // Brouillon avec des champs inconnus (web/future version)
        var draftJson = JObject.Parse("""
        {
          "version": 1,
          "webExtraProp": "garder-ce-champ",
          "supplier": { "mode": "existing", "id": 4, "webSupplierNote": "note-fournisseur" },
          "invoice": { "number": "F-ORIGINE", "date": "2026-11-02", "webDueDate": "2026-12-02" },
          "shipping": { "include": false, "carrierHint": "DHL" },
          "lines": [
            { "n": 1, "validated": true, "mode": "existing", "productId": 10, "variantId": 20, "webLineMemo": "memo-ligne-1" },
            { "n": 2, "validated": false, "mode": "existing", "productId": 51, "variantId": 77 }
          ]
        }
        """);

        var doc = CreateSampleDocument(draft: draftJson);
        var (vm, docs, _, _, _) = CreateViewModel(doc);
        await vm.InitAsync(42);

        vm.InvoiceNumber = "F-CORRIGEE-PAR-MOBILE";
        await vm.FlushDraftAsync();

        Assert.Single(docs.SaveDraftCalls);
        var saved = Assert.IsType<JObject>(docs.SaveDraftCalls[0].Draft);

        // Vérification de la conservation des champs inconnus
        Assert.Equal("garder-ce-champ", saved["webExtraProp"]?.ToString());
        Assert.Equal("note-fournisseur", saved["supplier"]?["webSupplierNote"]?.ToString());
        Assert.Equal("2026-12-02", saved["invoice"]?["webDueDate"]?.ToString());
        Assert.Equal("DHL", saved["shipping"]?["carrierHint"]?.ToString());
        Assert.Equal("memo-ligne-1", saved["lines"]?[0]?["webLineMemo"]?.ToString());

        // Et le champ modifié par le mobile est bien pris en compte
        Assert.Equal("F-CORRIGEE-PAR-MOBILE", saved["invoice"]?["number"]?.ToString());
    }

    // =========================================================================
    // Priorité 4 : La réception juste après la validation (5 tests avec dédoublement)
    // =========================================================================

    [Fact]
    public async Task Confirm_Success_WithCommandeId_ReceiveAccepted_CallsReceiveAsync()
    {
        var doc = CreateSampleDocument();
        var (vm, docs, _, reception, dialog) = CreateViewModel(doc);
        await vm.InitAsync(42);
        await vm.ValidateAllCommand.ExecuteAsync(null);

        docs.ConfirmResult = JObject.Parse("""{ "commande": { "id": 88 } }""");
        dialog.ConfirmResult = true; // "Marquer comme reçue maintenant"
        reception.ReceiveResult = new CommandeReception { ReceivedQuantity = 8, ReceivedLines = 2 };

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Single(reception.ReceiveCalls);
        Assert.Equal(88, reception.ReceiveCalls[0].CommandeId);
        Assert.Null(reception.ReceiveCalls[0].Body);
    }

    [Fact]
    public async Task Confirm_Success_WithCommandeId_ReceiveDeclined_DoesNotCallReceive()
    {
        var doc = CreateSampleDocument();
        var (vm, docs, _, reception, dialog) = CreateViewModel(doc);
        await vm.InitAsync(42);
        await vm.ValidateAllCommand.ExecuteAsync(null);

        docs.ConfirmResult = JObject.Parse("""{ "commande": { "id": 88 } }""");
        dialog.ConfirmResult = false; // "Plus tard"

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Empty(reception.ReceiveCalls);
    }

    [Fact]
    public async Task Receive_Http404_ShowsNotYetAvailableMessage()
    {
        var doc = CreateSampleDocument();
        var (vm, docs, _, reception, dialog) = CreateViewModel(doc);
        await vm.InitAsync(42);
        await vm.ValidateAllCommand.ExecuteAsync(null);

        docs.ConfirmResult = JObject.Parse("""{ "commande": { "id": 88 } }""");
        dialog.ConfirmResult = true;

        reception.ReceiveException = MakeApiException("NOT_FOUND", HttpStatusCode.NotFound);

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Contains(dialog.ShownInfos, m => m.Contains("La réception des commandes n'est pas encore disponible."));
    }

    [Fact]
    public async Task Receive_WithNeedsVariant_VariantChosen_SendsSecondReceive()
    {
        var doc = CreateSampleDocument();
        var (vm, docs, _, reception, dialog) = CreateViewModel(doc);
        await vm.InitAsync(42);
        await vm.ValidateAllCommand.ExecuteAsync(null);

        docs.ConfirmResult = JObject.Parse("""{ "commande": { "id": 88 } }""");
        dialog.ConfirmResult = true;

        var needs = new CommandeReceptionNeedsVariant
        {
            LineId = 15,
            ProductName = "Robe lin",
            Variants = new List<CommandeReceptionVariant>
            {
                new() { Id = 201, Size = "M", Color = "Rouge" },
                new() { Id = 202, Size = "L", Color = "Bleu" }
            }
        };

        var firstResult = new CommandeReception
        {
            ReceivedQuantity = 5,
            NeedsVariant = new List<CommandeReceptionNeedsVariant> { needs }
        };
        var secondResult = new CommandeReception
        {
            ReceivedQuantity = 3
        };

        reception.ReceiveResults.Enqueue(firstResult);
        reception.ReceiveResults.Enqueue(secondResult);

        // Simulation du choix de la première variante par le point d'entrée ChooseAsync
        vm.ChooseAsync = (title, labels) => Task.FromResult<string?>(labels[0]);

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Equal(2, reception.ReceiveCalls.Count);
        Assert.Null(reception.ReceiveCalls[0].Body);

        var secondBody = Assert.IsType<JObject>(reception.ReceiveCalls[1].Body);
        var lines = Assert.IsAssignableFrom<JArray>(secondBody["lines"]);
        Assert.Single(lines);
        Assert.Equal(15, lines[0]?["lineId"]?.Value<int>());
        Assert.Equal(201, lines[0]?["variantId"]?.Value<int>());
    }

    [Fact]
    public async Task Receive_WithNeedsVariant_LaterChosen_PostponesAndShowsSummary()
    {
        var doc = CreateSampleDocument();
        var (vm, docs, _, reception, dialog) = CreateViewModel(doc);
        await vm.InitAsync(42);
        await vm.ValidateAllCommand.ExecuteAsync(null);

        docs.ConfirmResult = JObject.Parse("""{ "commande": { "id": 88 } }""");
        dialog.ConfirmResult = true;

        var needs = new CommandeReceptionNeedsVariant
        {
            LineId = 15,
            ProductName = "Robe lin",
            Variants = new List<CommandeReceptionVariant>
            {
                new() { Id = 201, Size = "M", Color = "Rouge" }
            }
        };

        reception.ReceiveResult = new CommandeReception
        {
            ReceivedQuantity = 5,
            NeedsVariant = new List<CommandeReceptionNeedsVariant> { needs }
        };

        // Gérant choisit "Plus tard" (ou annule)
        vm.ChooseAsync = (title, labels) => Task.FromResult<string?>(null);

        await vm.ConfirmCommand.ExecuteAsync(null);

        // Seul le premier appel est parti, pas de second appel
        Assert.Single(reception.ReceiveCalls);
        Assert.Contains(dialog.ShownInfos, m => m.Contains("Certains articles restent à recevoir."));
    }

    // =========================================================================
    // Priorité 5 : ValidateAll, choix fournisseur et ValidateLineAsync (3 tests)
    // =========================================================================

    [Fact]
    public async Task ValidateAll_ValidatesCleanLines_OpensFirstLineWithProblem()
    {
        var doc = CreateSampleDocument();
        var (vm, _, _, _, dialog) = CreateViewModel(doc);
        await vm.InitAsync(42);

        // Rend la 2e ligne invalide (quantité vide)
        vm.Lines[1].QuantityText = "";

        await vm.ValidateAllCommand.ExecuteAsync(null);

        Assert.True(vm.Lines[0].IsValidated);
        Assert.False(vm.Lines[1].IsValidated);
        Assert.True(vm.Lines[1].IsEditing); // La première ligne en erreur s'ouvre

        Assert.Contains(dialog.ShownInfos, m => m.Contains("Une ligne est à corriger avant d'être validée"));
    }

    [Fact]
    public async Task ValidateLine_TogglesValidation_OrShowsErrorIfProblem()
    {
        var doc = CreateSampleDocument();
        var (vm, _, _, _, dialog) = CreateViewModel(doc);
        await vm.InitAsync(42);

        var line = vm.Lines[0];

        // 1. Ligne valide -> devient validée
        await vm.ValidateLineAsync(line);
        Assert.True(line.IsValidated);

        // 2. Même ligne rappelée -> se dévalide
        await vm.ValidateLineAsync(line);
        Assert.False(line.IsValidated);

        // 3. Ligne avec coût invalide
        line.UnitCostText = "gratuit";
        await vm.ValidateLineAsync(line);
        Assert.False(line.IsValidated);
        Assert.True(line.IsEditing);
        Assert.Contains(dialog.ShownInfos, m => m.Contains("Coût hors taxes invalide."));
    }

    [Fact]
    public async Task SupplierChoice_ExistingVersusNew_SwitchesModeAndBindings()
    {
        var doc = CreateSampleDocument();
        var (vm, _, suppliers, _, _) = CreateViewModel(doc);
        await vm.InitAsync(42);

        // Initialement : fournisseur reconnu (Mode & Co en CAD)
        Assert.True(vm.IsExistingSupplier);
        Assert.False(vm.IsNewSupplier);
        Assert.False(vm.HasSupplierWarning);

        // Sélection d'un fournisseur existant dont la devise diffère de la facture (USD vs CAD)
        vm.SelectedSupplier = vm.SupplierChoices.First(c => c.Supplier.id == 5);
        Assert.True(vm.HasSupplierWarning);
        Assert.Contains("Ce fournisseur achète en USD, la facture est en CAD", vm.SupplierWarning);

        // Bascule vers nouveau fournisseur
        vm.ChooseNewSupplierCommand.Execute(null);
        Assert.True(vm.IsNewSupplier);
        Assert.False(vm.IsExistingSupplier);
        Assert.False(vm.HasSupplierWarning); // Pas de warning pour un nouveau fournisseur

        // Retour à fournisseur existant
        vm.ChooseExistingSupplierCommand.Execute(null);
        Assert.False(vm.IsNewSupplier);
        Assert.True(vm.IsExistingSupplier);
    }
}
