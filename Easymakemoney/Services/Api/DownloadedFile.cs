namespace Easymakemoney.Services.Api;

/// <summary>Fichier rendu par le serveur (PDF des étiquettes…) ; Count = nombre annoncé par l'en-tête, s'il y en a un.</summary>
public sealed record DownloadedFile(byte[] Content, string ContentType, int? Count);
