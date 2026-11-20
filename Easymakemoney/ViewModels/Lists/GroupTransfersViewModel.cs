using Easymakemoney.Services.Api;
using Easymakemoney.Services.Dialog;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>Un transfert à envoyer, avec ce que l'écran propose après un refus de l'envoi.</summary>
    public partial class GroupTransferItem : ObservableObject
    {
        public GroupTransferItem(GroupTransfer transfer, bool isPending = false)
        {
            Transfer = transfer;
            _isPending = isPending;
        }

        public GroupTransfer Transfer { get; }

        public int Id => Transfer.Id;

        public string TargetText => Transfer.TargetText;

        public string RequestedText => Transfer.RequestedText;

        public bool HasRequestedText => Transfer.HasRequestedText;

        public string StateText => Transfer.StateText;

        public List<GroupTransferLine> Lines => Transfer.Lines;

        /// <summary>Dernier refus de l'envoi (null : aucun).</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanSend), nameof(SendText), nameof(NoticeText), nameof(HasNotice), nameof(ShortLines), nameof(HasShortLines))]
        private TransferSendRefusal? _refusal;

        /// <summary>TRANSFER_SEND_PENDING : le serveur vérifie et reprendra l'envoi tout seul ; aucun bouton.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanSend), nameof(NoticeText), nameof(HasNotice))]
        private bool _isPending;

        public bool CanSend => !IsPending && Transfer.IsSendable;

        /// <summary>« Envoyer », ou l'action proposée par le refus (« Réessayer », « Renvoyer »).</summary>
        public string SendText => Refusal?.ActionText is { Length: > 0 } action
            ? action
            : Transfer.State == "send_failed" ? "Renvoyer" : "Envoyer";

        public string NoticeText => IsPending ? TransferSendRefusal.PendingNotice : Refusal?.Message ?? string.Empty;

        public bool HasNotice => NoticeText.Length > 0;

        public List<TransferShortLine> ShortLines => Refusal?.ShortLines ?? new List<TransferShortLine>();

        public bool HasShortLines => ShortLines.Count > 0;
    }

    /// <summary>
    /// Transferts à envoyer (lot backend G-5, boutique SOURCE) : le siège a demandé du stock pour une autre boutique du groupe ;
    /// l'administrateur l'envoie, et le stock sort de sa boutique à ce moment-là.
    /// </summary>
    public partial class GroupTransfersViewModel : BaseViewModel
    {
        public const string ConfirmMessage = "Le stock de ces articles sortira de votre boutique maintenant.";

        public const string SentMessage = "Transfert envoyé : le stock est sorti de votre boutique. La boutique de destination peut le réceptionner.";

        private readonly IGroupTransferService _transferService;
        private readonly IDialogService _dialogService;

        /// <summary>Transfert dont l'envoi est en vérification (TRANSFER_SEND_PENDING) : il ne se renvoie pas.</summary>
        private int? _pendingId;
        private CancellationTokenSource? _pollingCts;
        private bool _isVisible;

        public GroupTransfersViewModel(IGroupTransferService transferService, IDialogService dialogService)
        {
            _transferService = transferService;
            _dialogService = dialogService;
        }

        public ObservableCollection<GroupTransferItem> Items { get; } = new();

        [ObservableProperty]
        private bool _isEmpty;

        /// <summary>Attente entre deux relectures (point d'entrée des tests).</summary>
        public Func<TimeSpan, CancellationToken, Task> DelayAsync { get; set; } = Task.Delay;

        /// <summary>Horloge de la relecture bornée (point d'entrée des tests).</summary>
        public Func<DateTime> Now { get; set; } = () => DateTime.UtcNow;

        /// <summary>Relecture en cours après TRANSFER_SEND_PENDING (null : aucune).</summary>
        public Task? PollingTask { get; private set; }

        /// <summary>Nombre de relectures faites pendant la vérification d'un envoi.</summary>
        public int PollCount { get; private set; }

        public async Task AppearingAsync()
        {
            _isVisible = true;
            await LoadAsync();
            if (_pendingId != null)
                StartPolling();
        }

        /// <summary>L'écran n'est plus visible : la relecture s'arrête.</summary>
        public void Disappearing()
        {
            _isVisible = false;
            StopPolling();
        }

        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            try
            {
                Apply(await _transferService.GetTransfersToSendAsync());
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

        private void Apply(List<GroupTransfer> transfers)
        {
            if (_pendingId is int id && !TransferPendingPolling.IsStillPending(transfers.FirstOrDefault(t => t.Id == id)))
                _pendingId = null;

            Items.Clear();
            foreach (var transfer in transfers)
                Items.Add(new GroupTransferItem(transfer, transfer.Id == _pendingId));
            IsEmpty = Items.Count == 0;
        }

        [RelayCommand]
        private async Task Send(GroupTransferItem? item)
        {
            if (item == null || !item.CanSend || IsBusy)
                return;

            var confirmed = await _dialogService.ConfirmAsync("Envoyer le transfert", ConfirmMessage, item.SendText, "Annuler");
            if (!confirmed)
                return;

            TransferSendRefusal? refusal = null;
            IsBusy = true;
            try
            {
                await _transferService.SendAsync(item.Id);
            }
            catch (ApiException ex)
            {
                refusal = TransferSendRefusal.From(ex.ApiError, ex.StatusCode, item.Lines);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                IsBusy = false;
                await _dialogService.ShowErrorAsync(ex);
                return;
            }
            finally
            {
                IsBusy = false;
            }

            if (refusal == null)
            {
                await _dialogService.ShowInfoAsync(SentMessage);
                await LoadAsync();
                return;
            }

            switch (refusal.Action)
            {
                case TransferSendAction.Reload:
                    await _dialogService.ShowInfoAsync(refusal.Message);
                    await LoadAsync();
                    break;
                case TransferSendAction.Wait:
                    // Ne pas renvoyer : le serveur reprendra l'envoi. La liste est relue un moment.
                    _pendingId = item.Id;
                    item.Refusal = refusal;
                    item.IsPending = true;
                    StartPolling();
                    break;
                default:
                    item.Refusal = refusal;
                    break;
            }
        }

        private void StartPolling()
        {
            StopPolling();
            if (!_isVisible || _pendingId == null)
                return;

            var cts = new CancellationTokenSource();
            _pollingCts = cts;
            PollingTask = PollAsync(cts.Token);
        }

        private void StopPolling()
        {
            _pollingCts?.Cancel();
            _pollingCts = null;
        }

        /// <summary>Relecture toutes les 5 s, tant que l'écran est visible et l'envoi en vérification, 2 minutes au plus.</summary>
        private async Task PollAsync(CancellationToken token)
        {
            var startedAt = Now();
            var attempts = 0;
            try
            {
                while (TransferPendingPolling.ShouldPollAgain(attempts, Now() - startedAt, _isVisible && !token.IsCancellationRequested, _pendingId != null))
                {
                    await DelayAsync(TransferPendingPolling.Interval, token);
                    if (token.IsCancellationRequested)
                        return;

                    attempts++;
                    PollCount++;
                    if (IsBusy)
                        continue;
                    try
                    {
                        Apply(await _transferService.GetTransfersToSendAsync());
                    }
                    catch (Exception ex)
                    {
                        // Une relecture manquée n'interrompt pas la vérification : la suivante suit dans 5 s.
                        Debug.WriteLine($"[Transferts] Relecture impossible : {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Écran quitté.
            }
        }
    }
}
