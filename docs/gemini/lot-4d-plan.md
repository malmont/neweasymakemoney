# Lot 4d : plan de Gemini

## Ce que j'ai lu
- `docs/gemini/lot-4d-consigne.md:1-29` : consigne du lot 4d, 6 sujets à couvrir, interdictions absolues (aucun XAML, aucun `Views/*.xaml.cs`, aucun appel réseau réel, aucun lancement d'application, pas de commit), `SiteCurrency` sous `[Collection("SiteCurrency")]`, pas de comparaison exacte des montants, gestion des bugs en `Skip = "BUG: ..."`, décompte Android (référence : 325 avertissements de `main`), contrôle d'encodage en dernier.
- `docs/periodes-gestion.md:1-124` : contrats P1 (fermeture/réouverture, 409), P2 (résultat `GET periodes/{id}/resultat`), P3 (reprise).
- `docs/perimetre-mobile-web.md:68-97` : lecture de documents, L0 (lignes d'achat) et R1 (réception explicite `POST commandes/{id}/reception`).
- `Easymakemoney/Services/PeriodResultService.cs:1-15` : appel `_httpService.GetAsync<PeriodResult>($"periodes/{periodId}/resultat")`.
- `Easymakemoney/Models/PeriodResult.cs:1-119` : modèle du résultat P2 avec `result.netRate`, `cogs.estimatedItems`, `cogs.estimatedAmount`, `otherRevenue.total`.
- `Easymakemoney/Services/CollectionCloseService.cs:12-25` : méthodes `CloseCollectionAsync` (`POST dashboard/collection/{id}/close`) et `ReopenCollectionAsync` (`POST dashboard/collection/{id}/reopen`).
- `Easymakemoney/Services/CommandeReceptionService.cs:1-17` : méthode `ReceiveAsync(int commandeId, JObject? body = null)` appelant `POST commandes/{commandeId}/reception` et retournant `CommandeReception.Read(...)`.
- `Easymakemoney/Models/CommandeReception.cs:42-140` : parsing JSON (`received`, `needsVariant`, `commande`), structure `VariantChoicesBody`, résumé `Summary`.
- `Easymakemoney/Services/ListFraisDePortService.cs:27-38` : `PostFraisDePort` (`POST commandes/{commandId}/frais-de-port`), `DeleteFraisDePort` (`DELETE commandes/{commandId}/frais-de-port`).
- `Easymakemoney/Services/ListNoteDeFraisServices.cs:17-36` : `PostNoteDeFrais` (`POST collections/{id}/notes-de-frais`), `DeleteNoteDeFrais` (`DELETE notes-de-frais/{id}`).
- `Easymakemoney/Models/Api/ApiError.cs:170-205` : traductions de `PERIOD_CLOSED` (« Cette période est clôturée : plus rien ne peut y être ajouté. ») et du code de champ `OUTSIDE_PERIOD` (« La date doit être comprise dans cette période. »).
- `Easymakemoney/ViewModels/Lists/InvoiceLineItemViewModel.cs:36-253` : carte de ligne de facture, interface `IInvoiceLineHost`, `IsNew`/`IsExisting`, `ShowDoubt`, `ToLine()`, `ApplyGroupValues`, `SetCurrency`.
- `Easymakemoney/Models/InvoiceConfirmationBuilder.cs:1-202` : logique de construction du JSON de confirmation de facture.
- `Easymakemoney.Tests/Models/InvoiceReviewTests.cs:1-425` : tests existants de l'extraction et du builder de confirmation.

## Prérequis à installer ou à vérifier (commandes exactes, rien n'est lancé)
- Branche active : `feature/lot-4d-tests` créée depuis `main` (`git switch -c feature/lot-4d-tests main` — déjà fait).
- Référence des avertissements Android sur `main` : **325 avertissements** (mesuré lors du Rebuild de clôture du lot 4c).
- Aucun outil ni paquet à installer.

## Étapes

### 1. Sujet 1 : `PeriodResultService`
- **Fichier ciblé** : création de `Easymakemoney.Tests/Services/PeriodResultServiceTests.cs` (sous `[Collection("SiteCurrency")]`, sauvegarde et restauration de culture).
- **Cas de test à ajouter** :
  1. `GetResultAsync_CallsCorrectUrl_AndDeserializesFullJson` :
     - Vérifie l'URL appelée : `periodes/42/resultat`.
     - JSON avec `frozen: true`, `sales`, `otherRevenue.total = 20000`, `cogs.estimatedItems = 2`, `cogs.estimatedAmount = 25000`, `result.netRate = null`.
     - Vérifie :
       - `result.netRate` nul est conservé nul (`pr.result.netRate == null`, `pr.HasNetRate == false`, `pr.NetRateText == string.Empty`).
       - `cogs.estimatedItems == 2`, `cogs.estimatedAmount == 25000`, `pr.HasEstimatedCogs == true`, `pr.EstimatedCogsText` contient les chiffres et le symbole attendu sans comparaison exacte.
       - `otherRevenue.total == 20000`, `pr.HasOtherRevenue == true`, `pr.OtherRevenueText` contient les chiffres et le symbole attendu.

### 2. Sujet 2 : `CollectionCloseService`
- **Analyse du comportement actuel du code** :
  - En cas de succès HTTP 200, `CloseCollectionAsync` et `ReopenCollectionAsync` exécutent `await _httpService.PostAsyncWithAuth<object>(url, null)` et retournent `true`.
  - En cas d'erreur HTTP (ex. 409 `PERIOD_CLOSED`, 409 `ALREADY_CLOSED`, 409 `NOT_CLOSED`), `HttpService` lève une `ApiException`. Comme `CollectionCloseService` ne contient aucun bloc `try/catch`, **l'`ApiException` est propagée directement à l'appelant** (la méthode ne retourne pas `false`).
- **Fichier ciblé** : création de `Easymakemoney.Tests/Services/CollectionCloseServiceTests.cs`.
- **Cas de test à ajouter** :
  1. `CloseCollectionAsync_Success_CallsCorrectUrlAndReturnsTrue` : vérifie l'appel à `dashboard/collection/10/close` avec corps `null` et retour `true`.
  2. `ReopenCollectionAsync_Success_CallsCorrectUrlAndReturnsTrue` : vérifie l'appel à `dashboard/collection/10/reopen` avec corps `null` et retour `true`.
  3. `CloseCollectionAsync_409PeriodClosed_ThrowsApiException` : simule une réponse 409 avec `{"error": "PERIOD_CLOSED"}` -> vérifie que l'`ApiException` est propagée avec `StatusCode == HttpStatusCode.Conflict` et `ex.ApiError.Code == "PERIOD_CLOSED"`.
  4. `ReopenCollectionAsync_409NotClosed_ThrowsApiException` : simule une réponse 409 avec `{"error": "NOT_CLOSED"}` -> vérifie la propagation de l'`ApiException`.

### 3. Sujet 3 : `CommandeReceptionService`
- **Fichier ciblé** : création de `Easymakemoney.Tests/Services/CommandeReceptionServiceTests.cs`.
- **Cas de test à ajouter** :
  1. `ReceiveAsync_WithNullBody_CallsCorrectUrlAndReturnsCommandeReception` :
     - Appel avec `body = null` (réception de tout ce qui reste).
     - Vérifie l'URL `commandes/7/reception` et la transmission du corps nul.
     - Lecture de la réponse JSON en `CommandeReception` (ex: `received` avec 2 lignes, 5 articles reçus, `remaining = 0`, `receptionStatus = "complete"`).
  2. `ReceiveAsync_WithBody_TransmitsBodyAsIs` :
     - Envoi d'un corps `JObject` (sélection de variantes `{ lines: [{ lineId: 1, variantId: 2 }] }`).
     - Vérifie que le payload est transmis tel quel vers `commandes/7/reception`.
  3. `ReceiveAsync_PartialReception_ParsesNeedsVariant` :
     - Réponse JSON avec tableau `needsVariant` (variantes avec `id`, `size`, `color`), `remaining = 3`, `receptionStatus = "partial"`.
     - Vérifie `reception.NeedsVariant.Count > 0`, `ChoiceLabels()`, et `reception.Remaining == 3`.

### 4. Sujet 4 : `ListFraisDePortService` et `ListNoteDeFraisServices`
- **Fichier ciblé** : création de `Easymakemoney.Tests/Services/FraisEtNotesServicesTests.cs`.
- **Cas de test à ajouter** :
  1. `PostFraisDePort_SendsCorrectUrlAndCentsAmount` :
     - Montant saisi `12.50` converti via `MoneyHelper.ToCents` donne `1250L` (jamais `125000`).
     - Vérifie l'appel `POST commandes/8/frais-de-port` avec `price == 1250`.
  2. `DeleteFraisDePort_CallsCorrectUrl` :
     - Vérifie l'appel `DELETE commandes/8/frais-de-port`.
  3. `PostNoteDeFrais_SendsCorrectUrlAndCentsAmount` :
     - Montant saisi `12.50` converti via `MoneyHelper.ToCents` donne `1250L`.
     - Vérifie l'appel `POST collections/3/notes-de-frais` avec `montant == 1250`.
  4. `DeleteNoteDeFrais_CallsCorrectUrl` :
     - Vérifie l'appel `DELETE notes-de-frais/19`.
  5. `Error_PeriodClosed_GeneratesFriendlyMessage` :
     - Réponse 409 avec `{"error": "PERIOD_CLOSED"}` -> `ApiError.FromHttpResponse` génère le message « Cette période est clôturée : plus rien ne peut y être ajouté. ».
  6. `Error_DateOutsidePeriod_GeneratesFriendlyValidationMessage` :
     - Réponse 422 avec `{"fields": {"date": "OUTSIDE_PERIOD"}}` -> `ApiError.FromHttpResponse` génère une erreur avec champ « Date » et message « La date doit être comprise dans cette période. ».

### 5. Sujet 5 : `InvoiceLineItemViewModel`
- **Fichier ciblé** : création de `Easymakemoney.Tests/ViewModels/InvoiceLineItemViewModelTests.cs` (sous `[Collection("SiteCurrency")]`).
- **Mise en place** : doublure `FakeInvoiceLineHost : IInvoiceLineHost` enregistrant les interactions (`OnLineChanged`, `OnLineEditRequested`, `ValidateLineAsync`, `AddLinePhotoAsync`). `SetPhoto` est expressément exclu car dépendant de MAUI.
- **Cas de test à ajouter** :
  1. `IsNew_And_IsExisting_ReflectSelectedLink` :
     - Initialisation avec `InvoiceLinkOption.New` -> `IsNew == true`, `IsExisting == false`.
     - Sélection d'un article existant -> `IsNew == false`, `IsExisting == true`.
  2. `ShowDoubt_WhenDoubtPresentAndNotValidated_ReturnsTrue` :
     - Ligne avec doute (`DoubtReason != null`) non validée -> `ShowDoubt == true`.
     - Dès que `IsValidated = true` -> `ShowDoubt == false`.
     - Ligne sans doute -> `ShowDoubt == false` même non validée.
  3. `ToLine_CapturesEnteredValues` :
     - Modification des champs saisis (nom, quantité, coût unitaire, coefficient, taille, couleur).
     - Appel `ToLine()` -> vérifie que l'objet `InvoiceReviewLine` retourné reprend fidèlement chaque valeur.
  4. `ApplyGroupValues_UpdatesNameAndCoefficient_WithoutTriggeringHostTouch` :
     - Appel `ApplyGroupValues("T-shirt Bio", "2.2")` -> vérifie la mise à jour de `Name` et `CoefficientText` sans déclencher `_host.OnLineChanged` ni annuler `IsValidated`.
  5. `SetCurrency_DocumentDiffersFromSite_WithoutRate_ShowsNotice` :
     - Document "USD", site "EUR", `exchangeRate = null` -> `SalePriceText` indique `(avant taux de change)` en USD.
  6. `SetCurrency_DocumentDiffersFromSite_WithRate_ConvertsPrice` :
     - Document "USD", site "EUR", `exchangeRate = 0.92` -> `SalePriceText` convertit le prix dans la devise du site.

### 6. Sujet 6 : `InvoiceConfirmationBuilder` (manques par rapport à `InvoiceReviewTests`)
- **Inventaire des manques identifiés dans `InvoiceReviewTests`** :
  Dans `InvoiceReviewTests.cs`, les cas suivants ne sont pas testés aujourd'hui :
  1. *Facture sans ligne* (`draft.Lines.Clear()`) : la branche `ordered.Count == 0` ajoutant l'erreur « La facture n'a aucune ligne. » n'est jamais exécutée.
  2. *Date de facture manquante ou invalide* : `draft.Invoice.Date` null, vide ou de format incorrect (« date-invalide ») ajoutant l'erreur « Indiquez la date de la facture. » n'est pas testé.
  3. *Fournisseur existant avec Id invalide* : `ModeExisting` avec `Id = 0` ou négatif retombant dans le repli d'erreur « Choisissez le fournisseur. » n'est pas testé (seul `Id = null` a été testé).
  4. *Ligne existante sans variante* : le cas `VariantId == null` ou `<= 0` (omission de la clé `variantId` dans le JSON) n'est pas testé (le test existant porte sur `VariantId = 77`).
  5. *Ligne nouvelle sans `groupKey`* : le cas `GroupKey = null` (omission de la clé `groupKey` dans `create`) n'est pas testé.
  6. *Ligne nouvelle sans coefficient* : `Coefficient = null` ou vide (omission de la clé `coefficient` dans `create`) n'est pas testé.
  7. *Ligne validée avec problème de saisie* : une ligne marquée `Validated = true` mais ayant une quantité à 0 ou un coût invalide rapportant l'erreur dans `Build` n'est pas testée (seul `LineProblem` isolé est testé).
  8. *Frais de port inclus avec montant à zéro* : `draft.Shipping.Include = true` avec `Amount = "0"` (omission de `shipping` sans erreur) n'est pas testé.
  9. *Numéro de facture absent* : `draft.Invoice.Number = null` ou vide (omission de `number` sans erreur) n'est pas testé.
- **Fichier ciblé** : création de `Easymakemoney.Tests/Models/InvoiceConfirmationBuilderMissingTests.cs` couvrant exclusivement ces 9 manques.

## Comment je vérifie
1. **Compilation des tests** :
   ```bash
   dotnet build Easymakemoney.Tests/Easymakemoney.Tests.csproj
   ```
2. **Exécution des tests unitaires** :
   ```bash
   dotnet test Easymakemoney.Tests
   ```
   *Attendu* : les 223 tests existants + tous les nouveaux tests passent à 100% (0 échec, 0 ignoré sauf bug applicatif avéré qui sera alors marqué en `Skip = "BUG: ..."`).
3. **Rebuild Android (`-t:Rebuild`)** :
   ```bash
   dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -t:Rebuild
   ```
   *Attendu* : 0 erreur, comparaison avec la référence de `main` (**325 avertissements**).
4. **Contrôle d'encodage et BOM contre `main`** :
   ```bash
   for f in $(git diff --name-only main); do a=$(git show main:"$f" 2>/dev/null | file -b -); b=$(file -b "$f" 2>/dev/null); [ -e "$f" ] && [ "$a" != "$b" ] && echo "DIFF $f: '$a' vs '$b'"; done; echo fin
   ```
   *Attendu* : affichage de `fin` seul.

## Risques et points incertains
- Dans `CollectionCloseService`, l'échec 409 propage l'`ApiException` ; nos tests valident ce comportement actuel sans modifier le code de l'application.
- `InvoiceLineItemViewModel` : instanciation avec doublure `FakeInvoiceLineHost`, sans appeler `SetPhoto`.
- Tests de devises : sous `[Collection("SiteCurrency")]` avec culture fixée et rétablie, assertions sur les symboles sans comparaison exacte de chaîne.

## Questions pour Claude ou le propriétaire
- Aucune question bloquante : les comportements et règles sont clairs et définis.

