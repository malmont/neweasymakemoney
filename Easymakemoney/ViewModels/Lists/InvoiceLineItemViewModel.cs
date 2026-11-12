using System.Globalization;
using Easymakemoney.Utilities;

namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>L'écran de vérification, vu par une carte de ligne.</summary>
    public interface IInvoiceLineHost
    {
        void OnLineChanged(InvoiceLineItemViewModel line, string property);
        void OnLineEditRequested(InvoiceLineItemViewModel line);
        Task ValidateLineAsync(InvoiceLineItemViewModel line);
        Task AddLinePhotoAsync(InvoiceLineItemViewModel line);
    }

    /// <summary>Choix du rattachement d'une ligne : « Nouvel article » ou un article déjà connu.</summary>
    public sealed class InvoiceLinkOption
    {
        public bool IsNew { get; init; }
        public int? ProductId { get; init; }
        public int? VariantId { get; init; }
        public string? ProductName { get; init; }
        public string Label { get; init; } = string.Empty;

        public static InvoiceLinkOption New { get; } = new() { IsNew = true, Label = "Nouvel article" };

        public static InvoiceLinkOption Existing(int id, string? name, string? code, int? variantId) => new()
        {
            ProductId = id,
            VariantId = variantId,
            ProductName = string.IsNullOrWhiteSpace(name) ? $"Article {id}" : name,
            Label = "Déjà connu : " + (string.IsNullOrWhiteSpace(name) ? $"article {id}" : name)
                + (string.IsNullOrWhiteSpace(code) ? string.Empty : $" ({code})"),
        };
    }

    /// <summary>Une carte de ligne de facture : lecture, corrections du gérant, validation et photo.</summary>
    public partial class InvoiceLineItemViewModel : ObservableObject
    {
        private readonly IInvoiceLineHost _host;
        private bool _applying;
        private string? _documentCurrency;
        private string? _siteCurrency;
        private double? _exchangeRate;

        public InvoiceLineItemViewModel(IInvoiceLineHost host, InvoiceExtractedLine read, InvoiceReviewLine state)
        {
            _host = host;
            N = read.N;
            ReadLabel = read.Label ?? read.DisplayLabel;
            Label = state.Label ?? read.Label ?? read.DisplayLabel;
            GroupKey = state.GroupKey ?? read.GroupKey;
            DoubtReason = InvoiceReview.DoubtReason(read);
            ReadCostText = read.UnitCostRead.HasValue && read.UnitCostRead != read.UnitCost
                ? $"Montant lu sur la facture : {InvoiceReview.FormatAmount(read.UnitCostRead.Value)}"
                : null;

            LinkOptions.Add(InvoiceLinkOption.New);
            if (read.Product != null)
                LinkOptions.Add(InvoiceLinkOption.Existing(read.Product.Id, read.Product.Name, read.Product.Code, read.Product.VariantId));
            foreach (var candidate in read.Candidates.Where(c => LinkOptions.All(o => o.ProductId != c.Id)))
                LinkOptions.Add(InvoiceLinkOption.Existing(candidate.Id, candidate.Name, candidate.Code, candidate.VariantId));

            Apply(state);
        }

        public int N { get; }
        public string ReadLabel { get; }
        public string Label { get; }
        public string? GroupKey { get; }
        public string? DoubtReason { get; }
        public string? ReadCostText { get; }
        public bool HasReadCost => ReadCostText != null;
        public ObservableCollection<InvoiceLinkOption> LinkOptions { get; } = new();

        [ObservableProperty] private InvoiceLinkOption? selectedLink;
        [ObservableProperty] private string? name;
        [ObservableProperty] private string? quantityText;
        [ObservableProperty] private string? unitCostText;
        [ObservableProperty] private string? coefficientText;
        [ObservableProperty] private string? size;
        [ObservableProperty] private string? color;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ValidateText))]
        [NotifyPropertyChangedFor(nameof(ShowDoubt))]
        private bool isValidated;

        [ObservableProperty] private bool isEditing;
        [ObservableProperty] private ImageSource? photoSource;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PhotoText))]
        private bool hasPhoto;

        [ObservableProperty] private bool isUploadingPhoto;
        [ObservableProperty] private string? groupText;

        public bool IsNew => SelectedLink?.IsNew != false;
        public bool IsExisting => !IsNew;
        public bool HasGroup => !string.IsNullOrEmpty(GroupText);
        public bool ShowDoubt => !IsValidated && DoubtReason != null;
        public string ValidateText => IsValidated ? "Validée ✓" : "Valider";
        public string PhotoText => HasPhoto ? "Autre photo" : "Photo";

        public string Title => IsNew
            ? (string.IsNullOrWhiteSpace(Name) ? ReadLabel : Name!.Trim())
            : SelectedLink?.ProductName ?? ReadLabel;

        public string StatusText => IsNew ? "Nouvel article" : $"Déjà connu : {SelectedLink?.ProductName}";

        public string QuantityCostText
        {
            get
            {
                var quantity = InvoiceReview.ParseQuantity(QuantityText);
                var cost = InvoiceReview.ParseCents(UnitCostText);
                var quantityPart = quantity.HasValue ? $"Quantité {quantity}" : "Quantité à corriger";
                var costPart = cost.HasValue
                    ? $"{SiteCurrency.FormatCents(cost.Value, CultureInfo.CurrentCulture, _documentCurrency)} HT l'unité"
                    : "coût à corriger";
                return $"{quantityPart} · {costPart}";
            }
        }

        private string? _serverPriceText;

        /// <summary>Prix de vente calculé par le serveur (aperçu APERÇU-FACT) ; null tant que la route n'existe pas (estimation locale).</summary>
        public string? ServerPriceText
        {
            get => _serverPriceText;
            set
            {
                if (_serverPriceText == value)
                    return;
                _serverPriceText = value;
                OnPropertyChanged(nameof(SalePriceText));
            }
        }

        public string SalePriceText
        {
            get
            {
                if (ServerPriceText != null)
                    return ServerPriceText;
                // Le prix de vente d'un article créé est calculé par le serveur (aperçu APERÇU-FACT), jamais par l'appli.
                return IsExisting ? "Prix de vente : inchangé" : "Prix de vente : calculé par le serveur";
            }
        }

        public string SizeColorText
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(Size)) parts.Add($"Taille {Size!.Trim()}");
                if (!string.IsNullOrWhiteSpace(Color)) parts.Add($"Couleur {Color!.Trim()}");
                return string.Join(" · ", parts);
            }
        }

        public bool HasSizeColor => SizeColorText.Length > 0;

        /// <summary>État de la ligne pour le brouillon et la validation.</summary>
        public InvoiceReviewLine ToLine() => new()
        {
            N = N,
            Validated = IsValidated,
            Label = Label,
            Mode = IsNew ? InvoiceReviewDraft.ModeNew : InvoiceReviewDraft.ModeExisting,
            ProductId = IsNew ? null : SelectedLink?.ProductId,
            VariantId = IsNew ? null : SelectedLink?.VariantId,
            ProductName = IsNew ? null : SelectedLink?.ProductName,
            Name = Name,
            GroupKey = GroupKey,
            Quantity = QuantityText,
            UnitCost = UnitCostText,
            Coefficient = CoefficientText,
            Size = Size,
            Color = Color,
        };

        public void SetValidated(bool validated) => IsValidated = validated;

        public void SetPhoto(ImageSource? source)
        {
            PhotoSource = source;
            HasPhoto = true;
        }

        /// <summary>Nom et coefficient repris d'une autre ligne du même article, sans toucher à la validation.</summary>
        public void ApplyGroupValues(string? groupName, string? coefficient)
        {
            _applying = true;
            try
            {
                Name = groupName;
                CoefficientText = coefficient;
            }
            finally
            {
                _applying = false;
            }
            RefreshDisplay();
        }

        public void SetCurrency(string? documentCurrency, string? siteCurrency, double? exchangeRate)
        {
            _documentCurrency = documentCurrency;
            _siteCurrency = siteCurrency;
            _exchangeRate = exchangeRate;
            OnPropertyChanged(nameof(QuantityCostText));
            OnPropertyChanged(nameof(SalePriceText));
        }

        private void Apply(InvoiceReviewLine state)
        {
            _applying = true;
            try
            {
                InvoiceLinkOption? link = InvoiceLinkOption.New;
                if (state.IsExisting && state.ProductId is > 0)
                {
                    link = LinkOptions.FirstOrDefault(o => o.ProductId == state.ProductId);
                    if (link == null)
                    {
                        // Article choisi dans un brouillon et absent des propositions actuelles.
                        link = InvoiceLinkOption.Existing(state.ProductId.Value, state.ProductName, null, state.VariantId);
                        LinkOptions.Add(link);
                    }
                }
                SelectedLink = link;
                Name = state.Name;
                QuantityText = state.Quantity;
                UnitCostText = state.UnitCost;
                CoefficientText = state.Coefficient;
                Size = state.Size;
                Color = state.Color;
                IsValidated = state.Validated;
            }
            finally
            {
                _applying = false;
            }
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            OnPropertyChanged(nameof(IsNew));
            OnPropertyChanged(nameof(IsExisting));
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(QuantityCostText));
            OnPropertyChanged(nameof(SalePriceText));
            OnPropertyChanged(nameof(SizeColorText));
            OnPropertyChanged(nameof(HasSizeColor));
        }

        /// <summary>Une correction après validation demande de valider à nouveau la ligne.</summary>
        private void Touch(string property)
        {
            RefreshDisplay();
            if (_applying)
                return;
            if (IsValidated)
                IsValidated = false;
            _host.OnLineChanged(this, property);
        }

        /// <summary>Un champ vide qui passe de null à « » (création du contrôle) n'est pas une correction.</summary>
        private void TouchText(string? oldValue, string? newValue, string property)
        {
            if ((oldValue ?? string.Empty) == (newValue ?? string.Empty))
            {
                RefreshDisplay();
                return;
            }
            Touch(property);
        }

        partial void OnSelectedLinkChanged(InvoiceLinkOption? oldValue, InvoiceLinkOption? newValue)
        {
            // Un sélecteur qui se vide en se recréant ne choisit rien : on garde le choix précédent.
            if (newValue == null && !_applying)
            {
                _applying = true;
                try
                {
                    SelectedLink = oldValue ?? InvoiceLinkOption.New;
                }
                finally
                {
                    _applying = false;
                }
                return;
            }
            Touch(nameof(SelectedLink));
        }

        partial void OnNameChanged(string? oldValue, string? newValue) => TouchText(oldValue, newValue, nameof(Name));
        partial void OnQuantityTextChanged(string? oldValue, string? newValue) => TouchText(oldValue, newValue, nameof(QuantityText));
        partial void OnUnitCostTextChanged(string? oldValue, string? newValue) => TouchText(oldValue, newValue, nameof(UnitCostText));
        partial void OnCoefficientTextChanged(string? oldValue, string? newValue) => TouchText(oldValue, newValue, nameof(CoefficientText));
        partial void OnSizeChanged(string? oldValue, string? newValue) => TouchText(oldValue, newValue, nameof(Size));
        partial void OnColorChanged(string? oldValue, string? newValue) => TouchText(oldValue, newValue, nameof(Color));
        partial void OnGroupTextChanged(string? value) => OnPropertyChanged(nameof(HasGroup));

        [RelayCommand]
        private void ToggleEdit() => _host.OnLineEditRequested(this);

        [RelayCommand]
        private Task Validate() => _host.ValidateLineAsync(this);

        [RelayCommand]
        private Task AddPhoto() => _host.AddLinePhotoAsync(this);
    }
}
