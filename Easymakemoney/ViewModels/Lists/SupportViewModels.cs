using System.Net;
using Easymakemoney.Services.Api;
using Easymakemoney.Services.Dialog;
using Easymakemoney.Services.Session;
using Easymakemoney.ViewModels.Statistiques;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>Choix dans une liste (feuille d'actions) ; remplaçable dans les tests.</summary>
    public delegate Task<string?> SupportChooser(string title, string[] choices);

    /// <summary>Module Assistance : la boîte des demandes, filtrée par état et par personne qui la suit.</summary>
    public partial class SupportTicketListViewModel : BaseViewModel
    {
        private readonly ISupportService _service;
        private readonly IDialogService _dialogService;
        private int _page = 1;
        private bool _loaded;

        public SupportTicketListViewModel(ISupportService service, IDialogService dialogService)
        {
            _service = service;
            _dialogService = dialogService;
            Filters = new ObservableCollection<StatsChoice>(new[]
            {
                new StatsChoice("", "Toutes") { IsSelected = true },
                new StatsChoice("new", "Nouvelles"),
                new StatsChoice("open", "En cours"),
                new StatsChoice("waiting_customer", "Attend le client"),
                new StatsChoice("closed", "Fermées"),
            });
        }

        /// <summary>Vrai après une création ou une modification : la liste se relit au retour.</summary>
        public static bool TicketsChanged { get; set; }

        public Func<string, Task> NavigateAsync { get; set; } = route => Shell.Current?.GoToAsync(route) ?? Task.CompletedTask;

        public ObservableCollection<StatsChoice> Filters { get; }
        public ObservableCollection<SupportTicket> Tickets { get; } = new();

        [ObservableProperty]
        private bool _onlyMine;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _hasMore;

        [ObservableProperty]
        private bool _isEmpty;

        /// <summary>Module Assistance non accordé au site (403 MODULE_INACTIVE, module « support »).</summary>
        [ObservableProperty]
        private bool _isModuleInactive;

        [ObservableProperty]
        private string _totalText = string.Empty;

        public string? Status => Filters.FirstOrDefault(filter => filter.IsSelected)?.Code is { Length: > 0 } code ? code : null;

        partial void OnOnlyMineChanged(bool value) => _ = ReloadAsync();

        public async Task InitializeAsync()
        {
            if (_loaded && !TicketsChanged)
                return;
            TicketsChanged = false;
            await ReloadAsync();
            _loaded = true;
        }

        public Task ReloadAsync()
        {
            _page = 1;
            return LoadAsync(append: false);
        }

        private async Task LoadAsync(bool append)
        {
            if (IsBusy)
                return;
            IsBusy = true;
            try
            {
                var list = await _service.GetTicketsAsync(Status, OnlyMine ? "me" : null, SearchText, _page);
                if (!append)
                    Tickets.Clear();
                foreach (var ticket in list.items)
                    Tickets.Add(ticket);
                HasMore = list.HasMore;
                IsEmpty = Tickets.Count == 0;
                IsModuleInactive = false;
                TotalText = list.total == 0 ? string.Empty : $"{list.total} demande{(list.total > 1 ? "s" : "")}";
            }
            catch (ApiException ex) when (ex.ApiError.Code == ModuleInactiveInfo.Code)
            {
                Tickets.Clear();
                IsModuleInactive = true;
                IsEmpty = false;
                HasMore = false;
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
        private async Task SelectFilter(string code)
        {
            foreach (var filter in Filters)
                filter.IsSelected = filter.Code == code;
            await ReloadAsync();
        }

        [RelayCommand]
        private Task Search() => ReloadAsync();

        [RelayCommand]
        private async Task LoadMore()
        {
            if (!HasMore || IsBusy)
                return;
            _page++;
            await LoadAsync(append: true);
        }

        [RelayCommand]
        private Task Open(SupportTicket? ticket) =>
            ticket == null ? Task.CompletedTask : NavigateAsync($"SupportTicketPage?TicketId={ticket.id}");

        [RelayCommand]
        private Task Create() => NavigateAsync("SupportTicketCreatePage");
    }

    /// <summary>Fiche d'une demande : fil des messages, réponse au client, note interne, état, priorité et attribution.</summary>
    public partial class SupportTicketViewModel : BaseViewModel
    {
        public const string ModeReply = "reply";
        public const string ModeNote = "note";
        public const string ModeCustomer = "customer";

        private readonly ISupportService _service;
        private readonly IDialogService _dialogService;
        private readonly ISessionService? _session;

        public SupportTicketViewModel(ISupportService service, IDialogService dialogService, ISessionService? session = null)
        {
            _service = service;
            _dialogService = dialogService;
            _session = session;
            ChooseAsync = (title, choices) => _dialogService.ChooseAsync(title, choices);
            Modes = new ObservableCollection<StatsChoice>(new[]
            {
                new StatsChoice(ModeReply, "Répondre au client") { IsSelected = true },
                new StatsChoice(ModeNote, "Note interne"),
                new StatsChoice(ModeCustomer, "Message reçu du client"),
            });
        }

        public SupportChooser ChooseAsync { get; set; }

        public int TicketId { get; set; }

        public ObservableCollection<StatsChoice> Modes { get; }
        public ObservableCollection<SupportMessage> Messages { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasTicket), nameof(CanReply))]
        private SupportTicketDetail? _ticket;

        [ObservableProperty]
        private string _replyText = string.Empty;

        public bool HasTicket => Ticket != null;
        public bool CanReply => Ticket is { IsClosed: false, anonymized: false };
        public string Mode => Modes.FirstOrDefault(mode => mode.IsSelected)?.Code ?? ModeReply;

        public string SendText => Mode switch
        {
            ModeNote => "Ajouter la note",
            ModeCustomer => "Consigner le message du client",
            _ => "Envoyer au client",
        };

        public string ReplyHint => Mode switch
        {
            ModeNote => "Visible seulement par l'équipe, jamais envoyée au client.",
            ModeCustomer => "Ce que le client a dit (appel, comptoir) : le délai de réponse repart.",
            _ => "Envoyée au client par courriel, dans sa langue. La demande passe en « Attend le client ».",
        };

        public async Task LoadAsync()
        {
            if (TicketId <= 0 || IsBusy)
                return;
            IsBusy = true;
            try
            {
                var ticket = await _service.GetTicketAsync(TicketId);
                Messages.Clear();
                foreach (var message in ticket.messages)
                    Messages.Add(message);
                Ticket = ticket;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await ShowErrorAsync(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void SelectMode(string code)
        {
            foreach (var mode in Modes)
                mode.IsSelected = mode.Code == code;
            OnPropertyChanged(nameof(Mode));
            OnPropertyChanged(nameof(SendText));
            OnPropertyChanged(nameof(ReplyHint));
        }

        [RelayCommand]
        private async Task Send()
        {
            if (!CanReply || IsBusy)
                return;
            var text = ReplyText?.Trim() ?? string.Empty;
            if (text.Length == 0)
            {
                await _dialogService.ShowInfoAsync("Écrivez d'abord le message.");
                return;
            }

            var mode = Mode;
            if (mode == ModeReply && !await _dialogService.ConfirmAsync(
                    "Répondre au client",
                    $"Envoyer cette réponse à {Ticket!.CustomerText} par courriel ?",
                    "Envoyer", "Annuler"))
                return;

            IsBusy = true;
            try
            {
                await _service.AddMessageAsync(TicketId, text, isInternal: mode == ModeNote, fromCustomer: mode == ModeCustomer);
                ReplyText = string.Empty;
                SupportTicketListViewModel.TicketsChanged = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await ShowErrorAsync(ex);
            }
            finally
            {
                IsBusy = false;
            }
            await LoadAsync();
        }

        [RelayCommand]
        private async Task ChangeStatus()
        {
            if (Ticket == null || IsBusy)
                return;
            var next = SupportTexts.NextStatuses(Ticket.status);
            if (next.Count == 0)
                return;
            var labels = next.Select(SupportTexts.Status).ToArray();
            var choice = await ChooseAsync($"État de {Ticket.reference}", labels);
            var index = Array.IndexOf(labels, choice);
            if (index < 0)
                return;
            await PatchAsync(new Dictionary<string, object?> { ["status"] = next[index] });
        }

        [RelayCommand]
        private async Task ChangePriority()
        {
            if (Ticket == null || IsBusy)
                return;
            var options = SupportTexts.Priorities.Where(priority => priority != Ticket.priority).ToArray();
            var labels = options.Select(SupportTexts.Priority).ToArray();
            var choice = await ChooseAsync("Priorité", labels);
            var index = Array.IndexOf(labels, choice);
            if (index < 0)
                return;
            await PatchAsync(new Dictionary<string, object?> { ["priority"] = options[index] });
        }

        /// <summary>« M'attribuer » si la demande n'est pas à moi, sinon « Ne plus suivre ».</summary>
        [RelayCommand]
        private async Task ToggleAssign()
        {
            if (Ticket == null || IsBusy)
                return;
            var me = _session?.CurrentUser?.Id ?? 0;
            if (me <= 0)
                return;
            object? assignedTo = Ticket.assignedTo?.id == me ? null : me;
            await PatchAsync(new Dictionary<string, object?> { ["assignedTo"] = assignedTo });
        }

        public string AssignText => _session?.CurrentUser?.Id is { } me && me > 0 && Ticket?.assignedTo?.id == me ? "Ne plus suivre" : "M'attribuer";

        partial void OnTicketChanged(SupportTicketDetail? value) => OnPropertyChanged(nameof(AssignText));

        private async Task PatchAsync(Dictionary<string, object?> changes)
        {
            IsBusy = true;
            try
            {
                await _service.UpdateAsync(TicketId, changes);
                SupportTicketListViewModel.TicketsChanged = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await ShowErrorAsync(ex);
            }
            finally
            {
                IsBusy = false;
            }
            await LoadAsync();
        }

        /// <summary>Codes du contrat traduits en message simple ; le reste passe par la boîte d'erreur commune.</summary>
        private async Task ShowErrorAsync(Exception ex)
        {
            if (ex is ApiException api && SupportTexts.ErrorText(api.ApiError.Code) is { } text)
                await _dialogService.ShowInfoAsync(text);
            else
                await _dialogService.ShowErrorAsync(ex);
        }
    }

    /// <summary>Création d'une demande par l'équipe (appel, comptoir, courriel recopié).</summary>
    public partial class SupportTicketCreateViewModel : BaseViewModel
    {
        private readonly ISupportService _service;
        private readonly IDialogService _dialogService;

        public SupportTicketCreateViewModel(ISupportService service, IDialogService dialogService)
        {
            _service = service;
            _dialogService = dialogService;
            Channels = new ObservableCollection<StatsChoice>(new[]
            {
                new StatsChoice("phone", "Téléphone") { IsSelected = true },
                new StatsChoice("counter", "Comptoir"),
                new StatsChoice("email", "Courriel recopié"),
                new StatsChoice("agent", "Autre"),
            });
            Priorities = new ObservableCollection<StatsChoice>(SupportTexts.Priorities.Select(code =>
                new StatsChoice(code, SupportTexts.Priority(code)) { IsSelected = code == "normal" }));
        }

        public Func<Task> GoBackAsync { get; set; } = () => Shell.Current?.GoToAsync("..") ?? Task.CompletedTask;

        public ObservableCollection<StatsChoice> Channels { get; }
        public ObservableCollection<StatsChoice> Priorities { get; }

        [ObservableProperty] private string _subject = string.Empty;
        [ObservableProperty] private string _body = string.Empty;
        [ObservableProperty] private string _customerName = string.Empty;
        [ObservableProperty] private string _customerEmail = string.Empty;
        [ObservableProperty] private string _orderIdText = string.Empty;

        [RelayCommand]
        private void SelectChannel(string code)
        {
            foreach (var channel in Channels)
                channel.IsSelected = channel.Code == code;
        }

        [RelayCommand]
        private void SelectPriority(string code)
        {
            foreach (var priority in Priorities)
                priority.IsSelected = priority.Code == code;
        }

        /// <summary>Contrôles simples avant l'envoi ; le serveur reste juge (422 VALIDATION_FAILED).</summary>
        public SupportTicketCreateRequest? BuildRequest(out string? error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(Subject))
                error = "Indiquez l'objet de la demande.";
            else if (string.IsNullOrWhiteSpace(Body))
                error = "Décrivez la demande du client.";
            else if (string.IsNullOrWhiteSpace(CustomerName) && string.IsNullOrWhiteSpace(CustomerEmail))
                error = "Indiquez au moins le nom ou le courriel du client.";
            else if (!string.IsNullOrWhiteSpace(CustomerEmail) && !CustomerEmail.Contains('@'))
                error = "Le courriel du client ne semble pas valide.";
            else if (!string.IsNullOrWhiteSpace(OrderIdText) && (!int.TryParse(OrderIdText.Trim().TrimStart('#'), out var parsed) || parsed <= 0))
                error = "Le numéro de commande doit être un nombre.";
            if (error != null)
                return null;

            int? orderId = int.TryParse(OrderIdText?.Trim().TrimStart('#'), out var id) && id > 0 ? id : null;
            return new SupportTicketCreateRequest
            {
                Subject = Subject,
                Body = Body,
                Channel = Channels.First(channel => channel.IsSelected).Code,
                Priority = Priorities.First(priority => priority.IsSelected).Code,
                CustomerName = CustomerName,
                CustomerEmail = CustomerEmail,
                OrderId = orderId,
            };
        }

        [RelayCommand]
        private async Task Save()
        {
            if (IsBusy)
                return;
            if (BuildRequest(out var error) is not { } request)
            {
                await _dialogService.ShowInfoAsync(error!);
                return;
            }

            IsBusy = true;
            try
            {
                var ticket = await _service.CreateAsync(request);
                SupportTicketListViewModel.TicketsChanged = true;
                await _dialogService.ShowInfoAsync($"Demande {ticket.reference} créée.", "Assistance");
                await GoBackAsync();
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
    }
}
