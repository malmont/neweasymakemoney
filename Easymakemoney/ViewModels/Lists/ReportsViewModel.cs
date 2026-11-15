using Easymakemoney.Services.Api;
using Easymakemoney.Services.Dialog;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>
    /// « Mes rapports » (backend DOC-1 et DOC-2) : les rapports PDF du site, du plus récent au plus ancien ; demander un bilan de
    /// période ou d'année (avis de l'IA en option, 1 unité décomptée seulement s'il est rédigé), suivre sa fabrication, l'ouvrir, l'effacer.
    /// </summary>
    public partial class ReportsViewModel : BaseViewModel
    {
        /// <summary>Relecture toutes les 3 s pendant 2 minutes au plus, comme le web.</summary>
        private const int PollDelayMs = 3000;
        private const int MaxPolls = 40;

        private readonly IGestionReportService _service;
        private readonly IDialogService _dialogService;

        public ReportsViewModel(IGestionReportService service, IDialogService dialogService)
        {
            _service = service;
            _dialogService = dialogService;
        }

        public ObservableCollection<GestionReport> Reports { get; } = new();

        [ObservableProperty]
        private bool _isEmpty;

        /// <summary>Bilan demandé depuis l'écran d'une période ou de l'année, lancé une seule fois à l'ouverture.</summary>
        public (string Kind, int? Id)? PendingRequest { get; set; }

        /// <summary>Attente entre deux relectures ; les tests la remplacent.</summary>
        public Func<int, Task> Delay { get; set; } = ms => Task.Delay(ms);

        /// <summary>Ouverture du PDF (partage) ; les tests la remplacent.</summary>
        public Func<DownloadedFile, string, Task> OpenFile { get; set; } = OpenWithShareAsync;

        public async Task LoadAsync()
        {
            await RunAsync(RefreshAsync);
            if (PendingRequest is { } request)
            {
                PendingRequest = null;
                if (request.Kind == "period" && request.Id is { } periodId)
                    await RequestPeriodAsync(periodId);
                else if (request.Kind == "year")
                    await RequestYearAsync(request.Id);
            }
        }

        private async Task RefreshAsync()
        {
            var list = await _service.ListAsync(1);
            Reports.Clear();
            foreach (var report in list.reports)
                Reports.Add(report);
            IsEmpty = Reports.Count == 0;
        }

        public Task RequestPeriodAsync(int periodId) => RunAsync(async () =>
        {
            var withAdvice = await AskAdviceAsync();
            await FollowAsync(await _service.RequestPeriodAsync(periodId, withAdvice));
        });

        public Task RequestYearAsync(int? year) => RunAsync(async () =>
        {
            var withAdvice = await AskAdviceAsync();
            await FollowAsync(await _service.RequestYearAsync(year, withAdvice));
        });

        [RelayCommand]
        private Task RequestCurrentYear() => RequestYearAsync(null);

        [RelayCommand]
        private Task Open(GestionReport? report) => RunAsync(async () =>
        {
            if (report == null)
                return;
            if (report.IsPending)
            {
                await FollowAsync(report);
                return;
            }
            if (report.IsFailed)
            {
                await _dialogService.ShowInfoAsync(report.StateText, report.title ?? "Rapport");
                return;
            }
            await OpenFile(await _service.DownloadAsync(report.id), report.download?.filename ?? $"rapport-{report.id}.pdf");
        });

        [RelayCommand]
        private Task Delete(GestionReport? report) => RunAsync(async () =>
        {
            if (report == null)
                return;
            if (report.IsPending)
                throw new UserMessageException("Ce rapport est en préparation : il pourra être effacé quand il sera prêt.");
            if (!await _dialogService.ConfirmAsync("Effacer le rapport", $"Effacer « {report.title} » ? Il ne pourra plus être téléchargé.", "Effacer", "Annuler"))
                return;
            await _service.DeleteAsync(report.id);
            await RefreshAsync();
        });

        private Task<bool> AskAdviceAsync() => _dialogService.ConfirmAsync("Avis de l'IA",
            "Ajouter au bilan l'avis de l'IA ? Il coûte 1 unité, décomptée seulement s'il est rédigé. Sans avis, le bilan ne coûte rien.",
            "Avec l'avis", "Sans l'avis");

        /// <summary>Attend la fin de la fabrication, puis ouvre le PDF ; la liste est relue dans tous les cas.</summary>
        private async Task FollowAsync(GestionReport report)
        {
            var current = report;
            for (var i = 0; current.IsPending && i < MaxPolls; i++)
            {
                await Delay(PollDelayMs);
                current = await _service.GetAsync(current.id);
            }

            await RefreshAsync();
            if (current.IsDone)
                await OpenFile(await _service.DownloadAsync(current.id), current.download?.filename ?? $"rapport-{current.id}.pdf");
            else if (current.IsFailed)
                await _dialogService.ShowInfoAsync(current.StateText, current.title ?? "Rapport");
            else
                await _dialogService.ShowInfoAsync("Le rapport est encore en préparation. Il apparaîtra prêt dans « Mes rapports » d'ici quelques instants.");
        }

        private async Task RunAsync(Func<Task> action)
        {
            if (IsBusy)
                return;
            IsBusy = true;
            try
            {
                await action();
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

        private static async Task OpenWithShareAsync(DownloadedFile file, string fileName)
        {
            var safe = string.Concat(fileName.Split(Path.GetInvalidFileNameChars()));
            var path = Path.Combine(FileSystem.CacheDirectory, string.IsNullOrWhiteSpace(safe) ? "rapport.pdf" : safe);
            await File.WriteAllBytesAsync(path, file.Content);
            await Share.Default.RequestAsync(new ShareFileRequest { Title = "Rapport", File = new ShareFile(path, "application/pdf") });
        }
    }
}
