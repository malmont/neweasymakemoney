namespace Easymakemoney.Utilities
{
    /// <summary>
    /// Page racine de la fenêtre (application à une seule fenêtre) : remplace `Application.Current.MainPage`, obsolète
    /// depuis .NET 9. Même page : MainPage est posée sur la première fenêtre au démarrage (App.xaml.cs).
    /// </summary>
    public static class AppPage
    {
        public static Page? Current => Application.Current?.Windows.FirstOrDefault()?.Page;
    }
}
