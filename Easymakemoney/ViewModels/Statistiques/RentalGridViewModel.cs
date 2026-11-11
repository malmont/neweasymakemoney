using System.Globalization;
using Easymakemoney.Converters;
using Easymakemoney.Services.Api;
using Easymakemoney.Services.Dialog;

namespace Easymakemoney.ViewModels.Statistiques
{
    /// <summary>
    /// Grilles de location (lot backend LOC4) : changer les tarifs d'une grille en pourcentage, avec aperçu, accord explicite quand la grille sert
    /// à plusieurs articles, et annulation pendant 24 h. Pour un seul article, la promotion de location est proposée à la place.
    /// </summary>
    public partial class RentalGridViewModel : BaseViewModel
    {
        private readonly IRentalGridService _service;
        private readonly IDialogService _dialogService;
        private RentalGridRequest? _previewedRequest;
        private bool _loaded;

        public RentalGridViewModel(IRentalGridService service, IDialogService dialogService)
        {
            _service = service;
            _dialogService = dialogService;
        }

        public ObservableCollection<RentalGrid> Grids { get; } = new();
        public ObservableCollection<RentalGridModification> Modifications { get; } = new();
        public ObservableCollection<string> ChangeLines { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSelection), nameof(SelectionText))]
        private RentalGrid? _selectedGrid;

        public bool HasSelection => SelectedGrid != null;
        public string SelectionText => SelectedGrid is { } grid ? $"{grid.name} : {grid.RatesText}" : string.Empty;

        [ObservableProperty]
        private string? _percentText = "10";

        [ObservableProperty]
        private bool _roundToDollar = true;

        /// <summary>Baisse plutôt que hausse : le clavier numérique de l'iPhone n'a pas de signe moins.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsIncrease))]
        private bool _isDecrease;

        public bool IsIncrease => !IsDecrease;

        [RelayCommand]
        private void SetDirection(string direction) => IsDecrease = direction == "down";

        partial void OnIsDecreaseChanged(bool value) => ClearPreview();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPreview), nameof(CanApply))]
        private RentalGridPreview? _preview;

        public bool HasPreview => Preview != null;
        public bool CanApply => Preview is { HasChanges: true, previewHash: not null };

        [ObservableProperty]
        private bool _hasModifications;

        [ObservableProperty]
        private bool _isEmpty;

        public async Task InitializeAsync()
        {
            if (_loaded)
                return;
            _loaded = true;
            await LoadAsync();
        }

        public async Task LoadAsync()
        {
            IsBusy = true;
            try
            {
                var currency = SiteCurrency.CurrentCurrency;
                var grids = await _service.GetGridsAsync();
                Grids.Clear();
                foreach (var grid in grids)
                {
                    grid.Currency = currency;
                    Grids.Add(grid);
                }
                IsEmpty = Grids.Count == 0;
                await LoadModificationsAsync();
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

        private async Task LoadModificationsAsync()
        {
            var currency = SiteCurrency.CurrentCurrency;
            var modifications = await _service.GetModificationsAsync();
            Modifications.Clear();
            foreach (var modification in modifications)
            {
                modification.Currency = currency;
                Modifications.Add(modification);
            }
            HasModifications = Modifications.Count > 0;
        }

        [RelayCommand]
        private void SelectGrid(RentalGrid? grid)
        {
            SelectedGrid = grid;
            ClearPreview();
        }

        partial void OnPercentTextChanged(string? value) => ClearPreview();
        partial void OnRoundToDollarChanged(bool value) => ClearPreview();

        private void ClearPreview()
        {
            Preview = null;
            _previewedRequest = null;
            ChangeLines.Clear();
        }

        public RentalGridRequest? BuildRequest(out string? error)
        {
            error = null;
            if (SelectedGrid == null)
            {
                error = "Choisissez d'abord une grille.";
                return null;
            }
            var parsed = ParseSignedPercent(PercentText);
            double? percent = parsed is { } p && IsDecrease ? -Math.Abs(p) : parsed;
            if (percent is not { } value || value != Math.Round(value) || value == 0 || value < -50 || value > 200)
            {
                error = IsDecrease
                    ? "La baisse doit être un nombre entier entre 1 et 50 %."
                    : "La hausse doit être un nombre entier entre 1 et 200 %.";
                return null;
            }
            return new RentalGridRequest { RentalPackId = SelectedGrid.id, Percent = (int)value, RoundToDollar = RoundToDollar };
        }

        /// <summary>Pourcentage avec signe : « -10 », « −10 » (moins typographique) ou « +10 ». Le convertisseur commun refuse les signes (montants).</summary>
        public static double? ParseSignedPercent(string? text)
        {
            var trimmed = text?.Trim() ?? string.Empty;
            var negative = trimmed.StartsWith('-') || trimmed.StartsWith('−');
            if (negative || trimmed.StartsWith('+'))
                trimmed = trimmed[1..];
            return DecimalInputConverter.Parse(trimmed) is { } value ? (negative ? -value : value) : null;
        }

        [RelayCommand]
        private async Task ComputePreview()
        {
            if (IsBusy)
                return;
            var request = BuildRequest(out var error);
            if (request == null)
            {
                await _dialogService.ShowInfoAsync(error!);
                return;
            }

            IsBusy = true;
            try
            {
                var preview = await _service.PreviewAsync(request);
                preview.Currency = SiteCurrency.CurrentCurrency;
                Preview = preview;
                _previewedRequest = request;
                ChangeLines.Clear();
                foreach (var line in preview.ChangeLines())
                    ChangeLines.Add(line);
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
        private async Task Apply()
        {
            if (Preview is not { HasChanges: true, previewHash: { } hash } preview || _previewedRequest is not { } request || IsBusy)
                return;

            var message = preview.IsShared
                ? $"{preview.SharedText}\nLes réservations déjà faites gardent leur prix. Vous pourrez annuler pendant 24 h."
                : "Les nouveaux tarifs s'appliquent aux prochaines réservations, en boutique et en caisse. Les réservations déjà faites gardent leur prix. Vous pourrez annuler pendant 24 h.";
            if (!await _dialogService.ConfirmAsync("Changer la grille", message, "Changer", "Annuler"))
                return;

            IsBusy = true;
            try
            {
                await _service.ApplyAsync(request, hash, allowShared: preview.IsShared);
                await _dialogService.ShowInfoAsync("La grille est changée.", "Grille de location");
                ClearPreview();
                SelectedGrid = null;
                IsBusy = false;
                await LoadAsync();
            }
            catch (ApiException ex) when (ex.ApiError.Code is "PRICES_CHANGED" or "SHARED_GRID")
            {
                await _dialogService.ShowInfoAsync("La grille a changé depuis l'aperçu. Vérifiez le nouvel aperçu.");
                IsBusy = false;
                await ComputePreview();
            }
            catch (ApiException ex) when (ex.ApiError.Code == "NOTHING_TO_CHANGE")
            {
                await _dialogService.ShowInfoAsync("Rien à changer : les tarifs seraient identiques.");
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
        private async Task Undo(RentalGridModification? modification)
        {
            if (modification is not { CanUndo: true } || IsBusy)
                return;
            if (!await _dialogService.ConfirmAsync("Annuler le changement", $"Remettre les tarifs de « {modification.Title} » comme avant ?", "Remettre", "Garder"))
                return;

            try
            {
                await _service.UndoAsync(modification.id);
                await _dialogService.ShowInfoAsync("Les anciens tarifs sont remis.", "Grille de location");
            }
            catch (ApiException ex) when (ex.ApiError.Code is "UNDO_EXPIRED" or "GRID_EDITED_SINCE" or "ALREADY_UNDONE")
            {
                var text = ex.ApiError.Code switch
                {
                    "UNDO_EXPIRED" => "Les 24 heures sont passées : ce changement ne peut plus être annulé.",
                    "GRID_EDITED_SINCE" => "La grille a été modifiée depuis : l'annulation remettrait d'autres tarifs. Changez-la à nouveau si besoin.",
                    _ => "Ce changement est déjà annulé.",
                };
                await _dialogService.ShowInfoAsync(text, "Grille de location");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
            }
            await LoadAsync();
        }
    }
}
