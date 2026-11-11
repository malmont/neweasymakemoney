# Lot 4f : plan de Gemini

## Ce que j'ai lu

1. `docs/gemini/lot-4f-consigne.md:1-32` : consigne du lot 4f, périmètre des ViewModels des nouveaux écrans (Statistiques, Année, exercice comptable, Locations, Frais fixes, Mouvements de trésorerie), référence (521 tests sur `main`, 325 avertissements Android), consigne de ne pas re-tester ce qui est déjà couvert par les tests de modèles et services, création de faux dédiés dans `Easymakemoney.Tests` si nécessaire, dates relatives à `DateTime.Today`, et règles absolues (aucun fichier de l'application modifié, aucun XAML, pas d'appel réseau réel, pas de commit).
2. `Easymakemoney/ViewModels/Statistiques/StatistiquesViewModel.cs:1-335` : `InitializeAsync` avec chargement unique via `StatistiquesPageLoaderState` ; sélection de plage (`today`, `7d`, `month`, `year`) ; sélection de canal (`all`, `ecommerce`, `pos`, `mobile_app`) conservant la plage active ; sélection de `period` avec recherche de la période contenant aujourd'hui via `Contains` puis envoi de `periodId` ; plage `custom` déclenchée uniquement par `ApplyCustomDates` avec validation de l'ordre des dates et limite de 366 jours ; sélection de métrique (`revenue`, `grossMargin`, `orders`) affectant `ChartIsMoney` et `ChartPoints` sans nouvelle requête HTTP ; affichage de `SelectedPointText` ; capture des erreurs via `_dialogService.ShowErrorAsync` ; regroupement des cartes deux par deux dans `CardRows`.
3. `Easymakemoney/ViewModels/Statistiques/YearStatisticsViewModel.cs:1-231` (`YearStatisticsViewModel`) : premier chargement sans `year` ; navigation `PreviousYear` / `NextYear` (bornes 2000 et exercice en cours, mise à jour de `CanGoBack` et `CanGoForward`) ; `SelectPrice` envoyant `pricePercent` et conservant l'exercice affiché ; `AskComment` réservé aux exercices terminés (`Data.IsFinished == true`), interception spécifique de l'erreur 429 `COMMENT_LIMIT` affichant un message d'information sans erreur ; drapeau statique `FiscalYearChanged` provoquant le rechargement de l'exercice en cours lors d'`InitializeAsync` et sa remise à `false` ; masquage des leviers pour un exercice terminé.
4. `Easymakemoney/ViewModels/Statistiques/YearStatisticsViewModel.cs:234-321` (`FiscalYearViewModel`) : lecture de `GET gestion/reglages/exercice` et sélection du mois correspondant ; calcul de `IsChanged` ; `Save` inactif sans changement ; confirmation par `ConfirmAsync` (refusée = aucun `PUT`, acceptée = `PUT gestion/reglages/exercice` avec `{"startMonth":…}`, `FiscalYearChanged = true` et message d'information).
5. `Easymakemoney/ViewModels/Statistiques/RentalStatisticsViewModel.cs:1-169` : `InitializeAsync` exécuté une seule fois sans plage ; plage `current` (sans dates) ; plage `30d` (dates calculées depuis `DateTime.Today`) ; plage `period` (chargement des périodes puis envoi de `periodId`) ; plage `custom` validée via `ApplyCustomDates` ; `WeekdayPoints` et `SelectedDayText`.
6. `Easymakemoney/ViewModels/Lists/FixedCostsViewModel.cs:1-89` : `LoadAsync` envoyant `gestion/frais-fixes?all=1&locale=fr`, remplissant `Items` triés (actifs, à venir, terminés) et `Categories` ; commande `Delete` demandant confirmation (refusée = aucune suppression, acceptée = `DELETE gestion/tresorerie/mouvements/{id}` puis rechargement).
7. `Easymakemoney/ViewModels/Lists/TreasuryMovementViewModel.cs:1-143` : validation du nom et du montant (messages d'information sans requête HTTP) ; calcul de `FixedCostChoice` (valeur envoyée seulement si différente de la règle par défaut de la catégorie pour une sortie récurrente) ; sérialisation de `TreasuryMovementRequest` vers `POST gestion/tresorerie/mouvements` ; navigation `if (Shell.Current is { } shell)` inoffensive en test unitaire quand `Shell.Current` est `null`.
8. `Easymakemoney/Services/IListCollectionService.cs:1-10` : interface demandée par `StatistiquesViewModel` et `RentalStatisticsViewModel` pour charger les périodes.
9. Tests existants :
   - `Easymakemoney.Tests/Models/UnifiedStatisticsTests.cs` : teste la désérialisation du JSON, `Cards()`, `ComparisonText`, `ChartPoints()`, `StatistiquesPeriodTests.CurrentPeriod_ContainsToday`, et `UnifiedStatisticsService.GetAsync`. Ne teste pas le comportement du `StatistiquesViewModel`.
   - `Easymakemoney.Tests/Models/YearStatisticsTests.cs` : teste les modèles `YearStatistics`, `FiscalYearSettings`, et `YearStatisticsService`. Ne teste pas les ViewModels `YearStatisticsViewModel` et `FiscalYearViewModel`.
   - `Easymakemoney.Tests/Models/RentalStatisticsTests.cs` : teste le modèle `RentalStatistics` et `RentalQuery`. Ne teste pas `RentalStatisticsViewModel`.
   - `Easymakemoney.Tests/Models/FixedCostsTests.cs` : teste `FixedCostList`, `FixedCostItem`, `FixedCostRule`. Ne teste pas `FixedCostsViewModel`.
   - `Easymakemoney.Tests/Fakes/FakeDialogService.cs:1-28` et `FakeHttpService.cs:1-67` : outils de faux déjà en place.

---

## Prérequis à installer ou à vérifier (commandes exactes, rien n'est lancé)

- Aucun paquet ni workload à installer.
- Branche de travail active :
  ```bash
  git status
  ```
  (branche `feature/lot-4f-tests`, créée depuis `main`, working tree propre).
- Référence des tests existants : 521 tests passants sur `main`.
- Référence des avertissements Android : 325 avertissements.

---

## Étapes

Tous les ajouts sont faits exclusivement dans le projet `Easymakemoney.Tests/`. Aucun fichier de l'application sous `Easymakemoney/` n'est modifié.

### 1. Faux de test : `Easymakemoney.Tests/Fakes/FakeListCollectionService.cs`
Création d'un faux implémentant `IListCollectionService` pour alimenter les listes de périodes de `StatistiquesViewModel` et `RentalStatisticsViewModel` sans passer par `HttpService` :
- Propriété `CollectionsToReturn` (`ObservableCollection<ListCollection>`).
- Méthode `GetCollectionList()` retournant `CollectionsToReturn`.
- Méthodes `PostCollection` et `DeleteCollection` retournant `Task.FromResult(true)`.

---

### 2. Tests de `StatistiquesViewModel` (`Easymakemoney.Tests/ViewModels/StatistiquesViewModelTests.cs`)
Classe sous `[Collection("SiteCurrency")]`.
- **Ce qui existe déjà** : `UnifiedStatisticsTests` teste le service et les modèles.
- **Ce que j'ajoute** :
  1. `InitializeAsync_LoadsOnceWithDefaultQuery` : vérifie qu'`InitializeAsync()` lance une seule requête GET `gestion/statistiques?range=month&source=all&locale=fr`, et qu'un deuxième appel immédiat n'émet aucune nouvelle requête.
  2. `SelectRange_UpdatesUrl_ForStandardRanges` : vérifie qu'appeler `SelectRangeCommand` pour `"today"`, `"7d"`, `"month"` et `"year"` envoie respectivement les URLs attendues avec `range=today`, `range=7d`, `range=month`, `range=year`.
  3. `SelectSource_KeepsCurrentRange` : après sélection de `"7d"`, `SelectSourceCommand.ExecuteAsync("pos")` envoie `gestion/statistiques?range=7d&source=pos&locale=fr`.
  4. `SelectRange_Period_SelectsPeriodContainingToday_AndQueriesPeriodId` : avec deux périodes dont une seule contient `DateTime.Today` (ex. id = 42), `SelectRangeCommand.ExecuteAsync("period")` positionne `SelectedPeriod` sur celle-ci et émet la requête `gestion/statistiques?periodId=42&source=all&locale=fr`.
  5. `SelectRange_Custom_DoesNotSendRequestUntilApplyCustomDates_AndValidatesDates` :
     - Sélectionner `"custom"` n'envoie aucune requête immédiate.
     - `CustomTo < CustomFrom` : `ApplyCustomDatesCommand` n'envoie aucune requête et affiche le message `"La date de fin doit suivre la date de début."` dans `dialogService.ShownInfos`.
     - `(CustomTo - CustomFrom) >= 366 jours` : n'envoie aucune requête et affiche `"Choisissez 366 jours au plus."`.
     - Dates valides (ex. du `Today.AddDays(-10)` au `Today`) : envoie `gestion/statistiques?from=...&to=...&source=all&locale=fr`.
  6. `SelectMetric_Orders_SwitchesChartIsMoneyAndPoints_WithoutNewRequest` :
     - Après chargement de données contenant des points de graphique, passer la métrique à `"orders"` via `SelectMetricCommand.Execute("orders")` met `ChartIsMoney` à `false` et met à jour `ChartPoints` sans émettre aucune nouvelle requête HTTP.
  7. `SelectedPointText_ReflectsSelection` :
     - Avant sélection (`SelectedPointIndex = -1`) : vaut le texte d'aide par défaut `"Touchez une barre pour lire sa valeur. En gris : l'an dernier aux mêmes dates."`.
     - Après sélection d'un point (`SelectedPointIndex = 0`) : affiche le libellé et la valeur du point sélectionné.
  8. `LoadAsync_OnError_CallsShowErrorAsync` :
     - Si le service lève une exception, `dialogService.ShownErrors` contient l'exception et `IsBusy` repasse à faux.
  9. `CardRows_PairsCardsTwoByTwo` :
     - Vérifie que les cartes retournées par les statistiques sont appairées deux par deux dans `CardRows`.

---

### 3. Tests de `YearStatisticsViewModel` (`Easymakemoney.Tests/ViewModels/YearStatisticsViewModelTests.cs`)
Classe sous `[Collection("SiteCurrency")]`, nettoyant `YearStatisticsViewModel.FiscalYearChanged = false` au Dispose.
- **Ce qui existe déjà** : `YearStatisticsTests` teste le modèle et le service.
- **Ce que j'ajoute** :
  1. `InitializeAsync_FirstLoad_RequestsWithoutYear` :
     - Premier chargement : envoie `gestion/statistiques/annee?locale=fr` (sans `year`).
     - Met à jour `_currentStartYear` avec l'année reçue (ex. 2026).
     - `CanGoBack` est vrai (> 2000), `CanGoForward` est faux.
     - Un second appel à `InitializeAsync()` sans modification de l'exercice ne recharge pas.
  2. `PreviousYear_And_NextYear_NavigateWithBounds` :
     - Avec 2026 affiché, `PreviousYearCommand` demande `year=2025` (`CanGoForward` devient vrai).
     - Puis `NextYearCommand` redemande `year=2026` (`CanGoForward` redevient faux).
     - À l'année 2000, `PreviousYearCommand` ne fait rien (`CanGoBack` vaut faux).
     - À l'année en cours (`_currentStartYear`), `NextYearCommand` ne fait rien (`CanGoForward` vaut faux).
  3. `SelectPrice_SendsPricePercent_AndKeepsDisplayedYear` :
     - Sur l'exercice 2025, sélectionner le levier prix `"+10 %"` (`SelectPriceCommand.ExecuteAsync("10")`) émet `gestion/statistiques/annee?year=2025&pricePercent=10&locale=fr`.
  4. `AskComment_OnlyForFinishedYear_SendsPostToContractRoute` :
     - Si `Data.IsFinished == false` : `AskCommentCommand` ne part pas (aucun POST).
     - Si `Data.IsFinished == true` (ex. exercice 2025 terminé) : émet `POST gestion/statistiques/annee/avis?year=2025&locale=fr` et renseigne `Comment`.
  5. `AskComment_429CommentLimit_ShowsInfoDialogInsteadOfError` :
     - Si l'appel d'avis lève une `ApiException` avec code `"COMMENT_LIMIT"` (HTTP 429) :
     - `dialogService.ShownInfos` reçoit `"Le guide a déjà donné 10 avis aujourd'hui. Réessayez demain."`.
     - `dialogService.ShownErrors` reste vide.
  6. `FiscalYearChanged_TriggersReloadOfCurrentYear_AndResetsFlag` :
     - Initialisation initiale effectuée.
     - Positionner `YearStatisticsViewModel.FiscalYearChanged = true`.
     - Appeler `InitializeAsync()` : l'exercice en cours est rechargé (GET sans `year`), et `FiscalYearChanged` est remis à `false`.
  7. `FinishedYear_HidesLevers` :
     - Si `Data.IsFinished == true` : `Levers` est vide et `HasLevers` est `false`.
     - Si `Data.IsFinished == false` : `Levers` est alimenté et `HasLevers` est `true`.

---

### 4. Tests de `FiscalYearViewModel` (`Easymakemoney.Tests/ViewModels/FiscalYearViewModelTests.cs`)
Classe sous `[Collection("SiteCurrency")]`, nettoyant `YearStatisticsViewModel.FiscalYearChanged = false` au Dispose.
- **Ce qui existe déjà** : `FiscalYearSettings` est testé dans `YearStatisticsTests`.
- **Ce que j'ajoute** :
  1. `InitializeAsync_SelectsSavedMonth` :
     - Lit `GET gestion/reglages/exercice`.
     - Si `startMonth = 4`, `SelectedMonth` correspond au mois d'avril (`Code == "4"`).
     - `IsChanged` est `false`.
  2. `IsChanged_DetectsMonthModification` :
     - Modifier `SelectedMonth` vers septembre (`Code == "9"`) passe `IsChanged` à `true`.
     - Le rétablir à avril repasse `IsChanged` à `false`.
  3. `Save_WhenNotChanged_DoesNothing` :
     - Si `IsChanged == false`, `SaveCommand` n'appelle ni confirmation ni requête `PUT`.
  4. `Save_WhenConfirmationRefused_DoesNotSendPut` :
     - Si `IsChanged == true` et `dialogService.ConfirmResult = false` : `SaveCommand` demande confirmation mais n'émet aucun `PUT` ; `YearStatisticsViewModel.FiscalYearChanged` reste `false`.
  5. `Save_WhenConfirmationAccepted_SendsPutAndSetsFlagAndShowsConfirmation` :
     - Si confirmation acceptée (`ConfirmResult = true`) :
     - Émet `PUT gestion/reglages/exercice` avec corps sérialisé `{"startMonth":9}`.
     - Positionne `YearStatisticsViewModel.FiscalYearChanged = true`.
     - Affiche `"L'exercice est enregistré."` dans `dialogService.ShownInfos`.
     - `IsChanged` redevient `false`.

---

### 5. Tests de `RentalStatisticsViewModel` (`Easymakemoney.Tests/ViewModels/RentalStatisticsViewModelTests.cs`)
Classe sous `[Collection("SiteCurrency")]`.
- **Ce qui existe déjà** : `RentalStatisticsTests` teste le modèle et la requête.
- **Ce que j'ajoute** :
  1. `InitializeAsync_LoadsOnceWithoutRange` :
     - Premier appel : émet `gestion/statistiques/locations?locale=fr` (sans dates ni période).
     - Second appel : ne recharge pas.
  2. `SelectRange_30d_UsesDatesRelativeToToday` :
     - `SelectRangeCommand.ExecuteAsync("30d")` émet `gestion/statistiques/locations?from={Today.AddDays(-29):yyyy-MM-dd}&to={Today:yyyy-MM-dd}&locale=fr`.
  3. `SelectRange_Period_SendsPeriodIdAfterSelection` :
     - `SelectRangeCommand.ExecuteAsync("period")` charge les périodes depuis `IListCollectionService`.
     - `SelectedPeriod = p` (id = 15) émet `gestion/statistiques/locations?periodId=15&locale=fr`.
  4. `SelectRange_Custom_ValidatesDatesBeforeSending` :
     - `CustomTo < CustomFrom` : affiche `"La date de fin doit suivre la date de début."`, aucune requête.
     - `(CustomTo - CustomFrom) >= 366 jours` : affiche `"Choisissez 366 jours au plus."`, aucune requête.
     - Dates valides : émet `gestion/statistiques/locations?from=...&to=...&locale=fr`.
  5. `WeekdayPoints_And_SelectedDayText` :
     - Initialisé avec données : avant sélection (`SelectedDayIndex = -1`), `SelectedDayText` vaut `"Touchez un jour pour lire son occupation moyenne."` ; après sélection (`SelectedDayIndex = 0`), vaut le libellé du point (`WeekdayPoints[0].Label`).

---

### 6. Tests de `FixedCostsViewModel` (`Easymakemoney.Tests/ViewModels/FixedCostsViewModelTests.cs`)
Classe sous `[Collection("SiteCurrency")]`.
- **Ce qui existe déjà** : `FixedCostsTests` teste le modèle et le tri.
- **Ce que j'ajoute** :
  1. `LoadAsync_RequestsAllWithLocaleFr_AndPopulatesSortedItemsAndCategories` :
     - `LoadAsync()` envoie `GET gestion/frais-fixes?all=1&locale=fr`.
     - Alimente `Items` avec les éléments triés selon `SortedItems(DateTime.Today)` (actifs en premier, puis à venir, puis terminés).
     - Alimente `Categories`, `MonthlyTotalText`, et positionne `IsEmpty`.
  2. `Delete_WhenRefused_DoesNotSendDelete` :
     - Sur un item avec `CanDelete == true`, si `ConfirmResult = false`, `DeleteCommand` demande confirmation mais n'émet aucun appel DELETE (`fakeHttp.DeletedUrls` est vide).
  3. `Delete_WhenAccepted_SendsDeleteAndReloads` :
     - Si `ConfirmResult = true`, `DeleteCommand` émet `DELETE gestion/tresorerie/mouvements/{id}` puis recharge automatiquement les frais fixes (nouvel appel `GET gestion/frais-fixes?all=1&locale=fr`).
  4. `Delete_WhenItemCannotBeDeleted_DoesNothing` :
     - Sur un item avec `CanDelete == false` ou null, `DeleteCommand` ne demande pas de confirmation et n'envoie rien.

---

### 7. Tests de `TreasuryMovementViewModel` (`Easymakemoney.Tests/ViewModels/TreasuryMovementViewModelTests.cs`)
Classe sous `[Collection("SiteCurrency")]`.
- **Ce qui existe déjà** : `TreasuryTests` teste `TreasuryService` et les modèles.
- **Ce que j'ajoute** :
  1. `Save_Validations_DoNotSendRequest` :
     - `Label` vide : affiche `"Donnez un nom au mouvement (par exemple « Loyer du local »)."` dans `ShownInfos`, aucune requête.
     - `AmountText` nul ou <= 0 : affiche `"Saisissez un montant supérieur à zéro."`, aucune requête.
     - Récurrent avec `HasEndDate == true` et `EndDate < StartDate` : affiche `"La date de fin doit suivre la date de début."`, aucune requête.
  2. `Save_DefaultCategory_FixedCostOmittedInSerializedBody` :
     - Mouvement récurrent mensuel pour `"rent"` (loyer = frais fixe par défaut) sans que le gérant ne touche à la case : `FixedCostChoice` vaut `null`.
     - `SaveCommand.ExecuteAsync()` envoie `POST gestion/tresorerie/mouvements`.
     - Le JSON sérialisé ne contient **pas** `"fixedCost"`.
     - Vérification de la navigation : `Shell.Current` étant `null` en test unitaire, la condition `if (Shell.Current is { } shell)` s'évalue à faux et n'échoue pas.
  3. `Save_FixedCostModifiedByManager_FixedCostIncludedInSerializedBody` :
     - Mouvement récurrent pour `"loan"` (non frais fixe par défaut) avec case cochée par le gérant (`IsFixedCost = true`) : `FixedCostChoice` vaut `true`.
     - `SaveCommand.ExecuteAsync()` : le corps sérialisé envoyé contient `"fixedCost":true`.
  4. `Save_OneTimeMovement_FixedCostOmittedInSerializedBody` :
     - Mouvement ponctuel (`SelectedRecurrence = none`) : `CanBeFixedCost == false` et `FixedCostChoice == null`.
     - Le corps sérialisé ne contient pas `"fixedCost"`.

---

## Comment je vérifie

1. **Exécution des tests xUnit** :
   ```bash
   dotnet test Easymakemoney.Tests
   ```
   Résultat attendu : 521 tests existants + tous les nouveaux tests du lot 4f passent avec succès (0 échec, 0 skip).
2. **Rebuild Android** :
   ```bash
   dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -t:Rebuild
   ```
   Résultat attendu : compilation réussie avec exactement 325 avertissements (référence de `main`). La ligne des avertissements sera collée brute dans le compte rendu.
3. **Contrôle d'encodage contre `main`** :
   Vérification UTF-8 sans BOM et fins de ligne LF strictes pour tous les fichiers modifiés et ajoutés par rapport à leurs voisins sur `main`.

---

## Risques et points incertains

- **Drapeau statique `YearStatisticsViewModel.FiscalYearChanged`** : ce drapeau étant statique partagé en mémoire, chaque classe de test le manipulant (`FiscalYearViewModelTests` et `YearStatisticsViewModelTests`) doit impérativement le réinitialiser à `false` dans `Dispose()` pour ne pas contaminer d'autres tests.
- **Dates relatives à aujourd'hui** : toutes les assertions sur les plages `"30d"`, `"custom"` ou périodes doivent calculer leurs attentes via `DateTime.Today` et jamais de dates en dur, afin d'éviter tout échec dépendant du jour d'exécution.
- **Navigation Maui `Shell.Current`** : en test unitaire, `Shell.Current` est `null`. Nous avons vérifié dans le code source de `TreasuryMovementViewModel.cs:139` que la navigation est gardée par `if (Shell.Current is { } shell)`, ce qui évite toute exception.

---

## Questions pour Claude ou le propriétaire

Aucune question bloquante : le comportement des ViewModels et les contrats des services sont parfaitement alignés avec le code et la consigne.

