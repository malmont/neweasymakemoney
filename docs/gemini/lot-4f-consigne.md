# Lot 4f : tests des ViewModels des nouveaux écrans (Statistiques, Année, exercice, Locations, Frais fixes)

Consigne de Claude du 11/11/2026. **Écris d'abord ton plan** dans `docs/gemini/lot-4f-plan.md`, puis arrête-toi. Tu ne codes rien avant que je l'aie validé.

- Branche : `feature/lot-4f-tests`, créée depuis `main` (`git switch -c feature/lot-4f-tests main`).
- Pas de commit.
- Référence : 521 tests sur `main`, 325 avertissements Android.

Les modèles et services de ces écrans sont déjà testés : `UnifiedStatisticsTests`, `YearStatisticsTests`, `RentalStatisticsTests`, `FixedCostsTests`, `UiDialogStyleTests`. Lis-les d'abord et **ne reteste pas ce qu'ils couvrent déjà**. Ce lot porte sur le **comportement des ViewModels** : quelles requêtes partent (URL exacte, corps), quand elles ne partent pas, ce qui est affiché ou demandé au gérant. Dans ton plan, liste pour chaque ViewModel les cas que tu ajoutes.

Outils : `FakeHttpService` (`RequestedUrls`, `PostedAuthData`, `PutData`, `DeletedUrls`, `ResultToReturn`) et `FakeDialogService` (`ShownInfos`, `ShownErrors`, `ConfirmResult`). Quand un ViewModel dépend d'une interface que `FakeHttpService` ne peut pas servir (par exemple `IListCollectionService`, qui demande un autre type que les statistiques), écris un petit faux **dans le projet de tests**.

| ViewModel | À vérifier |
|---|---|
| `StatistiquesViewModel` | `InitializeAsync` charge une seule fois (`range=month&source=all&locale=fr`) ; `SelectRange` pour `today`, `7d`, `month` et `year` (URL) ; `SelectSource` garde la plage choisie ; `period` choisit la période qui contient aujourd'hui (méthode `Contains`) puis demande `periodId` ; `custom` ne part qu'avec `ApplyCustomDates`, et une fin avant le début ou plus de 366 jours donne un message, sans requête ; `SelectMetric` (`orders`) change `ChartIsMoney` et `ChartPoints` sans nouvelle requête ; `SelectedPointText` avant et après le choix d'un point ; une erreur passe par `ShowErrorAsync` ; `CardRows` regroupe les cartes deux par deux |
| `YearStatisticsViewModel` | Premier chargement sans `year` ; `PreviousYear` et `NextYear` (`year=`, bornes 2000 et exercice en cours, `CanGoBack`, `CanGoForward`) ; `SelectPrice` envoie `pricePercent` et garde l'exercice affiché ; `AskComment` ne part que pour un exercice terminé (POST `gestion/statistiques/annee/avis?year=…&locale=fr`) ; 429 `COMMENT_LIMIT` donne un message d'information, pas une erreur ; `FiscalYearChanged` à vrai : `InitializeAsync` recharge l'exercice en cours et remet le drapeau à faux ; un exercice terminé n'affiche aucun levier |
| `FiscalYearViewModel` | Le mois lu (`GET gestion/reglages/exercice`) est sélectionné ; `IsChanged` ; `Save` sans changement n'envoie rien ; confirmation refusée = aucun `PUT` ; confirmation acceptée = `PUT` avec `{"startMonth":…}`, `YearStatisticsViewModel.FiscalYearChanged` à vrai, message de confirmation. Remets le drapeau à faux à la fin de chaque test (il est statique) |
| `RentalStatisticsViewModel` | `InitializeAsync` une seule fois (sans plage) ; `30d` (dates d'aujourd'hui), `period` (`periodId` après le choix), `custom` (validation comme les Statistiques) ; `WeekdayPoints` et `SelectedDayText` |
| `FixedCostsViewModel` | `LoadAsync` demande `all=1&locale=fr`, remplit `Items` dans l'ordre (actifs, à venir, terminés) et `Categories` ; `Delete` refusé = aucune suppression ; accepté = `DELETE gestion/tresorerie/mouvements/{id}` puis rechargement |
| `TreasuryMovementViewModel` | `Save` : corps envoyé à `gestion/tresorerie/mouvements` avec et sans `fixedCost` (catégorie par défaut, case changée par le gérant, mouvement ponctuel), et messages de validation (nom vide, montant nul) sans requête. La navigation de fin (`Shell.Current`) est nulle en test : vérifie qu'elle ne fait pas échouer le test, sinon signale-le |

Mêmes règles que les lots 4 à 4e :
- aucun fichier XAML ni `Views/*.xaml.cs`, aucun fichier de l'application modifié, aucun appel au backend, aucun lancement de l'application ;
- pas de reformatage, encodage intact, aucune clé ni adresse réelle, pas de commit ;
- `SiteCurrency` dans la `[Collection("SiteCurrency")]`, culture fixée puis rétablie ;
- pas de comparaison exacte des montants formatés ;
- les dates « aujourd'hui » viennent de `DateTime.Today` : construis les dates attendues à partir de `DateTime.Today`, jamais en dur ;
- un bug trouvé donne un test en `Skip` avec la mention BUG.

Vérification, avec les sorties collées telles quelles dans `docs/gemini/lot-4f-compte-rendu.md` :
1. `dotnet test Easymakemoney.Tests` ;
2. Android (`-t:Rebuild`) : colle la ligne des avertissements telle quelle, sans lui chercher de cause ;
3. en dernier, le contrôle d'encodage contre `main`.
