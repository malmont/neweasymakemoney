namespace Easymakemoney.Services.Api;

/// <summary>Un fichier à envoyer dans un formulaire multipart.</summary>
public sealed record UploadFile(Stream Content, string FileName, string ContentType);
