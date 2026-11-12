# Lot 4m : plan de Gemini

## Ce que j'ai lu

- `docs/gemini/lot-4m-consigne.md` : consigne du lot, règles de sélection d'au plus 25 tests selon les priorités (ce qui part au serveur, codes d'erreur traités à part, IA, chargement et effacement des résultats périmés).
- `Easymakemoney/ViewModels/Lists/PeriodBudgetViewModel.cs` :
  - `PeriodBudgetViewModel(IPeriodBudgetService, IDialogService)` (l.13-17) : dépendances injectées.
  - `LoadAsync()` (l.40-59) : chargement du budget via `_budgetService.GetAsync(PeriodId)`, peuplement de `Budget`, `PeriodName`, `Lines`, gestion de `IsBusy` et transmission des exceptions à `_dialogService.ShowErrorAsync`.
  - `EditLine(PeriodBudgetLine? line)` (l.61-84) : utilise `Application.Current?.Windows.FirstOrDefault()?.Page?.DisplayPromptAsync` pour saisir le montant prévu du poste.
  - `CopyLastYear()` (l.86-100) : confirmation demandée au gérant ("Reprendre l'an dernier") ; si confirmé, appel à `_budgetService.CopyAsync(PeriodId)`.
- `Easymakemoney/ViewModels/Lists/PeriodSimulationViewModel.cs` :
  - `PeriodSimulationViewModel(ISimulationService, IPeriodBudgetService, IDialogService)` (l.20-25) : dépendances injectées.
  - `StartAsync()` (l.81-85) : lance la simulation initiale si `Simulation == null`.
  - `Schedule()` (l.123-130) : debounce de 450 ms avec `Task.Delay(450)` et `MainThread.BeginInvokeOnMainThread` avant d'appeler `RunAsync()`.
  - `AskGuide()` (l.210-238) : aucun appel IA automatique ; sur action explicite, appel à `_simulationService.CommentAsync`. Traitement spécifique du code `COMMENT_LIMIT` (l.225-228) avec message dédié ("Le guide a déjà donné 10 avis aujourd'hui. Réessayez demain.").
  - `SaveAdvertising()` (l.270-299) : confirmation demandée puis enregistrement du prévu publicité via `_budgetService.SavePlannedAsync`.
  - `ApplyCandidate(candidate)` (l.241-267) : confirmation demandée puis enregistrement de la répartition complète via `_budgetService.SaveLinesAsync`.
- `Easymakemoney/ViewModels/Lists/PreparationViewModel.cs` :
  - `PreparationViewModel(IPreparationService, IPeriodService, IDialogService)` (l.25-32) : dépendances injectées.
  - `StartAsync()` et `LoadEstimatesAsync()` (l.123-173) : charge les estimations de la période cible (`current` ou `next` via `_periodService.GetCurrentPeriodAsync`), initialise les choix de méthodes de ventes.
  - `AddPurchase()` (l.181-205) et `RemovePurchase()` (l.207-212) : validation des montants et coefficients (entre 1 et 20), ajout/suppression et effacement des résultats périmés (`ClearResult()`).
  - `Simulate()` (l.260-294) : validation de la cohérence des dates de voyage (`TripEnd >= TripStart`) et du budget voyage, appel à `_service.SimulateAsync(request)`, stockage de `_simulatedRequest`.
  - `AskGuide()` (l.296-324) : conditionné à l'existence d'une simulation préalable (`_simulatedRequest != null`), appel à `_service.AdviseAsync(request)`, traitement de `COMMENT_LIMIT`.
  - `Validate()` (l.334-370) : confirmation gérant, validation par `_service.ValidateAsync(request, hash)`. Traitement spécifique de `PRICES_CHANGED` (l.355-360) : message d'avertissement et relance automatique de `Simulate()`.
  - `ClearResult()` (l.376-382) : efface simulation et avis dès qu'un paramètre (méthode de vente, voyage, achats) est modifié.
- `Easymakemoney.Tests/Models/PeriodBudgetTests.cs`, `PeriodSimulationTests.cs` et `PreparationTests.cs` : tests existants des modèles et des routes HTTP brutes.
- `Easymakemoney/Services/IPeriodBudgetService.cs`, `ISimulationService.cs`, `IPreparationService.cs`, `IPeriodService.cs` : contrats des services à doubler par des faux.

## Prérequis à installer ou à vérifier (commandes exactes, rien n'est lancé)

Aucun outil ni paquet à installer. Vérifications à lancer après validation :
- Tests unitaires : `dotnet test Easymakemoney.Tests`
- Rebuild Android : `dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home`
- Vérification d'encodage : `git diff --check main`

## Étapes

### 1. Faux de test dans `Easymakemoney.Tests/Fakes/`

1. **`FakePeriodBudgetService.cs` (nouveau)** : implémente `IPeriodBudgetService` (`GetAsync`, `SavePlannedAsync`, `CopyAsync`, `SaveLinesAsync`). Enregistre les appels et fournit les résultats configurables ou exceptions.
2. **`FakeSimulationService.cs` (nouveau)** : implémente `ISimulationService` (`SimulateAsync`, `CommentAsync`). Enregistre les appels avec les leviers transmis.
3. **`FakePreparationService.cs` (nouveau)** : implémente `IPreparationService` (`GetEstimatesAsync`, `SimulateAsync`, `AdviseAsync`, `ValidateAsync`). Enregistre les requêtes et les hashes de validation.
4. **`FakePeriodService.cs` (nouveau)** : implémente `IPeriodService` (`GetCurrentPeriodAsync`, `UpdateBudgetAsync`).

### 2. Tests des ViewModels dans `Easymakemoney.Tests/ViewModels/`

Création de fichiers de tests ciblés, ordonnés selon les priorités de la consigne (au plus 25 tests au total) :

#### A. PeriodBudgetViewModel (6 tests)
1. **`Budget_Load_SetsBudgetAndLines_WhenPeriodValid`** : `LoadAsync` avec `PeriodId = 7` appelle `GetAsync(7)`, remplit `Budget`, `PeriodName`, `Lines`, et active `CanEdit` et `HasBudget`.
2. **`Budget_Load_WhenPeriodClosed_SetsReadOnly_CanEditFalse`** : lorsque `ReadOnly = true`, `CanEdit` reste `false` même avec un budget chargé (pas de modification possible).
3. **`Budget_CopyLastYear_Confirmed_CallsCopyAsync`** : `CopyLastYearCommand` ouvre le dialogue de confirmation "Reprendre l'an dernier" ; si accepté par le gérant, émet l'appel `_budgetService.CopyAsync(7)`.
4. **`Budget_CopyLastYear_Declined_DoesNotCallCopy`** : si le gérant refuse dans la boîte de dialogue, aucun appel réseau n'est émis.
5. **`Budget_CopyLastYear_SourceNotFound_ShowsErrorDialog`** : si `CopyAsync` lève une `ApiException` (`BUDGET_SOURCE_NOT_FOUND` / 404), l'erreur est transmise au dialogue d'erreur `ShowErrorAsync`.
6. **`Budget_Load_HttpError_ShowsDialogAndResetsBusy`** : si le chargement initial échoue, `ShowErrorAsync` est appelé et `IsBusy` revient à false.

#### B. PeriodSimulationViewModel (10 tests)
7. **`Simulation_Start_WhenSimulationNull_CallsSimulateAsync`** : `StartAsync` avec `PeriodId = 7` appelle `_simulationService.SimulateAsync(7, levers)` et remplit `Simulation`, `Rows`, `CanSaveAdvertising`.
8. **`Simulation_Reset_ClearsParametersAndLevers`** : `ResetCommand` réinitialise `AdvertisingText = null`, `PromotionPercent = 0`, `PricePercent = 0` et déclenche la mise à jour des leviers.
9. **`Simulation_PresetPromotion_SetsPercentAndRestrictsToProducts`** : `PresetPromotion(15, [1, 2])` configure `PromotionPercent = 15`, mémorise les articles choisis et met à jour `PromotionText`.
10. **`Simulation_AskGuide_ExplicitAction_CallsCommentAsync`** : aucun appel IA n'est émis au chargement ou lors des modifications de curseurs ; seul `AskGuideCommand` déclenche `CommentAsync(7, CurrentLevers())`.
11. **`Simulation_AskGuide_Error_CommentLimit_ShowsSpecificMessage`** : si `CommentAsync` renvoie l'erreur 429 `COMMENT_LIMIT`, `_dialogService.ShowInfoAsync` affiche "Le guide a déjà donné 10 avis aujourd'hui. Réessayez demain." (message spécifique, pas de dialogue d'erreur brut).
12. **`Simulation_AskGuide_Error_AiUnavailable_KeepsSimulationRows`** : si l'avis du guide échoue, la simulation en cours (`Simulation`) et ses lignes (`Rows`) restent affichées intactes (les chiffres restent).
13. **`Simulation_SaveAdvertising_Confirmed_CallsSavePlannedAsync`** : `SaveAdvertisingCommand` demande confirmation avec le nouveau prévu calculé ; si confirmé, appelle `_budgetService.SavePlannedAsync(7, "advertising", newPlanned)`, réinitialise `AdvertisingText` et affiche "Budget publicité mis à jour.".
14. **`Simulation_SaveAdvertising_Declined_DoesNotSave`** : si refusé, aucun appel à `SavePlannedAsync` n'est émis.
15. **`Simulation_ApplyCandidate_Confirmed_CallsSaveLinesAsyncAndRefreshes`** : `ApplyCandidateCommand` demande confirmation ; si confirmé, applique la répartition via `_budgetService.SaveLinesAsync(7, candidate.planned)` et relance le calcul.
16. **`Simulation_Error_PeriodClosed_DisplaysApiErrorMessage`** : si `SimulateAsync` échoue avec une `ApiException` (ex: `PERIOD_CLOSED`), `ErrorText` prend le message de l'exception et `HasError` devient `true`.

#### C. PreparationViewModel (9 tests)
17. **`Preparation_Start_LoadsEstimatesForTargetPeriod`** : `StartAsync` charge les estimations (`GetEstimatesAsync`) pour la période visée (interroge `GetCurrentPeriodAsync` si `next`), peuple `PeriodTitle`, `SalesMethods`, et sélectionne la méthode recommandée.
18. **`Preparation_TargetChange_ReloadsEstimatesAndClearsPreviousResult`** : modifier `SelectedTarget` recharge les estimations pour la nouvelle période et réinitialise les résultats existants via `ClearResult()`.
19. **`Preparation_AddPurchase_ValidAmount_AddsToCollectionAndClearsInputs`** : `AddPurchaseCommand` valide le montant, ajoute l'achat à `Purchases`, réinitialise les champs de saisie et efface les résultats de simulation périmés.
20. **`Preparation_AddPurchase_InvalidAmountOrCoefficient_ShowsInfoDialog`** : montant <= 0 ou coefficient invalide (< 1 ou > 20) -> affichage de `ShowInfoAsync` avec message explicatif, aucun ajout à la liste.
21. **`Preparation_Simulate_ValidRequest_CallsSimulateAsyncAndPopulatesRows`** : `SimulateCommand` construit la requête `PreparationRequest`, appelle `_service.SimulateAsync(request)`, remplit `Simulation`, `Rows` et mémorise `_simulatedRequest`.
22. **`Preparation_Simulate_InvalidTripDates_ShowsInfoDialog`** : voyage activé avec `TripEnd < TripStart` -> dialogue d'information "La fin du voyage doit suivre son début." et aucun appel réseau.
23. **`Preparation_AskGuide_RequiresPreviousSimulation_CallsAdviseAsync`** : `AskGuideCommand` n'émet aucun appel si aucune simulation n'a été faite (`_simulatedRequest == null`) ; avec simulation, appelle `AdviseAsync` et remplit `Candidates`.
24. **`Preparation_Validate_Confirmed_CallsValidateAsyncAndNavigatesBack`** : `ValidateCommand` avec simulation valide demande confirmation ; si confirmé, appelle `_service.ValidateAsync(request, previewHash)` et affiche "Plan validé : ...".
25. **`Preparation_Validate_PricesChanged_ShowsWarningAndResimulates`** : si `ValidateAsync` renvoie `PRICES_CHANGED`, dialogue d'avertissement affiché ("Le stock, les coûts ou la trésorerie ont changé...") et relance automatique de `Simulate()`.

## Comment je vérifie

1. `dotnet test Easymakemoney.Tests` :
   - Résultat attendu : 964 tests passants (939 actuels + 25 nouveaux), 0 échec, 0 ignoré.
2. Compilation Android :
   - `dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home`
   - Résultat attendu : exactement 216 avertissements, 0 erreur.
3. Contrôle d'encodage :
   - `git diff --check main` vierge.

## Risques et points incertains

- **`PeriodBudgetViewModel.EditLine`** : utilise `Application.Current?.Windows.FirstOrDefault()?.Page?.DisplayPromptAsync`. Sans application MAUI active sous xUnit, ce chemin n'est pas testable directement sans point d'entrée public de prompt.
- **`PeriodSimulationViewModel.Schedule`** : les changements de valeurs de curseurs déclenchent un délai de 450 ms via `Task.Delay`. Pour respecter la règle stricte « Aucune attente réelle dans les tests », un point d'entrée direct pour forcer l'exécution de la simulation sans délai permettrait de tester le recalcul immédiat.
- **`SiteCurrency`** : les 3 ViewModels formatent des montants monétaires. Les classes de test utiliseront `[Collection("SiteCurrency")]`, une culture fixe et la réinitialisation dans `Dispose()`.

## Questions pour Claude ou le propriétaire

1. **Point d'entrée pour la saisie de ligne de budget (`PeriodBudgetViewModel.EditLine`)** :
   Dans `EditLine(PeriodBudgetLine? line)` (l.64), le ViewModel appelle `Application.Current?.Windows.FirstOrDefault()?.Page?.DisplayPromptAsync(...)`. Sous xUnit, `Application.Current` est `null`. Souhaites-tu ajouter un délégué remplaçable public dans `PeriodBudgetViewModel` (par exemple `public Func<string, string, string?, Task<string?>> PromptAsync { get; set; }` similaire à `ChooseAsync` du lot 4l) pour permettre de tester la saisie et l'enregistrement d'un montant prévu ?
2. **Délai de simulation (`PeriodSimulationViewModel.Schedule`)** :
   Dans `PeriodSimulationViewModel`, `Schedule()` attend 450 ms via `Task.Delay` avant d'invoquer la méthode privée `RunAsync()`. Pour tester le rafraîchissement des leviers sans attente réelle, souhaites-tu exposer une méthode publique `public Task RunSimulationAsync() => RunAsync();` ou une propriété de délai débrayable `public TimeSpan DebounceDelay { get; set; }` ?

