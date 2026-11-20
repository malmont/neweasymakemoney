using System.ComponentModel;
using System.Globalization;
using Easymakemoney.Converters;
using Easymakemoney.Services.Dialog;
using Easymakemoney.Utilities;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>
    /// Prêts (lot backend FIS-1) : la liste, avec en tête le total restant dû aujourd'hui des prêts dans la devise du site.
    /// Un prêt dans une autre devise est listé, avec la mention qu'il reste hors trésorerie et résultats.
    /// </summary>
    public partial class LoanListViewModel : BaseViewModel
    {
        private readonly ILoanService _loanService;
        private readonly IDialogService _dialogService;

        public LoanListViewModel(ILoanService loanService, IDialogService dialogService)
        {
            _loanService = loanService;
            _dialogService = dialogService;
        }

        /// <summary>Navigation (point d'entrée des tests).</summary>
        public Func<string, Task> NavigateAsync { get; set; } = route => Shell.Current?.GoToAsync(route) ?? Task.CompletedTask;

        public ObservableCollection<Loan> Loans { get; } = new();

        [ObservableProperty]
        private string _remainingTotalText = string.Empty;

        [ObservableProperty]
        private bool _hasOtherCurrency;

        [ObservableProperty]
        private bool _isEmpty;

        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            try
            {
                var list = await _loanService.ListAsync();
                var siteCurrency = SiteCurrency.CurrentCurrency;
                Loans.Clear();
                foreach (var loan in list.loans)
                    Loans.Add(loan);
                IsEmpty = Loans.Count == 0;
                RemainingTotalText = SiteCurrency.FormatCents(list.RemainingTotal(siteCurrency), null, siteCurrency);
                HasOtherCurrency = list.HasOtherCurrency(siteCurrency);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private Task Add() => NavigateAsync(nameof(LoanEditPage));

        [RelayCommand]
        private Task Open(Loan? loan) =>
            loan == null ? Task.CompletedTask : NavigateAsync($"{nameof(LoanDetailPage)}?LoanId={loan.id}");
    }

    /// <summary>
    /// Fiche d'un prêt : restant dû, prochaine échéance, coût total du crédit (le capital à part : il n'est jamais une charge),
    /// l'échéancier complet et les remboursements anticipés. Un directeur de groupe entré dans la boutique est en lecture seule.
    /// </summary>
    public partial class LoanDetailViewModel : BaseViewModel
    {
        private readonly ILoanService _loanService;
        private readonly IDialogService _dialogService;

        public LoanDetailViewModel(ILoanService loanService, IDialogService dialogService)
        {
            _loanService = loanService;
            _dialogService = dialogService;
        }

        /// <summary>Navigation (point d'entrée des tests).</summary>
        public Func<string, Task> NavigateAsync { get; set; } = route => Shell.Current?.GoToAsync(route) ?? Task.CompletedTask;

        public int LoanId { get; private set; }

        public ObservableCollection<LoanInstalment> Schedule { get; } = new();

        public ObservableCollection<LoanPrepayment> Prepayments { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasLoan))]
        private Loan? _loan;

        public bool HasLoan => Loan != null;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasNoPrepayments))]
        private bool _hasPrepayments;

        public bool HasNoPrepayments => !HasPrepayments;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanWrite))]
        private bool _isReadOnly;

        public bool CanWrite => !IsReadOnly;

        public string ReadOnlyText => LoanRules.ReadOnlyMessage;

        public string CapitalNotChargeText => LoanRules.CapitalNotChargeText;

        // Ajout d'un remboursement anticipé
        [ObservableProperty]
        private bool _isPrepaymentFormVisible;

        [ObservableProperty]
        private DateTime _prepaymentDate = DateTime.Today;

        [ObservableProperty]
        private string? _prepaymentAmountText;

        [ObservableProperty]
        private string? _prepaymentFeesText;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPrepaymentError))]
        private string _prepaymentError = string.Empty;

        public bool HasPrepaymentError => PrepaymentError.Length > 0;

        public async Task LoadAsync(int loanId)
        {
            if (IsBusy || loanId <= 0)
                return;

            LoanId = loanId;
            IsBusy = true;
            try
            {
                Apply(await _loanService.GetAsync(loanId));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void Apply(Loan loan)
        {
            Loan = loan;
            Schedule.Clear();
            foreach (var line in loan.ScheduleWithCurrency())
                Schedule.Add(line);
            Prepayments.Clear();
            foreach (var prepayment in loan.SortedPrepayments())
                Prepayments.Add(prepayment);
            HasPrepayments = Prepayments.Count > 0;
        }

        [RelayCommand]
        private void TogglePrepaymentForm()
        {
            IsPrepaymentFormVisible = !IsPrepaymentFormVisible;
            PrepaymentError = string.Empty;
        }

        [RelayCommand]
        private async Task AddPrepayment()
        {
            if (IsBusy || Loan == null || IsReadOnly)
                return;

            PrepaymentError = string.Empty;
            if (DecimalInputConverter.Parse(PrepaymentAmountText) is not { } amount || amount <= 0)
            {
                PrepaymentError = "Saisissez un montant supérieur à zéro.";
                return;
            }

            long? fees = null;
            if (!string.IsNullOrWhiteSpace(PrepaymentFeesText))
            {
                if (DecimalInputConverter.Parse(PrepaymentFeesText) is not { } parsedFees || parsedFees < 0)
                {
                    PrepaymentError = "Les frais doivent être un montant positif.";
                    return;
                }
                fees = MoneyHelper.ToCents(parsedFees);
            }

            var cents = MoneyHelper.ToCents(amount);
            var currency = Loan.currency;
            var message = $"Rembourser {SiteCurrency.FormatCents(cents, null, currency)} par avance le {TreasuryDates.Short(TreasuryDates.Iso(PrepaymentDate))}"
                + (fees is > 0 ? $", avec {SiteCurrency.FormatCents(fees.Value, null, currency)} de frais" : string.Empty)
                + " ? La date de fin ne change pas : les échéances suivantes baissent.";
            if (!await _dialogService.ConfirmAsync("Remboursement anticipé", message, "Ajouter", "Annuler"))
                return;

            IsBusy = true;
            try
            {
                await _loanService.AddPrepaymentAsync(Loan.id, PrepaymentDate, cents, fees);
                Apply(await _loanService.GetAsync(Loan.id));
                PrepaymentAmountText = null;
                PrepaymentFeesText = null;
                IsPrepaymentFormVisible = false;
            }
            catch (Exception ex) when (LoanRules.FieldErrors(ex) is { Count: > 0 } errors)
            {
                PrepaymentError = string.Join(Environment.NewLine, errors.Values.Distinct());
            }
            catch (Exception ex)
            {
                await HandleWriteErrorAsync(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task DeletePrepayment(LoanPrepayment? prepayment)
        {
            if (IsBusy || Loan == null || prepayment == null || IsReadOnly)
                return;

            var confirmed = await _dialogService.ConfirmAsync("Retirer ce remboursement",
                $"Retirer le remboursement anticipé du {TreasuryDates.Short(prepayment.date)} ? L'échéancier est recalculé.", "Retirer", "Annuler");
            if (!confirmed)
                return;

            IsBusy = true;
            try
            {
                Apply(await _loanService.DeletePrepaymentAsync(Loan.id, prepayment.Index));
            }
            catch (Exception ex)
            {
                await HandleWriteErrorAsync(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private Task Edit() =>
            Loan == null || IsReadOnly ? Task.CompletedTask : NavigateAsync($"{nameof(LoanEditPage)}?LoanId={Loan.id}");

        [RelayCommand]
        private async Task Delete()
        {
            if (IsBusy || Loan == null || IsReadOnly)
                return;

            var confirmed = await _dialogService.ConfirmAsync("Supprimer ce prêt",
                $"Supprimer « {Loan.label} », son échéancier et ses remboursements anticipés ? Il sort de la trésorerie et des résultats.", "Supprimer", "Annuler");
            if (!confirmed)
                return;

            IsBusy = true;
            try
            {
                await _loanService.DeleteAsync(Loan.id);
            }
            catch (Exception ex)
            {
                await HandleWriteErrorAsync(ex);
                return;
            }
            finally
            {
                IsBusy = false;
            }

            await NavigateAsync("..");
        }

        private async Task HandleWriteErrorAsync(Exception ex)
        {
            Debug.WriteLine(ex);
            if (LoanRules.IsReadOnlyRefusal(ex))
            {
                IsReadOnly = true;
                IsPrepaymentFormVisible = false;
                await _dialogService.ShowInfoAsync(LoanRules.ReadOnlyMessage, "Prêts");
                return;
            }
            await _dialogService.ShowErrorAsync(ex);
        }
    }

    /// <summary>
    /// Création et modification d'un prêt. L'aperçu se calcule en direct sur le serveur (POST gestion/prets/apercu),
    /// 400 ms après le dernier changement ; la demande précédente est annulée et une réponse ancienne n'écrase jamais
    /// une plus récente. Rien n'est écrit avant « Enregistrer » ; une modification n'envoie que les champs changés.
    /// </summary>
    public partial class LoanEditViewModel : BaseViewModel
    {
        private static readonly string[] ErrorGroups = { "label", "lender", "amount", "rate", "duration", "frequency", "dates", "type", "deferral", "insurance", "fees", "notes", "general" };

        /// <summary>Champs dont le changement relance l'aperçu (le prêteur et les notes n'y comptent pas).</summary>
        private static readonly HashSet<string> PreviewInputs = new()
        {
            nameof(Label), nameof(AmountText), nameof(RateText), nameof(DurationText), nameof(SelectedFrequency), nameof(SelectedType),
            nameof(DisbursedOn), nameof(FirstDueOn), nameof(HasDeferral), nameof(DeferralMonthsText), nameof(SelectedDeferralKind),
            nameof(SelectedInsuranceMode), nameof(InsuranceAmountText), nameof(InsuranceRateText), nameof(FeesText),
        };

        private readonly ILoanService _loanService;
        private readonly IDialogService _dialogService;
        private readonly Dictionary<string, string> _errors = new();
        private Dictionary<string, object?> _initialBody = new();
        private CancellationTokenSource? _previewCts;
        private int _previewVersion;
        private bool _suspendPreview;

        public LoanEditViewModel(ILoanService loanService, IDialogService dialogService)
        {
            _loanService = loanService;
            _dialogService = dialogService;
            _suspendPreview = true;
            SelectedType = Types[0];
            SelectedFrequency = Frequencies[0];
            SelectedDeferralKind = DeferralKinds[0];
            SelectedInsuranceMode = InsuranceModes[0];
            DisbursedOn = DateTime.Today;
            FirstDueOn = DateTime.Today.AddMonths(1);
            _suspendPreview = false;
            PreviewMessage = MissingText(new[] { "le nom", "le montant", "le taux", "la durée" });
        }

        /// <summary>Retour à l'écran précédent après l'enregistrement (point d'entrée des tests).</summary>
        public Func<Task> GoBackAsync { get; set; } = () => Shell.Current?.GoToAsync("..") ?? Task.CompletedTask;

        /// <summary>Délai avant l'aperçu, après le dernier changement (réduit dans les tests).</summary>
        public TimeSpan PreviewDelay { get; set; } = TimeSpan.FromMilliseconds(400);

        /// <summary>Dernier aperçu lancé (attendu par les tests).</summary>
        public Task PendingPreview { get; private set; } = Task.CompletedTask;

        public int LoanId { get; private set; }

        public bool IsNew => LoanId == 0;

        public string PageTitle => IsNew ? "Nouveau prêt" : "Modifier le prêt";

        public IReadOnlyList<TreasuryChoice> Types => LoanRules.Types;
        public IReadOnlyList<TreasuryChoice> Frequencies => LoanRules.Frequencies;
        public IReadOnlyList<TreasuryChoice> DeferralKinds => LoanRules.DeferralKinds;
        public IReadOnlyList<TreasuryChoice> InsuranceModes => LoanRules.InsuranceModes;

        [ObservableProperty]
        private string? _label;

        [ObservableProperty]
        private string? _lender;

        [ObservableProperty]
        private string? _amountText;

        [ObservableProperty]
        private string? _rateText;

        [ObservableProperty]
        private string? _durationText;

        [ObservableProperty]
        private TreasuryChoice? _selectedFrequency;

        [ObservableProperty]
        private TreasuryChoice? _selectedType;

        [ObservableProperty]
        private DateTime _disbursedOn;

        [ObservableProperty]
        private DateTime _firstDueOn;

        [ObservableProperty]
        private bool _hasDeferral;

        [ObservableProperty]
        private string? _deferralMonthsText;

        [ObservableProperty]
        private TreasuryChoice? _selectedDeferralKind;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsFixedInsurance), nameof(IsRateInsurance))]
        private TreasuryChoice? _selectedInsuranceMode;

        public bool IsFixedInsurance => SelectedInsuranceMode?.Code == "fixed";
        public bool IsRateInsurance => SelectedInsuranceMode?.Code is "initial" or "remaining";

        [ObservableProperty]
        private string? _insuranceAmountText;

        [ObservableProperty]
        private string? _insuranceRateText;

        [ObservableProperty]
        private string? _feesText;

        [ObservableProperty]
        private string? _notes;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanWrite))]
        private bool _isReadOnly;

        public bool CanWrite => !IsReadOnly;

        public string ReadOnlyText => LoanRules.ReadOnlyMessage;

        // Aperçu
        public ObservableCollection<LoanInstalment> PreviewLines { get; } = new();

        [ObservableProperty]
        private bool _hasPreview;

        [ObservableProperty]
        private bool _isPreviewLoading;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPreviewMessage))]
        private string _previewMessage = string.Empty;

        public bool HasPreviewMessage => PreviewMessage.Length > 0;

        [ObservableProperty]
        private string _previewInstalmentText = string.Empty;

        [ObservableProperty]
        private string _previewCreditCostText = string.Empty;

        [ObservableProperty]
        private string _previewCostDetailText = string.Empty;

        [ObservableProperty]
        private string _previewCapitalText = string.Empty;

        // Erreurs sous les champs
        public string LabelError => Error("label");
        public string LenderError => Error("lender");
        public string AmountError => Error("amount");
        public string RateError => Error("rate");
        public string DurationError => Error("duration");
        public string FrequencyError => Error("frequency");
        public string DatesError => Error("dates");
        public string TypeError => Error("type");
        public string DeferralError => Error("deferral");
        public string InsuranceError => Error("insurance");
        public string FeesError => Error("fees");
        public string NotesError => Error("notes");
        public string GeneralError => Error("general");
        public bool HasLabelError => LabelError.Length > 0;
        public bool HasLenderError => LenderError.Length > 0;
        public bool HasAmountError => AmountError.Length > 0;
        public bool HasRateError => RateError.Length > 0;
        public bool HasDurationError => DurationError.Length > 0;
        public bool HasFrequencyError => FrequencyError.Length > 0;
        public bool HasDatesError => DatesError.Length > 0;
        public bool HasTypeError => TypeError.Length > 0;
        public bool HasDeferralError => DeferralError.Length > 0;
        public bool HasInsuranceError => InsuranceError.Length > 0;
        public bool HasFeesError => FeesError.Length > 0;
        public bool HasNotesError => NotesError.Length > 0;
        public bool HasGeneralError => GeneralError.Length > 0;

        public async Task InitAsync(int loanId)
        {
            if (loanId <= 0)
                return;

            IsBusy = true;
            try
            {
                Load(await _loanService.GetAsync(loanId));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Remplit le formulaire depuis la fiche ; la saisie de départ sert à n'envoyer que les champs changés.</summary>
        public void Load(Loan loan)
        {
            var culture = CultureInfo.CurrentCulture;
            _suspendPreview = true;
            LoanId = loan.id;
            Label = loan.label;
            Lender = loan.lender;
            AmountText = CentsInput(loan.amount, culture);
            RateText = LoanRules.PercentInput(loan.ratePpm, culture);
            DurationText = loan.durationMonths.ToString(CultureInfo.InvariantCulture);
            SelectedFrequency = Frequencies.FirstOrDefault(choice => choice.Code == loan.frequencyMonths.ToString(CultureInfo.InvariantCulture)) ?? Frequencies[0];
            SelectedType = Types.FirstOrDefault(choice => choice.Code == loan.type) ?? Types[0];
            DisbursedOn = TreasuryDates.Parse(loan.disbursedOn) ?? DateTime.Today;
            FirstDueOn = TreasuryDates.Parse(loan.firstDueOn) ?? DisbursedOn.AddMonths(1);
            HasDeferral = loan.deferralMonths > 0;
            DeferralMonthsText = loan.deferralMonths > 0 ? loan.deferralMonths.ToString(CultureInfo.InvariantCulture) : null;
            SelectedDeferralKind = DeferralKinds.FirstOrDefault(choice => choice.Code == loan.deferralKind) ?? DeferralKinds[0];
            SelectedInsuranceMode = InsuranceModes.FirstOrDefault(choice => choice.Code == (loan.insuranceMode ?? "none")) ?? InsuranceModes[0];
            InsuranceAmountText = loan.insuranceAmount is { } insurance ? CentsInput(insurance, culture) : null;
            InsuranceRateText = loan.insuranceRatePpm is { } insuranceRate ? LoanRules.PercentInput(insuranceRate, culture) : null;
            FeesText = loan.fees > 0 ? CentsInput(loan.fees, culture) : null;
            Notes = loan.notes;
            _initialBody = BuildBody(forPreview: false);
            _suspendPreview = false;
            OnPropertyChanged(nameof(IsNew));
            OnPropertyChanged(nameof(PageTitle));
            SchedulePreview();
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName != null && PreviewInputs.Contains(e.PropertyName))
                SchedulePreview();
        }

        /// <summary>Relance l'aperçu après le délai ; la demande précédente (attente ou appel) est annulée.</summary>
        public void SchedulePreview()
        {
            if (_suspendPreview)
                return;
            _previewCts?.Cancel();
            var cts = new CancellationTokenSource();
            _previewCts = cts;
            var version = ++_previewVersion;
            PendingPreview = RunPreviewAsync(version, cts.Token);
        }

        private async Task RunPreviewAsync(int version, CancellationToken token)
        {
            try
            {
                if (PreviewDelay > TimeSpan.Zero)
                    await Task.Delay(PreviewDelay, token);
                token.ThrowIfCancellationRequested();

                var missing = MissingFields();
                if (missing.Count > 0)
                {
                    ClearPreview(MissingText(missing));
                    return;
                }

                IsPreviewLoading = true;
                var preview = await _loanService.PreviewAsync(BuildBody(forPreview: true), token);
                if (token.IsCancellationRequested || !LoanRules.IsLatestPreview(version, _previewVersion))
                    return;
                SetErrors(new Dictionary<string, string>());
                ApplyPreview(preview);
            }
            catch (OperationCanceledException)
            {
                // Une saisie plus récente a pris le relais.
            }
            catch (Exception ex) when (!token.IsCancellationRequested && LoanRules.IsLatestPreview(version, _previewVersion))
            {
                Debug.WriteLine(ex);
                var errors = LoanRules.FieldErrors(ex);
                if (errors.Count > 0)
                {
                    // Refus 422 de l'aperçu : les erreurs sous les champs, sans boîte de dialogue.
                    SetErrors(errors);
                    ClearPreview("Corrigez les champs signalés pour voir l'aperçu.");
                }
                else
                {
                    ClearPreview("Aperçu indisponible pour le moment.");
                }
            }
            catch (Exception ex)
            {
                // Réponse d'une demande dépassée : ignorée.
                Debug.WriteLine(ex);
            }
            finally
            {
                if (LoanRules.IsLatestPreview(version, _previewVersion))
                    IsPreviewLoading = false;
            }
        }

        private void ApplyPreview(LoanPreview preview)
        {
            var currency = preview.loan.currency;
            string Money(long cents) => SiteCurrency.FormatCents(cents, null, currency);
            var frequency = preview.loan.frequencyMonths > 0 ? preview.loan.frequencyMonths : SelectedFrequencyMonths;
            var type = preview.loan.type ?? SelectedType?.Code;
            var instalment = preview.RegularInstalment();

            PreviewInstalmentText = instalment == null
                ? string.Empty
                : type == "constant_capital"
                    ? $"Première échéance : {Money(instalment.total)} {LoanRules.PerFrequency(frequency)}, puis en baisse"
                    : $"Échéance : {Money(instalment.total)} {LoanRules.PerFrequency(frequency)}";
            PreviewCreditCostText = Money(preview.totals.creditCost);
            var detail = new List<string> { $"intérêts {Money(preview.totals.interest)}" };
            if (preview.totals.insurance > 0)
                detail.Add($"assurance {Money(preview.totals.insurance)}");
            if (preview.totals.fees > 0)
                detail.Add($"frais {Money(preview.totals.fees)}");
            PreviewCostDetailText = string.Join(" · ", detail);
            PreviewCapitalText = $"Capital emprunté, à part : {Money(preview.totals.capital)}";
            PreviewLines.Clear();
            foreach (var line in preview.FirstLines(3))
                PreviewLines.Add(line);
            PreviewMessage = string.Empty;
            HasPreview = true;
        }

        private void ClearPreview(string message)
        {
            HasPreview = false;
            PreviewLines.Clear();
            PreviewMessage = message;
        }

        [RelayCommand]
        private async Task Save()
        {
            if (IsBusy || IsReadOnly)
                return;

            var local = LocalErrors();
            SetErrors(local);
            if (local.Count > 0)
                return;

            // L'aperçu en cours ne doit pas réécrire les erreurs de l'enregistrement.
            _previewCts?.Cancel();
            _previewVersion++;

            var body = BuildBody(forPreview: false);
            IsBusy = true;
            try
            {
                if (IsNew)
                {
                    await _loanService.CreateAsync(body.Where(pair => pair.Value != null).ToDictionary(pair => pair.Key, pair => pair.Value));
                }
                else
                {
                    var changes = LoanRules.ChangedFields(_initialBody, body);
                    if (changes.Count > 0)
                        await _loanService.UpdateAsync(LoanId, changes);
                }
            }
            catch (Exception ex) when (LoanRules.FieldErrors(ex) is { Count: > 0 } errors)
            {
                SetErrors(errors);
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                if (LoanRules.IsReadOnlyRefusal(ex))
                {
                    IsReadOnly = true;
                    await _dialogService.ShowInfoAsync(LoanRules.ReadOnlyMessage, "Prêts");
                }
                else
                {
                    await _dialogService.ShowErrorAsync(ex);
                }
                return;
            }
            finally
            {
                IsBusy = false;
            }

            await GoBackAsync();
        }

        /// <summary>
        /// Corps de la fiche : toutes les clés, à null quand elles ne s'appliquent pas. L'aperçu n'envoie ni le prêteur ni les notes.
        /// </summary>
        public Dictionary<string, object?> BuildBody(bool forPreview)
        {
            var insuranceMode = SelectedInsuranceMode?.Code is { } mode && mode != "none" ? mode : null;
            var body = new Dictionary<string, object?>
            {
                ["label"] = Label?.Trim() ?? string.Empty,
                ["amount"] = Cents(AmountText),
                ["ratePpm"] = LoanRules.PercentToPpm(RateText),
                ["durationMonths"] = Integer(DurationText),
                ["frequencyMonths"] = SelectedFrequencyMonths,
                ["type"] = SelectedType?.Code ?? "annuity",
                ["disbursedOn"] = TreasuryDates.Iso(DisbursedOn),
                ["firstDueOn"] = TreasuryDates.Iso(FirstDueOn),
                ["deferralMonths"] = HasDeferral ? Integer(DeferralMonthsText) : 0,
                ["deferralKind"] = HasDeferral ? SelectedDeferralKind?.Code : null,
                ["insuranceMode"] = insuranceMode,
                ["insuranceAmount"] = insuranceMode == "fixed" ? Cents(InsuranceAmountText) : null,
                ["insuranceRatePpm"] = insuranceMode is "initial" or "remaining" ? LoanRules.PercentToPpm(InsuranceRateText) : null,
                ["fees"] = string.IsNullOrWhiteSpace(FeesText) ? 0L : Cents(FeesText),
            };
            if (!forPreview)
            {
                body["lender"] = string.IsNullOrWhiteSpace(Lender) ? null : Lender.Trim();
                body["notes"] = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();
            }
            return body;
        }

        private int SelectedFrequencyMonths =>
            int.TryParse(SelectedFrequency?.Code, NumberStyles.Integer, CultureInfo.InvariantCulture, out var months) ? months : 1;

        /// <summary>Champs à saisir avant de pouvoir calculer l'aperçu.</summary>
        private List<string> MissingFields()
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(Label))
                missing.Add("le nom");
            if (Cents(AmountText) is not > 0)
                missing.Add("le montant");
            if (LoanRules.PercentToPpm(RateText) == null)
                missing.Add("le taux");
            if (Integer(DurationText) is not > 0)
                missing.Add("la durée");
            return missing;
        }

        private static string MissingText(IReadOnlyList<string> missing) =>
            $"Saisissez {string.Join(", ", missing.Take(missing.Count - 1))}{(missing.Count > 1 ? " et " : string.Empty)}{missing[^1]} pour voir l'aperçu.";

        private Dictionary<string, string> LocalErrors()
        {
            var errors = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(Label) || Label.Trim().Length > LoanRules.MaxLabel)
                errors["label"] = LoanRules.ErrorText("LOAN_LABEL_INVALID");
            if (Lender?.Trim().Length > LoanRules.MaxLender)
                errors["lender"] = $"Le nom du prêteur fait {LoanRules.MaxLender} caractères au plus.";
            if (Cents(AmountText) is not > 0)
                errors["amount"] = LoanRules.ErrorText("LOAN_AMOUNT_INVALID");
            if (LoanRules.PercentToPpm(RateText) is not { } rate || rate > LoanRules.MaxRatePpm)
                errors["rate"] = LoanRules.ErrorText("LOAN_RATE_INVALID");
            if (Integer(DurationText) is not { } duration || duration <= 0 || duration > LoanRules.MaxDurationMonths)
                errors["duration"] = LoanRules.ErrorText("LOAN_DURATION_INVALID");
            if (FirstDueOn.Date <= DisbursedOn.Date)
                errors["dates"] = LoanRules.ErrorText("LOAN_DATE_INVALID");
            if (HasDeferral && Integer(DeferralMonthsText) is not > 0)
                errors["deferral"] = LoanRules.ErrorText("LOAN_DEFERRAL_INVALID");
            if (!string.IsNullOrWhiteSpace(FeesText) && Cents(FeesText) is not >= 0)
                errors["fees"] = LoanRules.ErrorText("LOAN_FEES_INVALID");
            if (Notes?.Trim().Length > LoanRules.MaxNotes)
                errors["notes"] = $"Les notes font {LoanRules.MaxNotes} caractères au plus.";
            return errors;
        }

        private void SetErrors(IReadOnlyDictionary<string, string> errors)
        {
            _errors.Clear();
            foreach (var (group, text) in errors)
            {
                var key = ErrorGroups.Contains(group) ? group : "general";
                _errors[key] = _errors.TryGetValue(key, out var existing) && existing != text ? $"{existing}{Environment.NewLine}{text}" : text;
            }
            foreach (var group in ErrorGroups)
            {
                var name = char.ToUpperInvariant(group[0]) + group[1..];
                OnPropertyChanged($"{name}Error");
                OnPropertyChanged($"Has{name}Error");
            }
        }

        private string Error(string group) => _errors.TryGetValue(group, out var text) ? text : string.Empty;

        private static long? Cents(string? text) =>
            DecimalInputConverter.Parse(text) is { } value ? MoneyHelper.ToCents(value) : null;

        private static int? Integer(string? text) =>
            int.TryParse(text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

        private static string CentsInput(long cents, CultureInfo culture) => (cents / 100m).ToString("0.##", culture);
    }
}
