using System.Net;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.ViewModels.Lists;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

/// <summary>Lot IA-FACT-1 : conversation d'une facture fournisseur (modèle, réponses, confirmation, annulation, navigation).</summary>
public class InvoiceConversationTests
{
    private readonly FakeConversationService _service = new();
    private readonly FakeDocumentReaderService _documents = new();
    private readonly FakeDialogService _dialog = new();

    private InvoiceConversationViewModel CreateViewModel(List<string>? routes = null) =>
        new(_service, _documents, _dialog)
        {
            PollDelay = TimeSpan.Zero,
            NavigateAsync = route => { routes?.Add(route); return Task.CompletedTask; },
        };

    private static InvoiceConversation Thread(string state, string? hash = null, params ConversationQuestion[] questions) => new()
    {
        id = 7,
        state = state,
        previewHash = hash,
        document = new ConversationDocument { id = 42 },
        questions = questions.ToList(),
        openQuestions = questions.Count(q => !q.IsAnswered),
    };

    private static ConversationQuestion SupplierQuestion() => new()
    {
        id = "supplier",
        kind = "supplier",
        title = "Quel fournisseur ?",
        required = true,
        input = new ConversationInput { type = "choice" },
        options =
        {
            new ConversationOption { value = new JObject { ["id"] = 3 }, label = "Grossiste Caraïbes" },
            new ConversationOption { value = JObject.Parse("{\"create\":{\"name\":\"Nouveau grossiste\"}}"), label = "Créer « Nouveau grossiste »" },
        },
    };

    private static ApiException Error(HttpStatusCode status, string json) =>
        new(ApiError.FromHttpResponse(json, status, null), status);

    // ---- Modèle -------------------------------------------------------------

    [Fact]
    public void IsReady_RequiresPreviewHash()
    {
        Assert.False(Thread("ready").IsReady);
        Assert.True(Thread("ready", "abc").IsReady);
        Assert.False(Thread("questions", "abc").IsReady);
    }

    [Fact]
    public void AnswerText_ReadsOptionCreateMoneyAndDate()
    {
        var supplier = SupplierQuestion();
        supplier.answer = new JObject { ["id"] = 3 };
        Assert.Equal("Grossiste Caraïbes", supplier.AnswerText);

        supplier.answer = JObject.Parse("{\"create\":{\"name\":\"Autre\"}}");
        Assert.Equal("Créer « Autre »", supplier.AnswerText);

        var date = new ConversationQuestion { id = "invoice.date", input = new ConversationInput { type = "date" }, answer = "2026-11-05" };
        Assert.Contains("novembre 2026", date.AnswerText);

        var unanswered = new ConversationQuestion { id = "x", answer = JValue.CreateNull() };
        Assert.False(unanswered.IsAnswered);
        Assert.Null(unanswered.AnswerText);
    }

    [Fact]
    public void ProposedSupplier_ReadsAsLuSurLaFacture_AndIsNotToAnswer()
    {
        var question = SupplierQuestion();
        question.answer = new JObject { ["id"] = 3 };
        question.proposed = true;
        var item = new ConversationQuestionItem(question);
        Assert.Equal("Lu sur la facture : Grossiste Caraïbes", item.AnswerText);
        Assert.False(item.IsToAnswer);
        Assert.True(item.Options[0].IsSelected);

        question.proposed = false;
        question.answer = null;
        item = new ConversationQuestionItem(question);
        Assert.True(item.IsToAnswer);
        Assert.Null(item.AnswerText);
    }

    [Fact]
    public void OptionIsCreate_OnlyForCreateObject()
    {
        var question = SupplierQuestion();
        Assert.False(question.options[0].IsCreate);
        Assert.True(question.options[1].IsCreate);
    }

    // ---- Options et saisies ----------------------------------------------------

    [Fact]
    public void OptionBuildValue_ClonesServerValue()
    {
        var question = SupplierQuestion();
        var item = new ConversationOptionItem(question.options[0], question);
        var value = item.BuildValue();
        Assert.True(JToken.DeepEquals(new JObject { ["id"] = 3 }, value));
        Assert.NotSame(question.options[0].value, value);
    }

    [Fact]
    public void ScalarOptions_AmountsModeAndPeriod_DoNotCrash()
    {
        var question = new ConversationQuestion
        {
            id = "amountsMode",
            kind = "amountsMode",
            options = { new ConversationOption { value = "ht", label = "Hors taxes" }, new ConversationOption { value = "ttc", label = "Taxes comprises" } },
            answer = "ttc",
        };
        var item = new ConversationOptionItem(question.options[0], question);
        Assert.False(item.IsCreate);
        Assert.Null(item.CreateName);
        Assert.Equal("ht", item.BuildValue()!.Value<string>());
        Assert.Equal("Taxes comprises", question.AnswerText);
        Assert.True(new ConversationOptionItem(question.options[1], question).IsSelected);
    }

    [Fact]
    public void OptionBuildValue_CreateUsesEditedName_AndRefusesEmptyName()
    {
        var question = SupplierQuestion();
        var item = new ConversationOptionItem(question.options[1], question);
        Assert.Equal("Nouveau grossiste", item.CreateName);
        Assert.False(item.IsProductCreate);

        item.CreateName = "  Grossiste du Sud ";
        Assert.Equal("Grossiste du Sud", item.BuildValue()!["create"]!["name"]!.Value<string>());

        item.CreateName = "   ";
        Assert.Null(item.BuildValue());
    }

    [Fact]
    public void OptionBuildValue_ProductCreateCarriesCoefficient()
    {
        var question = new ConversationQuestion
        {
            id = "line:2",
            kind = "line.product",
            options = { new ConversationOption { value = JObject.Parse("{\"create\":{\"name\":\"Savon\",\"coefficient\":2}}"), label = "Créer « Savon »" } },
        };
        var item = new ConversationOptionItem(question.options[0], question);
        Assert.True(item.IsProductCreate);
        Assert.Equal("2", item.CreateCoefficientText);

        item.CreateCoefficientText = "2,5";
        var value = item.BuildValue()!;
        Assert.Equal(2.5m, value["create"]!["coefficient"]!.Value<decimal>());
        Assert.Equal("Savon", value["create"]!["name"]!.Value<string>());
    }

    [Fact]
    public void InputValue_MoneyInCents_NumberPositive_DateIso()
    {
        var money = new ConversationQuestionItem(new ConversationQuestion { id = "unitCost:1", input = new ConversationInput { type = "money", currency = "EUR" } })
        {
            InputText = "24,90",
        };
        Assert.Equal(2490L, money.BuildInputValue()!.Value<long>());
        Assert.Equal("Montant (EUR)", money.InputLabel);

        money.InputText = "abc";
        Assert.Null(money.BuildInputValue());

        var rate = new ConversationQuestionItem(new ConversationQuestion { id = "exchangeRate", input = new ConversationInput { type = "number" } })
        {
            InputText = "1,36",
        };
        Assert.Equal(1.36m, rate.BuildInputValue()!.Value<decimal>());
        rate.InputText = "0";
        Assert.Null(rate.BuildInputValue());

        var date = new ConversationQuestionItem(new ConversationQuestion { id = "invoice.date", input = new ConversationInput { type = "date" }, answer = "2026-11-05" });
        Assert.Equal(new DateTime(2026, 11, 5), date.DateValue);
        Assert.Equal("2026-11-05", date.BuildInputValue()!.Value<string>());
    }

    // ---- Lecture et réponses -----------------------------------------------------

    [Fact]
    public async Task InitAsync_FollowsReadingUntilQuestions()
    {
        _service.OpenResult = Thread("reading");
        _service.GetResults.Enqueue(Thread("reading"));
        _service.GetResults.Enqueue(Thread("questions", null, SupplierQuestion()));
        var vm = CreateViewModel();

        await vm.InitAsync(42);

        Assert.Equal(new[] { 42 }, _service.OpenCalls);
        Assert.Equal(2, _service.GetCalls.Count);
        Assert.True(vm.IsAsking);
        Assert.Single(vm.Questions);
        Assert.Equal(2, vm.Questions[0].Options.Count);
        Assert.Equal("1 question à régler avant la commande.", vm.StatusText);
    }

    [Fact]
    public async Task InitAsync_OpenFails_ShowsError()
    {
        _service.OpenException = new HttpRequestException("réseau");
        var vm = CreateViewModel();

        await vm.InitAsync(42);

        Assert.Single(_dialog.ShownErrors);
        Assert.Null(vm.Conversation);
    }

    [Fact]
    public async Task ChooseOption_SendsAnswerAndAppliesThread()
    {
        _service.OpenResult = Thread("questions", null, SupplierQuestion());
        var vm = CreateViewModel();
        await vm.InitAsync(42);
        _service.AnswerResult = Thread("ready", "hash-1");

        await vm.ChooseOptionCommand.ExecuteAsync(vm.Questions[0].Options[0]);

        var call = Assert.Single(_service.AnswerCalls);
        Assert.Equal(7, call.Id);
        Assert.Equal("supplier", call.QuestionId);
        Assert.True(JToken.DeepEquals(new JObject { ["id"] = 3 }, call.Value));
        Assert.True(vm.IsReady);
        Assert.Empty(vm.Questions);
    }

    [Fact]
    public async Task ChooseOption_CreateWithoutName_ShowsErrorWithoutCall()
    {
        _service.OpenResult = Thread("questions", null, SupplierQuestion());
        var vm = CreateViewModel();
        await vm.InitAsync(42);
        vm.Questions[0].Options[1].CreateName = "";

        await vm.ChooseOptionCommand.ExecuteAsync(vm.Questions[0].Options[1]);

        Assert.Empty(_service.AnswerCalls);
        Assert.Equal("Indiquez un nom.", vm.Questions[0].ErrorText);
    }

    [Fact]
    public async Task Answer_Validation422_ShowsServerMessageOnQuestion()
    {
        _service.OpenResult = Thread("questions", null, SupplierQuestion());
        var vm = CreateViewModel();
        await vm.InitAsync(42);
        _service.AnswerException = Error(HttpStatusCode.UnprocessableEntity,
            "{\"code\":\"VALIDATION_FAILED\",\"message\":\"Données invalides\",\"errors\":[{\"path\":\"answers[0]\",\"message\":\"Ce fournisseur n'existe plus.\"}]}");

        await vm.ChooseOptionCommand.ExecuteAsync(vm.Questions[0].Options[0]);

        Assert.Equal("Ce fournisseur n'existe plus.", vm.Questions[0].ErrorText);
        Assert.True(vm.Questions[0].HasError);
        Assert.Empty(_dialog.ShownErrors);
    }

    [Fact]
    public async Task Answer_ConversationNotOpen_ReloadsThread()
    {
        _service.OpenResult = Thread("questions", null, SupplierQuestion());
        var vm = CreateViewModel();
        await vm.InitAsync(42);
        _service.AnswerException = Error(HttpStatusCode.Conflict, "{\"code\":\"CONVERSATION_NOT_OPEN\",\"message\":\"x\",\"state\":\"confirmed\"}");
        _service.GetResults.Enqueue(Thread("confirmed"));

        await vm.ChooseOptionCommand.ExecuteAsync(vm.Questions[0].Options[0]);

        Assert.True(vm.IsConfirmed);
        Assert.Single(_dialog.ShownInfos);
        Assert.Empty(_dialog.ShownErrors);
    }

    [Fact]
    public async Task SubmitInput_InvalidMoney_ShowsErrorWithoutCall()
    {
        var question = new ConversationQuestion { id = "unitCost:1", kind = "line.unitCost", input = new ConversationInput { type = "money" } };
        _service.OpenResult = Thread("questions", null, question);
        var vm = CreateViewModel();
        await vm.InitAsync(42);
        vm.Questions[0].InputText = "vingt";

        await vm.SubmitInputCommand.ExecuteAsync(vm.Questions[0]);

        Assert.Empty(_service.AnswerCalls);
        Assert.StartsWith("Montant invalide", vm.Questions[0].ErrorText);
    }

    // ---- Confirmation et annulation ----------------------------------------------

    [Fact]
    public async Task Confirm_SendsPreviewHash_AndAppliesConfirmedThread()
    {
        _service.OpenResult = Thread("ready", "hash-1");
        var vm = CreateViewModel();
        await vm.InitAsync(42);
        var confirmed = Thread("confirmed");
        confirmed.undo = new ConversationUndo { available = true, until = "2026-11-13T10:00:00Z" };
        _service.ConfirmResult = new JObject { ["thread"] = JObject.FromObject(confirmed) };

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Equal((7, "hash-1"), Assert.Single(_service.ConfirmCalls));
        Assert.True(vm.IsConfirmed);
        Assert.True(vm.CanUndo);
        Assert.True(vm.HasUndoText);
        Assert.False(vm.HasPreview);
        Assert.Single(_dialog.ShownInfos);
    }

    [Fact]
    public async Task Confirm_Refused_DoesNotCallService()
    {
        _service.OpenResult = Thread("ready", "hash-1");
        var vm = CreateViewModel();
        await vm.InitAsync(42);
        _dialog.ConfirmResult = false;

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Empty(_service.ConfirmCalls);
    }

    [Fact]
    public async Task Confirm_NotReady_DoesNothing()
    {
        _service.OpenResult = Thread("questions", "hash-1", SupplierQuestion());
        var vm = CreateViewModel();
        await vm.InitAsync(42);

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Empty(_dialog.ConfirmCalls);
        Assert.Empty(_service.ConfirmCalls);
    }

    [Fact]
    public async Task Confirm_PreviewChanged_ReloadsAndInforms()
    {
        _service.OpenResult = Thread("ready", "hash-1");
        var vm = CreateViewModel();
        await vm.InitAsync(42);
        _service.ConfirmException = Error(HttpStatusCode.Conflict, "{\"code\":\"PREVIEW_CHANGED\",\"message\":\"x\"}");
        _service.GetResults.Enqueue(Thread("ready", "hash-2"));

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Single(_service.GetCalls);
        Assert.Equal("hash-2", vm.Conversation!.previewHash);
        Assert.StartsWith("L'aperçu a changé", Assert.Single(_dialog.ShownInfos));
        Assert.Empty(_dialog.ShownErrors);
    }

    [Fact]
    public async Task Undo_Blocked_ReloadsAndShowsError()
    {
        var confirmed = Thread("confirmed");
        confirmed.undo = new ConversationUndo { available = true };
        _service.OpenResult = confirmed;
        var vm = CreateViewModel();
        await vm.InitAsync(42);
        _service.UndoException = Error(HttpStatusCode.Conflict, "{\"code\":\"UNDO_BLOCKED\",\"message\":\"Un article a déjà été vendu.\"}");
        var blocked = Thread("confirmed");
        blocked.undo = new ConversationUndo { available = false, blockers = { new ConversationBlocker { code = "SOLD", message = "Un article a déjà été vendu." } } };
        _service.GetResults.Enqueue(blocked);

        await vm.UndoCommand.ExecuteAsync(null);

        Assert.Single(_service.UndoCalls);
        Assert.Single(_dialog.ShownErrors);
        Assert.False(vm.CanUndo);
        Assert.True(vm.HasBlockers);
        Assert.Equal("Un article a déjà été vendu.", Assert.Single(vm.Blockers));
    }

    [Fact]
    public async Task Undo_Done_AppliesThread()
    {
        var confirmed = Thread("confirmed");
        confirmed.undo = new ConversationUndo { available = true };
        _service.OpenResult = confirmed;
        var vm = CreateViewModel();
        await vm.InitAsync(42);
        _service.UndoResult = new JObject { ["undone"] = true, ["thread"] = JObject.FromObject(Thread("questions", null, SupplierQuestion())) };

        await vm.UndoCommand.ExecuteAsync(null);

        Assert.True(vm.IsAsking);
        Assert.StartsWith("Facture annulée", Assert.Single(_dialog.ShownInfos));
    }

    [Fact]
    public async Task ReviewInDetail_NavigatesToReviewPage()
    {
        _service.OpenResult = Thread("questions", null, SupplierQuestion());
        var routes = new List<string>();
        var vm = CreateViewModel(routes);
        await vm.InitAsync(42);

        await vm.ReviewInDetailCommand.ExecuteAsync(null);

        Assert.Equal("InvoiceReviewPage?DocumentId=42", Assert.Single(routes));
    }

    [Fact]
    public async Task Failed_ShowsFailureText_AndRereadReopens()
    {
        var failed = Thread("failed");
        failed.document!.error = "DOCUMENT_UNREADABLE";
        _service.OpenResult = failed;
        var vm = CreateViewModel();
        await vm.InitAsync(42);
        Assert.True(vm.IsFailed);
        Assert.StartsWith("La facture est illisible", vm.StatusText);

        _service.OpenResult = Thread("questions", null, SupplierQuestion());
        await vm.RereadCommand.ExecuteAsync(null);

        Assert.Equal(new[] { 42 }, _documents.RereadCalls);
        Assert.Equal(2, _service.OpenCalls.Count);
        Assert.True(vm.IsAsking);
    }

    // ---- IA-FACT-2-A : ajout à une commande ouverte -----------------------------------

    private static InvoiceConversation ReadyToAdd()
    {
        var thread = Thread("ready", "hash-a");
        thread.preview = new InvoicePreview
        {
            currency = "EUR",
            invoiceCurrency = "EUR",
            lines =
            {
                new InvoicePreviewLine
                {
                    n = 1, quantity = 2, unitCost = 1200, kind = "existing",
                    lineBefore = new InvoicePreviewOrderLine { quantity = 3, unitCost = 1000 },
                    lineAfter = new InvoicePreviewOrderLine { quantity = 5, unitCost = 1080 },
                },
            },
            target = new InvoicePreviewTarget
            {
                commandeId = 9, name = "Commande textile", currency = "EUR", budgetBefore = 3000, budgetAfter = 5400,
                shipping = new InvoicePreviewShipping { current = 800, added = 1200, total = 2000 },
            },
        };
        return thread;
    }

    [Fact]
    public async Task Target_ShowsAddButtonBudgetShippingAndOrderLine()
    {
        _service.OpenResult = ReadyToAdd();
        var vm = CreateViewModel();
        await vm.InitAsync(42);

        Assert.True(vm.AddsToOrder);
        Assert.Equal("Ajouter à la commande", vm.ConfirmButtonText);
        Assert.StartsWith("Ajout à la commande « Commande textile »", vm.TargetText);
        Assert.Contains("Budget :", vm.TargetText);
        Assert.Contains("Frais de port :", vm.TargetText);
        Assert.StartsWith("Déjà dans la commande : 3 × ", vm.PreviewLines[0].OrderLineText);
        Assert.Contains("→ 5 × ", vm.PreviewLines[0].OrderLineText);
    }

    [Fact]
    public async Task RefusedTarget_IsNotAnAdd()
    {
        var thread = ReadyToAdd();
        thread.preview!.target!.refused = "COMMANDE_CLOSED";
        _service.OpenResult = thread;
        var vm = CreateViewModel();
        await vm.InitAsync(42);

        Assert.False(vm.AddsToOrder);
        Assert.Null(vm.TargetText);
        Assert.Equal("Créer la commande", vm.ConfirmButtonText);
    }

    [Fact]
    public async Task ConfirmAdd_UsesAddWording()
    {
        _service.OpenResult = ReadyToAdd();
        var vm = CreateViewModel();
        await vm.InitAsync(42);
        _service.ConfirmResult = new JObject { ["thread"] = JObject.FromObject(Thread("confirmed")) };

        await vm.ConfirmCommand.ExecuteAsync(null);

        Assert.Equal("Ajouter à la commande", _dialog.ConfirmCalls[0].Accept);
        Assert.StartsWith("Facture ajoutée à la commande", Assert.Single(_dialog.ShownInfos));
        Assert.False(vm.AddsToOrder);
    }

    // ---- IA-FACT-2-B : phrase libre -------------------------------------------------

    [Fact]
    public async Task SendMessage_AppliesThread_ShowsReplyAndIgnored()
    {
        _service.OpenResult = Thread("questions", null, SupplierQuestion(), new ConversationQuestion { id = "unitCost:1", title = "Coût de la ligne 1", input = new ConversationInput { type = "money" } });
        var vm = CreateViewModel();
        await vm.InitAsync(42);
        Assert.True(vm.CanWrite);
        vm.MessageText = "  c'est Grossiste Caraïbes, la ligne 1 coûte à peu près rien ";
        var after = Thread("questions", null, new ConversationQuestion { id = "unitCost:1", title = "Coût de la ligne 1", input = new ConversationInput { type = "money" } });
        _service.MessageResult = new JObject
        {
            ["reply"] = "J'ai retenu le fournisseur.",
            ["applied"] = new JArray(new JObject { ["questionId"] = "supplier", ["value"] = new JObject { ["id"] = 3 } }),
            ["ignored"] = new JArray(new JObject { ["questionId"] = "unitCost:1", ["reason"] = "VALUE_NOT_IN_SENTENCE" }),
            ["thread"] = JObject.FromObject(after),
        };

        await vm.SendMessageCommand.ExecuteAsync(null);

        Assert.Equal((7, "c'est Grossiste Caraïbes, la ligne 1 coûte à peu près rien"), Assert.Single(_service.MessageCalls));
        Assert.Single(vm.Questions);
        Assert.Null(vm.MessageText);
        Assert.StartsWith("J'ai retenu le fournisseur.", vm.ReplyText);
        Assert.Contains("• Coût de la ligne 1 : la valeur n'est pas écrite dans votre phrase", vm.ReplyText);
    }

    [Fact]
    public async Task SendMessage_EmptyOrTooLong_NoCall()
    {
        _service.OpenResult = Thread("questions", null, SupplierQuestion());
        var vm = CreateViewModel();
        await vm.InitAsync(42);

        vm.MessageText = "   ";
        await vm.SendMessageCommand.ExecuteAsync(null);
        Assert.True(vm.HasMessageError);

        vm.MessageText = new string('a', InvoiceConversationViewModel.MaxMessageLength + 1);
        await vm.SendMessageCommand.ExecuteAsync(null);
        Assert.Equal("1000 caractères au plus.", vm.MessageError);
        Assert.Empty(_service.MessageCalls);
    }

    [Theory]
    [InlineData("AI_DAILY_LIMIT", 429, "La limite de réponses")]
    [InlineData("AI_CREDITS_EXHAUSTED", 402, "Plus d'unités IA")]
    [InlineData("AI_NOT_INCLUDED", 403, "La phrase libre n'est pas comprise")]
    [InlineData("AI_UNAVAILABLE", 503, "L'IA est indisponible")]
    public async Task SendMessage_AiRefusal_ShownUnderTheBox(string code, int status, string start)
    {
        _service.OpenResult = Thread("ready", "h", SupplierQuestion());
        var vm = CreateViewModel();
        await vm.InitAsync(42);
        vm.MessageText = "prix hors taxes";
        _service.MessageException = Error((HttpStatusCode)status, $"{{\"code\":\"{code}\",\"message\":\"x\"}}");

        await vm.SendMessageCommand.ExecuteAsync(null);

        Assert.StartsWith(start, vm.MessageError);
        Assert.Equal("prix hors taxes", vm.MessageText);
        Assert.Empty(_dialog.ShownErrors);
    }

    [Fact]
    public async Task CanWrite_OnlyWhileTheThreadWaitsForAnswers()
    {
        _service.OpenResult = Thread("confirmed");
        var vm = CreateViewModel();
        await vm.InitAsync(42);
        Assert.False(vm.CanWrite);
        vm.MessageText = "bonjour";

        await vm.SendMessageCommand.ExecuteAsync(null);

        Assert.Empty(_service.MessageCalls);
    }

    // ---- Abandon (IA-ACT-1) -----------------------------------------------------------

    [Fact]
    public async Task Abandon_AfterConfirmation_ClosesTheThread()
    {
        _service.OpenResult = Thread("questions", null, SupplierQuestion());
        var vm = CreateViewModel();
        await vm.InitAsync(42);

        await vm.AbandonCommand.ExecuteAsync(null);

        Assert.Equal(new[] { 7 }, _service.AbandonCalls);
        Assert.True(vm.IsAbandoned);
        Assert.False(vm.CanWrite);
        Assert.StartsWith("Facture abandonnée", vm.StatusText);
    }

    [Fact]
    public async Task Abandon_Refused_DoesNotCall()
    {
        _service.OpenResult = Thread("ready", "h");
        var vm = CreateViewModel();
        await vm.InitAsync(42);
        _dialog.ConfirmResult = false;

        await vm.AbandonCommand.ExecuteAsync(null);

        Assert.Empty(_service.AbandonCalls);
        Assert.True(vm.IsReady);
    }

    // ---- Service ---------------------------------------------------------------

    [Fact]
    public async Task Service_UsesConversationRoutes()
    {
        var http = new FakeHttpService { ResultToReturn = new InvoiceConversation() };
        var service = new InvoiceConversationService(http);

        await service.OpenAsync(42);
        await service.AnswerAsync(7, "supplier", new JObject { ["id"] = 3 });

        Assert.Equal("gestion/conversations?locale=fr", http.PostedAuthData[0].Url);
        Assert.Equal(42, ((JObject)http.PostedAuthData[0].Data)["documentId"]!.Value<int>());
        Assert.Equal("gestion/conversations/7/answers?locale=fr", http.PostedAuthData[1].Url);
        var answer = ((JObject)http.PostedAuthData[1].Data)["answers"]![0]!;
        Assert.Equal("supplier", answer["questionId"]!.Value<string>());
        Assert.Equal(3, answer["value"]!["id"]!.Value<int>());
    }

    private sealed class FakeConversationService : IInvoiceConversationService
    {
        public List<int> OpenCalls { get; } = new();
        public List<int> GetCalls { get; } = new();
        public List<(int Id, string QuestionId, JToken? Value)> AnswerCalls { get; } = new();
        public List<(int Id, string Hash)> ConfirmCalls { get; } = new();
        public List<int> UndoCalls { get; } = new();

        public InvoiceConversation OpenResult { get; set; } = new();
        public Queue<InvoiceConversation> GetResults { get; } = new();
        public InvoiceConversation AnswerResult { get; set; } = new();
        public JObject? ConfirmResult { get; set; }
        public JObject? UndoResult { get; set; }
        public Exception? OpenException { get; set; }
        public Exception? AnswerException { get; set; }
        public Exception? ConfirmException { get; set; }
        public Exception? UndoException { get; set; }

        public Task<InvoiceConversation> OpenAsync(int documentId)
        {
            OpenCalls.Add(documentId);
            return OpenException != null ? Task.FromException<InvoiceConversation>(OpenException) : Task.FromResult(OpenResult);
        }

        public Task<InvoiceConversation> GetAsync(int id)
        {
            GetCalls.Add(id);
            return Task.FromResult(GetResults.Count > 0 ? GetResults.Dequeue() : OpenResult);
        }

        public Task<InvoiceConversation> AnswerAsync(int id, string questionId, JToken? value)
        {
            AnswerCalls.Add((id, questionId, value));
            return AnswerException != null ? Task.FromException<InvoiceConversation>(AnswerException) : Task.FromResult(AnswerResult);
        }

        public Task<JObject?> ConfirmAsync(int id, string previewHash)
        {
            ConfirmCalls.Add((id, previewHash));
            return ConfirmException != null ? Task.FromException<JObject?>(ConfirmException) : Task.FromResult(ConfirmResult);
        }

        public List<(int Id, string Text)> MessageCalls { get; } = new();
        public JObject? MessageResult { get; set; }
        public Exception? MessageException { get; set; }

        public Task<JObject?> SendMessageAsync(int id, string text)
        {
            MessageCalls.Add((id, text));
            return MessageException != null ? Task.FromException<JObject?>(MessageException) : Task.FromResult(MessageResult);
        }

        public List<int> AbandonCalls { get; } = new();
        public Exception? AbandonException { get; set; }

        public Task<InvoiceConversation> AbandonAsync(int id)
        {
            AbandonCalls.Add(id);
            return AbandonException != null
                ? Task.FromException<InvoiceConversation>(AbandonException)
                : Task.FromResult(new InvoiceConversation { id = id, state = "abandoned" });
        }

        public Task<JObject?> UndoAsync(int id)
        {
            UndoCalls.Add(id);
            return UndoException != null ? Task.FromException<JObject?>(UndoException) : Task.FromResult(UndoResult);
        }
    }
}
