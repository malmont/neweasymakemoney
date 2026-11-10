using Easymakemoney.Converters;
using Easymakemoney.Services.Api;
using Easymakemoney.Services.Dialog;
using Easymakemoney.Utilities;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>
    /// Simulateur d'une période : le gérant bouge le prix, une promotion ou la publicité et voit l'effet aussitôt.
    /// Rien n'est enregistré, sauf s'il ajoute la publicité simulée à son budget.
    /// </summary>
    public partial class PeriodSimulationViewModel : BaseViewModel
    {
        private readonly ISimulationService _simulationService;
        private readonly IPeriodBudgetService _budgetService;
        private readonly IDialogService _dialogService;
        private CancellationTokenSource? _pending;
        private int _requestVersion;

        public PeriodSimulationViewModel(ISimulationService simulationService, IPeriodBudgetService budgetService, IDialogService dialogService)
        {
            _simulationService = simulationService;
            _budgetService = budgetService;
            _dialogService = dialogService;
        }

        public int PeriodId { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSimulation))]
        private PeriodSimulation? _simulation;

        public bool HasSimulation => Simulation != null;

        public ObservableCollection<SimulationRow> Rows { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PriceText))]
        private double _pricePercent;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PromotionText))]
        private double _promotionPercent;

        [ObservableProperty]
        private string? _advertisingText;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string? _errorText;

        public bool HasError => !string.IsNullOrEmpty(ErrorText);

        [ObservableProperty]
        private bool _canSaveAdvertising;

        public string PriceText => PricePercent switch
        {
            0 => "Prix de vente inchangés",
            > 0 => $"Prix de vente : +{PricePercent:0} %",
            _ => $"Prix de vente : −{Math.Abs(PricePercent):0} %",
        };

        public string PromotionText => PromotionPercent == 0
            ? "Aucune promotion"
            : $"Promotion de {PromotionPercent:0} % sur le stock restant";

        public async Task StartAsync()
        {
            if (Simulation == null)
                await RunAsync();
        }

        partial void OnPricePercentChanged(double value)
        {
            var rounded = Math.Round(value);
            if (rounded != value)
            {
                PricePercent = rounded;
                return;
            }
            Schedule();
        }

        partial void OnPromotionPercentChanged(double value)
        {
            var rounded = Math.Round(value);
            if (rounded != value)
            {
                PromotionPercent = rounded;
                return;
            }
            Schedule();
        }

        partial void OnAdvertisingTextChanged(string? value) => Schedule();

        [RelayCommand]
        private void Reset()
        {
            AdvertisingText = null;
            PromotionPercent = 0;
            PricePercent = 0;
            Schedule();
        }

        /// <summary>Attend que le gérant ait fini de bouger un curseur avant de relancer le calcul.</summary>
        private void Schedule()
        {
            _pending?.Cancel();
            var pending = _pending = new CancellationTokenSource();
            _ = Task.Delay(450, pending.Token).ContinueWith(
                _ => MainThread.BeginInvokeOnMainThread(async () => await RunAsync()),
                pending.Token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
        }

        private long AdvertisingDeltaCents() =>
            DecimalInputConverter.Parse(AdvertisingText) is { } amount && amount > 0 ? MoneyHelper.ToCents(amount) : 0;

        private async Task RunAsync()
        {
            if (PeriodId <= 0)
                return;

            var advertising = AdvertisingDeltaCents();
            var levers = new SimulationLevers
            {
                pricePercent = PricePercent == 0 ? null : PricePercent,
                promotion = PromotionPercent == 0 ? null : new SimulationPromotion { percent = PromotionPercent, scope = "all" },
                advertisingDelta = advertising == 0 ? null : advertising,
            };

            var version = ++_requestVersion;
            IsBusy = true;
            try
            {
                var simulation = await _simulationService.SimulateAsync(PeriodId, levers);
                if (version != _requestVersion)
                    return; // une réponse plus récente arrive

                Simulation = simulation;
                ErrorText = null;
                Rows.Clear();
                foreach (var row in simulation.Rows())
                    Rows.Add(row);
                CanSaveAdvertising = advertising > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                if (version == _requestVersion)
                    ErrorText = ex is ApiException api ? api.Message : "Simulation impossible pour le moment.";
            }
            finally
            {
                if (version == _requestVersion)
                    IsBusy = false;
            }
        }

        /// <summary>Ajoute la publicité simulée au prévu « Publicité » de la période (seule écriture de cet écran, après confirmation).</summary>
        [RelayCommand]
        private async Task SaveAdvertising()
        {
            var advertising = AdvertisingDeltaCents();
            var plannedAdvertising = Simulation?.plannedToSave?.lines.FirstOrDefault(line => line.category == "advertising")?.planned ?? 0;
            if (advertising <= 0 || PeriodId <= 0)
                return;

            var newPlanned = plannedAdvertising + advertising;
            var confirmed = await _dialogService.ConfirmAsync(
                "Ajouter au budget",
                $"Le prévu « Publicité » de la période passera à {Simulation!.Money(newPlanned)}.",
                "Ajouter", "Annuler");
            if (!confirmed)
                return;

            try
            {
                await _budgetService.SavePlannedAsync(PeriodId, "advertising", newPlanned);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
                return;
            }

            AdvertisingText = null;
            await _dialogService.ShowInfoAsync("Budget publicité mis à jour.");
        }
    }
}
