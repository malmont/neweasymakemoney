# Lot 4e : plan de Gemini

## Ce que j'ai lu

1. `docs/gemini/lot-4e-consigne.md:1-32` : consigne du lot 4e, les 8 sujets à tester, la référence (443 tests sur `main`, 325 avertissements Android), l'obligation de ne pas re-tester ce qui est déjà couvert, et les règles strictes (aucun fichier sous `Easymakemoney/`, aucun XAML, aucun appel backend réel, pas de commit, `SiteCurrency` sous `[Collection("SiteCurrency")]`).
2. `docs/etat-fonctions-gestion.md` : contrats des fonctions de gestion (trésorerie, voyages, prix/promotions, préparation de période, lignes à recevoir).
3. `Easymakemoney/Services/HttpService.cs:90-113` : méthode `PutAsyncWithAuth<T>(string url, object data)` qui sérialise en JSON, appelle `_httpClient.PutAsync`, transforme les erreurs réseau en `ApiException.NoConnection`, et délègue la gestion de réponse à `HandleResponseAsync<T>` (lignes 244-268).
4. `Easymakemoney/ViewModels/Lists/PeriodSimulationViewModel.cs:225`, `PreparationViewModel.cs:311,355`, `PromotionViewModels.cs:205,284`, `PriceViewModels.cs:216,295` : gestion explicite des erreurs métier 409 et 429 par code d'erreur (`PRICES_CHANGED`, `PROMOTION_EDITED_SINCE`, `PRICE_EDITED_SINCE`, `COMMENT_LIMIT`, `TRIP_OVERLAP`).
5. `Easymakemoney/Services/ITreasuryService.cs:1-30` et `TreasuryService.cs:1-51` : méthodes de consultation et mise à jour (`GetOverviewAsync`, `GetBalancesAsync`, `DeleteBalanceAsync`, `GetMovementsAsync`). `Easymakemoney.Tests/Models/TreasuryTests.cs:110-132` teste déjà l'écriture (`AddBalanceAsync`, `AddMovementAsync`, `UpdateSettingsAsync`, `DeleteMovementAsync`), mais pas les lectures ni la suppression de solde.
6. `Easymakemoney/Services/ITripService.cs:1-48` : `GetTripsAsync(page, limit)`, `UpdateAsync(tripId, request)` (PATCH avec corps complet), `DeleteAsync(tripId)`. `Easymakemoney.Tests/Models/TripTests.cs:44-67` teste déjà `CreateAsync`, `GetFollowUpAsync`, `AttachNotesAsync`, `AttachCommandesAsync`, mais pas `GetTripsAsync`, `UpdateAsync`, `DeleteAsync`.
7. `Easymakemoney/Services/IPriceService.cs:1-65` et `IPromotionService.cs:1-73` : `PreviewAsync` (périmètre unique) et `PromotionService.ApplyAsync` (previewHash et 3 accords). `Easymakemoney.Tests/Models/PriceChangeTests.cs:68-84` et `PromotionTests.cs:59-69` ne testent actuellement ni `PreviewAsync` sur les services, ni `PromotionService.ApplyAsync`.
8. `Easymakemoney/Services/IPreparationService.cs:1-37` et `Easymakemoney/Models/PeriodPreparation.cs:158-175` : `SimulateAsync` et `AdviseAsync` (avec route `locale=fr` et méthode `.Prepare()` qui marque le candidat recommandé `IsRecommended = true`). `Easymakemoney.Tests/Models/PreparationTests.cs:64-78` teste déjà `GetEstimatesAsync` et `ValidateAsync`, mais pas `SimulateAsync` ni `AdviseAsync`.
9. `Easymakemoney/Services/IToReceiveService.cs:1-21` et `Easymakemoney.Tests/Models/ToReceiveTests.cs:39-46` : route `commandes/lignes/a-recevoir?page=1&limit=200`. Il manque la validation de lecture d'une réponse vide `[]` via `HttpService`.
10. Données tolérantes :
    - `Easymakemoney/Models/PeriodPreparation.cs:8-26` : `PreparationValues.Cents(JToken? token, params string[] keys)` pour nombres, objets, null, et chaînes textuelles.
    - `Easymakemoney/Models/Treasury.cs:7-83` : `TreasuryOverview` avec séries nulles (`AlertSeries == null`).
    - `Easymakemoney/Models/PeriodSimulation.cs:40-59` : `PeriodSimulation.Rows()` avec `@base` et `simulated` nuls ou vides.
    - `Easymakemoney/Models/Promotion.cs:118,131-134` : `PromotionItem.DatesText` fait appel à une méthode privée `Local(string? iso)` qui exécute `date.ToLocalTime().ToString(...)`. `ToLocalTime()` dépend du fuseau horaire de la machine (`TimeZoneInfo.Local`) qui ne peut pas être modifié sans changer le code de l'application (interdit).

---

## Prérequis à installer ou à vérifier (commandes exactes, rien n'est lancé)

- Aucun paquet ni workload à installer.
- Vérification de la branche courante :
  ```bash
  git status
  ```
  (branche `feature/lot-4e-tests`, créée depuis `main`, working tree propre).
- Référence des tests existants : 443 tests passants sur `main`.
- Référence des avertissements Android : 325-326 avertissements.

---

## Étapes

Pour chaque sujet de la consigne, nous distinguons expressément ce qui existe déjà et ce qui est ajouté, sans modifier aucun fichier sous `Easymakemoney/`.

### 1. Sujet 1 : `HttpService.PutAsyncWithAuth`
- **Ce qui existe déjà** : dans `Easymakemoney.Tests/Services/HttpServiceTests.cs`, tests pour `GetAsync`, `PostAsyncWithAuth`, `PatchAsyncWithAuth`, `PostFilesAsync`, et coupures réseau sur GET / PATCH / PostFiles. Aucun test sur `PutAsyncWithAuth`.
- **Ce que j'ajoute** dans `Easymakemoney.Tests/Services/HttpServiceTests.cs` :
  - `PutAsyncWithAuth_SendsPutMethodWithJsonBodyAndReturnsDeserializedObject` : vérifie avec `TestHttpMessageHandler` que la méthode HTTP envoyée est `HttpMethod.Put`, que le corps est sérialisé en JSON UTF-8 (`application/json`), et que la réponse JSON est correctement désérialisée en objet.
  - `PutAsyncWithAuth_409Conflict_ThrowsApiExceptionWithParsedCode` : vérifie qu'une réponse 409 Conflict contenant `{"error":"PRICE_EDITED_SINCE"}` lève une `ApiException` avec `StatusCode == HttpStatusCode.Conflict` et `ApiError.Code == "PRICE_EDITED_SINCE"`.
  - `PutAsyncWithAuth_422ValidationError_ThrowsApiExceptionWithParsedFields` : vérifie qu'une réponse 422 UnprocessableEntity contenant `{"error":"VALIDATION_FAILED","fields":{"threshold":"MUST_BE_INTEGER_CENTS"}}` lève une `ApiException` avec `ApiError.Code == "VALIDATION_FAILED"` et le champ mappé dans `ApiError.Errors`.
  - `PutAsyncWithAuth_NetworkFailure_ThrowsApiExceptionWithNoConnection` : vérifie qu'une `HttpRequestException` sur PUT lève `ApiException` avec `ApiError.Code == "NO_CONNECTION"`.
  - `PutAsyncWithAuth_Timeout_ThrowsApiExceptionWithNoConnection` : vérifie qu'une `TaskCanceledException` (délai dépassé) sur PUT lève `ApiException` avec `ApiError.Code == "NO_CONNECTION"`.

### 2. Sujet 2 : Erreurs métier attendues par les écrans
- **Ce qui existe déjà** : `Easymakemoney.Tests/Models/ApiErrorTests.cs` teste `ApiError.FromHttpResponse` sur certains codes isolés.
- **Ce que j'ajoute** dans `Easymakemoney.Tests/Services/HttpServiceTests.cs` :
  - Tests d'intégration avec le **vrai** `HttpService` et `TestHttpMessageHandler` vérifiant que l'`ApiException` propagée porte exactement le code `ApiError.Code` que les ViewModels capturent :
    - `409 PRICES_CHANGED` (attendu par `PreparationViewModel`, `PromotionViewModels`, `PriceViewModels`).
    - `409 PROMOTION_EDITED_SINCE` (attendu par `PromotionViewModels`).
    - `409 PRICE_EDITED_SINCE` (attendu par `PriceViewModels`).
    - `429 COMMENT_LIMIT` (attendu par `PeriodSimulationViewModel` et `PreparationViewModel`).
    - `409 TRIP_OVERLAP` (attendu lors de la gestion des voyages).

### 3. Sujet 3 : `TreasuryService`
- **Ce qui existe déjà** : `Easymakemoney.Tests/Models/TreasuryTests.cs:110` teste `AddBalanceAsync`, `AddMovementAsync`, `UpdateSettingsAsync`, `DeleteMovementAsync`.
- **Ce que j'ajoute** dans `Easymakemoney.Tests/Models/TreasuryTests.cs` :
  - `Service_ReadAndQueryRoutes_MatchContract` :
    - `GetOverviewAsync(days: 30)` : vérifie l'appel GET vers `gestion/tresorerie?days=30&locale=fr` (et par défaut avec `days=90` : `gestion/tresorerie?days=90&locale=fr`).
    - `GetBalancesAsync(limit: 5)` : vérifie l'appel GET vers `gestion/tresorerie/soldes?limit=5` (et par défaut `limit=10`).
    - `DeleteBalanceAsync(42)` : vérifie l'appel DELETE vers `gestion/tresorerie/soldes/42`.
    - `GetMovementsAsync()` : vérifie l'appel GET vers `gestion/tresorerie/mouvements?locale=fr`.

### 4. Sujet 4 : `TripService`
- **Ce qui existe déjà** : `Easymakemoney.Tests/Models/TripTests.cs:44-117` teste `CreateAsync`, `GetFollowUpAsync`, `AttachNotesAsync`, `AttachCommandesAsync`.
- **Ce que j'ajoute** dans `Easymakemoney.Tests/Models/TripTests.cs` :
  - `Service_ListUpdateAndDelete_MatchContract` :
    - `GetTripsAsync(page: 2, limit: 15)` : vérifie l'appel GET vers `gestion/voyages?page=2&limit=15` (et valeurs par défaut `page=1&limit=30`).
    - `UpdateAsync(7, request)` : vérifie l'appel PATCH vers `gestion/voyages/7` avec le corps complet `TripRequest` sérialisé (champs `name`, `startDate`, `endDate`, `budgetTotal`, `budgetDaily`, `place`, `notes`, etc.).
    - `DeleteAsync(7)` : vérifie l'appel DELETE vers `gestion/voyages/7`.

### 5. Sujet 5 : `PriceService` et `PromotionService`
- **Ce qui existe déjà** : `PriceChangeTests.cs` teste `PriceChangeRequest.ToBody()`, `PriceService.ApplyAsync` et `PriceService.UndoAsync`. `PromotionTests.cs` teste `PromotionRequest.ToBody()`, `PromotionService.EndAsync` et `PromotionService.GetPromotionsAsync`.
- **Ce que j'ajoute** :
  - Dans `Easymakemoney.Tests/Models/PriceChangeTests.cs` :
    - `PriceService_PreviewAsync_SendsPostWithSingleScope` : vérifie que `PreviewAsync` envoie un POST vers `gestion/prix/apercu` avec le corps JSON portant un seul périmètre (ex. `commandeId: 3`, sans les autres périmètres).
  - Dans `Easymakemoney.Tests/Models/PromotionTests.cs` :
    - `PromotionService_PreviewAsync_SendsPostWithSingleScopeAndIsoDates` : vérifie que `PreviewAsync` envoie un POST vers `gestion/promotions/apercu` avec le périmètre unique (ex. `adviceId: 5`), dates ISO et pourcentage.
    - `PromotionService_ApplyAsync_SendsPreviewHashAndThreeAgreements` : vérifie que `ApplyAsync(request, previewHash, allowBelowCost, allowBigChange, allowReplace)` envoie un POST vers `gestion/promotions/appliquer` avec `previewHash`, `allowBelowCost`, `allowBigChange`, `allowReplace`, et les propriétés de la promotion.

### 6. Sujet 6 : `PreparationService`
- **Ce qui existe déjà** : `Easymakemoney.Tests/Models/PreparationTests.cs:64-78` teste `GetEstimatesAsync` et `ValidateAsync`.
- **Ce que j'ajoute** dans `Easymakemoney.Tests/Models/PreparationTests.cs` :
  - `PreparationService_SimulateAsync_SendsPostToContractRoute` : vérifie l'appel POST vers `gestion/preparation/simulation` avec le corps `PreparationRequest.ToBody()`.
  - `PreparationService_AdviseAsync_CallsRouteWithLocaleFr_AndPreparesAdvice` : vérifie l'appel POST vers `gestion/preparation/avis?locale=fr` avec le corps `ToBody()`, et vérifie que `.Prepare()` a bien marqué le candidat recommandé (`IsRecommended == true` pour la clé correspondant à `comment.recommended`, `false` pour les autres) et renseigné `candidate.Currency`.

### 7. Sujet 7 : `ToReceiveService`, `IToReceiveService`
- **Ce qui existe déjà** : `Easymakemoney.Tests/Models/ToReceiveTests.cs:39` vérifie la route `commandes/lignes/a-recevoir?page=1&limit=200` avec `FakeHttpService`.
- **Ce que j'ajoute** dans `Easymakemoney.Tests/Models/ToReceiveTests.cs` :
  - `ToReceiveService_EmptyApiResponse_ReturnsEmptyListAndBuildsEmptyGroups` : avec le vrai `HttpService` et un `TestHttpMessageHandler` renvoyant 200 OK avec le corps JSON `[]` :
    - `GetLinesAsync()` retourne une liste vide non-nulle (`Assert.NotNull(lines); Assert.Empty(lines);`).
    - `ToReceiveGroup.Build(lines)` retourne une liste vide de groupes sans exception (`Assert.Empty(groups);`).

### 8. Sujet 8 : Données tolérantes
- **Ce qui existe déjà** : `PreparationTests.Cents_ReadsNumberOrObject` teste les nombres entiers et objets basiques. `TreasuryTests.WithoutBalance_StatusAsksForOne` teste une absence de solde.
- **Ce que j'ajoute** :
  - Dans `Easymakemoney.Tests/Models/PreparationTests.cs` :
    - `Cents_ToleratesInvalidTextAndMalformedTokens` : vérifie que `PreparationValues.Cents` renvoie `null` lorsqu'on lui passe du texte (`JToken.Parse("\"1500\"")`, `new JValue("invalid")`), un objet sans montant valide, ou un tableau (`JArray`).
  - Dans `Easymakemoney.Tests/Models/TreasuryTests.cs` :
    - `Overview_WithNullSeries_DoesNotThrowAndReturnsFallbacks` : teste un `TreasuryOverview` où `balance = null`, `projection = null`, `projectionWithSales = null`, `alertSeries = null` :
      - `AlertSeries` est null
      - `Low14Text`, `LowText`, `EndText` renvoient tous `" — "`
      - `HasBelowThreshold` vaut `false`, `BelowThresholdText` est vide (`""`)
      - `UpcomingDays(DateTime.Today)` renvoie une liste vide `[]` sans lever de `NullReferenceException`.
  - Dans `Easymakemoney.Tests/Models/PeriodSimulationTests.cs` :
    - `Rows_WithEmptyOrNullScenarios_GeneratesAllRowsWithDashesWithoutThrowing` : instancie `PeriodSimulation` avec `@base = null` et `simulated = null` :
      - `Rows()` génère les 9 lignes complètes sans crash
      - chaque ligne a `BaseText == " — "`, `SimulatedText == " — "`, `DeltaText == ""`, `Tone == 0`
      - `Help` est renseigné pour chaque ligne.
  - **Signalement sur `PromotionItem.DatesText`** :
    - Conformément à la consigne (*« si ce n'est pas possible sans toucher au code, signale-le au lieu de tester »*), `PromotionItem.DatesText` (dans `Easymakemoney/Models/Promotion.cs:118`) appelle la méthode privée `Local(string? iso)` qui exécute en dur `date.ToLocalTime().ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)`.
    - `date.ToLocalTime()` dépend de `TimeZoneInfo.Local` de la machine hôte. En C# / .NET standard, `TimeZoneInfo.Local` n'a pas de setter public et dépend de la configuration de l'OS machine.
    - Il n'est donc pas possible de fixer le fuseau horaire de façon portable et isolée dans un test xUnit sans modifier le code de `PromotionItem` (par exemple en lui injectant un fuseau ou en appelant une surcharge `DatesTextFor(TimeZoneInfo tz)` comme cela avait été fait pour `Campaign.DatesTextFor(CultureInfo)`).
    - Comme la règle absolue interdit de modifier tout fichier sous `Easymakemoney/`, ce point est expressément **signalé** ici au lieu d'écrire un test instable dépendant du fuseau de la machine d'exécution.

---

## Comment je vérifie

1. **Exécution de la suite de tests xUnit** :
   ```bash
   dotnet test Easymakemoney.Tests
   ```
   Résultat attendu : l'ensemble des 443 tests existants + tous les nouveaux tests du lot 4e passent avec succès (0 échec, 0 skip hors éventuel bug signalé).
2. **Rebuild Android** :
   ```bash
   dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -t:Rebuild
   ```
   Résultat attendu : compilation réussie, nombre d'avertissements comparé à la référence (~325-326). La ligne des avertissements sera collée brute dans le compte rendu.
3. **Contrôle d'encodage contre `main`** :
   Vérification que tous les fichiers modifiés et ajoutés sont en UTF-8 sans BOM et conformes aux fichiers de `main`.

---

## Risques et points incertains

- **Isolation de `SiteCurrency`** : toutes les classes de test manipulant des devises doivent comporter `[Collection("SiteCurrency")]`, appeler `SiteCurrency.Reset()` à l'initialisation et au Dispose, et sauvegarder/restaurer `CultureInfo.CurrentCulture`.
- **Formatage monétaire** : aucune comparaison stricte de chaînes monétaires complètes (règle du projet : tester la présence du symbole monétaire et des chiffres attendus sans présumer des espaces insécables ou particularités locales).
- **Intégrité de l'application** : interdiction absolue de modifier le moindre fichier sous `Easymakemoney/` ou dans `Views/*.xaml.cs`. Seuls les fichiers de `Easymakemoney.Tests/` sont enrichis.

---

## Questions pour Claude ou le propriétaire

1. Pour `PromotionItem.DatesText`, nous avons bien confirmé qu'en l'absence de modification de `PromotionItem.cs` (qui utilise `ToLocalTime()` en dur dans une méthode privée statique), il n'est pas possible de fixer le fuseau horaire sans dépendre de l'OS machine. Nous appliquons donc la consigne : signalement dans le plan au lieu de tester. Claude valide-t-il ce choix ?

