# Lot 4c : plan de Gemini

## Ce que j'ai lu
- `docs/gemini/lot-4c-consigne.md:1-20` : consigne du lot 4c, cinq sujets à couvrir, interdictions absolues (aucun XAML, aucun code-behind `Views/*.xaml.cs`, aucun appel réseau réel, aucun lancement d'app, pas de commit), contrôle `SiteCurrency` sous `[Collection("SiteCurrency")]`, exécution des tests et Rebuild Android (référence : 324 avertissements).
- `docs/perimetre-mobile-web.md:68-83` : spécifications de la lecture des documents par l'IA (facture fournisseur, note de frais, facture de transport), route `gestion/documents`.
- `Easymakemoney/Services/HttpService.cs:90-118` : méthode `PostFilesAsync<T>`, construction du `MultipartFormDataContent`, ajout des champs texte, ajout des flux fichiers avec `part.Headers.ContentType` et nom de fichier, envoi POST, gestion des exceptions réseau/timeout converties en `ApiException.NoConnection(ex)`.
- `Easymakemoney/Services/Api/UploadFile.cs:4` : définition du record `UploadFile(Stream Content, string FileName, string ContentType)`.
- `Easymakemoney/Services/IDocumentReaderService.cs:1-13` : signature des méthodes `UploadAsync`, `GetAsync`, `RereadAsync`, `ConfirmAsync`, `DeleteAsync`.
- `Easymakemoney/Services/DocumentReaderService.cs:1-31` : implémentation du service : `BaseUrl = "gestion/documents"`, `UploadAsync` transmettant les champs `type` et `locale = "fr"` avec clé `files[]`, `GetAsync` (`gestion/documents/{id}`), `RereadAsync` (`gestion/documents/{id}/relire`), `ConfirmAsync` (`gestion/documents/{id}/confirmer`), `DeleteAsync` (`gestion/documents/{id}`).
- `Easymakemoney/Models/GestionDocument.cs:1-14` : modèle représentant le document lu avec propriétés `id`, `type`, `status`, `error`, et `JObject? extraction`.
- `Easymakemoney/Models/ListCommand.cs:14-22` : propriétés `currency` et `BudgetText` appelant `SiteCurrency.FormatCents(budget.Value, CultureInfo.CurrentCulture, currency)`.
- `Easymakemoney/Models/FraisDePortCommandeRow.cs:21-23` : propriété `PriceText` appelant `SiteCurrency.FormatCents(price, CultureInfo.CurrentCulture, Commande.currency)`.
- `Easymakemoney/Utilities/SiteCurrency.cs:118-136` : méthode `FormatCents(cents, culture, currencyCode)` priorisant `currencyCode` sur `CurrentCurrency`.
- `Easymakemoney/ViewModels/FormModels/CommandFormModel.cs:40-47, 56-67` : calculs de `NeedsExchangeRate`, `BudgetLabel`, `ExchangeRateLabel`, et notifications `PropertyChanged`.
- `Easymakemoney.Tests/Fakes/TestHttpMessageHandler.cs:1-31` : capture des requêtes HTTP pour inspection des entêtes, corps et méthodes.
- `Easymakemoney.Tests/Fakes/FakeHttpService.cs:1-57` : doublure de `IHttpService` pour tests unitaires de services sans handler HTTP complet.

## Prérequis à installer ou à vérifier (commandes exactes, rien n'est lancé)
- Branche active : `feature/lot-4c-tests` créée depuis `main` (`git switch -c feature/lot-4c-tests main` — déjà exécuté).
- Aucun paquet ni outil à installer.
- Nombre d'avertissements de référence Android sur `main` post-lot 4b : **324 avertissements** (constaté lors du Rebuild de clôture du lot 4b).

## Étapes

### 1. Sujet 1 : `HttpService.PostFilesAsync`
- **Fichier ciblé** : `Easymakemoney.Tests/Services/HttpServiceTests.cs` (complétion).
- **Cas de test à ajouter** :
  1. `PostFilesAsync_Success_SendsMultipartWithFieldsAndFiles` : avec `TestHttpMessageHandler`, envoi de champs texte (`type: "facture"`, `locale: "fr"`) et de 2 fichiers `UploadFile` (flux en mémoire, nom `facture.pdf` et type MIME `application/pdf`). Vérification :
     - Méthode `HttpMethod.Post`
     - Entête `Content-Type` de type `multipart/form-data`
     - Présence de la partie texte `type` contenant `"facture"`
     - Présence de la partie texte `locale` contenant `"fr"`
     - Présence des deux parties de formulaire avec le nom de champ `files[]`, le bon `FileName` et le bon `ContentType`
     - Désérialisation réussie du modèle retourné en réponse (200 OK).
  2. `PostFilesAsync_422ValidationError_ThrowsApiException` : réponse 422 avec corps JSON ApiError (`error` ou `fields`) -> vérification que `ApiException.Validation` est levée.
  3. `PostFilesAsync_402PaymentRequired_ThrowsApiException` : réponse 402 -> vérification de la levée d'`ApiException`.
  4. `PostFilesAsync_NetworkFailure_ThrowsApiExceptionWithNoConnection` : exception `HttpRequestException` levée par le handler -> `ApiException` levée avec `IsNoConnection == true`.
  5. `PostFilesAsync_Timeout_ThrowsApiExceptionWithNoConnection` : exception `TaskCanceledException` ou `TimeoutException` levée par le handler -> `ApiException` levée avec `IsNoConnection == true`.

### 2. Sujet 2 : `DocumentReaderService`
- **Fichier ciblé** : création de `Easymakemoney.Tests/Services/DocumentReaderServiceTests.cs`.
- **Cas de test à ajouter** (en s'appuyant sur `FakeHttpService` ou `TestHttpMessageHandler`) :
  1. `UploadAsync_CallsPostFilesAsyncWithCorrectUrlFieldsAndFiles` : vérification de l'URL cible `"gestion/documents"`, des champs `type` et `locale: "fr"`, du nom de champ `"files[]"`, et de la liste de fichiers transmise.
  2. `GetAsync_CallsGetAsyncWithCorrectUrl` : pour `id = 15`, vérification de l'URL demandée `"gestion/documents/15"`.
  3. `RereadAsync_CallsPostAsyncWithAuthWithCorrectUrlAndNullBody` : pour `id = 15`, vérification de l'URL `"gestion/documents/15/relire"` et corps `null`.
  4. `ConfirmAsync_CallsPostAsyncWithAuthWithCorrectUrlAndBody` : pour `id = 15` et un objet payload quelconque, vérification de l'URL `"gestion/documents/15/confirmer"` et de la transmission inchangée de l'objet.
  5. `DeleteAsync_CallsDeleAsyncWithAuthWithCorrectUrl` : pour `id = 15`, vérification de l'URL `"gestion/documents/15"`.

### 3. Sujet 3 : `GestionDocument`
- **Fichier ciblé** : création de `Easymakemoney.Tests/Models/GestionDocumentTests.cs`.
- **Cas de test à ajouter** :
  1. `GestionDocument_DeserializesDoneStatus_WithFullExtractionObject` : désérialisation d'un JSON complet avec `id: 10`, `type: "facture"`, `status: "done"`, `error: null`, et un objet `extraction` JSON imbriqué (fournisseur, lignes de commande, totaux). Vérification que `extraction` est conservé en `JObject` et que ses propriétés internes sont accessibles et typées (`(string)doc.extraction["fournisseur"]`).
  2. `GestionDocument_DeserializesFailedStatus_WithError` : désérialisation avec `status: "failed"`, `error: "Document illisible ou corrompu"`, `extraction: null`.
  3. `GestionDocument_DeserializesProcessingStatus` : désérialisation avec `status: "processing"`, `error: null`, `extraction: null`.
  4. `GestionDocument_EmptyOrNullExtraction_HandledGracefully` : vérification de la résilience si `extraction` est absent ou vide.

### 4. Sujet 4 : `ListCommand.BudgetText` et `FraisDePortCommandeRow.PriceText`
- **Fichiers ciblés** :
  - Création de `Easymakemoney.Tests/Models/ListCommandTests.cs` (décoré `[Collection("SiteCurrency")]`, sauvegarde et restauration de `CultureInfo.CurrentCulture`).
  - Complétion de `Easymakemoney.Tests/Models/FraisDePortCommandeRowTests.cs` (décoré `[Collection("SiteCurrency")]`, sauvegarde et restauration de `CultureInfo.CurrentCulture`).
- **Cas de test à ajouter** :
  - **`ListCommand.BudgetText`** :
    1. `BudgetText_WhenBudgetNull_ReturnsEmpty` : `budget = null` -> `BudgetText == string.Empty`.
    2. `BudgetText_WhenCurrencySpecified_FormatsInCommandCurrencyNotSiteCurrency` : `SiteCurrency.SetCurrency("EUR")`, `budget = 150000` (1500 $), `currency = "USD"`, culture `fr-FR` -> le montant est formaté avec la devise de la commande `USD` (`$` ou `US$`) et non en euros (`€`).
    3. `BudgetText_WhenCurrencyNull_FallsBackToSiteCurrency` : `SiteCurrency.SetCurrency("EUR")`, `budget = 150000`, `currency = null`, culture `fr-FR` -> formaté en devise du site (`1 500,00 €`).
    4. `BudgetText_CultureFormatting` : vérification des séparateurs selon la culture active (ex. `en-US` vs `fr-FR`).
  - **`FraisDePortCommandeRow.PriceText`** :
    1. `PriceText_WhenFraisNull_ReturnsEmpty` : `Frais = null` -> `PriceText == string.Empty`.
    2. `PriceText_WhenCommandeCurrencySpecified_FormatsInCommandCurrencyNotSiteCurrency` : `SiteCurrency.SetCurrency("EUR")`, `Frais.price = 2500` (25 $), `Commande.currency = "USD"`, culture `fr-FR` -> formaté dans la devise de la commande `USD` (non en `€`).
    3. `PriceText_WhenCommandeCurrencyNull_FallsBackToSiteCurrency` : `SiteCurrency.SetCurrency("EUR")`, `Frais.price = 2500`, `Commande.currency = null`, culture `fr-FR` -> formaté dans la devise du site (`25,00 €`).

### 5. Sujet 5 : `CommandFormModel`
- **Fichier ciblé** : création de `Easymakemoney.Tests/ViewModels/CommandFormModelTests.cs` (décoré `[Collection("SiteCurrency")]`).
- **Cas de test à ajouter** :
  1. `NeedsExchangeRate_WhenNoFournisseurSelected_ReturnsFalse` : `SelectedFournisseurs = null` -> `NeedsExchangeRate == false`, `BudgetLabel == "Budget"`, `ExchangeRateLabel == string.Empty`.
  2. `NeedsExchangeRate_WhenFournisseurHasNoPurchaseCurrency_ReturnsFalse` : `SelectedFournisseurs.purchaseCurrency = null` ou `""` -> `NeedsExchangeRate == false`, `BudgetLabel == "Budget"`.
  3. `NeedsExchangeRate_WhenFournisseurCurrencyMatchesSiteCurrency_ReturnsFalse` : `SiteCurrency.SetCurrency("EUR")`, fournisseur avec `purchaseCurrency = "EUR"` (et test avec minuscules `"eur"`) -> `NeedsExchangeRate == false`, `BudgetLabel == "Budget"`.
  4. `NeedsExchangeRate_WhenFournisseurCurrencyDiffersFromSite_ReturnsTrue` : `SiteCurrency.SetCurrency("EUR")`, fournisseur avec `purchaseCurrency = "USD"` -> `NeedsExchangeRate == true`, `BudgetLabel == "Budget (USD)"`, `ExchangeRateLabel == "Taux : 1 USD en EUR"`.
  5. `NeedsExchangeRate_WhenSiteCurrencyNotSetAndFournisseurHasCurrency_ReturnsTrue` : `SiteCurrency.Reset()`, fournisseur avec `purchaseCurrency = "USD"` -> `NeedsExchangeRate == true`, `ExchangeRateLabel == "Taux : 1 USD en devise du site"`.
  6. `SelectedFournisseurs_TriggersPropertyChangedForDependentProperties` : espionnage de l'événement `PropertyChanged` sur `CommandFormModel` ; lors de la modification de `SelectedFournisseurs`, vérification que les notifications sont bien émises pour `SelectedFournisseurs`, `NeedsExchangeRate`, `BudgetLabel` et `ExchangeRateLabel`.

## Comment je vérifie
1. **Compilation des tests** :
   ```bash
   dotnet build Easymakemoney.Tests/Easymakemoney.Tests.csproj
   ```
2. **Exécution de tous les tests unitaires** :
   ```bash
   dotnet test Easymakemoney.Tests
   ```
   *Attendu* : l'ensemble des 196 tests actuels + les nouveaux tests passent à 100% (0 échec, 0 ignoré sauf bug applicatif avéré qui serait alors marqué en `Skip = "BUG: ..."`).
3. **Rebuild Android (`-t:Rebuild`)** :
   ```bash
   dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -t:Rebuild
   ```
   *Attendu* : 0 erreur, comparaison avec le nombre d'avertissements de référence de `main` (324 avertissements).
4. **Contrôle d'encodage et BOM contre `main`** :
   ```bash
   for f in $(git diff --name-only main); do a=$(git show main:"$f" 2>/dev/null | file -b -); b=$(file -b "$f" 2>/dev/null); [ -e "$f" ] && [ "$a" != "$b" ] && echo "DIFF $f: '$a' vs '$b'"; done; echo fin
   ```
   *Attendu* : affichage de `fin` seul (aucun fichier modifié ne doit changer d'encodage ni perdre son BOM).

## Risques et points incertains
- Dans `CommandFormModel`, le constructeur invoque `LoadTransporteurs()` (`async void`). En lui injectant un `GetListFournisseursUseCase` configuré avec `ListFourniseursServices(new FakeHttpService())` dont la liste retournée est vide et synchrone, aucune exception d'arrière-plan ne peut perturber l'exécution des tests.
- La lecture du flux dans `PostFilesAsync` consomme `file.Content`. Dans les tests unitaires avec `TestHttpMessageHandler`, l'assertion sur les flux ou le contenu des parties multipart doit tenir compte du fait que le flux aura été envoyé via le handler.
- Les tests manipulant `SiteCurrency` doivent impérativement faire partie de la collection xUnit `[Collection("SiteCurrency")]` afin d'éviter toute concurrence entre threads de test lors de la mutation de `SiteCurrency.CurrentCurrency`.

## Questions pour Claude ou le propriétaire
- Aucune question : le périmètre des 5 sujets est précis, et le code de l'application supporte déjà l'intégralité des fonctionnalités à tester.

