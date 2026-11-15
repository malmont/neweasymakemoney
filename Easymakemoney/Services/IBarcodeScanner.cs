namespace Easymakemoney.Services
{
    /// <summary>Lecture d'un code-barres à l'appareil photo ; null si l'utilisateur ferme sans lire.</summary>
    public interface IBarcodeScanner
    {
        Task<string?> ScanAsync();
    }

    public class CameraBarcodeScanner : IBarcodeScanner
    {
        public async Task<string?> ScanAsync()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
                throw new Easymakemoney.Services.Dialog.UserMessageException("L'appareil photo n'est pas autorisé. Autorisez-le dans les réglages du téléphone, ou tapez le code.");

            var page = new BarcodeScannerPage();
            var navigation = Shell.Current?.Navigation;
            if (navigation == null)
                return null;
            await navigation.PushModalAsync(page);
            return await page.Result;
        }
    }
}
