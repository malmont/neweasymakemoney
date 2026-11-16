namespace Easymakemoney.Services
{
    /// <summary>Lecture d'un code-barres à l'appareil photo ; null si l'utilisateur ferme sans lire.</summary>
    public interface IBarcodeScanner
    {
        Task<string?> ScanAsync();

        /// <summary>Lecture d'un QR code (ex. l'étiquette de jumelage d'un boîtier) ; null si l'utilisateur ferme sans lire.</summary>
        Task<string?> ScanQrAsync();
    }

    public class CameraBarcodeScanner : IBarcodeScanner
    {
        public Task<string?> ScanAsync() => ScanWithAsync(qr: false);

        public Task<string?> ScanQrAsync() => ScanWithAsync(qr: true);

        private static async Task<string?> ScanWithAsync(bool qr)
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
                throw new Easymakemoney.Services.Dialog.UserMessageException("L'appareil photo n'est pas autorisé. Autorisez-le dans les réglages du téléphone, ou tapez le code.");

            var page = new BarcodeScannerPage(qr);
            var navigation = Shell.Current?.Navigation;
            if (navigation == null)
                return null;
            await navigation.PushModalAsync(page);
            return await page.Result;
        }
    }
}
