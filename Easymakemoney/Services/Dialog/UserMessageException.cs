namespace Easymakemoney.Services.Dialog;

/// <summary>
/// Refus rédigé pour le gérant (saisie incomplète, choix manquant) : <see cref="DialogService"/> montre son message tel quel, au lieu
/// du texte générique réservé aux erreurs imprévues.
/// </summary>
public class UserMessageException : Exception
{
    public UserMessageException(string message) : base(message)
    {
    }
}
