using System.Globalization;
using Easymakemoney.Converters;
using Easymakemoney.Services.Api;
using Easymakemoney.Services.Dialog;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>
    /// Lot IA-FACT-1 : une facture fournisseur menée de la photo à la commande, en conversation. Le serveur lit le document, pose ses
    /// questions, calcule l'aperçu et l'empreinte ; l'application affiche et renvoie les réponses (gratuites, aucune IA). Confirmation
    /// avec l'empreinte, annulation d'un bloc sous 24 heures. Aucun calcul métier ici.
    /// </summary>
    public partial class InvoiceConversationViewModel : BaseViewModel
    {
        private const int MaxPolls = 60;

        private readonly IInvoiceConversationService _conversations;
        private readonly IDocumentReaderService _documents;
        private readonly IDialogService _dialogService;
        private bool _stopped;
        private int _documentId;

        public InvoiceConversationViewModel(IInvoiceConversationService conversations, IDocumentReaderService documents, IDialogService dialogService)
        {
            _conversations = conversations;
            _documents = documents;
            _dialogService = dialogService;
        }

        /// <summary>Attente entre deux relectures du fil pendant la lecture (point d'entrée des tests).</summary>
        public TimeSpan PollDelay { get; set; } = TimeSpan.FromSeconds(4);

        /// <summary>Navigation (point d'entrée des tests).</summary>
        public Func<string, Task> NavigateAsync { get; set; } = route => Shell.Current?.GoToAsync(route) ?? Task.CompletedTask;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsReading), nameof(IsFailed), nameof(IsAsking), nameof(IsReady), nameof(IsConfirmed),
            nameof(IsAbandoned), nameof(SummaryText), nameof(HasPreview), nameof(LinesTotalText), nameof(CanUndo), nameof(UndoText),
            nameof(HasBlockers), nameof(CreditsText), nameof(StatusText), nameof(CanReviewInDetail), nameof(HasSummary),
            nameof(HasLinesTotal), nameof(HasUndoText), nameof(HasCredits))]
        private InvoiceConversation? conversation;

        public ObservableCollection<ConversationQuestionItem> Questions { get; } = new();
        public ObservableCollection<ConversationPreviewLine> PreviewLines { get; } = new();
        public ObservableCollection<string> Blockers { get; } = new();

        public bool IsReading => Conversation?.IsReading == true;
        public bool IsFailed => Conversation?.IsFailed == true;
        public bool IsAsking => Conversation?.IsAsking == true;
        public bool IsReady => Conversation?.IsReady == true;
        public bool IsConfirmed => Conversation?.IsConfirmed == true;
        public bool IsAbandoned => Conversation?.IsAbandoned == true;
        public bool CanReviewInDetail => Conversation != null && !IsConfirmed && !IsAbandoned && !IsReading;

        public string? SummaryText => Conversation?.summary?.text;
        public bool HasPreview => Conversation?.preview != null && !IsConfirmed;
        public string? LinesTotalText => Conversation?.preview?.LinesTotalText;
        public bool HasSummary => !string.IsNullOrWhiteSpace(SummaryText);
        public bool HasLinesTotal => LinesTotalText != null;
        public bool HasUndoText => UndoText != null;
        public bool HasCredits => CreditsText != null;

        public string StatusText => Conversation?.state switch
        {
            "reading" => "Lecture de la facture en cours…",
            "failed" => Conversation.FailureText,
            "questions" => Conversation.openQuestions is > 0 and var n
                ? $"{n} question{(n > 1 ? "s" : string.Empty)} à régler avant la commande."
                : "Réglez les points de l'aperçu avant la commande.",
            "ready" => "Tout est prêt : vérifiez l'aperçu, puis créez la commande.",
            "confirmed" => "Commande créée.",
            "abandoned" => "Facture abandonnée : rien n'a été créé.",
            _ => string.Empty,
        };

        public bool CanUndo => IsConfirmed && Conversation?.undo?.available == true;
        public bool HasBlockers => Blockers.Count > 0;

        public string? UndoText => Conversation?.undo is { until: { } until } && DateTimeOffset.TryParse(until, CultureInfo.InvariantCulture, DateTimeStyles.None, out var at)
            ? $"Annulable jusqu'au {FrenchDate.Format(at.LocalDateTime, "d MMMM 'à' H' h 'mm")}"
            : null;

        public string? CreditsText => Conversation?.credits is { remaining: { } remaining, monthly: { } monthly }
            ? $"Unités IA : {remaining} sur {monthly} ce mois-ci"
            : null;

        /// <summary>Ouvre le fil du document (ou reprend celui déjà ouvert), puis suit la lecture.</summary>
        public async Task InitAsync(int documentId)
        {
            _documentId = documentId;
            _stopped = false;
            try
            {
                Apply(await _conversations.OpenAsync(documentId));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                await _dialogService.ShowErrorAsync(e);
                return;
            }
            await FollowReadingAsync();
        }

        public void Stop() => _stopped = true;

        private async Task FollowReadingAsync()
        {
            for (var attempt = 0; attempt < MaxPolls && IsReading && !_stopped; attempt++)
            {
                await Task.Delay(PollDelay);
                if (_stopped || Conversation == null)
                    return;
                try
                {
                    Apply(await _conversations.GetAsync(Conversation.id));
                }
                catch (Exception e)
                {
                    // Réseau faible : la lecture continue sur le serveur, on réessaie au prochain tour.
                    Debug.WriteLine(e);
                }
            }
        }

        /// <summary>Le fil rendu par le serveur remplace l'affichage : questions, aperçu et annulation sont recalculés par lui.</summary>
        public void Apply(InvoiceConversation thread)
        {
            Conversation = thread;

            Questions.Clear();
            foreach (var question in thread.questions)
                Questions.Add(new ConversationQuestionItem(question));

            PreviewLines.Clear();
            if (thread.preview is { } preview && !thread.IsConfirmed)
                foreach (var line in preview.lines)
                    PreviewLines.Add(new ConversationPreviewLine(line, preview));

            Blockers.Clear();
            foreach (var blocker in thread.undo?.blockers ?? new())
                if (!string.IsNullOrWhiteSpace(blocker.message))
                    Blockers.Add(blocker.message!);
            OnPropertyChanged(nameof(HasBlockers));
        }

        private async Task SendAnswerAsync(ConversationQuestionItem item, JToken value)
        {
            if (IsBusy || Conversation == null)
                return;
            IsBusy = true;
            item.ErrorText = null;
            try
            {
                Apply(await _conversations.AnswerAsync(Conversation.id, item.Question.id, value));
            }
            catch (ApiException e) when (e.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
            {
                item.ErrorText = e.ApiError.Errors.FirstOrDefault()?.Message ?? e.Message;
            }
            catch (ApiException e) when (e.ApiError.Code == "CONVERSATION_NOT_OPEN")
            {
                // Lecture relancée, facture confirmée ou fil abandonné ailleurs : on montre l'état réel du fil.
                await ReloadAsync();
                await _dialogService.ShowInfoAsync("Cette facture a changé d'état entre-temps : l'écran est à jour.");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                await _dialogService.ShowErrorAsync(e);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ChooseOption(ConversationOptionItem? option)
        {
            if (option == null)
                return;
            var item = Questions.FirstOrDefault(q => q.Options.Contains(option));
            if (item == null || option.BuildValue() is not { } value)
            {
                if (item != null)
                    item.ErrorText = option.IsCreate ? "Indiquez un nom." : "Choix invalide.";
                return;
            }
            await SendAnswerAsync(item, value);
        }

        [RelayCommand]
        private async Task SubmitInput(ConversationQuestionItem? item)
        {
            if (item == null)
                return;
            if (item.BuildInputValue() is not { } value)
            {
                item.ErrorText = item.InputType switch
                {
                    "money" => "Montant invalide : saisissez par exemple 24,90.",
                    "number" => "Nombre invalide : saisissez un nombre supérieur à 0.",
                    _ => "Valeur invalide.",
                };
                return;
            }
            await SendAnswerAsync(item, value);
        }

        [RelayCommand]
        private async Task Confirm()
        {
            if (IsBusy || Conversation is not { IsReady: true, previewHash: { } hash } thread)
                return;
            if (!await _dialogService.ConfirmAsync("Créer la commande",
                    "Créer la commande, ses lignes d'achat et les articles nouveaux d'après cet aperçu ? Vous pourrez l'annuler pendant 24 heures.",
                    "Créer la commande", "Annuler"))
                return;

            IsBusy = true;
            try
            {
                var result = await _conversations.ConfirmAsync(thread.id, hash);
                if (result?["thread"]?.ToObject<InvoiceConversation>() is { } updated)
                    Apply(updated);
                else
                    Apply(await _conversations.GetAsync(thread.id));
                await _dialogService.ShowInfoAsync("Commande créée. Elle reste annulable pendant 24 heures.");
            }
            catch (ApiException e) when (e.ApiError.Code is "PREVIEW_CHANGED" or "CONVERSATION_NOT_READY")
            {
                // L'aperçu a changé (prix, taux, catalogue) : le serveur recalcule, le gérant relit avant de confirmer.
                await ReloadAsync();
                await _dialogService.ShowInfoAsync(e.ApiError.Code == "PREVIEW_CHANGED"
                    ? "L'aperçu a changé depuis son affichage. Vérifiez-le, puis confirmez de nouveau."
                    : "Il reste un point à régler avant de créer la commande.");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                await _dialogService.ShowErrorAsync(e);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task Undo()
        {
            if (IsBusy || Conversation is not { IsConfirmed: true } thread)
                return;
            if (!await _dialogService.ConfirmAsync("Annuler cette facture",
                    "Retirer d'un bloc tout ce que cette facture a créé (commande, lignes, articles nouveaux, frais de port) ? La facture redevient à vérifier.",
                    "Tout retirer", "Garder"))
                return;

            IsBusy = true;
            try
            {
                var result = await _conversations.UndoAsync(thread.id);
                if (result?["thread"]?.ToObject<InvoiceConversation>() is { } updated)
                    Apply(updated);
                else
                    await ReloadAsync();
                await _dialogService.ShowInfoAsync("Facture annulée : tout ce qu'elle avait créé est retiré.");
            }
            catch (ApiException e) when (e.ApiError.Code is "UNDO_BLOCKED" or "UNDO_EXPIRED" or "ALREADY_UNDONE" or "UNDO_UNAVAILABLE")
            {
                await ReloadAsync();
                await _dialogService.ShowErrorAsync(e);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                await _dialogService.ShowErrorAsync(e);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task Reread()
        {
            if (IsBusy || _documentId == 0)
                return;
            if (!await _dialogService.ConfirmAsync("Relire la facture",
                    "Relancer la lecture par l'IA ? Cela consomme une unité IA du mois.", "Relire", "Annuler"))
                return;
            try
            {
                await _documents.RereadAsync(_documentId);
                Apply(await _conversations.OpenAsync(_documentId));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                await _dialogService.ShowErrorAsync(e);
                return;
            }
            await FollowReadingAsync();
        }

        [RelayCommand]
        private Task ReviewInDetail() =>
            _documentId > 0 ? NavigateAsync($"{nameof(InvoiceReviewPage)}?DocumentId={_documentId}") : Task.CompletedTask;

        public async Task ReloadAsync()
        {
            if (Conversation == null)
                return;
            try
            {
                Apply(await _conversations.GetAsync(Conversation.id));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
    }

    /// <summary>Une question du serveur et la saisie du gérant pour y répondre.</summary>
    public partial class ConversationQuestionItem : ObservableObject
    {
        public ConversationQuestionItem(ConversationQuestion question)
        {
            Question = question;
            Options = question.options.Select(option => new ConversationOptionItem(option, question)).ToList();
            if (question.InputType == "date" && DateTime.TryParseExact(question.answer?.Type == JTokenType.String ? question.answer.Value<string>() : null,
                    "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
                dateValue = day;
        }

        public ConversationQuestion Question { get; }
        public IReadOnlyList<ConversationOptionItem> Options { get; }

        public string Title => Question.title ?? string.Empty;
        public string? Text => Question.text;
        public bool HasText => !string.IsNullOrWhiteSpace(Text);
        public bool IsRequired => Question.required;
        public bool IsAnswered => Question.IsAnswered;
        public bool IsToAnswer => IsRequired && !IsAnswered;
        public string? AnswerText => Question.AnswerText is { } text
            ? (Question.proposed ? $"Lu sur la facture : {text}" : $"Réponse : {text}")
            : null;
        public string InputType => Question.InputType;
        public bool IsChoice => InputType == "choice";
        public bool IsDate => InputType == "date";
        public bool IsMoney => InputType == "money";
        public bool IsNumber => InputType == "number";
        public bool HasInput => IsDate || IsMoney || IsNumber;
        public string InputLabel => IsMoney && Question.input?.currency is { } currency ? $"Montant ({currency})" : IsNumber ? "Valeur" : "Date";

        [ObservableProperty]
        private string? inputText;

        [ObservableProperty]
        private DateTime dateValue = DateTime.Today;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string? errorText;

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorText);

        /// <summary>Valeur à envoyer pour une saisie (montant en cents, nombre, date) ; null si elle est illisible.</summary>
        public JToken? BuildInputValue()
        {
            switch (InputType)
            {
                case "money":
                    var amount = DecimalInputConverter.Parse(InputText);
                    return amount is null or < 0 ? null : new JValue(MoneyHelper.ToCents(amount));
                case "number":
                    var number = DecimalInputConverter.Parse(InputText);
                    return number is null or <= 0 ? null : new JValue(number.Value);
                case "date":
                    return new JValue(DateValue.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                default:
                    return null;
            }
        }
    }

    /// <summary>Une option d'une question ; une option de création porte un nom (et un coefficient pour un article) modifiables.</summary>
    public partial class ConversationOptionItem : ObservableObject
    {
        private readonly ConversationQuestion _question;

        public ConversationOptionItem(ConversationOption option, ConversationQuestion question)
        {
            Option = option;
            _question = question;
            if (option.value is JObject { } value && value["create"] is JObject create)
            {
                createName = create.Value<string>("name");
                createCoefficientText = create["coefficient"]?.Type is JTokenType.Integer or JTokenType.Float
                    ? create.Value<double>("coefficient").ToString("0.##", CultureInfo.CurrentCulture)
                    : null;
            }
        }

        public ConversationOption Option { get; }
        public string Label => Option.label;
        public bool IsCreate => Option.IsCreate;
        public bool IsProductCreate => IsCreate && _question.kind == "line.product";
        public bool IsSelected => _question.AnsweredOption == Option;

        [ObservableProperty]
        private string? createName;

        [ObservableProperty]
        private string? createCoefficientText;

        /// <summary>La valeur de l'option telle que le serveur l'a donnée ; pour une création, avec le nom (et le coefficient) saisis.</summary>
        public JToken? BuildValue()
        {
            if (Option.value is not { } value)
                return null;
            if (!IsCreate)
                return value.DeepClone();

            var name = EditPayload.Text(CreateName);
            if (name == null)
                return null;
            var built = (JObject)value.DeepClone();
            var create = (JObject)built["create"]!;
            create["name"] = name;
            if (IsProductCreate && DecimalInputConverter.Parse(CreateCoefficientText) is > 0 and var coefficient)
                create["coefficient"] = coefficient;
            return built;
        }
    }

    /// <summary>Une ligne de l'aperçu calculé par le serveur.</summary>
    public class ConversationPreviewLine
    {
        public ConversationPreviewLine(InvoicePreviewLine line, InvoicePreview preview)
        {
            Title = $"Ligne {line.n}";
            var quantity = line.quantity.ToString("0.##", CultureInfo.CurrentCulture);
            CostText = line.unitCost is { } cost
                ? $"{quantity} × {SiteCurrency.FormatCents(cost, CultureInfo.CurrentCulture, preview.invoiceCurrency)}"
                : $"{quantity} × coût à compléter";
            PriceText = preview.PriceText(line);
            IsUnresolved = line.kind == "unresolved";
        }

        public string Title { get; }
        public string CostText { get; }
        public string PriceText { get; }
        public bool IsUnresolved { get; }
    }
}
