# Lot 4j : plan de Gemini

## Ce que j'ai lu

### Consigne et règles
- `docs/gemini/lot-4j-consigne.md` : consigne du lot 4j (ViewModels voyages et publicités, faux dans `Easymakemoney.Tests/Fakes/`, pas de modification de l'application ni de XAML, référence 216 avertissements Android, distinction claire avec les tests de modèles existants).
- `GEMINI.md` : circuit, § 1 bis (boîte aux lettres), § 1 ter (file d'attente : un seul plan d'avance, aucun code avant validation).

### Tests existants dans `Easymakemoney.Tests/Models/`
- `TripTests.cs:10-164` : teste la désérialisation de `TripFollowUp`, les propriétés de `Trip` (`IsForeign`, `IsCurrent`), les appels réseau de `TripService` via `FakeHttpService`, et les traductions `ApiError` de `TRIP_OVERLAP` et `BEFORE_START`.
- `CampaignTests.cs:9-124` : teste la désérialisation de `Campaign` (contrat avec devises étrangères, montants dans la devise et montants site) et les getters calculés (`AmountText`, `DatesText`, `TargetText`).
- `CampaignRequestBuilderTests.cs:8-196` : teste la validation d'entrées et la construction du JSON JObject (`Build`, `BuildConfirmation`, gestion des devises et taux de change, erreurs de validation).
- `CampaignProfitabilityTests.cs:8-203` : teste la désérialisation du contrat de rentabilité et la classe `CampaignProfitabilityReport` (couleurs, libellés, métriques avant/pendant).
- `CampaignDocumentPrefillTests.cs:8-69` : teste l'extraction de champs (`CampaignDocumentPrefill.Read`) depuis le JSON d'un justificatif de régie.

### Code de production concerné
- `Easymakemoney/ViewModels/Lists/TripViewModels.cs` :
  - `TripListViewModel` (lignes 8-63) : chargement liste, indicateur `IsEmpty`, gestion d'erreur `ShowErrorAsync`, commandes sans Shell (`NewTrip`, `Open`).
  - `TripFollowUpViewModel` (lignes 65-152) : chargement en parallèle (`GetTripAsync` + `GetFollowUpAsync`), jours de dépense (`SpendingDays`), confirmation de suppression (refus = pas de requête, accord = `DeleteAsync`), gestion d'erreur (`ShowErrorAsync`), navigation sans Shell (`Edit`, retour après suppression).
  - `TripFormViewModel` (lignes 154-332) : mode création vs édition (`TripId`, `IsEdit`, `PageTitle`), chargement via `LoadAsync(id)` (population des champs, devise étrangère), validations locales dans `Save()` (nom vide, dates inversées, budget total absent/invalide, budget journalier invalide, taux manquant si devise étrangère), construction et envoi de `TripRequest` (`CreateAsync` ou `UpdateAsync`), exclusion devise si devise du site, gestion des erreurs d'API (dont `TRIP_OVERLAP`), navigation sans Shell.
- `Easymakemoney/ViewModels/Lists/CampaignListViewModel.cs` (lignes 6-170) : chargement première page (`ListAsync(1, 50)`), pagination (`LoadMore` avec dédoublonnage), placeholders vides (`ShowEmptyState`, `EmptyTitle`, `EmptyText`), confirmation de suppression (refus = aucune requête, confirmation = `DeleteAsync` et suppression de la collection observable), traitement de `CAMPAIGN_NOT_FOUND` (info et suppression de la liste) et `PERIOD_CLOSED`, navigation sans Shell.
- `Easymakemoney/ViewModels/Lists/CampaignProfitabilityViewModel.cs` (lignes 5-86) : initialisation et rechargement en parallèle (`GetAsync` + `GetProfitabilityAsync`), construction de `CampaignProfitabilityReport`, gestion de `CAMPAIGN_NOT_FOUND` (`LoadError = "Cette campagne n'existe plus."`), indisponibilité (`data == null`), erreurs réseau.
- `Easymakemoney/ViewModels/Lists/CampaignFormViewModel.cs` (lignes 23-515) :
  - Modes : création directe (`InitAsync(0, 0)`), modification (`InitAsync(id, 0)` avec `GetAsync`), relance (`InitAsync(0, 0, fromId)` avec `CopyCampaignAsync` qui duplique montant, canal, devise, cibles et suffixe le nom avec « (relance) » sur une durée identique à compter d'aujourd'hui), préremplissage depuis document (`UseDocumentAsync(docId)` avec `DocumentReaderUseCase.GetAsync`).
  - Statuts document invalides (`confirmed`, `expired`, `reading`, etc. refusés avec `ShowInfoAsync`).
  - Ciblage d'articles : bascule `IsTargeting` (si faux : vide la sélection et cible tout le magasin ; si vrai : recherche via `GetAvailableProductsUseCase`), ajout/retrait de cibles (`ToggleProduct`, `RemoveTarget`), mise à jour de `TargetSummary`.
  - Validation et enregistrement : validation via `CampaignRequestBuilder`, envoi via `DocumentReaderUseCase.ConfirmAsync` (si document), `ICampaignService.UpdateAsync` (si édition) ou `ICampaignService.CreateAsync` (si création), traitement spécifique de `ApiException` (`CAMPAIGN_NOT_FOUND`, `PERIOD_CLOSED`, etc. via `ShowInfoAsync`), affichage confirmation et navigation sans Shell.

### Services sous-jacents
- `Easymakemoney/Services/ITripService.cs:3-17`
- `Easymakemoney/Services/ICampaignService.cs:5-16`
- `Easymakemoney/Services/IDocumentReaderService.cs:6-17` (injecté dans `DocumentReaderUseCase`)
- `Easymakemoney/Services/IProductAttachmentService.cs:3-8` (injecté dans `GetAvailableProductsUseCase`)

## Prérequis à installer ou à vérifier (commandes exactes, rien n'est lancé)

Aucun outil ni paquet à installer.
Vérification de la base de tests avant de coder le lot (quand la boîte aux lettres donnera le feu vert) :
```
dotnet test Easymakemoney.Tests
```
Référence actuelle constatée : **808 tests passants**, 0 échec.
Référence Android : **216 avertissements**, 0 erreur.

## Étapes

### Étape 1 : Création des faux dans `Easymakemoney.Tests/Fakes/`
1. `FakeTripService.cs` :
   - Implémente `ITripService`.
   - Propriétés d'enregistrement d'appels : `GetTripsCalls`, `GetTripCalls`, `GetFollowUpCalls`, `CreateCalls`, `UpdateCalls`, `DeleteCalls`, `AttachNotesCalls`, `AttachCommandesCalls`.
   - Résultats configurables : `TripsResult`, `TripResult`, `FollowUpResult`.
   - Exceptions configurables : `GetTripsException`, `GetTripException`, `GetFollowUpException`, `CreateException`, `UpdateException`, `DeleteException`.
2. `FakeCampaignService.cs` :
   - Implémente `ICampaignService`.
   - Propriétés d'enregistrement d'appels : `ListCalls` (liste des tuples `(page, limit)`), `GetCalls`, `CreateCalls`, `UpdateCalls`, `DeleteCalls`, `GetProfitabilityCalls`.
   - Résultats configurables : `ListResult`, `CampaignResult`, `ProfitabilityResult`.
   - Exceptions configurables : `ListException`, `GetException`, `CreateException`, `UpdateException`, `DeleteException`, `ProfitabilityException`.
3. `FakeDocumentReaderService.cs` :
   - Implémente `IDocumentReaderService`.
   - Propriétés d'enregistrement d'appels : `GetCalls`, `ConfirmCalls` (tuples `(id, body)`), `ListCalls`, `UploadCalls`, `DeleteCalls`.
   - Résultats configurables : `DocumentResult`, `ConfirmResult`.
   - Exceptions configurables : `GetException`, `ConfirmException`.
4. `FakeProductAttachmentService.cs` :
   - Implémente `IProductAttachmentService`.
   - Propriétés d'enregistrement d'appels : `GetAvailableCalls` (tuples `(query, page, limit, allProducts)`), `AttachCalls`, `DetachCalls`.
   - Résultats configurables : `AvailableProductsResult`.
   - Exceptions configurables : `GetAvailableException`.

### Étape 2 : Tests des ViewModels de voyages dans `Easymakemoney.Tests/ViewModels/TripViewModelsTests.cs`
Fichier placé dans la collection `[Collection("SiteCurrency")]`.

1. **`TripListViewModel`** :
   - `TripList_LoadAsync_PopulatesTripsAndSetsIsEmptyFalse` : charge 2 voyages, vérifie que `Trips` contient les 2 éléments et `IsEmpty == false`. *(Ajoute par rapport à `TripTests` : vérifie la synchronisation de l'ObservableCollection et l'état vide du ViewModel).*
   - `TripList_LoadAsync_Empty_SetsIsEmptyTrue` : retour liste vide, vérifie `IsEmpty == true`. *(Ajoute : comportement de l'écran vide).*
   - `TripList_LoadAsync_Error_ShowsErrorDialog` : exception réseau sur `GetTripsAsync`, vérifie que `ShowErrorAsync` est appelé et `IsBusy == false`. *(Ajoute : traitement de panne réseau).*
   - `TripList_Commands_WithoutShell_ExecuteSafely` : exécute `NewTripCommand` et `OpenCommand(trip)` sans Shell, vérifie l'absence de plantage. *(Ajoute : robustesse UI hors runtime MAUI).*

2. **`TripFollowUpViewModel`** :
   - `TripFollowUp_LoadAsync_LoadsTripAndSpendingDays` : charge un voyage et son suivi, vérifie la population de `Trip`, `FollowUp` et `Days` (avec `SpendingDays`). *(Ajoute par rapport à `TripTests` : orchestration conjointe de deux appels et projection dans la collection observable).*
   - `TripFollowUp_LoadAsync_Error_ShowsErrorDialog` : exception levée (ex: `TRIP_NOT_FOUND` ou 500), vérifie `ShowErrorAsync`. *(Ajoute : notification gérant).*
   - `TripFollowUp_Delete_ConfirmationRefused_DoesNotCallService` : gérant clique sur Annuler dans le dialogue de confirmation, vérifie qu'aucun appel à `DeleteAsync` n'est émis. *(Ajoute : garde-fou de confirmation).*
   - `TripFollowUp_Delete_Confirmed_CallsDeleteAsync` : gérant confirme, vérifie l'appel `DeleteAsync(TripId)` et l'exécution sécurisée du retour sans Shell. *(Ajoute : chaîne de suppression complète).*
   - `TripFollowUp_Delete_Error_ShowsErrorDialog` : exception sur suppression, vérifie affichage d'erreur et aucun plantage. *(Ajoute : gestion d'échec de suppression).*

3. **`TripFormViewModel`** :
   - `TripForm_Init_CreateMode_InitializesDefaults` : `TripId == 0`, vérifie `IsEdit == false`, `PageTitle == "Nouveau voyage"`, `CurrencyChoices` initialisé. *(Ajoute : état initial création).*
   - `TripForm_LoadAsync_EditMode_PopulatesFieldsAndCurrency` : `LoadAsync(42)` charge un voyage en devises étrangères (EUR, taux 1.48), vérifie les champs, `IsEdit == true`, `PageTitle == "Modifier le voyage"`, `SelectedCurrency == "EUR"`, `NeedsExchangeRate == true`. *(Ajoute : hydratation formulaire modification).*
   - `TripForm_Save_ValidationFailures_DoNotSendRequest` (`[Theory]`) :
     - Nom vide
     - Date de fin antérieure à la date de début
     - Budget total vide ou égal à 0
     - Budget journalier négatif
     - Devise étrangère sans taux de change
     Vérifie pour chaque cas qu'une explication est montrée via `ShowInfoAsync` et qu'aucun appel à `CreateAsync` ou `UpdateAsync` n'est envoyé. *(Ajoute par rapport à `CampaignRequestBuilderTests` : vérification que les règles de validation du formulaire bloquent l'envoi réseau et informent le gérant).*
   - `TripForm_Save_Create_SiteCurrency_OmitsCurrencyAndRate` : création avec devise du site, vérifie l'appel à `CreateAsync` avec `currency == null` et `exchangeRate == null`, dates au format Iso, budget converti en centimes. *(Ajoute : contrat de création voyage local).*
   - `TripForm_Save_Edit_ForeignCurrency_SendsCurrencyAndRate` : modification (`TripId = 5`) avec devise EUR et taux 1.48, vérifie l'appel `UpdateAsync(5, request)` avec `currency == "EUR"` et `exchangeRate == 1.48`. *(Ajoute : contrat de modification voyage étranger).*
   - `TripForm_Save_ApiConflict_TripOverlap_ShowsErrorDialog` : serveur renvoie 409 `TRIP_OVERLAP`, vérifie que l'erreur est interceptée et transmise à `ShowErrorAsync`. *(Ajoute : retour utilisateur sur chevauchement de voyage).*

### Étape 3 : Tests des ViewModels de campagnes dans `Easymakemoney.Tests/ViewModels/CampaignViewModelsTests.cs`
Fichier placé dans la collection `[Collection("SiteCurrency")]`.

1. **`CampaignListViewModel`** :
   - `CampaignList_LoadFirstPageAsync_PopulatesListAndSetsEmptyState` : charge 2 campagnes, vérifie que `Campaigns` contient les 2 éléments, `ShowEmptyState == false`. *(Ajoute par rapport à `CampaignTests` : initialisation de liste observable et visibilité de l'état vide).*
   - `CampaignList_LoadFirstPageAsync_Empty_ShowsEmptyPlaceholder` : 0 résultat, vérifie `ShowEmptyState == true`, `EmptyTitle == "Aucune campagne"`. *(Ajoute : affichage guidant le gérant).*
   - `CampaignList_LoadFirstPageAsync_Error_ShowsErrorDialogAndErrorPlaceholder` : erreur réseau, vérifie appel à `ShowErrorAsync` et `EmptyTitle == "Chargement impossible"`. *(Ajoute : gestion de panne réseau).*
   - `CampaignList_LoadMore_LoadsNextPageAndAppendsWithoutDuplicates` : première page de 50 éléments, `LoadMore` demande la page 2 avec limit 50 et ajoute les éléments sans doublon. *(Ajoute : pagination progressive et anti-doublon).*
   - `CampaignList_Delete_ConfirmationRefused_DoesNotSendRequest` : refus gérant, aucune requête `DeleteAsync`. *(Ajoute : confirmation utilisateur).*
   - `CampaignList_Delete_Confirmed_RemovesFromList` : confirmation gérant, appelle `DeleteAsync(id)` et retire la campagne de `Campaigns`. *(Ajoute : mise à jour instantanée de la vue).*
   - `CampaignList_Delete_404CampaignNotFound_ShowsInfoAndRemovesFromList` : exception `CAMPAIGN_NOT_FOUND` sur suppression, vérifie message d'information (« Cette campagne n'existe plus. ») et retrait de la campagne de la liste. *(Ajoute : résilience face à une suppression distante concurrente).*
   - `CampaignList_Delete_409PeriodClosed_ShowsInfoAndKeepsInList` : exception `PERIOD_CLOSED`, vérifie message explicatif (« Cette campagne appartient à une période clôturée... ») et conservation dans la liste. *(Ajoute : respect de la clôture de période).*

2. **`CampaignProfitabilityViewModel`** :
   - `CampaignProfitability_InitAsync_LoadsCampaignAndReport` : charge campagne et rapport de rentabilité complet, vérifie `IsLoaded == true`, `Report != null`, `GainColor` non neutre, `LoadError == null`. *(Ajoute par rapport à `CampaignProfitabilityTests` : cycle de vie du ViewModel et synchronisation).*
   - `CampaignProfitability_InitAsync_404NotFound_SetsLoadError` : `ApiException` `CAMPAIGN_NOT_FOUND`, vérifie `LoadError == "Cette campagne n'existe plus."` et `IsLoaded == false`. *(Ajoute : message propre au gérant).*
   - `CampaignProfitability_InitAsync_NullReport_SetsUnavailableError` : rapport nul renvoyé par le service, vérifie `LoadError == "Rentabilité indisponible pour le moment."`. *(Ajoute : cas où les statistiques ne sont pas encore calculées).*

3. **`CampaignFormViewModel`** :
   - `CampaignForm_InitAsync_CreateMode_DefaultsToSiteCurrencyAndWholeStore` : création directe (`0, 0`), vérifie `IsEdit == false`, `HasDocument == false`, `CanReadInvoice == true`, `TargetSummary == "Toutes les ventes du magasin"`. *(Ajoute : initialisation du formulaire).*
   - `CampaignForm_InitAsync_EditMode_LoadsCampaignAndPopulatesFields` : modification (`CampaignId = 10`), charge la campagne et peuple nom, canal, montant, dates, cibles d'articles. *(Ajoute : hydratation complète).*
   - `CampaignForm_InitAsync_EditMode_404NotFound_SetsLoadError` : campagne introuvable, vérifie `LoadError == "Cette campagne n'existe plus."`. *(Ajoute : gestion de campagne supprimée).*
   - `CampaignForm_InitAsync_Relance_CopiesParametersWithSlidingDatesAndRenames` : relance (`fromCampaignId = 10`), vérifie que le nom devient `"{nom} (relance)"`, dates glissantes à partir d'aujourd'hui, montant, canal et devises copiés, `HasDocument == false`. *(Ajoute : comportement du bouton Relancer conseillé par le guide).*
   - `CampaignForm_UseDocumentAsync_PrefillsFromExtractedInvoice` : justificatif extrait (statut `extracted`), vérifie le préremplissage automatique du montant, canal, marchand, date, remarques, et `DocumentId` positionné. *(Ajoute : intégration OCR dans le formulaire).*
   - `CampaignForm_UseDocumentAsync_NonExtractedStatus_ShowsAppropriateMessage` (`[Theory]`) :
     - statut `confirmed` -> « Cette facture est déjà enregistrée. »
     - statut `expired` -> « Cette facture n'a pas été validée à temps... »
     - statut `reading` -> « La lecture de la facture n'est pas terminée... »
     Vérifie que le dialogue informatif approprié est affiché et `DocumentId` reste 0. *(Ajoute : sécurisation contre la confirmation de factures non prêtes).*
   - `CampaignForm_Targeting_ToggleAndRemove_UpdatesTargetProductsAndSummary` : bascule `IsTargeting = true`, coche un produit (devient ciblé, `TargetSummary == "1 article ciblé"`), décoche ou supprime la cible (`RemoveTarget`), puis désactive `IsTargeting = false` (redevient `Toutes les ventes du magasin`). *(Ajoute : logique interactive de ciblage).*
   - `CampaignForm_Save_ValidationErrors_ShowsMissingFieldsDialog` : nom manquant ou montant vide, vérifie que le dialogue « À compléter » est affiché et qu'aucune requête n'est émise. *(Ajoute : validation client avant requête).*
   - `CampaignForm_Save_Create_SendsCreateRequest` : sauvegarde d'une nouvelle campagne, vérifie l'appel `_campaigns.CreateAsync` avec le corps JSON attendu. *(Ajoute : émission création).*
   - `CampaignForm_Save_Edit_SendsUpdateRequest` : sauvegarde d'une modification, vérifie l'appel `_campaigns.UpdateAsync(id, body)`. *(Ajoute : émission modification).*
   - `CampaignForm_Save_WithDocument_CallsConfirmDocumentAsync` : sauvegarde avec facture attachée, vérifie l'appel `_documents.ConfirmAsync(docId, body)` au lieu de `_campaigns.CreateAsync`. *(Ajoute : aiguillage vers la confirmation de justificatif).*
   - `CampaignForm_Save_ApiError_ShowsTranslatedMessage` : erreur API telle que `PERIOD_CLOSED` ou `RATE_REQUIRED`, vérifie que le message traduit est présenté via `ShowInfoAsync("...", "Enregistrement impossible")`. *(Ajoute : gestion des rejets backend).*

Total des tests prévus : **14 tests** pour les voyages + **18 tests** pour les campagnes = **32 méthodes de test** (environ 40 exécutions avec les Theories).

## Comment je vérifie

1. **Exécution des tests xUnit** :
   ```
   dotnet test Easymakemoney.Tests
   ```
   Résultat attendu : 808 + environ 40 tests = **~848 tests passants**, 0 échec, 0 ignoré.
2. **Rebuild Android** :
   ```
   dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home
   ```
   Résultat attendu : **216 avertissements**, 0 erreur.
3. **Contrôle d'encodage et fins de ligne** :
   ```
   git diff --check main
   ```
   Résultat attendu : aucune sortie, code de sortie 0.

## Risques et points incertains

1. **Dépendances de `CampaignFormViewModel`** :
   `CampaignFormViewModel` reçoit `DocumentReaderUseCase` et `GetAvailableProductsUseCase` (qui sont des classes concrètes, non des interfaces). Ces UseCases prennent respectivement `IDocumentReaderService` et `IProductAttachmentService`. En créant `FakeDocumentReaderService` et `FakeProductAttachmentService`, nous instancions simplement ces UseCases dans les tests unitaires sans aucune difficulté.
2. **Méthodes asynchrones avec délai de saisie (`SearchAfterDelayAsync`)** :
   Dans `CampaignFormViewModel`, le changement de `ProductSearchText` déclenche un délai de 300 ms avec `CancellationTokenSource`. Dans les tests unitaires de ciblage, pour tester la sélection directe sans dépendre d'un timer, nous pouvons manipuler `TargetProducts` et `SearchResults` directement ou appeler les commandes synchrones `ToggleProduct` et `RemoveTarget`.
3. **Absence de runtime Shell (`Shell.Current`)** :
   Les commandes de navigation (`NewTrip`, `Open`, `Edit`, retour `..` après `Save`) testent si `Shell.Current is { } shell`. En test unitaire xUnit hors runtime MAUI, `Shell.Current` est `null`. Les tests vérifieront que ces commandes s'exécutent en toute sécurité sans lever de `NullReferenceException`.

## Questions pour Claude ou le propriétaire

Aucune question bloquante : le code source des ViewModels et des modèles a été lu intégralement, le comportement réel est parfaitement clair et conforme aux règles établies.
Le plan est prêt pour validation dans la boîte aux lettres.

