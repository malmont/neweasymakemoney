using System.Net;
using System.Net.Http.Headers;
using Easymakemoney.Models.Api;
using Xunit;

namespace Easymakemoney.Tests.Models;

public class ApiErrorTests
{
    [Fact]
    public void FromHttpResponse_ErrorsArray_ExtractsValidationErrors()
    {
        var json = """
        {
            "error": "VALIDATION_FAILED",
            "message": "Erreur de validation",
            "errors": [
                { "path": "email", "message": "Format d'email invalide" },
                { "path": "password", "message": "Mot de passe trop court" }
            ]
        }
        """;

        var error = ApiError.FromHttpResponse(json, HttpStatusCode.UnprocessableEntity, null);

        Assert.Equal("VALIDATION_FAILED", error.Code);
        Assert.Equal("Erreur de validation", error.Message);
        Assert.Equal(2, error.Errors.Count);
        Assert.Equal("email", error.Errors[0].Path);
        Assert.Equal("Format d'email invalide", error.Errors[0].Message);
        Assert.Equal("password", error.Errors[1].Path);
        Assert.Equal("Mot de passe trop court", error.Errors[1].Message);
    }

    [Fact]
    public void FromHttpResponse_ViolationsArray_ExtractsValidationErrors()
    {
        var json = """
        {
            "violations": [
                { "propertyPath": "title", "message": "Le titre est obligatoire" }
            ]
        }
        """;

        var error = ApiError.FromHttpResponse(json, HttpStatusCode.BadRequest, null);

        Assert.Single(error.Errors);
        Assert.Equal("title", error.Errors[0].Path);
        Assert.Equal("Le titre est obligatoire", error.Errors[0].Message);
    }

    [Fact]
    public void FromHttpResponse_FieldsObject_TranslatesKnownFieldsAndCodes()
    {
        var json = """
        {
            "error": "VALIDATION_FAILED",
            "fields": {
                "budget": "MUST_BE_INTEGER_CENTS",
                "nomCollection": "REQUIRED",
                "unknownField": "CUSTOM_ERROR"
            }
        }
        """;

        var error = ApiError.FromHttpResponse(json, HttpStatusCode.UnprocessableEntity, null);

        Assert.Equal("VALIDATION_FAILED", error.Code);
        Assert.Equal(3, error.Errors.Count);

        var budgetErr = error.Errors.Find(e => e.Path == "Budget");
        Assert.NotNull(budgetErr);
        Assert.Equal("Montant invalide.", budgetErr.Message);

        var nomErr = error.Errors.Find(e => e.Path == "Nom");
        Assert.NotNull(nomErr);
        Assert.Equal("Ce champ est obligatoire.", nomErr.Message);

        // Champ inconnu : sans nom technique (Path == "")
        var unknownErr = error.Errors.Find(e => e.Path == string.Empty);
        Assert.NotNull(unknownErr);
        Assert.Equal("CUSTOM_ERROR", unknownErr.Message);
    }

    [Fact]
    public void FromHttpResponse_EmptyBody_ReturnsStatusCodeAndFallbackMessage()
    {
        var error = ApiError.FromHttpResponse(string.Empty, HttpStatusCode.InternalServerError, null);

        Assert.Equal("InternalServerError", error.Code);
        Assert.Equal("Erreur du serveur. Veuillez réessayer plus tard.", error.Message);
        Assert.Empty(error.Errors);
    }

    [Fact]
    public void FromHttpResponse_NullBody_ReturnsStatusCodeAndFallbackMessage()
    {
        var error = ApiError.FromHttpResponse(null, HttpStatusCode.NotFound, null);

        Assert.Equal("NotFound", error.Code);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
        Assert.Empty(error.Errors);
    }

    [Fact]
    public void FromHttpResponse_NonJsonBody_HandlesGracefullyWithoutThrowing()
    {
        var html = "<html><body>502 Bad Gateway nginx</body></html>";

        var error = ApiError.FromHttpResponse(html, HttpStatusCode.BadGateway, null);

        Assert.Equal("BadGateway", error.Code);
        Assert.Equal("Erreur du serveur. Veuillez réessayer plus tard.", error.Message);
        Assert.Empty(error.Errors);
    }

    [Fact]
    public void FromHttpResponse_RateLimited429_DefaultRetryAfterIs60Seconds()
    {
        var json = """{ "error": "RATE_LIMITED" }""";

        var error = ApiError.FromHttpResponse(json, (HttpStatusCode)429, null);

        Assert.Equal(60, error.RetryAfterSeconds);
        Assert.Contains("60", error.Message);
    }

    [Fact]
    public void FromHttpResponse_RateLimitedWithJsonRetryAfter_ParsesSeconds()
    {
        var json = """{ "error": "TOO_MANY_ATTEMPTS", "retryAfterSeconds": 45 }""";

        var error = ApiError.FromHttpResponse(json, (HttpStatusCode)429, null);

        Assert.Equal(45, error.RetryAfterSeconds);
        Assert.Contains("45", error.Message);
    }

    [Theory]
    [InlineData("FOURNISSEUR_NOT_FOUND", HttpStatusCode.NotFound)]
    [InlineData("TYPE_FOURNISSEUR_NOT_FOUND", HttpStatusCode.NotFound)]
    [InlineData("TYPE_NOTE_DE_FRAIS_NOT_FOUND", HttpStatusCode.NotFound)]
    [InlineData("TRANSPORTEUR_NOT_FOUND", HttpStatusCode.NotFound)]
    [InlineData("STYLE_NOT_FOUND", HttpStatusCode.NotFound)]
    [InlineData("CATEGORY_NOT_FOUND", HttpStatusCode.NotFound)]
    [InlineData("COLLECTION_NOT_FOUND", HttpStatusCode.NotFound)]
    [InlineData("COMMANDE_NOT_FOUND", HttpStatusCode.NotFound)]
    [InlineData("PRODUCT_NOT_FOUND", HttpStatusCode.NotFound)]
    [InlineData("SHIPPING_COST_ALREADY_SET", HttpStatusCode.Conflict)]
    [InlineData("COLLECTION_NOT_EMPTY", HttpStatusCode.Conflict)]
    [InlineData("FOURNISSEUR_IN_USE", HttpStatusCode.Conflict)]
    [InlineData("TRANSPORTEUR_IN_USE", HttpStatusCode.Conflict)]
    [InlineData("COMMANDE_CLOSED", HttpStatusCode.Conflict)]
    [InlineData("COLLECTION_CLOSED", HttpStatusCode.Conflict)]
    [InlineData("ALREADY_CLOSED", HttpStatusCode.Conflict)]
    [InlineData("PRODUCT_ALREADY_ATTACHED", HttpStatusCode.Conflict)]
    public void FromHttpResponse_Contract404And409Codes_PreservesCodeAndHidesRawCodeInFallback(string code, HttpStatusCode statusCode)
    {
        var json = $"{{\"error\":\"{code}\"}}";

        var error = ApiError.FromHttpResponse(json, statusCode, null);

        Assert.Equal(code, error.Code);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
        // Règle T8 : le message ne doit jamais contenir le code d'erreur brut
        Assert.DoesNotContain(code, error.Message);
    }

    [Fact]
    public void FromHttpResponse_ServerMessageProvided_UsesServerMessage()
    {
        var json = """
        {
            "error": "FOURNISSEUR_NOT_FOUND",
            "message": "Le fournisseur sélectionné n'existe plus."
        }
        """;

        var error = ApiError.FromHttpResponse(json, HttpStatusCode.NotFound, null);

        Assert.Equal("FOURNISSEUR_NOT_FOUND", error.Code);
        Assert.Equal("Le fournisseur sélectionné n'existe plus.", error.Message);
    }
}

