# Lot 4q : plan de Gemini

## Ce que j'ai lu

- `docs/gemini/lot-4q-consigne.md` (sur `main`) : objectif du lot (au plus 25 tests sur `BottomSheetPopupViewModel`, les 7 `FormModel`, `DashBoardCommandViewModel` et `DashBoardProductViewModel`), référence de **1 083 tests** sur `main` et **12 avertissements** Android, aucune modification de l'application.
- `Easymakemoney/ViewModels/Components/BottomSheetPopupViewModel.cs:37-238` :
  - `SaveAsync()` (lignes 178-236) gère les 7 branches booléennes (`IsCollectionForm`, `IsCommandForm`, `IsProductForm`, `IsProductVariantForm`, `IsNoteDeFraisForm`, `IsFournisseurForm`, `IsFraisDePortForm`), appelle le `Save*UseCase`, puis si `result == true` recharge le `ListNew*ViewModel` appelant (`GetListCollectionAsync`, `GetListCommandAsync`, `GetListProductAsync`, `GetlistProductVariantAsync`, `GetListNoteDeFraisAsync`, `GetListFournisseursAsync`, `GetListFraisDePort`) ; si `result == false`, il lève `new Exception("Failed to save ...")` en anglais (lignes 186, 192, 198, 204, 209, 214, 219) avant `await _popup.CloseAsync()` (ligne 222).
  - En cas d'exception, le `catch` (lignes 224-235) ne ferme pas le popup et appelle `_dialogService.ShowErrorAsync(ex)` (ou `AppDialogs.InfoAsync` si `_dialogService == null`).
  - `_popup.CloseAsync()` (lignes 222 et 237) est appelé directement sur `CommunityToolkit.Maui.Views.Popup` sans vérification de `null` ni délégué injectable : avec `popup: null`, `SaveAsync()` lève une `NullReferenceException` après avoir pourtant réussi l'enregistrement et le rechargement de la liste, et `CancelCommand` lève une `NullReferenceException` non attrapée.
- `Easymakemoney/Services/Dialog/DialogService.cs:45-62` : `ShowErrorAsync` affiche `apiEx.ApiError.Message` (+ `Errors`) pour une `ApiException`, mais remplace toute autre exception (ex. `InvalidOperationException("Image is required.")` ou `NullReferenceException`) par `"Une erreur est survenue, réessayez."`.
- Les 7 `FormModel` et `Save*UseCase` / `Create*UseCase` :
  - `CollectionFormModel.cs:7-54` + `SaveCollectionUseCase.cs:20-38` : `MoneyHelper.ToCents(BudgetCollection)` (`null -> 0`), dates au format `"yyyy-MM-ddTHH:mm:ssZ"`, `nomCollection`, `photoCollection`, `del`, `userId` lu depuis `_sessionService.GetUserAsync()`. Aucune validation de champ obligatoire côté client.
  - `CommandFormModel.cs:5-95` + `SaveCommandUseCase.cs:15-31` : `MoneyHelper.ToCents(Budget)`, date `"yyyy-MM-ddTHH:mm:ssZ"`, `name`, `photo`, `collectionId`, `fournisseur = SelectedFournisseurs`, `exchangeRate = commandForm.NeedsExchangeRate ? commandForm.ExchangeRate : null`. Aucune validation locale si `Name` ou `SelectedFournisseurs` est nul ; dans `ListNewCommandViewModel.GetListCommandAsync()` (`ListNewCommandViewModels.cs:46-50`), quand `CollectionId == 0`, aucune requête GET n'est envoyée et `"Commande enregistrée dans la période de sa date."` est affiché.
  - `ProductFormModel.cs:10-163` + `SaveProductUseCase.cs:13-24` : **seule garde explicite** : `ImageStream == null || string.IsNullOrEmpty(ImageFileName)` lève `InvalidOperationException("Image is required.")` sans appeler le service. Sinon `ToListProduct()` envoie `purchasePrice` tel quel (en `double`, non converti en cents), `coefficientMultiplier`, `category` (liste à 1 élément ou vide), `style` (sélectionné ou `new Styles()`), flux et nom de fichier image, et `CommandId`.
  - `ProductVariantFormModel.cs:9-82` + `SaveProductvariantUseCase.cs:12-26` : envoie `color`, `size`, `stockQuantity` avec `productId`, sans validation locale.
  - `NoteDeFraisForModel.cs:5-61` + `SaveNoteDeFraisUseCase.cs:15-30` : convertit `Montant` avec `MoneyHelper.ToCents`, date `"yyyy-MM-ddTHH:mm:ssZ"`, accède directement à `NoteDeFraisForm.SelectedTypeNoteDeFrais.id` (ligne 23) → lève `NullReferenceException` si `SelectedTypeNoteDeFrais == null` avant tout appel service.
  - `FournisseurFormModel.cs:5-97` + `SaveFournisseurUseCase.cs:12-28` : `CurrencyCode` vaut `null` quand `SelectedCurrency == "Devise du site"` (sinon `"CAD"`, `"EUR"`, `"USD"`), `Photo` du `FormModel` n'est pas recopiée dans `ListFournisseur`, et `SelectedTypeFournisseur.id` (ligne 23) lève `NullReferenceException` si aucun type n'est sélectionné.
  - `FraisDePortFormModel.cs:7-79` + `SaveFraisDePortUseCase.cs:14-26` : `price = MoneyHelper.ToCents(Prix)`, `transporteur = SelectedTransporteur`, sans validation locale ; sur `ListNewFraisDePortViewModel.cs:46`, `CommandId` est `private set` (mis à jour uniquement dans la méthode privée `AddFrais`, ligne 276).
- `Easymakemoney/ViewModels/Dashboard/DashBoardCommandViewModel.cs:8-140` :
  - `GetDashboardCommandAsync(int CommandId)` (lignes 55-74) charge `DashBoardCommand`, respecte la garde `IsBusy`, affiche les erreurs via `_dialogService.ShowErrorAsync(ex)`.
  - `ShowBottomSheetCreateCommand()` (lignes 76-99) et `ShowBottomSheetPopupListCommandView()` (lignes 102-126) affichent `"Vous devez choisir obligatoirement une période"` si `CollectionId == 0` ; pour `CollectionId != 0`, le chargement de liste et la sélection (`CommandId`, `CommandImage`, `CommandName`, `GetDashboardCommandAsync`) sont dans `BottomSheetPopupListView` + `AppPage.Current`. `ItemTapped()` (lignes 129-137) est `private` et appelle `Shell.Current`.
- `Easymakemoney/ViewModels/Dashboard/DashBoardProductViewModel.cs:9-112` :
  - `ShowBottomSheetPopupListProductView()` (lignes 75-111) affiche `"Vous devez choisir obligatoirement une commande"` si `CommandId == 0` ; pour `CommandId != 0`, la récupération (`_getListProductUseCase.ExecuteAsync(CommandId)`), la gestion d'erreur (`_dialogService.ShowErrorAsync(ex)`) et la sélection (`DashBoardProduct`, `ProductImage`, `ProductName`, `DisplayedQuantity = Product.freezeQuantity ?? Product.quantity ?? 0`) sont enfermées dans les lambdas de `BottomSheetPopupListView` sur `AppPage.Current`.
  - Constat additionnel : `ShowBottomSheetCreateProduct()` (lignes 54-72) ne vérifie pas `CommandId == 0` et ne recopie pas `CommandId` dans `_listNewProductViewModel.CommandId`.

## Prérequis à installer ou à vérifier (commandes exactes, rien n'est lancé)

Aucun outil à installer. À l'étape de code (après validation), partir de `main` à jour :

```bash
git switch main
git merge --ff-only origin/main
git switch -c feature/lot-4q-tests main
```

## Étapes

Note préalable : les faux et fichiers ci-dessous seront créés ou complétés **après** « plan validé, code » et les réponses aux questions de la section finale. **Aucun fichier de `Easymakemoney/` ne sera modifié par Gemini.**

### 1. Faux de test (`Easymakemoney.Tests/Fakes/`)

1. Compléter rétrocompatiblément les faux existants pour enregistrer les créations et simuler `PostResult = false` ou `PostException` :
   - `Easymakemoney.Tests/Fakes/FakeListCollectionService.cs` : ajouter `PostCalls`, `PostResult = true`, `PostException`.
   - `Easymakemoney.Tests/Fakes/FakeListProductsService.cs` : ajouter `PostCalls` (`(ListProduct Product, byte[] ImageBytes, string FileName, int CommandId)` en copiant le stream ou en notant sa longueur/contenu pour assertion après fermeture), `PostResult = true`, `PostException`.
   - `Easymakemoney.Tests/Fakes/FakeListNoteDeFraisServices.cs` : ajouter `PostResult = true`.
   - `Easymakemoney.Tests/Fakes/FakeListFournisseursServices.cs` : ajouter `PostResult = true`, `PostException`.
2. Créer les 3 faux manquants :
   - `Easymakemoney.Tests/Fakes/FakeListCategoryServices.cs` (`IListCategoryServices`).
   - `Easymakemoney.Tests/Fakes/FakeListStyleService.cs` (`IListStyleService`).
   - `Easymakemoney.Tests/Fakes/FakeDashboardCommandServices.cs` (`IDashboardCommandServices`).

### 2. Tests des 7 formulaires et de `BottomSheetPopupViewModel` (`Easymakemoney.Tests/ViewModels/BottomSheetPopupAndFormsTests.cs` — 18 tests)

Avec `[Collection("SiteCurrency")]` et `SiteCurrency.Reset()` en setup/teardown :

**A. Corps exact envoyé, rechargement de la liste et fermeture pour les 7 formulaires (7 tests)**
1. `CollectionForm_Save_SendsCentsDatesAndUserId_ReloadsCollections_AndClosesPopup` : vérifie `budgetCollection == 123456` pour `1234.56`, dates `"yyyy-MM-ddTHH:mm:ssZ"`, `nomCollection`, `photoCollection`, `del`, `userId`, appel à `GetCollectionList()`, popup fermé, 0 erreur.
2. `CommandForm_Save_SendsCentsCurrencyRateDatesAndCollectionId_ReloadsOrInforms_AndCloses` : vérifie `budget` en centimes, date, `collectionId`, `fournisseur`, `exchangeRate` renseigné quand la devise du fournisseur diffère de `SiteCurrency`, rechargement `GetCommandesList(CollectionId)` (et cas `CollectionId == 0` -> info `"Commande enregistrée dans la période de sa date."`), popup fermé.
3. `ProductForm_Save_SendsProductFieldsImageAndCommandId_ReloadsProducts_AndCloses` : vérifie `ToListProduct()` (`name`, `description`, `purchasePrice`, `coefficientMultiplier`, `category`, `style`), stream image non vide + `fileName` + `CommandId`, rechargement `GetProductsList(CommandId)`, popup fermé.
4. `ProductVariantForm_Save_SendsVariantFieldsAndProductId_ReloadsVariants_AndCloses` : vérifie `color`, `size`, `stockQuantity`, `ProductId`, rechargement `GetProductVariantList(ProductId)`, popup fermé.
5. `NoteDeFraisForm_Save_SendsCentsDateAndTypeId_ReloadsNotes_AndCloses` : vérifie `montant` en centimes, date `"yyyy-MM-ddTHH:mm:ssZ"`, `typeNoteDeFraisId`, `CollectionId`, rechargement `GetNoteDeFraisList(CollectionId)`, popup fermé.
6. `FournisseurForm_Save_SendsFieldsAndCurrencyCode_ReloadsFournisseurs_AndCloses` : vérifie `name`, `adresse`, `ville`, `pays`, `tel`, `currency` (`"USD"`), `typeFournisseur`, rechargement `GetFourniseursList()`, popup fermé.
7. `FraisDePortForm_Save_SendsCentsTransporteurAndCommandId_ReloadsFrais_AndCloses` : vérifie `price` en centimes, `name`, `facture`, `tracknumber`, `image`, `transporteur`, `commandId`, rechargement via `GetListFraisDePort()` (cf. question 4 sur `CommandId` en `private set` sur `ListNewFraisDePortViewModel`), popup fermé.

**B. Refus serveur, erreur technique et annulation (5 tests)**
8. `CollectionForm_Save_ServerRefusal_ShowsExactApiError_DoesNotReloadOrClose` : `ApiException` -> `ShownErrors` contient le message serveur exact, `GetCollectionList` non rappelé, popup resté ouvert.
9. `CommandForm_Save_ServerRefusal_ShowsExactApiError_DoesNotReloadOrClose` : `ApiException` -> message serveur affiché, `GetCommandesList` non rappelé, popup resté ouvert.
10. `NoteDeFraisAndFraisDePort_Save_ServerRefusal_ShowsExactApiError_DoesNotReloadOrClose` : refus `ApiException` sur note de frais et frais de port -> erreur montrée, listes non rechargées, popup non fermé.
11. `Save_WhenUseCaseReturnsFalse_ShowsEnglishTechnicalException_SkipBug` (`[Fact(Skip = "BUG: ...")]`) : montre le constat dans `BottomSheetPopupViewModel` (lignes 186, 192, 198, 204, 209, 214, 219) qui lève `"Failed to save ..."` en anglais quand un service renvoie `false` (ou vérifier l'exception réelle sans correction de l'app).
12. `CancelCommand_ClosesPopup_WithoutPostingOrReloading` : `CancelCommand` ferme le popup sans appeler aucun `Post*` (cf. question 1).

**C. Champs obligatoires et saisies invalides : ce qui part, ce qui ne part pas (6 tests)**
13. `CollectionForm_EmptyOrInvalidFields_StillPostsZeroCents` : vérifie que le code actuel n'a pas de garde locale (`BudgetCollection = null -> 0` cents, `NomCollection = null`, `EndDate < StartDate` partent au service).
14. `CommandForm_SameCurrencyDiscardsExchangeRate_AndEmptyNameStillPosts` : vérifie que `ExchangeRate` est forcé à `null` quand `NeedsExchangeRate == false` même si une valeur est saisie, et que `Name` vide part tout de même.
15. `ProductForm_MissingImage_DoesNotPost_AndReportsError_DoesNotClose` : vérifie que `ImageStream == null` ou `ImageFileName` vide bloque l'appel à `PostProducts`, lève `InvalidOperationException("Image is required.")` vers `ShowErrorAsync` sans fermer le popup ni recharger (avec constat anglais documenté).
16. `ProductForm_WithImageAndEmptyOptionalFields_StillPosts` : `ImageStream` + `ImageFileName` valides avec `Name = null`, `PurchasePrice = 0`, `SelectedCategory = null`, `SelectedStyles = null` -> requête envoyée.
17. `NoteDeFraisForm_MissingType_DoesNotPost_WhileEmptyAmountPostsZero` : `SelectedTypeNoteDeFrais == null` bloque l'appel service (`NullReferenceException` attrapée vers `ShowErrorAsync`, popup non fermé) ; avec un type choisi et `Montant = null`, la requête part à `0` cents.
18. `FournisseurForm_MissingType_DoesNotPost_AndSiteCurrencySendsNullCurrency` : `SelectedTypeFournisseur == null` bloque l'appel service (`NullReferenceException` vers `ShowErrorAsync`) ; `"Devise du site"` envoie `currency = null`, et `Photo` n'est pas transmis.

### 3. Tests de `DashBoardCommandViewModel` et `DashBoardProductViewModel` (`Easymakemoney.Tests/ViewModels/DashBoardCommandAndProductViewModelTests.cs` — 7 tests)

19. `DashBoardCommand_GetDashboardCommand_Success_PopulatesAndRespectsIsBusy` : charge `DashboardCommand`, vérifie `IsBusy == false` en fin, et ignore un appel concurrent quand `IsBusy == true`.
20. `DashBoardCommand_GetDashboardCommand_ServerError_ShowsErrorAndResetsIsBusy` : `ApiException` -> `ShownErrors` reçoit l'erreur serveur, `DashBoardCommand == null`, `IsBusy == false`.
21. `DashBoardCommand_WithoutPeriod_CreateAndListCommands_ShowRequiredPeriodInfo` : `CollectionId == 0` -> `ShowBottomSheetCreateCommandCommand` et `ShowBottomSheetPopupListCommandViewCommand` affichent `"Vous devez choisir obligatoirement une période"`.
22. `DashBoardCommand_SelectAndLoadCommands_HandlesEmptyAndSelection` : (selon le point d'entrée de la question 2) liste vide ne change pas `CommandId` et n'appelle pas `GetDashboardCommand` ; sélection d'une `ListCommand` renseigne `CommandId`, `CommandImage`, `CommandName` et charge `GetDashboardCommandAsync(command.id)` (plus `NavigateAsync` sur `ItemTapped` si ajouté).
23. `DashBoardProduct_WithoutCommand_ListCommand_ShowsRequiredCommandInfo` : `CommandId == 0` -> `ShowBottomSheetPopupListProductViewCommand` affiche `"Vous devez choisir obligatoirement une commande"`.
24. `DashBoardProduct_SelectProduct_UpdatesProductImageNameAndDisplayedQuantity` : (selon la question 3) vérifie `DisplayedQuantity` avec `freezeQuantity` prioritaire sur `quantity`, puis repli à `0`, et la mise à jour de `DashBoardProduct`, `ProductImage`, `ProductName`.
25. `DashBoardProduct_LoadProducts_EmptyAndServerError_ShowsErrorWithoutCrash` : (selon la question 3) liste vide sans sélection, et exception sur `GetProductsList(CommandId)` transmise à `_dialogService.ShowErrorAsync`.

Total : **25 tests** (dans le plafond de 25 fixé par la consigne).

## Comment je vérifie

Après validation et implémentation sur `feature/lot-4q-tests` :
- `dotnet test Easymakemoney.Tests --nologo` -> attendu : `1 083 + 25 = 1 108` tests passés (ou `1 107` passés + `1` ignoré si 1 test reste en `[Fact(Skip = "BUG: ...")]` selon les réponses de Claude), 0 échoué, 0 nouvel avertissement.
- `dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home --nologo` -> attendu : exactement `12 Warning(s)` et `0 Error(s)`.
- `git diff --check main` -> attendu : aucune sortie, code `0`.

## Risques et points incertains

- L'appel `await _popup.CloseAsync()` dans `BottomSheetPopupViewModel` bloque à la fois les tests de fermeture/non-fermeture du popup et la séparation succès vs `NullReferenceException` si `_popup` reste `null` ou non attaché à une page MAUI.
- Les lambdas internes de `DashBoardCommandViewModel.ShowBottomSheetPopupListCommandView` et `DashBoardProductViewModel.ShowBottomSheetPopupListProductView` passent par `BottomSheetPopupListView` + `AppPage.Current`.
- Les `CommandId` / `CollectionId` des listes dépendantes et les validations manquantes côté `*FormModel` peuvent déclencher des `NullReferenceException` ou des messages techniques en anglais : le plan propose de tester fidèlement le code tel qu'il est et de signaler chaque constat sans modifier l'application.

## Questions pour Claude ou le propriétaire

1. **`BottomSheetPopupViewModel` — fermeture injectable (`_popup.CloseAsync()`)** : pour pouvoir tester le succès sans `NullReferenceException`, l'annulation (`CancelCommand`) et vérifier que le popup **se ferme après succès** mais **reste ouvert après échec**, peux-tu exposer un délégué remplaçable `Func<Task> ClosePopupAsync` (ou `CloseAsync`) sur `BottomSheetPopupViewModel` (initialisé par défaut avec `_popup != null ? _popup.CloseAsync() : Task.CompletedTask`) ?
2. **`DashBoardCommandViewModel` — sélection, état vide et navigation** : `ShowBottomSheetPopupListCommandView` passe par `BottomSheetPopupListView` et `AppPage.Current`, et `ItemTapped` est `private` avec `Shell.Current`. Peux-tu exposer une méthode publique `LoadCommandsAsync()` / `SelectCommandAsync(ListCommand)` (et `NavigateAsync` si tu veux aussi tester la navigation de `ItemTappedCommand`) pour couvrir l'état vide et la sélection depuis un test unitaire ?
3. **`DashBoardProductViewModel` — chargement, état vide et erreur** : tout le chargement et la sélection d'article sont actuellement dans les lambdas de `ShowBottomSheetPopupListProductView` (via `BottomSheetPopupListView` + `AppPage.Current`). Peux-tu exposer des points d'entrée publics (ex. `LoadProductsAsync()` et `SelectProduct(ListProduct)`) sur `DashBoardProductViewModel` ?
4. **`ListNewFraisDePortViewModel.CommandId` (`private set`)** : `BottomSheetPopupViewModel` lit `_fraisDePortViewModel.CommandId` à la ligne 217, mais `CommandId` sur `ListNewFraisDePortViewModel` (ligne 46) est `private set` et n'est positionné que dans la méthode privée `AddFrais(FraisDePortCommandeRow?)` (ligne 276). Peux-tu soit rendre `CommandId` settable (`internal`/`public set` ou méthode de préparation), soit confirmer qu'il faut passer par un point d'entrée ou tester avec un helper dédié ?
5. **Constats à laisser en `Skip` BUG ou à tester tels quels ?** :
   - Sur `BottomSheetPopupViewModel`, un retour `false` d'un `Save*UseCase` lève `Exception("Failed to save ...")` en anglais, et `SaveProductUseCase` sans image lève `InvalidOperationException("Image is required.")` (qui en prod via `DialogService` devient le générique `"Une erreur est survenue, réessayez."`).
   - Sur `SaveNoteDeFraisUseCase` et `SaveFournisseurUseCase`, l'absence de `SelectedTypeNoteDeFrais` / `SelectedTypeFournisseur` lève `NullReferenceException`.
   - Sur `SaveFournisseurUseCase`, `FournisseurFormModel.Photo` n'est pas recopiée vers `ListFournisseur`.
   - Sur `DashBoardProductViewModel.ShowBottomSheetCreateProduct`, `CommandId == 0` n'est pas vérifié et `CommandId` n'est pas copié vers `_listNewProductViewModel.CommandId`.
   Préfères-tu des tests `[Fact]` qui figent ces comportements actuels, ou un (ou plusieurs) `[Fact(Skip = "BUG: ...")]` dédiés ?

