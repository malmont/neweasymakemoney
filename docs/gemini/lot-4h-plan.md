# Lot 4h : plan de Gemini

## Ce que j'ai lu
- `docs/gemini/lot-4h-consigne.md:1-52` : consigne de Claude pour le lot 4h, demandant les tests de comportement des ViewModels de promotions par variante (`VariantPromotionViewModel` et `VariantPromotionListViewModel`), l'ajout d'un faux `FakeVariantPromotionService`, la référence de 712 tests et 216 avertissements Android, la précision de ce que chaque test ajoute par rapport à `VariantPromotionTests.cs`, et l'absence totale de tests supplémentaires sur les lots MOD et CHE.
- `GEMINI.md:1-73` : consignes générales et modèle de plan.
- `Easymakemoney.Tests/Models/VariantPromotionTests.cs:1-138` :
  - Tests existants sur le modèle `VariantPromotionPreview` (`WritableCount` selon les drapeaux booléens `allowBelowCost`, `allowBigChange`, `allowReplace`, `allowAboveProductPromotion`, calcul de `PeriodText`, ligne 43-56).
  - Textes d'une ligne d'aperçu `VariantPromotionRow` (`PriceText`, `MarginText`, `AlertsText`, ligne 57-68).
  - Sérialisation JSON des requêtes `VariantPromotionRequest.ToBody()` et `ToApplyBody()`, ligne 69-79.
  - Routes du service HTTP `VariantPromotionService` (`apercu`, `appliquer`, `terminer`, `gestion/promotions/variantes?status=all`, ligne 80-99).
  - Textes et fin d'un `VariantPromotionItem` (`Title`, `PriceText`, `StatusText`, `CanEnd`, ligne 100-112).
  - Validation du pourcentage et filtrage des variantes dans `vm.BuildRequest(...)`, et choix par défaut dans `VariantPromotionChoice` (ligne 113-137).
- `Easymakemoney.Tests/Models/ManagementModuleTests.cs` et `ManagementReadinessTests.cs` : couvrent les modules MOD et CHE (aucun test de plus sur ces lots).
- `Easymakemoney/ViewModels/Lists/VariantPromotionViewModels.cs:1-355` :
  - `VariantPromotionChoice` (lignes 9-21) : porte l'état sélectionné `IsSelected` pour chaque ligne.
  - `VariantPromotionViewModel` (lignes 28-273) :
    - `Init(productId, productName)` (lignes 94-98).
    - Synchronisation et effacement : `OnStartDateChanged` (ajuste `EndDate` si fin < début ou si > MaxEndDate, et appelle `ClearPreview()`), `OnEndDateChanged`, `OnPercentTextChanged`, `OnSelectedRoundingChanged` (lignes 100-112).
    - `ClearPreview()` (lignes 113-128) : remet `Preview` à null, vide `Choices`, rafraîchit les drapeaux d'alerte `HasBelowCost`, `HasBigChange`, `HasExisting`, `HasAboveProductPromotion`.
    - `ComputePreview()` (lignes 156-192) : valide via `BuildRequest(null, out var error)`, appelle `PreviewAsync(request)`, instancie les `Choices`, réinitialise les drapeaux `AllowBelowCost = AllowBigChange = AllowReplace = AllowAboveProductPromotion = false`.
    - `Apply()` (lignes 194-272) :
      - Garde-fous sans `ApplyAsync` : si `Preview.IsOff` (`enabled: false`) -> message info "Les promotions par variante ne sont pas encore activées sur ce site..." ; si aucune variante cochée -> message info "Cochez au moins une variante à remiser." ; calcule un 2e aperçu pour les variantes cochées ; si `writable == 0` ou `hash == null` -> message info "Aucune variante cochée ne peut être remisée avec ces réglages : voyez les alertes de l'aperçu." ; si confirmation refusée -> retour sans requête.
      - Confirmation acceptée -> appel `_service.ApplyAsync(request, hash, AllowBelowCost, AllowBigChange, AllowReplace, AllowAboveProductPromotion)`. Positionne `VariantPromotionListViewModel.PromotionsChanged = true`. Message de confirmation distinct selon `result.skipped.Count > 0` ("Promotion créée. X variante(s) écartée(s)...") ou nul ("Promotion créée."). Retourne via `Shell.Current.GoToAsync("..")` si Shell est présent.
      - Erreurs gérées : 409 `PRICES_CHANGED` ("Des prix ont changé depuis l'aperçu...") et 409 `NOTHING_TO_APPLY` ("Rien à appliquer : voyez les alertes de l'aperçu.") affichent un message d'info sans `ShowErrorAsync` et relancent `ComputePreview()`. Code `VARIANT_PROMOTIONS_OFF` affiche un message d'info dédié sans erreur. Autres exceptions -> `ShowErrorAsync(ex)`.
  - `VariantPromotionListViewModel` (lignes 275-355) :
    - Drapeau statique `PromotionsChanged` (ligne 283).
    - `InitializeAsync()` (lignes 296-303) : ne charge qu'une fois, sauf si `PromotionsChanged == true` ; remet alors le drapeau à `false` et relit la liste.
    - `End(item)` (lignes 331-353) : vérifie `CanEnd`, confirmation avec message adapté selon `upcoming` ou `active` et mention des variantes associées au même `batchId`, appel `EndAsync(item.batchId)` puis rechargement. Erreurs transmises à `ShowErrorAsync(ex)`.
- `Easymakemoney/Services/IVariantPromotionService.cs:1-34` : interface et service des promotions par variante.
- `Easymakemoney.Tests/Fakes/FakeDialogService.cs:1-30` : possède déjà `ConfirmCalls` (titre, message, accept, cancel).

## Prérequis à installer ou à vérifier (commandes exactes, rien n'est lancé)
- Vérifier la branche courante :
  `git status` (doit être sur `feature/lot-4h-tests`, propre).
- Vérifier le nombre de tests sur `main` :
  `dotnet test Easymakemoney.Tests` -> **712 tests passants** (0 failed, 0 skipped).
- Vérifier la référence des avertissements Android sur main :
  `git log main --oneline | grep -i avertissements` -> **216 avertissements** (commit `761e026`).

## Étapes

### 1. Faux de test dans `Easymakemoney.Tests/Fakes/`
Créer `Easymakemoney.Tests/Fakes/FakeVariantPromotionService.cs` implémentant `IVariantPromotionService` :
- Enregistrement des appels :
  - `PreviewCalls : List<VariantPromotionRequest>`
  - `ApplyCalls : List<(VariantPromotionRequest Request, string PreviewHash, bool AllowBelowCost, bool AllowBigChange, bool AllowReplace, bool AllowAboveProductPromotion)>`
  - `EndCalls : List<int>`
  - `GetPromotionsCalls : List<string>`
- Résultats configurables :
  - `PreviewResult : VariantPromotionPreview`
  - `ApplyResult : VariantPromotionApplyResult`
  - `PromotionsResult : List<VariantPromotionItem>`
- Exceptions configurables (pour tester les codes d'erreur 409 et autres) :
  - `PreviewException : Exception?`
  - `ApplyException : Exception?`
  - `EndException : Exception?`
  - `GetPromotionsException : Exception?`

### 2. Tests de `VariantPromotionViewModel` (`Easymakemoney.Tests/ViewModels/VariantPromotionViewModelTests.cs`)
Classe sous `[Collection("SiteCurrency")]` avec culture `fr-CA` et remise à zéro de `VariantPromotionListViewModel.PromotionsChanged = false` avant et après chaque test (`Dispose`).
Toutes les dates des tests (`StartDate`, `EndDate`) partiront systématiquement de `DateTime.Today`, jamais en dur.

Tests prévus et ce qu'ils ajoutent par rapport à `VariantPromotionTests.cs` :
1. `Init_SetsProductIdAndScopeText` :
   - *Ce qu'il ajoute* : vérifie que `Init(productId, productName)` initialise correctement `ProductId` et `ScopeText` contenant le nom du produit et les mentions de non-cumul. (`VariantPromotionTests.cs` ne testait que `BuildRequest`).
2. `StartDate_AdjustsEndDateIfPastOrExceedsMax` :
   - *Ce qu'il ajoute* : vérifie que lorsque `StartDate` avance au-delà de `EndDate`, `EndDate` est automatiquement repoussé à `StartDate` ; et si `EndDate` dépasse `MaxEndDate` (StartDate + 89 jours), il est ramené à `MaxEndDate`.
3. `ClearPreview_WhenInputsChange` :
   - *Ce qu'il ajoute* : vérifie que modifier `StartDate`, `EndDate`, `PercentText` ou `SelectedRounding` remet `Preview` à null, vide `Choices`, et recalcule les propriétés `HasBelowCost`, `HasBigChange`, etc. (`VariantPromotionTests.cs` ne testait aucune réaction de propriété du ViewModel).
4. `Apply_WhenPreviewIsOff_ShowsInfoWithoutRequest` :
   - *Ce qu'il ajoute* : vérifie que si `Preview.IsOff == true` (`enabled: false` dans l'aperçu), `ApplyCommand` affiche immédiatement le message « Les promotions par variante ne sont pas encore activées sur ce site... », sans faire de second aperçu ni d'appel à `ApplyAsync`.
5. `Apply_WhenNoVariantSelected_ShowsInfoWithoutRequest` :
   - *Ce qu'il ajoute* : vérifie que si toutes les variantes sont décochées (ou si les seules lignes sont bloquées sans prix `IsBlocked`), `ApplyCommand` affiche « Cochez au moins une variante à remiser. », sans appel service.
6. `Apply_WhenNoWritableVariantWithSettings_ShowsInfoWithoutApply` :
   - *Ce qu'il ajoute* : vérifie que si les variantes cochées ont des alertes bloquantes (`BELOW_COST` ou `BIG_CHANGE`) et que les drapeaux d'autorisation (`AllowBelowCost`, etc.) ne sont pas cochés (`writable == 0`), `ApplyCommand` affiche « Aucune variante cochée ne peut être remisée avec ces réglages : voyez les alertes de l'aperçu. », sans demander de confirmation et sans appeler `ApplyAsync`.
7. `Apply_ConfirmationRefused_DoesNotSendApplyRequest` :
   - *Ce qu'il ajoute* : vérifie que si le dialogue de confirmation est affiché mais refusé par le gérant (`ConfirmResult = false`), `ApplyAsync` n'est pas appelé et `PromotionsChanged` reste faux.
8. `Apply_Confirmed_SendsAllCheckedAllowFlagsAndSetsPromotionsChanged` :
   - *Ce qu'il ajoute* : vérifie que lors d'une application confirmée, `ApplyAsync` reçoit exactement le `previewHash` du second aperçu, et les 4 drapeaux `allowBelowCost`, `allowBigChange`, `allowReplace`, `allowAboveProductPromotion` tels que cochés dans le ViewModel ; vérifie que `VariantPromotionListViewModel.PromotionsChanged` passe à `true` et que le message « Promotion créée. » est affiché.
9. `Apply_Confirmed_WithSkippedVariants_ShowsDifferentConfirmationMessage` :
   - *Ce qu'il ajoute* : vérifie que lorsque le résultat de `ApplyAsync` contient des variantes écartées (`result.skipped.Count > 0`), le message affiché est « Promotion créée. X variante(s) écartée(s) : voyez leurs alertes. ».
10. `Apply_ApiErrors_409PricesChanged_And_409NothingToApply_ShowsInfoAndRecomputesPreview` (Theory) :
    - *Ce qu'il ajoute* : vérifie que pour 409 `PRICES_CHANGED` et 409 `NOTHING_TO_APPLY`, le message informatif dédié est affiché, aucun `ShowErrorAsync` n'est appelé, et un nouvel appel `ComputePreview()` est automatiquement déclenché.
11. `Apply_ApiError_VariantPromotionsOff_ShowsSpecificInfoMessage` :
    - *Ce qu'il ajoute* : vérifie que l'erreur 409 `VARIANT_PROMOTIONS_OFF` affiche « Les promotions par variante ne sont pas activées sur ce site. » sans `ShowErrorAsync` et sans relancer l'aperçu.
12. `Apply_OtherException_ShowsErrorDialogOnce` :
    - *Ce qu'il ajoute* : vérifie qu'une exception inattendue (ex. `HttpRequestException`) appelle `ShowErrorAsync` exactement une fois.

### 3. Tests de `VariantPromotionListViewModel` (`Easymakemoney.Tests/ViewModels/VariantPromotionListViewModelTests.cs`)
Classe sous `[Collection("SiteCurrency")]` avec culture `fr-CA`. Remise à faux de `VariantPromotionListViewModel.PromotionsChanged = false` avant et après chaque test (`Dispose`).
Toutes les dates de test (`endedAt`, `startDate`, `endDate`) partent de `DateTime.Today`.

Tests prévus et ce qu'ils ajoutent par rapport à `VariantPromotionTests.cs` :
1. `InitializeAsync_LoadsOnce_UnlessPromotionsChanged` :
   - *Ce qu'il ajoute* : vérifie que `InitializeAsync` appelle `GetPromotionsAsync` au premier appel, ne le rappelle pas au second appel tant que `PromotionsChanged` est faux, mais le rappelle dès que `PromotionsChanged == true` et remet alors le champ à `false`.
2. `End_CannotEnd_DoesNothing` :
   - *Ce qu'il ajoute* : vérifie que si `item.CanEnd == false` (ex: promotion terminée ou `endedAt` renseigné aujourd'hui), aucune boîte de dialogue n'est présentée et `EndAsync` n'est pas appelé.
3. `End_ConfirmationRefused_DoesNotSendRequest` :
   - *Ce qu'il ajoute* : vérifie que si la confirmation de résiliation est refusée, aucun appel à `EndAsync` n'est émis.
4. `End_Confirmed_Upcoming_DifferentiatesMessageAndCallsEnd` :
   - *Ce qu'il ajoute* : vérifie que pour une promotion à venir (`status == "upcoming"`), la confirmation mentionne « Elle n'a pas commencé : elle ne s'appliquera jamais. », appelle `EndAsync(item.batchId)` et recharge la liste.
5. `End_Confirmed_Active_DifferentiatesMessageAndMentionsOtherVariants` :
   - *Ce qu'il ajoute* : vérifie que pour une promotion en cours (`status == "active"`), la confirmation mentionne « Elle s'arrête ce soir ; une vente d'aujourd'hui garde la remise. » et cite le nombre d'autres variantes du même lot (`batchId`), puis appelle `EndAsync` et recharge la liste.
6. `End_Exception_ShowsErrorDialogAndReloads` :
   - *Ce qu'il ajoute* : vérifie qu'une exception levée lors de `EndAsync` appelle `ShowErrorAsync` puis recharge la liste.

## Comment je vérifie
1. Tests unitaires :
   `dotnet test Easymakemoney.Tests`
   Résultat attendu : 100 % des tests passants (les 712 tests existants + tous les nouveaux tests du lot 4h).
2. Rebuild Android :
   `dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home`
   Résultat attendu : 0 erreur, comparaison avec les 216 avertissements de référence de main.
3. Contrôle d'encodage contre main :
   `git diff --check main`
   Résultat attendu : aucune erreur d'espace ou d'encodage.

## Risques et points incertains
- Champ statique `VariantPromotionListViewModel.PromotionsChanged` : risque d'interférence entre les tests si un test le laisse à `true`. Précaution : remise systématique à `false` dans le constructeur et le `Dispose` de chaque classe de test.
- Navigation `Shell.Current` dans `VariantPromotionViewModel.Apply` : `Shell.Current` est `null` en test xUnit unitaire, la condition `if (Shell.Current is { } shell) await shell.GoToAsync("..");` est franchie en toute sécurité sans plantage.
- Aucune modification des fichiers sous `Easymakemoney/` ni des fichiers XAML : toutes les créations et modifications sont confinées dans `Easymakemoney.Tests/`.

## Questions pour Claude ou le propriétaire
Aucune question bloquante : le contrat backend et les comportements des ViewModels sont entièrement spécifiés et clairs.

