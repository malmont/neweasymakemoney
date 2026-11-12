# Lot 4p : plan de Gemini

## Ce que j'ai lu

- `docs/gemini/lot-4p-consigne.md` : consigne du lot 4p (au plus 25 tests sur `StockLowViewModel`, `RepriseViewModel`, `ListNewProductsVariantViewModel`, `AllListsViewModel` et `DashBoardCollectionViewModel`, en commençant par ce qui écrit ; référence : ~1 059 tests sur `main`, Android 12 avertissements ; M-EDIT hors lot car déjà couvert par `ManagementEditTests`).
- `Easymakemoney/ViewModels/Lists/RepriseViewModel.cs:16-269` :
  - `LoadAsync()` lit `_repriseUseCase.GetStateAsync()`, remplit `Commandes`, et si `!IsActive` appelle automatiquement `SimulateAsync()` (`SimulateAsync(Months, coefficient, 1, 100)` — lecture/simulation uniquement, aucune écriture).
  - `EditCost(RepriseProduct)` vérifie `product.CanEditCost` (`costSource != "commande"`), appelle `_dialogService.PromptAsync("Coût d'achat", ...)` et convertit le montant positif en centimes dans `product.EditedCostCents` et `_edited[product.productId]` (ou affiche `"Montant invalide."`).
  - `Apply()` demande `_dialogService.ConfirmAsync(...)` ; si confirmé, appelle `_repriseUseCase.ApplyAsync(Months, coefficient, _edited.Values)`, vide `_edited`, recharge `LoadAsync()`, et affiche `"Historique repris."`. En erreur, appelle `_dialogService.ShowErrorAsync(e)`.
  - `Cancel()` demande `_dialogService.ConfirmAsync(...)` ; si confirmé, appelle `_repriseUseCase.CancelAsync()`, recharge `LoadAsync()`, et affiche `"Reprise annulée."`. En erreur, appelle `_dialogService.ShowErrorAsync(e)`.
- `Easymakemoney/Models/Api/ApiError.cs:189-222` : `GetFallbackMessage` contient `PRODUCT_IN_USE`, `ALREADY_CLOSED`, `NOT_CLOSED`, `PERIOD_CLOSED`, etc., mais **ne contient pas encore** de message de secours dédié pour `REPRISE_ACTIVE` ni `REPRISE_NOT_FOUND`.
- `Easymakemoney/ViewModels/Dashboard/DashBoardCollectionViewModel.cs:46-350` :
  - `SelectCurrentPeriodIfNoneAsync()` lit `_periodUseCase.GetCurrentPeriodAsync()` : si nul, rien n'est chargé (`CollectionId == 0`) ; sinon appelle la méthode privée `SelectCollectionAsync(current)` qui renseigne `CollectionId`, `IsClosed`, `IsPeriod`, `IsReopened`, charge `GetDashBoardCollectionAsync` et `LoadPeriodResultAsync()`, puis met `IsClosedVisible = true`.
  - `ReopenCollection()` appelle `_dialogService.ConfirmAsync("Rouvrir la période", ...)`, puis `_getCollectionCloseUseCase.ReopenCollectionAsync(CollectionId)`, met `IsReopened = true` et `IsClosed = false`, et recharge tableau de bord + résultat (ou `ShowErrorAsync` en cas d'erreur).
  - `CloseCollection()` appelle **sans confirmation préalable** `_getCollectionCloseUseCase.CloseCollectionAsync(CollectionId)`, affiche `"Période clôturée"` (ou `"Collection clôturée"`), recharge `GetDashBoardCollectionAsync`, passe `IsClosed = true`, puis recharge `LoadPeriodResultAsync()` (ou `ShowErrorAsync` en cas d'erreur).
  - `EditBudget()` lit `_dialogService.PromptAsync("Budget de la période", ...)` avec `DecimalInputConverter.Parse(text)`, appelle `_periodUseCase.UpdateBudgetAsync(CollectionId, amount)` et recharge `GetDashBoardCollectionAsync`, ou affiche `"Montant invalide."`.
  - `OpenBudgetLines()`, `OpenSimulation()`, `ItemTapped()` passent directement par `Shell.Current` ; `ShowBottomSheetPopupListView()` et `ShowBottomSheetCreateList()` instancient `new Popup()` / `BottomSheetPopup` via `AppPage.Current`.
- `Easymakemoney/ViewModels/Lists/ListNewProductVariantViewModel.cs:6-125` : classe `ListNewProductsVariantViewModel`.
  - `GetlistProductVariantAsync()` charge `ListProductVariants` via `_getListProductsVariantUseCase.ExecuteAsync(ProductId)` (et `ShowErrorAsync` en erreur).
  - `DeleteProductVariant(ProductVariant)` appelle `_dialogService.ConfirmAsync("Confirmation", "Êtes-vous sûr de vouloir supprimer cette variante ?", "Supprimer", "Annuler")`, puis `_deleteProductsVariantUseCase.ExecuteAsync(id)` : si `true`, retire la variante de `ListProductVariants` et affiche `"Variante supprimée avec succès."` ; si `ApiException` (ex. 409 `PRODUCT_IN_USE`), conserve la variante et appelle `ShowErrorAsync(ex)`.
  - `ShowBottomSheet()` crée `new Popup()` et `new BottomSheetPopup(...)` via `AppPage.Current`. Côté formulaire et sauvegarde (`Easymakemoney/ViewModels/Components/BottomSheetPopupViewModel.cs:200-205` et `Easymakemoney/UseCase/ListProductVariantUseCase/SaveProductvariantUseCase.cs:12-26`), la création passe par `SaveProductvariantUseCase.ExecuteAsync(ProductVariantFormModel, ProductId)` suivi de `GetlistProductVariantAsync()`.
- `Easymakemoney/ViewModels/Lists/StockLowViewModel.cs:7-51` : implémente `IQueryAttributable` (`ApplyQueryAttributes`) sans appel HTTP : remplit `Items`, `PageTitle`, `Intro`, `SourcesText`, `IsEmpty` à partir du paramètre `"Tile"` (`HomeTile`) ou `"Items"` (+ `"Title"`, `"Intro"`), et ignore sans erreur un dictionnaire sans clé connue.
- `Easymakemoney/ViewModels/Lists/AllListsViewModel.cs:4-174` : 19 commandes `[RelayCommand]` de navigation passant directement par `Shell.Current?.GoToAsync(...)`, plus `NewCommand()` ouvrant un `BottomSheetPopup` avec `new Popup()` et `ServiceProviderServiceExtensions.GetRequiredService<CommandFormModel>(_services)`.
- Tests et faux existants lus : `Easymakemoney.Tests/Services/RepriseServiceTests.cs`, `Easymakemoney.Tests/ViewModels/AdviceViewModelTests.cs`, `Easymakemoney.Tests/ViewModels/ManagementListDeletionsTests.cs`, `Easymakemoney.Tests/ViewModels/CommandFormModelTests.cs`, `Easymakemoney.Tests/Fakes/FakeDialogService.cs`, `Easymakemoney.Tests/Fakes/FakeHttpService.cs`, `Easymakemoney.Tests/Fakes/FakePeriodService.cs`, `Easymakemoney.Tests/Fakes/FakeListCollectionService.cs`.

## Prérequis à installer ou à vérifier (commandes exactes, rien n'est lancé)

Aucun outil ni paquet à installer.
Commandes de vérification prévues après validation et codage (sur `feature/lot-4p-tests` depuis `main`) :
- `dotnet test Easymakemoney.Tests` (référence de départ sur `main` après fusion de 4o : 1 059 passants).
- `dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home` (référence attendue : 12 avertissements, 0 erreur).

## Étapes

### 1. Faux de services dans `Easymakemoney.Tests/Fakes/` (uniquement dans le projet de tests)

1. **`FakeRepriseService.cs`** (nouveau) : implémente `IRepriseService` avec traçabilité de `SimulateCalls`, `ApplyCalls` (incluant les produits modifiés reçus), `CancelCalls`, réponses configurables (`StateToReturn`, `SimulationToReturn`) et exceptions configurables (`GetStateException`, `ApplyException`, `CancelException`).
2. **`FakeDashBoardCollectionService.cs`** (nouveau), **`FakeCollectionCloseService.cs`** (nouveau), **`FakePeriodResultService.cs`** (nouveau) : implémentent `IDashBoardCollectionService`, `ICollectionCloseService` et `IPeriodResultService` avec compteurs `CloseCalls` / `ReopenCalls`, résultats configurables et exceptions simulables.
3. **`FakeListProductVariantService.cs`** (nouveau), **`FakeListColorsService.cs`** (nouveau), **`FakeListSizesService.cs`** (nouveau) : implémentent `IListProductVarianstService`, `IListColorsServices` et `IListSizesServices` pour tester `ListNewProductsVariantViewModel`, `ProductVariantFormModel` et `SaveProductvariantUseCase`.
4. Réutiliser `FakeDialogService.cs`, `FakePeriodService.cs`, `FakeListCollectionService.cs`, `FakeHttpService.cs` et `FakeSessionService.cs` existants sans casser les tests existants.

### 2. Tests unitaires xUnit dans `Easymakemoney.Tests/ViewModels/` (24 tests)

#### A. Reprise d'historique (`RepriseViewModelTests.cs` — 7 tests, `[Collection("SiteCurrency")]`)

1. **`Load_InactiveState_SimulatesWithoutWriting_AndPopulatesProducts`** : `active == false` → appelle `GetStateAsync` et `SimulateAsync(12, null, 1, 100)`, `ApplyCalls == 0`, `CancelCalls == 0`, `ShowSimulation == true`, `Products` remplie.
2. **`Load_ActiveState_PopulatesCommandes_WithoutSimulation`** : `active == true` avec `commandes` → `IsActive == true`, `SimulateCalls == 0`, `Commandes` renseignée (`ShowSimulation == false`).
3. **`EditCost_PromptedAmount_UpdatesProductInCents_AndSendsInApply`** : `EditCostCommand` sur article éditable (`costSource != "commande"`) avec saisie `"12,50"` → `EditedCostCents == 1250`, ignoré sur `costSource == "commande"` ; puis `ApplyCommand` confirmé transmet cet article avec `1250` à `ApplyAsync`.
4. **`Apply_Declined_DoesNotSendRequest_And_Confirmed_AppliesAndReloads`** : confirmation refusée → `ApplyCalls == 0` ; confirmation acceptée → `ApplyCalls == 1`, recharge `LoadAsync()`, affiche `"Historique repris."`.
5. **`Apply_ServerRefusesRepriseActive_ShowsErrorWithDisplayedMessage`** : `ApplyAsync` lève `ApiException` (`REPRISE_ACTIVE`, 409) → `FakeDialogService.ShownErrors` reçoit l'`ApiException` et son `Message` correspond au texte affiché attendu.
6. **`Cancel_DeclinedAndConfirmed_HandlesConfirmationAndRefresh`** : confirmation refusée → `CancelCalls == 0` ; confirmation acceptée → `CancelCalls == 1`, recharge `LoadAsync()`, affiche `"Reprise annulée."`.
7. **`Cancel_ServerRefusesRepriseNotFound_ShowsErrorWithDisplayedMessage`** : `CancelAsync` lève `ApiException` (`REPRISE_NOT_FOUND`, 404) → `FakeDialogService.ShownErrors` reçoit l'`ApiException` et son `Message` correspond au texte affiché attendu, sans message `"Reprise annulée."`.

#### B. Écran d'une période (`DashBoardCollectionViewModelTests.cs` — 7 tests, `[Collection("SiteCurrency")]`)

8. **`SelectCurrentPeriod_Null_StaysEmpty_WithoutDashBoardOrResultCalls`** : `GetCurrentPeriodAsync()` retourne `null` → `CollectionId == 0`, `DashBoardCollection == null`, `PeriodResult == null`, `IsClosedVisible == false`, aucun appel à `DashBoardCollection` ni `PeriodResult`.
9. **`SelectCurrentPeriod_OpenPeriod_LoadsDashBoardAndPeriodResult_SetsPermissions`** : période ouverte (`IsPeriod = true, IsClosed = false`) → `DashBoardCollection` et `PeriodResult` (`HasPeriodResult == true`) chargés, `IsClosedVisible == true`, `CanEditBudget == true`, `CanReopen == false`.
10. **`SelectCurrentPeriod_ClosedPeriod_SetsReopenPermission_AndLoadErrorShown`** : période clôturée (`IsClosed = true, IsPeriod = true`) → `CanReopen == true`, `CanEditBudget == false` ; erreur serveur sur `GetResultAsync` ou `GetDashBoardCollection` → `ShownErrors` reçoit l'`ApiException` sans lever d'exception non attrapée.
11. **`ReopenCollection_Declined_DoesNotCallService_And_Confirmed_ReopensAndReloads`** : confirmation refusée → `ReopenCalls == 0`, `IsClosed == true` ; confirmation acceptée → `ReopenCalls == 1`, `IsClosed == false`, `IsReopened == true`, `CanClose == true`, recharge tableau de bord et résultat.
12. **`ReopenCollection_ServerRefusal_ShowsError_AndKeepsClosedState`** : serveur refuse la réouverture (`NOT_CLOSED`, 409) → `ShownErrors` reçoit l'`ApiException` avec son message, `IsClosed` reste `true` et `IsReopened` reste `false`.
13. **`CloseCollection_ClosesAndReloads_And_ServerRefusalShowsError`** : `CloseCollectionCommand` appelle `CloseCollectionAsync`, affiche `"Période clôturée"`, passe `IsClosed = true` et recharge le tableau de bord et le résultat ; si le serveur refuse (`ALREADY_CLOSED`, 409), `ShownErrors` reçoit l'`ApiException` avec son message et `IsClosed` reste `false`. (Sous réserve de la question sur la confirmation à la clôture.)
14. **`EditBudget_ValidAmountAndInvalidAmount_UpdatesBudgetOrShowsError`** : saisie `"1 500,50"` → `UpdateBudgetAsync` appelé avec `150050` centimes puis rechargement ; saisie `"-5"` → affiche `"Montant invalide."` sans appel service.

#### C. Variantes (`ListNewProductsVariantViewModelTests.cs` — 5 tests)

15. **`GetListProductVariant_LoadsVariants_AndHandlesServerError`** : `GetlistProductVariantAsync()` peuple `ListProductVariants` pour `ProductId` ; en cas d'erreur serveur, `ShownErrors` reçoit l'exception et `IsBusy` revient à `false`.
16. **`CreateVariant_SavesViaFormModelAndUseCase_AndReloadsList`** : `ProductVariantFormModel` (couleur, taille, stock) passé à `SaveProductvariantUseCase.ExecuteAsync(..., ProductId)` appelle `PostProductVariant` avec le payload exact, puis `GetlistProductVariantAsync()` recharge la nouvelle variante.
17. **`DeleteVariant_Declined_DoesNotCallService_AndKeepsItem`** : confirmation refusée → `DeleteCalls` vide, variante conservée dans `ListProductVariants`.
18. **`DeleteVariant_Confirmed_RemovesItem_AndShowsSuccessInfo`** : confirmation acceptée et retour `true` → `DeleteCalls` contient l'identifiant, variante retirée, info `"Variante supprimée avec succès."`.
19. **`DeleteVariant_Error_ProductInUse_KeepsItem_AndShowsApiErrorMessage`** : refus 409 `PRODUCT_IN_USE` → variante conservée dans `ListProductVariants`, `ShownErrors` reçoit l'`ApiException` dont `Message` est « Cet article a des ventes, des achats ou des mouvements de stock : retirez-le de la vente au lieu de le supprimer. » (ou texte serveur).

#### D. Stock bas et toutes les listes (`StockLowAndAllListsViewModelTests.cs` — 5 tests, `[Collection("SiteCurrency")]`)

20. **`StockLow_FromTile_PopulatesItemsIntroAndSources_AndFormatsDaysLeft`** : `ApplyQueryAttributes` avec `"Tile"` (`HomeTile` contenant `LowStockItem`, `text`, `sources`, `currency`) → `PageTitle == "Stock bas"`, `Intro == tile.text`, `SourcesText` formaté, `Items` peuplés (`StockText`, `DaysLeftText`), `IsEmpty == false`.
21. **`StockLow_FromAdviceItems_SetsCustomTitleAndFormatsPrices`** : `ApplyQueryAttributes` avec `"Items"`, `"Title"`, `"Intro"` → titre et intro appliqués, `SourcesText == null`, `PriceText` formaté avec `SiteCurrency`, `IsEmpty == false`.
22. **`StockLow_EmptyList_SetsIsEmptyTrue_AndInvalidQueryDoesNotThrow`** : vignette sans ligne → `Items` vide et `IsEmpty == true` ; dictionnaire sans `"Tile"` ni `"Items"` → aucune exception et état inchangé.
23. **`AllLists_NavigationCommands_ExecuteSafelyOrVerifyRoutes`** : appelle les principales commandes de navigation de `AllListsViewModel` (`NavigateToRepriseCommand`, `NavigateToSupplierInvoiceCommand`, `NavigateToToClassifyCommand`, `NavigateToCollectionCommand`, `NavigateToFiscalYearCommand`, `NavigateToReadinessCommand`, etc.) : sans Shell, aucune exception ; si `NavigateAsync` est ajouté sur `AllListsViewModel`, vérification exacte des routes cibles.
24. **`AllLists_RemainingNavigationCommands_ExecuteSafelyOrVerifyRoutes`** : couvre le reste des routes proposées (`NavigateToNoteDeFraisCommand`, `NavigateToNoteFournisseurCommand`, `NavigateToFraisDePortCommand`, `NavigateToAdvertisingCommand`, `NavigateToTripsCommand`, `NavigateToToReceiveCommand`, `NavigateToPreparationCommand`, `NavigateToPromotionsCommand`, `NavigateToVariantPromotionsCommand`, `NavigateToPriceBatchesCommand`, `NavigateToFixedCostsCommand`, `NavigateToTreasuryCommand`, `NavigateToSupportCommand`).

## Comment je vérifie

1. `dotnet test Easymakemoney.Tests` : les 24 nouveaux tests verts, portant le total à **1 083 tests passants** (1 059 de référence + 24), 0 échec, 0 ignoré.
2. `dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home` : exactement **12 avertissements**, 0 erreur.
3. `git diff --check main` vierge, et vérification qu'aucun fichier sous `Easymakemoney/` n'est modifié.

## Risques et points incertains

1. **`REPRISE_ACTIVE` et `REPRISE_NOT_FOUND` absents d'`ApiError.GetFallbackMessage`** (`Easymakemoney/Models/Api/ApiError.cs:189-222`) : contrairement à `PRODUCT_IN_USE` ou `ALREADY_CLOSED`, un corps serveur sans champ `message` renverrait aujourd'hui le texte générique `"Une erreur est survenue lors de la communication avec le serveur."`.
2. **`CloseCollection` sans confirmation** (`Easymakemoney/ViewModels/Dashboard/DashBoardCollectionViewModel.cs:245-268`) : seul `ReopenCollection` appelle `ConfirmAsync`, alors que la consigne indique « clôture et réouverture avec confirmation ».
3. **Navigation `Shell.Current` et popups `new Popup()`/`AppPage.Current` inatteignables sous `net10.0`** :
   - `AllListsViewModel` (19 commandes) et `DashBoardCollectionViewModel` (`OpenBudgetLines`, `OpenSimulation`, `ItemTapped`) vérifient `Shell.Current is { } shell` directement ; sans propriété `NavigateAsync`, la route cible ne peut pas être capturée en test.
   - `ListNewProductsVariantViewModel.ShowBottomSheet()`, `AllListsViewModel.NewCommand()` et `DashBoardCollectionViewModel.ShowBottomSheetPopupListView()` / `ShowBottomSheetCreateList()` instancient `new Popup()` / `BottomSheetPopup`, inatteignables hors interface MAUI.

## Questions pour Claude ou le propriétaire

1. **`REPRISE_ACTIVE` et `REPRISE_NOT_FOUND` dans `ApiError.cs`** : Faut-il ajouter des messages de secours dédiés dans `ApiError.GetFallbackMessage` (quels textes exacts ?), ou bien vérifie-t-on dans les tests le message retourné par le JSON serveur (comme pour `OTP_INVALID`) ?
2. **`DashBoardCollectionViewModel.CloseCollection`** : Dois-je tester `CloseCollection()` tel qu'il est actuellement (sans `ConfirmAsync`, à la différence de `ReopenCollection()` qui a `ConfirmAsync`), ou prévois-tu d'ajouter une confirmation sur `CloseCollection()` avant codage ? Confirmes-tu aussi que `SelectCurrentPeriodIfNoneAsync()` suffit pour couvrir période ouverte, fermée et vide (sans exposer `SelectCollectionAsync`) ?
3. **`Shell.Current` sur `AllListsViewModel` (et `DashBoardCollectionViewModel`) et création de variante (`ShowBottomSheet`)** :
   - Souhaites-tu ajouter `public Func<string, Task> NavigateAsync { get; set; } = route => Shell.Current?.GoToAsync(route) ?? Task.CompletedTask;` sur `AllListsViewModel` (et éventuellement `DashBoardCollectionViewModel`) pour asserter les routes exactes, ou préfères-tu le motif sans Shell (`AdviceViewModelTests`) ?
   - Pour la création d'une variante dans `ListNewProductsVariantViewModel`, tester directement `ProductVariantFormModel` + `SaveProductvariantUseCase` + `GetlistProductVariantAsync()` te convient-il (étant donné que `ShowBottomSheet()` crée un `Popup` MAUI) ?

