using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace Easymakemoney.Views.Lists
{
    /// <summary>Page modale de lecture : le premier code lu ferme la page et rend sa valeur ; « Fermer » rend null.</summary>
    public class BarcodeScannerPage : ContentPage
    {
        private readonly TaskCompletionSource<string?> _result = new();
        private readonly CameraBarcodeReaderView _reader;

        public Task<string?> Result => _result.Task;

        public BarcodeScannerPage(bool qr = false)
        {
            Title = qr ? "Scanner le QR code" : "Scanner un code-barres";
            BackgroundColor = Colors.Black;

            _reader = new CameraBarcodeReaderView
            {
                Options = new BarcodeReaderOptions
                {
                    Formats = qr ? BarcodeFormat.QrCode : BarcodeFormats.OneDimensional,
                    AutoRotate = true,
                    Multiple = false,
                },
                IsDetecting = true,
            };
            _reader.BarcodesDetected += OnDetected;

            var close = new Button
            {
                Text = "Fermer",
                Margin = new Thickness(16),
                HeightRequest = 48,
                CornerRadius = 12,
                FontFamily = "InterSemiBold",
            };
            close.Clicked += async (_, _) => await CloseAsync(null);

            var hint = new Label
            {
                Text = qr ? "Placez le QR code dans le cadre." : "Placez le code-barres dans le cadre.",
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(16, 24, 16, 8),
            };

            var grid = new Grid { RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) } };
            grid.Add(hint, 0, 0);
            grid.Add(_reader, 0, 1);
            grid.Add(close, 0, 2);
            Content = grid;
        }

        private void OnDetected(object? sender, BarcodeDetectionEventArgs e)
        {
            var value = e.Results?.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.Value))?.Value;
            if (value == null)
                return;
            _reader.IsDetecting = false;
            MainThread.BeginInvokeOnMainThread(async () => await CloseAsync(value.Trim()));
        }

        private async Task CloseAsync(string? value)
        {
            if (!_result.TrySetResult(value))
                return;
            _reader.BarcodesDetected -= OnDetected;
            if (Navigation.ModalStack.Contains(this))
                await Navigation.PopModalAsync();
        }

        protected override bool OnBackButtonPressed()
        {
            _ = CloseAsync(null);
            return true;
        }
    }
}
