# Lot 4f : revue du plan par Claude (11/11/2026)

Plan **validé avec les corrections ci-dessous**. Tu peux coder en les appliquant ; pas de nouveau plan à m'envoyer.

## Corrections obligatoires

1. **Route des locations fausse (§ 5).** La route est `gestion/locations/statistiques`, pas `gestion/statistiques/locations`. Exemples
   exacts (voir `RentalStatisticsTests.Service_Query`) :
   - `gestion/locations/statistiques?locale=fr` ;
   - `gestion/locations/statistiques?periodId=15&locale=fr` ;
   - `gestion/locations/statistiques?from=…&to=…&locale=fr`.
2. **`FakeHttpService` ne lève jamais d'erreur et rend un seul `ResultToReturn` pour tous les appels**, converti dans le type demandé.
   Conséquences :
   - **Erreurs** (`LoadAsync_OnError…`, `AskComment_429…`) : écris un faux **du service** dans `Easymakemoney.Tests/Fakes/`, par
     exemple `FakeUnifiedStatisticsService` et `FakeYearStatisticsService`, avec une propriété `ExceptionToThrow`. Pour le 429, lève
     une vraie `ApiException` dont `ApiError.Code == "COMMENT_LIMIT"` : regarde comment `ApiErrorTests` en construit une. Ne modifie pas
     `FakeHttpService`, d'autres tests en dépendent.
   - **Enchaînement de types** : avant `AskComment`, remplace `ResultToReturn` par un `YearComment` (le chargement attendait un
     `YearStatistics`). Sinon le cast échoue, l'erreur part dans `ShowErrorAsync` et le test passerait pour une mauvaise raison.
     Vérifie aussi `ShownErrors` vide dans les cas « succès ».
   - **`FiscalYearViewModel.Save`** : `ResultToReturn` doit être un `FiscalYearSetting` avec le **nouveau** mois (9). `IsChanged` repasse
     à faux parce que le ViewModel applique la réponse du `PUT`.
3. **Drapeau statique `FiscalYearChanged`** : xUnit exécute en parallèle les classes de collections différentes. `YearStatisticsViewModelTests`
   et `FiscalYearViewModelTests` doivent être **dans la même collection** (`[Collection("SiteCurrency")]`, comme tu le prévois : garde-la
   sur les deux), et remettre le drapeau à faux **au début et à la fin** de chaque test (constructeur et `Dispose`).
4. **Pas d'accès aux membres privés** : `_currentStartYear`, `_requestVersion` ou `_loaded` ne se lisent pas, ni par réflexion. Vérifie
   leur effet par ce qui est public : `CanGoForward`, `RequestedUrls`, nombre de requêtes.
5. **Montants formatés** : `SelectedPointText` après sélection et `MonthlyTotalText` ne se comparent pas en entier. Utilise `Contains` sur
   le libellé (par exemple le mois) et vérifie seulement qu'un montant est présent.

## Précisions

- `StatistiquesViewModel` en mode `period` : le chargement part depuis `OnSelectedPeriodChanged` sans être attendu (`_ = LoadAsync()`).
  Avec `FakeHttpService`, la tâche se termine tout de suite. Si le test devient instable, dis-le au lieu d'ajouter des `Task.Delay`.
- `StatistiquesViewModel`, plage `period` : la requête porte `periodId` **et** `source=all`
  (`gestion/statistiques?periodId=42&source=all&locale=fr`), comme dans ton plan.
- `TreasuryMovementViewModel` :
  - fixe la culture à `fr-CA` et saisis le montant sans séparateur (`"1500"`) ;
  - le cas « loan coché » doit aussi vérifier qu'en remettant la catégorie sur `rent` **sans toucher la case**, la case garde le choix du
    gérant (règle « suit la catégorie tant que le gérant n'y a pas touché »).
- `FixedCostsViewModel`, cas `CanDelete == false` : un `FixedCostItem` avec `id = 0`.
- `YearStatisticsViewModel`, borne 2000 : rends un `YearStatistics` avec `year = 2000` et vérifie qu'aucune requête ne part.
- Commande Android du compte rendu : ajoute `-p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home`,
  comme aux lots précédents.

Le reste du plan est bon : ne reteste pas les modèles, les dates sont construites depuis `DateTime.Today`, aucun fichier de
l'application n'est touché, pas de commit.
