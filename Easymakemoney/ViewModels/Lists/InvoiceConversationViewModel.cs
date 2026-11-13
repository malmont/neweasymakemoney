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
        public const int MaxMessageLength = 1000;

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
            nameof(HasLinesTotal), nameof(HasUndoText), nameof(HasCredits), nameof(AddsToOrder), nameof(TargetText),
            nameof(ConfirmButtonText), nameof(CanWrite))]
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

        /// <summary>IA-FACT-2-B : le fil attend une réponse, par bouton (gratuit) ou par une phrase (1 unité d'IA).</summary>
        public bool CanWrite => IsAsking || IsReady;

        [ObservableProperty]
        private string? messageText;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasMessageError))]
        private string? messageError;

        public bool HasMessageError => !string.IsNullOrWhiteSpace(MessageError);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasReply))]
        private string? replyText;

        public bool HasReply => !string.IsNullOrWhiteSpace(ReplyText);

        /// <summary>IA-FACT-2-A : la facture s'ajoute à une commande ouverte au lieu d'en créer une.</summary>
        public bool AddsToOrder => Conversation?.preview?.AddsToOrder == true && !IsConfirmed;

        public string ConfirmButtonText => AddsToOrder ? "Ajouter à la commande" : "Créer la commande";

        /// <summary>La commande cible et son budget avant et après, montants du serveur.</summary>
        public string? TargetText
        {
            get
            {
                if (!AddsToOrder || Conversation?.preview?.target is not { } target)
                    return null;
                string Money(long cents) => SiteCurrency.FormatCents(cents, CultureInfo.CurrentCulture, target.currency);
                var text = $"Ajout à la commande « {target.name ?? $"n° {target.commandeId}"} »";
                if (target.budgetBefore is { } before && target.budgetAfter is { } after)
                    text += $"\nBudget : {Money(before)} → {Money(after)}";
                if (target.shipping is { added: > 0 } shipping && shipping.current is { } current && shipping.total is { } total)
                    text += $"\nFrais de port : {Money(current)} → {Money(total)}";
                return text;
            }
        }
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
            "ready" => AddsToOrder
                ? "Tout est prêt : vérifiez l'aperçu, puis ajoutez la facture à la commande."
                : "Tout est prêt : vérifiez l'aperçu, puis créez la commande.",
            "confirmed" => "Facture enregistrée dans la commande.",
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
            var adds = AddsToOrder;
            if (!await _dialogService.ConfirmAsync(ConfirmButtonText,
                    adds
                        ? "Ajouter les lignes de cette facture à la commande d'après cet aperçu ? Vous pourrez annuler l'ajout pendant 24 heures."
                        : "Créer la commande, ses lignes d'achat et les articles nouveaux d'après cet aperçu ? Vous pourrez l'annuler pendant 24 heures.",
                    ConfirmButtonText, "Annuler"))
                return;

            IsBusy = true;
            try
            {
                var result = await _conversations.ConfirmAsync(thread.id, hash);
                if (result?["thread"]?.ToObject<InvoiceConversation>() is { } updated)
                    Apply(updated);
                else
                    Apply(await _conversations.GetAsync(thread.id));
                await _dialogService.ShowInfoAsync(adds
                    ? "Facture ajoutée à la commande. L'ajout reste annulable pendant 24 heures."
                    : "Commande créée. Elle reste annulable pendant 24 heures.");
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
                    "Retirer d'un bloc tout ce que cette facture a créé ou ajouté (commande ou lignes ajoutées, articles nouveaux, frais de port) ? La facture redevient à vérifier.",
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
                await _dialogService.ShowInfoAsync("Facture annulée : tout ce qu'elle avait créé ou ajouté est retiré.");
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
        private async Task Abandon()
        {
            if (IsBusy || Conversation is not { } thread || !CanWrite)
                return;
            if (!await _dialogService.ConfirmAsync("Abandonner cette facture",
                    "Fermer cette conversation sans rien créer ? La facture lue reste dans vos documents et pourra être reprise.",
                    "Abandonner", "Garder"))
                return;

            IsBusy = true;
            try
            {
                Apply(await _conversations.AbandonAsync(thread.id));
            }
            catch (ApiException e) when (e.ApiError.Code == "CONVERSATION_NOT_OPEN")
            {
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
        private async Task SendMessage()
        {
            if (IsBusy || Conversation is not { } thread || !CanWrite)
                return;
            var text = MessageText?.Trim();
            MessageError = null;
            if (string.IsNullOrEmpty(text))
            {
                MessageError = "Écrivez votre réponse, par exemple : « le fournisseur est Grossiste Caraïbes, prix hors taxes ».";
                return;
            }
            if (text.Length > MaxMessageLength)
            {
                MessageError = $"{MaxMessageLength} caractères au plus.";
                return;
            }

            IsBusy = true;
            try
            {
                var result = await _conversations.SendMessageAsync(thread.id, text);
                var updated = result?["thread"]?.ToObject<InvoiceConversation>();
                Apply(updated ?? await _conversations.GetAsync(thread.id));
                ReplyText = BuildReply(result, Conversation);
                MessageText = null;
            }
            catch (ApiException e) when (e.ApiError.Code == "CONVERSATION_NOT_OPEN")
            {
                await ReloadAsync();
                MessageError = "Cette facture a changé d'état entre-temps : l'écran est à jour.";
            }
            catch (ApiException e) when (AiRefusalText(e) is { } refusal)
            {
                MessageError = refusal;
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

        /// <summary>Refus de la phrase libre, en clair ; les boutons restent gratuits.</summary>
        private static string? AiRefusalText(ApiException e) => e.ApiError.Code switch
        {
            "AI_DAILY_LIMIT" => "La limite de réponses de l'IA pour aujourd'hui est atteinte. Répondez avec les boutons : c'est gratuit.",
            "AI_CREDITS_EXHAUSTED" => "Plus d'unités IA ce mois-ci. Répondez avec les boutons : c'est gratuit.",
            "AI_NOT_INCLUDED" => "La phrase libre n'est pas comprise dans votre forfait. Répondez avec les boutons.",
            "AI_UNAVAILABLE" => "L'IA est indisponible pour le moment ; aucune unité n'a été utilisée. Réessayez plus tard ou répondez avec les boutons.",
            "VALIDATION_FAILED" => e.ApiError.Errors.FirstOrDefault()?.Message ?? e.Message,
            _ => null,
        };

        /// <summary>La phrase de l'IA, puis ce que le serveur a écarté, question par question.</summary>
        public static string? BuildReply(JObject? result, InvoiceConversation? thread)
        {
            if (result == null)
                return null;
            var lines = new List<string>();
            if (result.Value<string>("reply") is { Length: > 0 } reply)
                lines.Add(reply);
            if (result["ignored"] is JArray ignored)
                foreach (var item in ignored.OfType<JObject>())
                {
                    var questionId = item.Value<string>("questionId");
                    var title = thread?.questions.FirstOrDefault(q => q.id == questionId)?.title ?? questionId ?? "Réponse";
                    lines.Add($"• {title} : {IgnoredReasonText(item.Value<string>("reason"))}");
                }
            return lines.Count > 0 ? string.Join("\n", lines) : null;
        }

        public static string IgnoredReasonText(string? reason) => reason switch
        {
            "NOT_AN_OPTION" => "ce choix n'est pas proposé, choisissez avec un bouton",
            "NAME_NOT_IN_SENTENCE" => "le nom n'est pas écrit dans votre phrase",
            "VALUE_NOT_IN_SENTENCE" => "la valeur n'est pas écrite dans votre phrase, donnez-la en chiffres",
            "ABANDON_BY_BUTTON_ONLY" => "pour abandonner, utilisez le bouton",
            "UNKNOWN_QUESTION" => "cette question n'existe pas",
            "AMBIGUOUS_OPTION" => "plusieurs choix correspondent, choisissez avec un bouton",
            "OPTION_NOT_IN_SENTENCE" => "le choix n'est pas nommé dans votre phrase",
            null or "" => "réponse non retenue",
            _ => "réponse refusée par le contrôle, répondez avec les boutons",
        };

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
            string Line(InvoicePreviewOrderLine orderLine) =>
                $"{orderLine.quantity.ToString("0.##", CultureInfo.CurrentCulture)} × "
                + (orderLine.unitCost is { } unit ? SiteCurrency.FormatCents(unit, CultureInfo.CurrentCulture, preview.target?.currency ?? preview.currency) : "?");
            if (line.lineBefore is { } before && line.lineAfter is { } after)
                OrderLineText = $"Déjà dans la commande : {Line(before)} → {Line(after)}";
        }

        /// <summary>IA-FACT-2-A : la ligne existante de la commande, avant et après l'ajout (coût moyen pondéré calculé par le serveur).</summary>
        public string? OrderLineText { get; }
        public bool HasOrderLine => OrderLineText != null;

        public string Title { get; }
        public string CostText { get; }
        public string PriceText { get; }
        public bool IsUnresolved { get; }
    }
}
