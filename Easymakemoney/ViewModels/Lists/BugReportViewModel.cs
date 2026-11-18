using System.Net;
using Easymakemoney.Services.Api;
using Easymakemoney.Services.Dialog;
using Easymakemoney.Services.Incidents;
using Easymakemoney.ViewModels.Statistiques;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>
    /// « Signaler un problème » (lot INC-4) : une description, l'endroit du problème et une capture facultative, choisie par la
    /// personne, jamais prise d'office. Une case confirme que la capture ne montre aucune donnée de client. Dessous, « Mes signalements ».
    /// </summary>
    public partial class BugReportViewModel : BaseViewModel
    {
        public const string NoCustomerDataText = "N'écrivez pas de données de client.";
        public const string ScreenshotCheckText = "Vérifiez que la capture ne montre aucune donnée de client (nom, panier, numéro de carte).";

        private readonly IBugReportService _service;
        private readonly IDialogService _dialogService;
        private readonly IIncidentEnvironment _environment;
        private byte[]? _screenshotBytes;
        private string? _screenshotName;
        private string? _screenshotType;

        public BugReportViewModel(IBugReportService service, IDialogService dialogService, IIncidentEnvironment environment)
        {
            _service = service;
            _dialogService = dialogService;
            _environment = environment;
            Sections = new ObservableCollection<StatsChoice>(Models.BugReportRules.Sections
                .Select((section, index) => new StatsChoice(section.Code, section.Label) { IsSelected = index == 0 }));
        }

        /// <summary>Choix d'une image (remplacé dans les tests) : nom du fichier et contenu, ou null si la personne renonce.</summary>
        public Func<Task<(string FileName, byte[] Bytes)?>> PickImageAsync { get; set; } = PickFromGalleryAsync;

        public ObservableCollection<StatsChoice> Sections { get; }
        public ObservableCollection<Models.BugReport> Reports { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CountText), nameof(CanSend))]
        private string _description = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanSend))]
        private bool _noCustomerDataChecked;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasScreenshot), nameof(HasNoScreenshot), nameof(CanSend))]
        private ImageSource? _screenshotPreview;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasDescriptionError))]
        private string _descriptionError = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasScreenshotError))]
        private string _screenshotError = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasResultMessage))]
        private string _resultMessage = string.Empty;

        [ObservableProperty]
        private bool _hasReports;

        public bool HasScreenshot => _screenshotBytes != null;
        public bool HasNoScreenshot => !HasScreenshot;
        public bool HasDescriptionError => DescriptionError.Length > 0;
        public bool HasScreenshotError => ScreenshotError.Length > 0;
        public bool HasResultMessage => ResultMessage.Length > 0;
        public string CountText => $"{Description.Trim().Length} / {Models.BugReportRules.MaxDescription}";
        public string Screen => Sections.FirstOrDefault(section => section.IsSelected)?.Code ?? "Autre";

        public bool CanSend
        {
            get
            {
                var length = Description.Trim().Length;
                return !IsBusy && length >= Models.BugReportRules.MinDescription && length <= Models.BugReportRules.MaxDescription
                       && (!HasScreenshot || NoCustomerDataChecked);
            }
        }

        public async Task LoadAsync()
        {
            try
            {
                var list = await _service.GetMineAsync();
                Reports.Clear();
                foreach (var report in list.reports)
                    Reports.Add(report);
                HasReports = Reports.Count > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await ShowErrorAsync(ex);
            }
        }

        [RelayCommand]
        private void SelectSection(string code)
        {
            foreach (var section in Sections)
                section.IsSelected = section.Code == code;
            OnPropertyChanged(nameof(Screen));
        }

        [RelayCommand]
        private async Task PickScreenshot()
        {
            ScreenshotError = string.Empty;
            try
            {
                if (await PickImageAsync() is not { } picked)
                    return;
                var type = Models.BugReportRules.ContentTypeOf(picked.FileName);
                if (type == null)
                {
                    ScreenshotError = "Choisissez une image PNG, JPEG ou WebP.";
                    return;
                }
                if (picked.Bytes.LongLength > Models.BugReportRules.MaxScreenshotBytes)
                {
                    ScreenshotError = "L'image dépasse 5 Mo : choisissez une capture plus légère.";
                    return;
                }
                _screenshotBytes = picked.Bytes;
                _screenshotName = Path.GetFileName(picked.FileName);
                _screenshotType = type;
                NoCustomerDataChecked = false;
                var bytes = picked.Bytes;
                ScreenshotPreview = ImageSource.FromStream(() => new MemoryStream(bytes));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await ShowErrorAsync(ex);
            }
        }

        [RelayCommand]
        private void RemoveScreenshot() => ForgetScreenshot();

        [RelayCommand]
        private async Task Send()
        {
            if (!CanSend)
                return;
            IsBusy = true;
            OnPropertyChanged(nameof(CanSend));
            DescriptionError = string.Empty;
            ScreenshotError = string.Empty;
            ResultMessage = string.Empty;
            try
            {
                var screenshot = _screenshotBytes == null
                    ? null
                    : new UploadFile(new MemoryStream(_screenshotBytes), _screenshotName ?? "capture.png", _screenshotType ?? "image/png");
                var result = await _service.SendAsync(Description.Trim(), Screen, ContextJson(), screenshot);
                var id = result.report?.id;
                ResultMessage = id is > 0 ? $"Merci, votre signalement n° {id} est transmis." : "Merci, votre signalement est transmis.";
                Description = string.Empty;
                ForgetScreenshot();
                await LoadAsync();
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.UnprocessableEntity)
            {
                var fields = ex.ApiError.Body?["fields"];
                if (fields?["description"] != null)
                    DescriptionError = $"La description doit faire de {Models.BugReportRules.MinDescription} à {Models.BugReportRules.MaxDescription} caractères.";
                if (fields?["screenshot"] != null)
                    ScreenshotError = "Cette image n'est pas acceptée : PNG, JPEG ou WebP de 5 Mo au plus.";
                if (!HasDescriptionError && !HasScreenshotError)
                    ResultMessage = ex.ApiError.Message;
            }
            catch (ApiException ex) when (ex.ApiError.Code == "BUG_REPORT_LIMIT")
            {
                ResultMessage = "Trop de signalements aujourd'hui. Réessayez demain.";
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                ResultMessage = "Ce compte ne peut pas envoyer de signalement.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await ShowErrorAsync(ex);
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(CanSend));
            }
        }

        /// <summary>Contexte technique, liste blanche d'INC-2 : système, modèle de l'appareil, langue, connexion. Rien de personnel.</summary>
        public string ContextJson()
        {
            var context = new Dictionary<string, object>();
            void Put(string key, string? value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    context[key] = value;
            }
            Put("os", _environment.Os);
            Put("osVersion", _environment.OsVersion);
            Put("device", _environment.Device);
            Put("locale", _environment.Locale);
            context["online"] = _environment.IsOnline;
            return System.Text.Json.JsonSerializer.Serialize(context);
        }

        private void ForgetScreenshot()
        {
            _screenshotBytes = null;
            _screenshotName = null;
            _screenshotType = null;
            NoCustomerDataChecked = false;
            ScreenshotPreview = null;
            OnPropertyChanged(nameof(HasScreenshot));
            OnPropertyChanged(nameof(HasNoScreenshot));
            OnPropertyChanged(nameof(CanSend));
        }

        private static async Task<(string FileName, byte[] Bytes)?> PickFromGalleryAsync()
        {
            var photo = await MediaPicker.Default.PickPhotoAsync();
            if (photo == null)
                return null;
            await using var stream = await photo.OpenReadAsync();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory);
            return (photo.FileName, memory.ToArray());
        }

        private Task ShowErrorAsync(Exception ex) => _dialogService.ShowErrorAsync(ex);
    }
}
