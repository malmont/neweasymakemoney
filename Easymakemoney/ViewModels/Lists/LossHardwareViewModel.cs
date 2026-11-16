using System.Net;
using Easymakemoney.Services.Api;
using Easymakemoney.Services.Dialog;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>
    /// Matériel de la Prévention des pertes, sur le téléphone : tableau de santé (une pastille par appareil, relu toutes les 5 s
    /// tant que l'écran est ouvert, SANS prolonger la session), liste des boîtiers et jumelage d'un boîtier par son QR code.
    /// Aucune clé n'est montrée ici : clé, nouvelle clé et révocation restent sur le web.
    /// </summary>
    public partial class LossHardwareViewModel : BaseViewModel
    {
        public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

        private readonly ILossPreventionService _service;
        private readonly IDialogService _dialogService;
        private readonly IBarcodeScanner _scanner;
        private CancellationTokenSource? _polling;

        public LossHardwareViewModel(ILossPreventionService service, IDialogService dialogService, IBarcodeScanner scanner)
        {
            _service = service;
            _dialogService = dialogService;
            _scanner = scanner;
        }

        /// <summary>Attente entre deux lectures du tableau de santé (remplacée dans les tests).</summary>
        public Func<TimeSpan, CancellationToken, Task> DelayAsync { get; set; } = Task.Delay;

        public ObservableCollection<LossHealthDevice> Devices { get; } = new();
        public ObservableCollection<LossBox> Boxes { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSummary))]
        private LossHealthSummary? _summary;

        public bool HasSummary => Summary != null;

        [ObservableProperty]
        private bool _hasBoxes;

        /// <summary>Lecture arrêtée : la session s'est éteinte pendant que l'écran restait ouvert.</summary>
        [ObservableProperty]
        private bool _isPollingPaused;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanPair))]
        private bool _isModuleInactive;

        public bool CanPair => !IsModuleInactive;

        // Jumelage
        [ObservableProperty]
        private string _pairCode = string.Empty;

        [ObservableProperty]
        private string _pairName = string.Empty;

        [ObservableProperty]
        private string _pairPlace = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsPairIdle))]
        private bool _isPairing;

        public bool IsPairIdle => !IsPairing;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPairMessage))]
        private string _pairMessage = string.Empty;

        public bool HasPairMessage => PairMessage.Length > 0;

        public const string PausedText = "Mise à jour arrêtée : votre session s'est fermée. Touchez « Reprendre » pour vous reconnecter.";

        /// <summary>Ouverture de l'écran : boîtiers et santé, puis lecture de la santé toutes les 5 s.</summary>
        public async Task StartAsync()
        {
            await LoadBoxesAsync();
            if (IsModuleInactive)
                return;
            await RefreshHealthAsync();
            if (IsPollingPaused)
                return;
            StartPolling();
        }

        public void Stop()
        {
            _polling?.Cancel();
            _polling?.Dispose();
            _polling = null;
        }

        private void StartPolling()
        {
            Stop();
            var cts = new CancellationTokenSource();
            _polling = cts;
            _ = PollAsync(cts.Token);
        }

        private async Task PollAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await DelayAsync(PollInterval, token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                if (token.IsCancellationRequested)
                    return;
                await RefreshHealthAsync();
                if (IsPollingPaused || IsModuleInactive)
                    return;
            }
        }

        /// <summary>Une lecture du tableau de santé, sans rafraîchir la session ; un 401 arrête la lecture.</summary>
        public async Task RefreshHealthAsync()
        {
            try
            {
                var health = await _service.GetHealthAsync();
                Devices.Clear();
                foreach (var device in health.devices)
                    Devices.Add(device);
                Summary = health.summary;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                IsPollingPaused = true;
                Stop();
            }
            catch (ApiException ex) when (ex.ApiError.Code == ModuleInactiveInfo.Code)
            {
                IsModuleInactive = true;
                Stop();
            }
            catch (Exception ex)
            {
                // Réseau coupé ou erreur passagère : la lecture suivante réessaie, sans fenêtre d'erreur toutes les 5 s.
                Debug.WriteLine(ex);
            }
        }

        private async Task LoadBoxesAsync()
        {
            try
            {
                var list = await _service.GetBoxesAsync();
                Boxes.Clear();
                foreach (var box in list.items)
                    Boxes.Add(box);
                HasBoxes = Boxes.Count > 0;
                IsModuleInactive = false;
            }
            catch (ApiException ex) when (ex.ApiError.Code == ModuleInactiveInfo.Code)
            {
                IsModuleInactive = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
            }
        }

        /// <summary>« Reprendre » : une lecture ordinaire (qui reconnecte si besoin), puis la lecture de la santé repart.</summary>
        [RelayCommand]
        private async Task Resume()
        {
            IsPollingPaused = false;
            await StartAsync();
        }

        [RelayCommand]
        private async Task ScanCode()
        {
            try
            {
                var value = await _scanner.ScanQrAsync();
                if (value == null)
                    return;
                var code = Models.LossPairingCode.Normalize(value);
                if (code == null)
                {
                    PairMessage = "Ce QR code n'est pas celui d'un boîtier. Tapez le code imprimé sous le QR code.";
                    return;
                }
                PairCode = code;
                PairMessage = string.Empty;
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync(ex);
            }
        }

        [RelayCommand]
        private async Task Pair()
        {
            if (IsPairing)
                return;
            var code = Models.LossPairingCode.Normalize(PairCode);
            if (code == null)
            {
                PairMessage = "Le code a 9 caractères, par exemple KX7-42P-9QM.";
                return;
            }
            if (string.IsNullOrWhiteSpace(PairName))
            {
                PairMessage = "Donnez un nom au boîtier, par exemple « Boîtier de la réserve ».";
                return;
            }

            IsPairing = true;
            try
            {
                var result = await _service.PairBoxAsync(code, PairName.Trim(), PairPlace);
                PairCode = string.Empty;
                PairName = string.Empty;
                PairPlace = string.Empty;
                PairMessage = result.box is { } box
                    ? $"« {box.name} » est ajouté : {box.statusLabel}. Sa pastille passera au vert dès qu'il donnera signe de vie."
                    : "Le boîtier est ajouté.";
                await LoadBoxesAsync();
                await RefreshHealthAsync();
            }
            catch (ApiException ex) when (ex.ApiError.Code == "RATE_LIMITED")
            {
                var wait = ex.ApiError.RetryAfterSeconds is > 0 ? $" Réessayez dans {ex.ApiError.RetryAfterSeconds} s." : " Réessayez dans un moment.";
                PairMessage = "Trop d'essais." + wait;
            }
            catch (ApiException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity)
            {
                // Refus neutre du serveur (code inconnu, limite de boîtiers, champ refusé) : son texte tel quel.
                PairMessage = ex.ApiError.Message;
                if (ex.StatusCode == HttpStatusCode.NotFound)
                    await LoadBoxesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
            }
            finally
            {
                IsPairing = false;
            }
        }
    }
}
