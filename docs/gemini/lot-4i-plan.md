# Lot 4i : plan de Gemini

## Ce que j'ai lu
- `docs/gemini/lot-4i-consigne.md:1-53` : consigne du lot 4i pour les tests des ViewModels des promotions d'article (`PromotionCreateViewModel`, `PromotionListViewModel`) et des changements de prix (`PriceChangeViewModel`, `PriceBatchListViewModel`), avec création des faux `FakePromotionService` et `FakePriceService`.
- `GEMINI.md:1-73` : règles de travail, boîte aux lettres (§ 1 bis) et file d'attente (§ 1 ter).
- `docs/gemini/boite-aux-lettres.md:1-35` : message 3 demandant d'écrire le plan du lot 4i sans code ni commit sur la branche actuelle, puis de mettre à jour l'accusé de réception.
- `Easymakemoney.Tests/Models/PromotionTests.cs:1-153` : tests existants sur `PromotionPreview` (`WritableCount`), lignes d'aperçu, sérialisation de `PromotionRequest`, routes HTTP de `PromotionService`, et modèle `PromotionItem` (`CanEnd`, `FromAdmin`, `DatesTextIn`).
- `Easymakemoney.Tests/Models/PriceChangeTests.cs:1-119` : tests existants sur `PricePreview` (`WritableCount`), lignes d'aperçu, sérialisation de `PriceChangeRequest`, routes HTTP de `PriceService`, et modèle `PriceBatch` (`CanUndo`, `StatusText`).
- `Easymakemoney/ViewModels/Lists/PromotionViewModels.cs:1-331` :
  - `PromotionCreateViewModel` (lignes 12-233) :
    - Initialisation `Init(adviceId, commandeId, productId, percent)` (lignes 83-97). `CanChooseVariant` n'est vrai que si `ProductId > 0 && AdviceId <= 0 && CommandeId <= 0`.
    - Commande `OpenVariantPromotion` (lignes 103-108).
    - Validation `BuildRequest` (lignes 139-158) : remise entre 5 et 90 %, exclusion mutuelle des portées (`AdviceId` prime sur `CommandeId`, qui prime sur `ProductId`).
    - Réactions de saisie : `OnStartDateChanged` (lignes 110-117) ajuste `EndDate`, `ClearPreview()` réinitialise l'aperçu si remise, dates ou arrondi changent (lignes 119-137).
    - `ComputePreview()` (lignes 160-194) : affiche `ShowInfoAsync(error)` si remise invalide, sinon appelle `_promotionService.PreviewAsync(request)`.
    - `Apply()` (lignes 197-232) : bloqué si pas de hash, pas de requête, `!CanApply` (`WritableCount == 0`) ou `IsBusy`. Confirmation obligatoire. Envoi de l'empreinte et des autorisations `AllowBelowCost`, `AllowBigChange`, `AllowReplace`. Interception de 409 `PRICES_CHANGED` (message d'information et nouvel aperçu automatique, sans `ShowErrorAsync`).
  - `PromotionListViewModel` (lignes 236-331) :
    - `LoadAsync()` (lignes 252-275) : filtre les promotions `status is "active" or "planned"`, met à jour `IsEmpty`, gère les erreurs via `ShowErrorAsync`.
    - `End(promotion)` (lignes 277-322) : ne fait rien si `batchId == null`. Confirmation différente si `planned` (« Cette promotion n'a pas commencé... ») ou `active` (« La promotion s'arrête maintenant... »). Appel `EndAsync(batchId)`. Interception de 409 `PROMOTION_EDITED_SINCE` avec demande de confirmation pour terminer sur les autres articles via `EndAsync(batchId, onlyUntouched: true)` sans `ShowErrorAsync`.
    - `ShowAdminInfo(promotion)` (lignes 324-330) : affiche message d'info si `FromAdmin == true`.
- `Easymakemoney/ViewModels/Lists/PriceViewModels.cs:1-331` :
  - `PriceChangeViewModel` (lignes 12-233) :
    - Modes `percent` et `coefficient` : `ValueLabel` et `ValuePlaceholder` adaptés (lignes 63-65).
    - Initialisation `Init` (lignes 89-113) : selon article seul, conseil ou commande.
    - Validation `BuildRequest` (lignes 115-133) : message d'erreur adapté au mode (`coefficient` vs `percent`).
    - Réactions de saisie : modification de valeur, mode ou arrondi efface l'aperçu (lignes 177-191).
    - `ComputePreview()` (lignes 135-162) : message d'info sans requête si valeur invalide, sinon appel `PreviewAsync`.
    - `Apply()` (lignes 193-232) : confirmation obligatoire. Appel `ApplyAsync(request, hash, AllowBelowCost, AllowBigChange)`. Interception 409 `PRICES_CHANGED` avec message d'info et nouvel aperçu sans `ShowErrorAsync`. Autre erreur -> `ShowErrorAsync`.
  - `PriceBatchListViewModel` (lignes 236-331) :
    - `LoadAsync()` (lignes 252-275) : charge `GetBatchesAsync()`, met à jour `IsEmpty`, erreur via `ShowErrorAsync`.
    - `Undo(batch)` (lignes 277-322) : bloqué si `batch == null` ou `!batch.CanUndo`. Confirmation obligatoire. Appel `UndoAsync(batch.id)`. Interception 409 `PRICE_EDITED_SINCE` avec confirmation partielle pour annuler le reste via `UndoAsync(batch.id, onlyUntouched: true)` sans `ShowErrorAsync`.
- `Easymakemoney/Services/IPromotionService.cs:1-73` et `Easymakemoney/Services/IPriceService.cs:1-65` : contrats des services.
- `Easymakemoney.Tests/Fakes/FakeDialogService.cs:1-30` : possède déjà `ConfirmCalls`, `ShownInfos`, `ShownErrors`.

## Prérequis à installer ou à vérifier (commandes exactes, rien n'est lancé)
- Vérifier le statut de travail :
  `git status` (branche courante `feature/lot-4h-tests` en attente, working tree avec accuse-reception).
- Vérifier le nombre de tests existants sur le dépôt :
  `dotnet test Easymakemoney.Tests` -> **732 tests passants** (0 failed, 0 skipped).
- Vérifier la référence des avertissements Android sur main :
  `git log main --oneline | grep -i avertissements` -> **216 avertissements** (commit `761e026`).

## Étapes

### 1. Faux de test dans `Easymakemoney.Tests/Fakes/`
1. Créer `Easymakemoney.Tests/Fakes/FakePromotionService.cs` implémentant `IPromotionService` :
   - Enregistrement des appels :
     - `PreviewCalls : List<PromotionRequest>`
     - `ApplyCalls : List<(PromotionRequest Request, string PreviewHash, bool AllowBelowCost, bool AllowBigChange, bool AllowReplace)>`
     - `GetPromotionsCalls : List<string>`
     - `EndCalls : List<(int BatchId, bool OnlyUntouched)>`
   - Retours configurables : `PreviewResult`, `ApplyResult`, `PromotionsResult`, `EndResult`.
   - Exceptions configurables : `PreviewException`, `ApplyException`, `GetPromotionsException`, `EndException`.
2. Créer `Easymakemoney.Tests/Fakes/FakePriceService.cs` implémentant `IPriceService` :
   - Enregistrement des appels :
     - `PreviewCalls : List<PriceChangeRequest>`
     - `ApplyCalls : List<(PriceChangeRequest Request, string PreviewHash, bool AllowBelowCost, bool AllowBigChange)>`
     - `GetBatchesCalls : List<int>`
     - `UndoCalls : List<(int BatchId, bool OnlyUntouched)>`
   - Retours configurables : `PreviewResult`, `ApplyResult`, `BatchesResult`, `UndoResult`.
   - Exceptions configurables : `PreviewException`, `ApplyException`, `GetBatchesException`, `UndoException`.

### 2. Tests de `PromotionCreateViewModel` et `PromotionListViewModel` (`Easymakemoney.Tests/ViewModels/PromotionViewModelsTests.cs`)
Classe sous `[Collection("SiteCurrency")]` avec culture `fr-CA`. Dates relatives à `DateTime.Today`.

Tests prévus et ce qu'ils ajoutent par rapport à `PromotionTests.cs` :
1. `PromotionCreate_Init_FromProduct_Advice_OrCommande_SetsScopeAndCanChooseVariant` :
   - *Ce qu'il ajoute* : vérifie que `Init` affecte le bon `ScopeText`, configure `CanChooseVariant == true` uniquement pour un article seul (`ProductId > 0`), et faux pour un conseil ou une commande. (`PromotionTests.cs` ne testait aucun ViewModel de création).
2. `PromotionCreate_ComputePreview_InvalidPercent_ShowsInfoWithoutRequest` (Theory) :
   - *Ce qu'il ajoute* : vérifie qu'un pourcentage vide, non décimal, < 5 ou > 90 affiche le message « La remise doit être entre 5 et 90 %. », sans émettre d'appel à `PreviewAsync`.
3. `PromotionCreate_ComputePreview_ValidRequest_SendsCorrectScopeAndDates` :
   - *Ce qu'il ajoute* : vérifie la transmission de la requête d'aperçu complète, l'alimentation de `Rows`, et la réinitialisation des autorisations `AllowBelowCost = AllowBigChange = AllowReplace = false`.
4. `PromotionCreate_InputsChange_ClearsPreview` :
   - *Ce qu'il ajoute* : vérifie que changer `PercentText`, `StartDate`, `EndDate` ou `SelectedRounding` efface l'aperçu (`Preview == null`, `Rows.Clear()`, `CanApply == false`).
5. `PromotionCreate_Apply_WithoutApplicablePreview_DoesNothing` :
   - *Ce qu'il ajoute* : vérifie que si `CanApply == false` (`WritableCount == 0` en présence d'alertes bloquantes non autorisées), aucune confirmation n'est demandée et `ApplyAsync` n'est pas appelé.
6. `PromotionCreate_Apply_ConfirmationRefused_DoesNotSendRequest` :
   - *Ce qu'il ajoute* : vérifie que si le gérant refuse la confirmation, `ApplyAsync` n'est pas appelé.
7. `PromotionCreate_Apply_Confirmed_SendsHashAndAgreements_ShowsConfirmation` :
   - *Ce qu'il ajoute* : vérifie que lors d'une application confirmée, `ApplyAsync` reçoit l'empreinte, le même corps de requête et les 3 drapeaux d'autorisation cochés, et affiche « Promotion créée sur X article(s). ».
8. `PromotionCreate_Apply_409PricesChanged_ShowsInfoAndRecomputesPreview` :
   - *Ce qu'il ajoute* : vérifie que le code 409 `PRICES_CHANGED` affiche « Des prix ont changé depuis l'aperçu. Vérifiez le nouvel aperçu. » sans erreur, et relance automatiquement `ComputePreview()`.
9. `PromotionCreate_Apply_OtherException_ShowsErrorDialogOnce` :
   - *Ce qu'il ajoute* : vérifie qu'une exception non gérée passe par `ShowErrorAsync` exactement une fois.
10. `PromotionCreate_OpenVariantPromotion_WithoutShell_ExecutesSafely` :
    - *Ce qu'il ajoute* : teste l'exécution sécurisée de `OpenVariantPromotionCommand` sans Shell (garde `Shell.Current is not { } shell`).
11. `PromotionList_LoadAsync_FiltersActiveAndPlanned_AndHandlesError` :
    - *Ce qu'il ajoute* : vérifie que `LoadAsync` n'ajoute que les promotions `active` et `planned` (exclut `ended`), positionne `IsEmpty`, et appelle `ShowErrorAsync` en cas d'échec. (`PromotionTests.cs` testait uniquement `Item_EndableOnlyWhenCreatedFromApp`).
12. `PromotionList_End_ConfirmationRefused_DoesNotSendRequest` :
    - *Ce qu'il ajoute* : vérifie que le refus de confirmation n'émet aucun appel `EndAsync`.
13. `PromotionList_End_Confirmed_DifferentiatesPlannedAndActiveMessages_AndReloads` :
    - *Ce qu'il ajoute* : vérifie la distinction du message de confirmation pour une promotion `planned` (« Cette promotion n'a pas commencé : elle est annulée... ») vs `active` (« La promotion s'arrête maintenant... »), l'appel à `EndAsync(batchId)` et le rechargement de la liste.
14. `PromotionList_End_409PromotionEditedSince_PromptsPartialEndAndRetriesUntouched` :
    - *Ce qu'il ajoute* : vérifie que le code 409 `PROMOTION_EDITED_SINCE` déclenche une seconde confirmation (« Articles retouchés depuis... »), puis appelle `EndAsync(batchId, onlyUntouched: true)` sans `ShowErrorAsync`.
15. `PromotionList_ShowAdminInfo_DisplaysInfoForAdminPromotions` :
    - *Ce qu'il ajoute* : vérifie que pour une promotion venant de l'administration (`FromAdmin == true`, `batchId == null`), `ShowAdminInfoCommand` affiche le dialogue informatif « Cette promotion a été saisie dans l'administration du site : modifiez-la là-bas. ».

### 3. Tests de `PriceChangeViewModel` et `PriceBatchListViewModel` (`Easymakemoney.Tests/ViewModels/PriceViewModelsTests.cs`)
Classe sous `[Collection("SiteCurrency")]` avec culture `fr-CA`. Dates relatives à `DateTime.Today`.

Tests prévus et ce qu'ils ajoutent par rapport à `PriceChangeTests.cs` :
1. `PriceChange_Modes_SetsLabelsAndPlaceholders` :
   - *Ce qu'il ajoute* : vérifie que basculer entre le mode `percent` et `coefficient` adapte dynamiquement `ValueLabel` et `ValuePlaceholder`. (`PriceChangeTests.cs` ne testait aucun ViewModel).
2. `PriceChange_ComputePreview_InvalidValue_ShowsModeSpecificMessageWithoutRequest` :
   - *Ce qu'il ajoute* : vérifie qu'une saisie non numérique affiche « Saisissez un pourcentage (par exemple 5 ou -10). » en mode pourcentage et « Saisissez un coefficient (par exemple 2,2). » en mode coefficient, sans appel à `PreviewAsync`.
3. `PriceChange_ComputePreview_SendsCorrectScopeModeAndRounding` :
   - *Ce qu'il ajoute* : vérifie la transmission exacte du mode, de la valeur, de l'arrondi et de la portée unique (`ProductIds`, `CommandeId`, `AdviceId`), ainsi que l'alimentation de `Rows`.
4. `PriceChange_InputsChange_ClearsPreview` :
   - *Ce qu'il ajoute* : vérifie que changer `ValueText`, `SelectedMode` ou `SelectedRounding` efface l'aperçu (`Preview == null`, `Rows.Clear()`, `CanApply == false`).
5. `PriceChange_Apply_ConfirmationRefused_DoesNotSendRequest` :
   - *Ce qu'il ajoute* : vérifie que le refus de la confirmation n'émet aucun appel `ApplyAsync`.
6. `PriceChange_Apply_Confirmed_SendsHashAndAgreements_ShowsConfirmation` :
   - *Ce qu'il ajoute* : vérifie la transmission de l'empreinte et des autorisations `AllowBelowCost` et `AllowBigChange`, et l'affichage de « X prix changé(s). Annulation possible pendant 24 h. ».
7. `PriceChange_Apply_409PricesChanged_ShowsInfoAndRecomputesPreview` :
   - *Ce qu'il ajoute* : vérifie que le code 409 `PRICES_CHANGED` affiche « Des prix ont changé depuis l'aperçu. Vérifiez le nouvel aperçu avant d'appliquer. » sans erreur, et relance automatiquement `ComputePreview()`.
8. `PriceChange_Apply_OtherException_ShowsErrorDialogOnce` :
   - *Ce qu'il ajoute* : vérifie qu'une exception non gérée appelle `ShowErrorAsync` une seule fois.
9. `PriceBatchList_LoadAsync_PopulatesBatchesAndHandlesError` :
   - *Ce qu'il ajoute* : vérifie le chargement des lots, la mise à jour de `IsEmpty`, et la gestion d'erreur via `ShowErrorAsync`. (`PriceChangeTests.cs` testait uniquement les propriétés du modèle `PriceBatch`).
10. `PriceBatchList_Undo_CannotUndo_DoesNothing` :
    - *Ce qu'il ajoute* : vérifie que si `batch == null` ou `!batch.CanUndo`, aucune confirmation n'est demandée et `UndoAsync` n'est pas appelé.
11. `PriceBatchList_Undo_ConfirmationRefused_DoesNotSendRequest` :
    - *Ce qu'il ajoute* : vérifie que le refus de confirmation n'émet aucun appel `UndoAsync`.
12. `PriceBatchList_Undo_Confirmed_CallsUndoAndReloads` :
    - *Ce qu'il ajoute* : vérifie l'appel `UndoAsync(batch.id)`, l'affichage du message avec le nombre de prix restaurés (et mention du conseil si `adviceRestored == true`), puis le rechargement de la liste.
13. `PriceBatchList_Undo_409PriceEditedSince_PromptsPartialUndoAndRetriesUntouched` :
    - *Ce qu'il ajoute* : vérifie que le code 409 `PRICE_EDITED_SINCE` affiche la confirmation « Prix retouchés depuis... » puis appelle `UndoAsync(batch.id, onlyUntouched: true)` sans `ShowErrorAsync`.

## Comment je vérifie
1. Tests unitaires :
   `dotnet test Easymakemoney.Tests`
   Résultat attendu : 100 % des tests passants (732 existants + les 28 nouveaux tests du lot 4i).
2. Rebuild Android :
   `dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home`
   Résultat attendu : 0 erreur, référence stricte de 216 avertissements.
3. Contrôle d'encodage contre main :
   `git diff --check main`
   Résultat attendu : vide, aucune anomalie d'espace ou d'encodage.

## Risques et points incertains
- Absence de `Shell.Current` dans l'environnement xUnit : `OpenVariantPromotion` et les retours arrière (`GoToAsync("..")`) sont protégés par `if (Shell.Current is { } shell)`, ce qui garantit l'absence de plantage. La navigation sera testée au niveau de la méthode de commande sans Shell et signalée comme d'usage.
- Tous les faux seront strictement confinés dans `Easymakemoney.Tests/Fakes/`, aucun fichier applicatif sous `Easymakemoney/` ne sera modifié.

## Questions pour Claude ou le propriétaire
Aucune question bloquante : tous les comportements spécifiés dans la consigne correspondent exactement au code existant des ViewModels.

