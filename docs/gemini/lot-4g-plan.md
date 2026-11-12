# Lot 4g : plan de Gemini

## Ce que j'ai lu
- `docs/gemini/lot-4g-consigne.md:1-33` : consigne du lot 4g par Claude, spécifiant les tests des ViewModels de location (`RentalPromotionViewModel`, `RentalGridViewModel`, promotions de `RentalStatisticsViewModel`, et conseil `RENTAL_LOW_WEEKDAYS` dans `AdviceViewModel`), la création des faux (`FakeRentalPromotionService`, `FakeRentalGridService`), les règles strictes (aucun fichier d'application touché, aucun XAML, culture fr-CA rétablie, dates relatives à `DateTime.Today`, référence Android 216 avertissements).
- `GEMINI.md:1-73` : consignes générales et modèle de plan.
- `Easymakemoney/ViewModels/Statistiques/RentalPromotionViewModel.cs:1-218` :
  - `Init`: initialise `AdviceId`, `ProductId`, `PercentText`, `Weekdays` (codes 1 à 7) et `ScopeText` (ligne 60-76).
  - Validation et aperçu : `ComputePreview` (ligne 138-170) appelle `BuildRequest(out var error)` (ligne 112-135). Si erreur, `ShowInfoAsync(error)` sans appel service. Sinon appel `PreviewAsync(request)`.
  - Effacement de l'aperçu : `ClearPreview` (ligne 103-110) appelé lors des changements de `StartDate`, `EndDate`, `PercentText` ou bascule de jour `ToggleWeekday`.
  - Application : `Apply` (ligne 173-216) vérifie `Preview.CanApply`, demande confirmation, appelle `ApplyAsync(request, previewHash)`. Active le drapeau statique `RentalStatisticsViewModel.PromotionsChanged = true`.
  - Erreurs métier : interception de `ApiException` avec code 409 `PRICES_CHANGED`, `PROMOTION_OVERLAP` et 422 `NOT_RENTABLE`, affichage d'un message informatif via `ShowInfoAsync` sans `ShowErrorAsync`, puis relance automatique de `ComputePreview()`.
- `Easymakemoney/ViewModels/Statistiques/RentalGridViewModel.cs:1-270` :
  - `InitializeAsync`: idempotence via `_loaded`, charge `GetGridsAsync()` et `GetModificationsAsync()` (ligne 67-113).
  - `SelectGrid`: sélectionne la grille et vide l'aperçu (ligne 116-120).
  - `ComputePreview`: validation grille et pourcentage (ligne 132-150, 163-194). Si baisse (`IsDecrease`), pourcentage négatif (ex. -15). Gestion de `RoundToDollar`.
  - `Apply`: vérifie `CanApply`, confirmation (qui cite les articles si `preview.IsShared`), appel `ApplyAsync(request, hash, allowShared: preview.IsShared)`.
  - Erreurs `Apply`: 409 `PRICES_CHANGED` ou `SHARED_GRID` affiche message d'info et relance `ComputePreview()`. 422 `NOTHING_TO_CHANGE` affiche message d'info sans relance ni erreur.
  - `Undo`: vérifie `modification.CanUndo`, confirmation, appel `UndoAsync(modification.id)`. Erreurs 409 `UNDO_EXPIRED`, `GRID_EDITED_SINCE`, `ALREADY_UNDONE` interceptées avec message informatif dédié et sans `ShowErrorAsync`. Rechargement via `LoadAsync()`.
- `Easymakemoney/ViewModels/Statistiques/RentalStatisticsViewModel.cs:1-249` :
  - Promotions : constructeur avec 4e paramètre optionnel `IRentalPromotionService? promotions` (ligne 23-29).
  - `LoadPromotionsAsync`: ne retient que les promotions visibles (`p.IsVisible`, c'est-à-dire `status` active ou upcoming, y compris `endedAt` renseigné le jour même).
  - `EndPromotion`: vérifie `CanEnd`, confirmation (message adapté selon `upcoming` ou `active`), appel `EndAsync(batchId)` puis rechargement des promotions.
  - Drapeau `PromotionsChanged`: dans `InitializeAsync`, si `_loaded == true` et `PromotionsChanged == true`, relit uniquement les promotions sans relire les statistiques (ligne 77-84).
- `Easymakemoney/ViewModels/Lists/AdviceViewModel.cs:1-257` :
  - `CreatePromotion`: pour un conseil `RENTAL_LOW_WEEKDAYS`, `CanCreatePromotion` est vrai, `IsRentalPromotion` est vrai, mène vers `RentalPromotionPage` (ligne 199-210).
- `Easymakemoney/Models/RentalPromotion.cs:1-190` : modèles de requêtes, réponses, et `RentalWeekdays`.
- `Easymakemoney/Models/RentalGrid.cs:1-158` : modèles de requêtes, modifications et calcul de `CanUndo`.
- `Easymakemoney/Models/Advice.cs:1-233` : propriétés `CanCreatePromotion` et `IsRentalPromotion`.
- `Easymakemoney.Tests/Models/RentalPromotionTests.cs:1-147` et `RentalGridTests.cs:1-150` : tests existants sur les modèles et services HTTP (ne pas dupliquer).
- `Easymakemoney.Tests/Fakes/FakeDialogService.cs:1-28` : service de dialogue existant pour les tests (à compléter pour enregistrer les confirmations `ConfirmCalls`).
- `Easymakemoney.Tests/ViewModels/RentalStatisticsViewModelTests.cs:1-148` : tests existants sur les statistiques locatives sans les promotions.

## Prérequis à installer ou à vérifier (commandes exactes, rien n'est lancé)
- Vérifier la branche courante :
  `git status` (doit être sur `feature/lot-4g-tests`, propre).
- Vérifier la référence des avertissements Android sur main :
  `git log main --oneline | grep -i avertissements` (confirme 216 avertissements après le commit `761e026`).
- Vérifier que les 650 tests existants passent :
  `dotnet test Easymakemoney.Tests`

## Étapes

### 1. Faux de test dans `Easymakemoney.Tests/Fakes/`
1. Créer `Easymakemoney.Tests/Fakes/FakeRentalPromotionService.cs` implémentant `IRentalPromotionService` :
   - Enregistre les appels : `PreviewCalls` (`RentalPromotionRequest`), `ApplyCalls` (`(RentalPromotionRequest Request, string PreviewHash)`), `EndCalls` (`int batchId`), et compteur d'appels `GetPromotionsCalls`.
   - Résultats configurables : `PreviewResult`, `ApplyResult`, `PromotionsResult`.
   - Exceptions configurables (pour tester les erreurs 409 et 422 levées via `ApiException`) : `PreviewException`, `ApplyException`, `EndException`, `GetPromotionsException`.
2. Créer `Easymakemoney.Tests/Fakes/FakeRentalGridService.cs` implémentant `IRentalGridService` :
   - Enregistre les appels : `GetGridsCalls`, `PreviewCalls` (`RentalGridRequest`), `ApplyCalls` (`(RentalGridRequest Request, string PreviewHash, bool AllowShared)`), `UndoCalls` (`int changeId`), `GetModificationsCalls`.
   - Résultats configurables : `GridsResult`, `PreviewResult`, `ModificationsResult`.
   - Exceptions configurables : `GetGridsException`, `PreviewException`, `ApplyException`, `UndoException`, `GetModificationsException`.
3. Enrichir `Easymakemoney.Tests/Fakes/FakeDialogService.cs` :
   - Ajouter une collection `public List<(string Title, string Message, string Accept, string Cancel)> ConfirmCalls { get; } = new();`
   - Dans `ConfirmAsync(...)`, enregistrer l'appel : `ConfirmCalls.Add((title, message, accept, cancel));` afin de pouvoir tester les messages de confirmation (citation des articles sur grille partagée, message de résiliation selon statut de promotion).

### 2. Tests de `RentalPromotionViewModel` (`Easymakemoney.Tests/ViewModels/RentalPromotionViewModelTests.cs`)
Créer la classe de tests sous `[Collection("SiteCurrency")]` avec réinitialisation de culture ("fr-CA") et remise à zéro de `RentalStatisticsViewModel.PromotionsChanged = false` avant et après chaque test (`Dispose`).
Tests à implémenter :
1. `Init_WithProduct_SetsScopeTextAndWeekdays` : initialisation pour un article seul (`Init(0, 9, "Kayak", 20, "6,7")`), vérifie `ProductId == 9`, `AdviceId == 0`, `ScopeText` contenant « Kayak », `PercentText == "20"`, seuls samedi (6) et dimanche (7) sélectionnés.
2. `Init_WithAdvice_SetsScopeTextAndAllDays` : initialisation depuis un conseil (`Init(12, 0, null, 25, "1,2,3,4")`), vérifie `AdviceId == 12`, `ScopeText` du conseil, `PercentText == "25"`, jours 1 à 4 sélectionnés.
3. `ComputePreview_InvalidPercent_ShowsInfoDialogWithoutRequest` : pourcentage invalide (vide, < 5, > 90, décimal "12.5") -> `ShowInfoAsync`, aucun appel à `PreviewAsync`.
4. `ComputePreview_EndDateBeforeStartDate_ShowsInfoDialogWithoutRequest` : fin avant début -> `ShowInfoAsync`, aucun appel à `PreviewAsync`.
5. `ComputePreview_ValidRequest_SendsProductOrAdviceAndWeekdays` : vérifie le corps de requête transmis (articles vs conseil, dates, jours cochés), et le remplissage de `Rows`.
6. `ClearPreview_WhenInputsChange` : modifier `PercentText`, `StartDate`, `EndDate` ou basculer un jour via `ToggleWeekdayCommand` efface `Preview`, vide `Rows` et désactive `CanApply`.
7. `ToggleWeekday_KeepsAtLeastOneDaySelected` : tenter de décocher le dernier jour sélectionné ne le décoche pas.
8. `Apply_BlockedPreview_DoesNotPromptNorApply` : si l'aperçu bloque (`CanApply == false`, ex. chevauchement ou non louable) -> pas de confirmation, pas d'appel à `ApplyAsync`.
9. `Apply_ConfirmationRefused_DoesNotSendRequest` : confirmation refusée (`ConfirmResult = false`) -> aucun appel à `ApplyAsync`.
10. `Apply_Confirmed_SendsRequestAndSetsPromotionsChanged` : confirmation acceptée -> `ApplyAsync` appelé avec le même corps que la requête d'aperçu et le hash, `RentalStatisticsViewModel.PromotionsChanged` passe à `true`, message d'information affiché ("Promotion de location créée.").
11. `Apply_ApiError_409PricesChanged_409Overlap_422NotRentable_ShowsInfoAndRecomputesPreview` :
    - Pour chaque code d'erreur (simulé via `ApiException` avec `ApiError.FromHttpResponse`), vérifie le message d'info dédié (`ShowInfoAsync`), l'absence d'appel à `ShowErrorAsync`, et le recalcul automatique de l'aperçu (`PreviewAsync` rappelé).
12. `Apply_OtherException_ShowsErrorDialog` : une autre exception appelle `ShowErrorAsync`.

### 3. Tests de `RentalGridViewModel` (`Easymakemoney.Tests/ViewModels/RentalGridViewModelTests.cs`)
Créer la classe sous `[Collection("SiteCurrency")]` avec culture "fr-CA".
Tests à implémenter :
1. `InitializeAsync_LoadsGridsAndModificationsOnce` : appelle `InitializeAsync` deux fois, vérifie que `GetGridsAsync` et `GetModificationsAsync` ne sont appelés qu'une seule fois.
2. `SelectGrid_ClearsPreviewAndUpdatesSelectionText` : sélectionner une grille met à jour `SelectionText`, `HasSelection == true`, et efface l'aperçu précédent s'il existait.
3. `ComputePreview_NoGridSelected_ShowsInfoWithoutRequest` : sans grille choisie, `ShowInfoAsync` ("Choisissez d'abord une grille."), pas d'appel service.
4. `ComputePreview_InvalidPercent_ShowsInfoWithoutRequest` : pourcentage vide, <= 0, > 200 en hausse ou > 50 en baisse -> message d'info, pas de requête.
5. `ComputePreview_DecreaseDirection_SendsNegativePercent` : sélectionner direction "down" (`SetDirectionCommand.Execute("down")`) avec saisie "15" envoie `Percent = -15` dans la requête.
6. `ComputePreview_RoundToDollar_OptionReflectedInRequest` : vérifie la transmission de `RoundToDollar` (true ou false).
7. `InputsChange_ClearsPreview` : modifier `PercentText`, `RoundToDollar` ou `IsDecrease` efface l'aperçu.
8. `Apply_SharedGrid_RequiresAllowSharedAndConfirmsWithProductNames` : sur grille partagée (`preview.IsShared == true`), la confirmation cite les noms des articles et l'appel envoie `allowShared: true`.
9. `Apply_NonSharedGrid_SendsAllowSharedFalse` : sur grille non partagée, `allowShared: false`.
10. `Apply_ConfirmationRefused_DoesNotSendRequest` : refus de confirmation -> aucun appel `ApplyAsync`.
11. `Apply_Success_ClearsSelectionAndReloads` : succès -> message d'info, grille sélectionnée remise à null, aperçu effacé, rechargement des grilles et modifications.
12. `Apply_ApiError_409PricesChangedOrSharedGrid_ShowsInfoAndRecomputesPreview` : 409 `PRICES_CHANGED` ou `SHARED_GRID` -> message d'info et relance automatique de `ComputePreview()`.
13. `Apply_ApiError_422NothingToChange_ShowsInfoWithoutRecompute` : 422 `NOTHING_TO_CHANGE` -> message d'info "Rien à changer : les tarifs seraient identiques.", pas de recalcul.
14. `Undo_CannotUndo_DoesNothing` : si `modification.CanUndo == false` -> aucune confirmation, aucun appel `UndoAsync`.
15. `Undo_ConfirmationRefused_DoesNotSendRequest` : refus de confirmation -> aucun appel `UndoAsync`.
16. `Undo_Confirmed_CallsUndoAndReloads` : acceptation -> appel `UndoAsync(modification.id)`, message d'info, rechargement `LoadAsync()`.
17. `Undo_ApiErrors_ShowsSpecificInfoMessageWithoutError` : 409 `UNDO_EXPIRED`, `GRID_EDITED_SINCE`, `ALREADY_UNDONE` -> message propre à chaque code, pas de `ShowErrorAsync`, et rechargement.

### 4. Tests des promotions dans `RentalStatisticsViewModel` (`Easymakemoney.Tests/ViewModels/RentalStatisticsViewModelTests.cs`)
Compléter `RentalStatisticsViewModelTests.cs` (ou y ajouter la section promotions) en utilisant le constructeur à 4 dépendances :
1. `LoadPromotions_FiltersOnlyActiveAndUpcoming_IncludingEndedToday` :
   - Fournit une liste de promotions avec statuts variés : `active`, `upcoming`, `cancelled`, `ended`, et une `active` avec `endedAt` renseigné le jour même ("Se termine ce soir").
   - Vérifie que seules `active`, `upcoming` et `active` avec `endedAt` sont présentes dans `Promotions` (`IsVisible == true`).
   - Vérifie que pour celle se terminant ce soir, `StatusText == "Se termine ce soir"` et `CanEnd == false`.
2. `EndPromotion_CannotEnd_DoesNothing` : si `CanEnd == false`, aucune confirmation ni appel à `EndAsync`.
3. `EndPromotion_ConfirmationRefused_DoesNotSendRequest` : refus de confirmation -> pas d'appel à `EndAsync`.
4. `EndPromotion_Confirmed_DifferentiatesUpcomingAndActiveMessages` :
   - Pour promotion `upcoming` : confirmation cite « n'a pas commencé : elle ne s'appliquera jamais ».
   - Pour promotion `active` : confirmation cite « s'arrête ce soir. Une location qui commence aujourd'hui la garde ».
   - Appel à `EndAsync(promotion.batchId)` puis rechargement des promotions via `GetPromotionsAsync`.
5. `InitializeAsync_WhenPromotionsChanged_ReloadsOnlyPromotions` :
   - Initialise une première fois le ViewModel.
   - Positionne `RentalStatisticsViewModel.PromotionsChanged = true`.
   - Appelle `InitializeAsync()`.
   - Vérifie que `GetPromotionsAsync` est rappelé, mais que le service de statistiques locatives n'a pas été rappelé (une seule requête stats au total).
   - Vérifie que `PromotionsChanged` est repassé à `false`.

### 5. Tests de `AdviceViewModel` (`Easymakemoney.Tests/ViewModels/AdviceViewModelTests.cs`)
Créer la classe de test pour vérifier le conseil `RENTAL_LOW_WEEKDAYS` :
1. `Advice_RentalLowWeekdays_CanCreatePromotion_AndIsRentalPromotion` :
   - Pour un conseil avec `kind = "RENTAL_LOW_WEEKDAYS"`, `status = "new"`, `active = true` :
     - `CanCreatePromotion` est vrai.
     - `IsRentalPromotion` est vrai.
   - Pour un conseil avec `kind = "SLOW_STOCK"` :
     - `CanCreatePromotion` est vrai.
     - `IsRentalPromotion` est faux.
2. `CreatePromotionCommand_RentalLowWeekdays_ExecutesSafely` :
   - Exécute `CreatePromotionCommand` sur `AdviceViewModel`.
   - Note : en test unitaire, `Shell.Current` est `null`. La commande teste la condition de garde sans planter. Signalement explicite dans les tests et le compte rendu comme demandé dans la consigne.

## Comment je vérifie
1. Tests unitaires :
   `dotnet test Easymakemoney.Tests`
   Résultat attendu : 100 % des tests passants (les 650 existants + tous les nouveaux tests du lot 4g).
2. Compilation Android :
   `dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home`
   Résultat attendu : 0 erreur, comparaison avec les 216 avertissements de référence de main.
3. Contrôle d'encodage :
   `git diff --check main`
   Résultat attendu : aucun problème d'espace, de retour chariot ou d'encodage.

## Risques et points incertains
- État statique `RentalStatisticsViewModel.PromotionsChanged` : risque de pollution croisée entre les tests si un test laisse la valeur à `true`. Mesure de précaution : remise systématique à `false` dans le constructeur et dans le `Dispose` de chaque classe de test manipulant cette propriété.
- Environnement xUnit sans `Shell.Current` : `Shell.Current` est `null` lors de l'exécution des tests. Dans `RentalPromotionViewModel.Apply`, `if (Shell.Current is { } shell) await shell.GoToAsync("..");` est protégé par un test de nullité. De même dans `AdviceViewModel.CreatePromotion`, `Shell.Current is not { } shell` évite un crash. Comme demandé dans la consigne, nous testerons les propriétés de modèle `CanCreatePromotion` et `IsRentalPromotion` et signalerons l'absence de Shell pour la navigation effective.
- `FakeDialogService` : nous l'enrichissons avec `ConfirmCalls` pour vérifier fidèlement les textes de confirmation demandés par la consigne, sans modifier aucune signature existante.

## Questions pour Claude ou le propriétaire
Aucune question bloquante : la consigne du lot 4g est précise, les règles de gestion du backend et du ViewModel sont claires et conformes aux contrats documentés.

