# Lot 4l : plan de Gemini

## Ce que j'ai lu

- `docs/gemini/lot-4l-consigne.md` : consigne du lot, règles de sélection d'au plus 25 tests selon les 5 priorités, gestion des retours et alertes gérant, consigne explicite sur le test en `Skip` BUG pour la conservation des champs inconnus du brouillon.
- `Easymakemoney/ViewModels/Lists/InvoiceReviewViewModel.cs` :
  - `InvoiceReviewViewModel(DocumentReaderUseCase, IListFourniseursServices, ICommandeReceptionService, IDialogService)` (l.44-51) : dépendances injectées.
  - `InitAsync(int documentId)` et `LoadAsync()` (l.125-190) : chargement concurrent de `_documents.GetAsync`, `_suppliers.GetFourniseursList` et `_suppliers.GetTypeFournisseurs`. Statuts acceptés : seul `extracted` est appliqué ; `reading`, `confirmed`, `expired` ou autre alimentent `LoadError`.
  - `BuildSupplierChoices` (l.192-220) : ordonne les fournisseurs en plaçant le reconnu de l'extraction d'abord, puis les candidats proposés par score décroissant, puis la liste complète alphabétique.
  - `ApplyDocument` (l.221-291) : fusion du brouillon enregistré et de l'initial (`InvoiceReview.Merge`), peuplement des alertes doublon/confiance, initialisation de `_lastDraftJson` pour éviter le renvoi d'un brouillon inchangé.
  - `BuildDraft()` (l.347-364) : reconstruction de l'objet `InvoiceReviewDraft` à partir des propriétés du formulaire. Constat : ne conserve pas les propriétés JSON non modélisées du brouillon d'origine.
  - `RefreshState()` (l.370-434) : calcul des alertes devises (`SupplierWarning`), calcul du total des lignes, calcul de validation globale (`validated == Lines.Count && SupplierChosen`), pilotage de `CanConfirm` et `ConfirmHint`.
  - `ValidateLineAsync` (l.511-532) et `ValidateAll` (l.535-561) : contrôle unitaire et en lot par `InvoiceReview.LineProblem`, ouverture de la première ligne en erreur.
  - `AddLinePhotoAsync` (l.563-618) : utilise `Shell.Current.DisplayActionSheetAsync` et `MediaPicker.Default`.
  - `ScheduleDraftSave` (l.630-652), `SaveDraftNowAsync` (l.654-680), `FlushDraftAsync` (l.683-688) et `Stop` (l.871) : debounce 1000 ms, sauvegarde immédiate via `FlushDraftAsync`.
  - `ConfirmAsync` (l.695-785) : validation par `InvoiceConfirmationBuilder.Build`, envoi immédiat du brouillon en attente (`FlushDraftAsync`), appel `ConfirmAsync`, traitement spécifique des erreurs 409/422/etc. (`INVOICE_CONFIRMATION_PENDING`, `PERIOD_CLOSED`, `INVOICE_ALREADY_CONFIRMED`, `DOCUMENT_CONFIRMED`, `DOCUMENT_EXPIRED`), puis déclenchement de la réception si `commande.id > 0`.
  - `ReceiveAsync` et `TryReceiveAsync` (l.787-862) : premier appel sans corps pour recevoir ce qui peut l'être, gestion d'erreur HTTP 404 non critique (message "La réception des commandes n'est pas encore disponible. Votre facture est bien validée."), sélection des variantes via `Shell.Current.DisplayActionSheetAsync` (reporté en `postponed` si indisponible).
- `Easymakemoney.Tests/Models/InvoiceReviewTests.cs` : tests existants du modèle d'extraction, du tri pour révision et du builder.
- `Easymakemoney.Tests/Models/InvoiceConfirmationBuilderMissingTests.cs` : tests exhaustifs des cas d'erreurs du builder.
- `Easymakemoney.Tests/ViewModels/InvoiceLineItemViewModelTests.cs` : tests unitaires des lignes (`InvoiceLineItemViewModel`) avec `FakeInvoiceLineHost`.
- `Easymakemoney/Models/InvoiceReviewDraft.cs` : sérialisation `ToJson()` sans `[JsonExtensionData]`.
- `Easymakemoney.Tests/Fakes/FakeDocumentReaderService.cs` : à compléter pour `SaveDraftAsync` et `UploadLinePhotoAsync`.
- `Easymakemoney/Services/IListFourniseursServices.cs` et `ICommandeReceptionService.cs` : contrats des services fournisseurs et réception.

## Prérequis à installer ou à vérifier (commandes exactes, rien n'est lancé)

Aucun outil ni paquet à installer. Vérifications à lancer après autorisation :
- Tests unitaires : `dotnet test Easymakemoney.Tests`
- Rebuild Android : `dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home`
- Vérification d'encodage : `git diff --check main`

## Étapes

### 1. Faux de test dans `Easymakemoney.Tests/Fakes/`

1. **`FakeListFournisseursServices.cs` (nouveau)** :
   - Implémente `IListFourniseursServices`.
   - Fournit `ObservableCollection<ListFournisseur> Fournisseurs` et `ObservableCollection<TypeFournisseur> Types`.
   - Permet de simuler des erreurs via `Exception? GetFournisseursException` et `Exception? GetTypesException`.
2. **`FakeCommandeReceptionService.cs` (nouveau)** :
   - Implémente `ICommandeReceptionService`.
   - Enregistre `List<(int CommandeId, JObject? Body)> ReceiveCalls`.
   - Fournit `CommandeReception ReceiveResult` et `Exception? ReceiveException`.
3. **`FakeDocumentReaderService.cs` (complété)** :
   - Ajout de `List<(int Id, object Draft)> SaveDraftCalls`.
   - Implémentation réelle de `SaveDraftAsync(int id, object draft)` qui enregistre l'appel (au lieu de lever `NotImplementedException`).
   - Implémentation de `UploadLinePhotoAsync(int id, int n, UploadFile file)` enregistrant dans `List<(int Id, int N, UploadFile File)> UploadLinePhotoCalls`.

### 2. Tests de `InvoiceReviewViewModel` dans `Easymakemoney.Tests/ViewModels/InvoiceReviewViewModelTests.cs`

Création d'une suite de **25 tests** ciblés, isolés dans la collection xUnit `[Collection("SiteCurrency")]`, couvrant strictement les 5 priorités sans redondance avec les tests existants des modèles.

#### Priorité 1 : Ce qui part au serveur à la validation (6 tests)
1. **`Confirm_ExistingSupplier_ValidLines_SendsExpectedBody`** : validation avec fournisseur existant sélectionné ; vérifie que le corps envoyé contient `supplier.mode = "existing"`, `supplier.id`, les lignes avec leurs numéros, quantités, coûts unitaires en centimes et montants attendus.
2. **`Confirm_NewSupplier_ValidLines_SendsNewSupplierFields`** : validation en mode nouveau fournisseur ; vérifie l'envoi de `mode = "new"`, `supplier.name` et `supplier.typeFournisseurId`.
3. **`Confirm_WithShipping_SendsShippingAmount`** : validation incluant les frais de port (`IncludeShipping = true`, `ShippingAmountText = "15,00"`) ; vérifie l'inclusion de `shipping.include = true` et `shipping.amount = 1500` dans le corps.
4. **`Confirm_ForeignCurrency_SendsExchangeRate`** : facture en devise étrangère (ex: USD sur site CAD) ; vérifie que le taux de change saisi (`ExchangeRateText = "1,35"`) est transmis dans le corps sous `invoice.exchangeRate`.
5. **`Confirm_CannotConfirm_WhenLinesNotAllValidated`** : si au moins une ligne n'a pas été validée, `CanConfirm` est `false`, `ConfirmCommand.CanExecute` renvoie `false`, et aucun appel de confirmation n'est émis.
6. **`Confirm_CannotConfirm_WhenSupplierMissing`** : si toutes les lignes sont validées mais qu'aucun fournisseur n'est sélectionné (ou nouveau fournisseur sans nom ou sans type), `CanConfirm` est `false` et `ConfirmHint` indique `"Choisissez le fournisseur pour continuer."`.

#### Priorité 2 : Les codes d'erreur traités à part (7 tests)
7. **`Confirm_Error_InvoiceConfirmationPending_ShowsInformativeMessage`** : simulation de l'exception `ApiException` avec code `INVOICE_CONFIRMATION_PENDING` ; vérifie le message d'information affiché au gérant ("La validation des factures fournisseur arrive très bientôt...") et l'absence de second appel.
8. **`Confirm_Error_PeriodClosed_Accepted_RetriesWithTodayClassificationDate`** : sur erreur `PERIOD_CLOSED` avec `classificationDate == null`, dialogue de confirmation affiché ("Période clôturée") ; si le gérant accepte, un second appel est émis avec `classificationDate = DateTime.Today.ToString("yyyy-MM-dd")`.
9. **`Confirm_Error_PeriodClosed_Declined_NoRetry`** : sur erreur `PERIOD_CLOSED`, si le gérant refuse le classement dans la période en cours, aucun second appel n'est émis.
10. **`Confirm_Error_InvoiceAlreadyConfirmed_Accepted_RetriesWithForceTrue`** : sur erreur `INVOICE_ALREADY_CONFIRMED`, dialogue "Facture déjà enregistrée" affiché ; si le gérant confirme, un second appel est émis avec `force = true`.
11. **`Confirm_Error_InvoiceAlreadyConfirmed_Declined_NoRetry`** : sur erreur `INVOICE_ALREADY_CONFIRMED`, si le gérant refuse de forcer, aucun second appel n'est émis.
12. **`Confirm_Error_DocumentConfirmed_ShowsMessage`** : sur erreur `DOCUMENT_CONFIRMED`, vérifie l'affichage du dialogue "Cette facture est déjà validée." et l'absence de renvoi.
13. **`Confirm_Error_DocumentExpired_ShowsMessage`** : sur erreur `DOCUMENT_EXPIRED`, vérifie l'affichage du dialogue "Cette facture n'a pas été validée dans les 30 jours : elle a été effacée. Lisez-la à nouveau."

#### Priorité 3 : Le brouillon (5 tests)
14. **`Draft_NotSavedOnOpen`** : à l'ouverture de l'écran via `InitAsync`, le brouillon n'est pas envoyé au serveur (`SaveDraftCalls` reste vide).
15. **`Draft_SavedAfterModification_ViaFlushDraft`** : après modification d'un champ du formulaire (ex: `InvoiceNumber`) et appel de `FlushDraftAsync()`, un appel `SaveDraftAsync` est envoyé avec `version = 1`, les données mises à jour et le statut `DraftStatusText` mis à jour ("Brouillon enregistré à...").
16. **`Draft_ResumeOnOpen_RestoresPreviousState`** : chargement d'un document ayant un `draft` JSON enregistré ; vérifie que `InitAsync` restaure le fournisseur sélectionné, le numéro, la date, ainsi que le statut validé des lignes.
17. **`Draft_DuplicateContent_NotResentIfUnchanged`** : deux appels successifs à `FlushDraftAsync()` sans changement entre-temps n'émettent qu'un seul appel réseau.
18. **`Draft_PreservesUnknownFields_OnResave` [Fact(Skip = "BUG: les champs inconnus du brouillon ne sont pas conservés à la réécriture")]** : vérifie qu'un brouillon d'origine contenant un champ inconnu (ex: `"customProperty": "valeur"`) conserve ce champ lors de la réécriture du brouillon par `BuildDraft().ToJson()`. Conforme à la consigne de Claude pour documenter ce comportement.

#### Priorité 4 : La réception juste après la validation (4 tests)
19. **`Confirm_Success_WithCommandeId_ReceiveAccepted_CallsReceiveAsync`** : validation réussie renvoyant un `commande.id > 0` ; le gérant choisit "Marquer comme reçue maintenant" -> appel à `_reception.ReceiveAsync(commandeId, body: null)`.
20. **`Confirm_Success_WithCommandeId_ReceiveDeclined_DoesNotCallReceive`** : validation réussie avec `commande.id > 0` ; le gérant choisit "Plus tard" -> aucun appel à `ReceiveAsync`.
21. **`Receive_Http404_ShowsNotYetAvailableMessage`** : `_reception.ReceiveAsync` lève une `ApiException` avec code 404 (non LINE_NOT_FOUND) -> affichage du dialogue "La réception des commandes n'est pas encore disponible. Votre facture est bien validée." (l.844-849).
22. **`Receive_WithNeedsVariant_ChoicePostponedWhenShellUnavailable`** : premier appel de réception renvoyant `needsVariant` ; sous test unitaire (`Shell.Current == null`), la sélection de variante est reportée (`postponed`) et le résumé final mentionne les lignes reportées.

#### Priorité 5 : ValidateAll, choix fournisseur et ValidateLineAsync (3 tests)
23. **`ValidateAll_ValidatesCleanLines_OpensFirstLineWithProblem`** : `ValidateAll` valide l'ensemble des lignes valides et ouvre en édition (`IsEditing = true`) la première ligne comportant une erreur (ex: quantité manquante), avec affichage de la boîte d'information correspondante.
24. **`ValidateLine_TogglesValidation_OrShowsErrorIfProblem`** : `ValidateLineAsync` valide une ligne correcte (et la dévalide si elle était déjà validée) ; sur une ligne avec anomalie, elle n'est pas validée et la boîte de dialogue d'information détaille le problème.
25. **`SupplierChoice_ExistingVersusNew_SwitchesModeAndBindings`** : test des commandes `ChooseNewSupplier()` et `ChooseExistingSupplier()`, vérifiant `IsNewSupplier`, `IsExistingSupplier`, et le déclenchement de l'avertissement `SupplierWarning` en cas d'incohérence de devise entre le fournisseur et la facture.

## Comment je vérifie

1. `dotnet test Easymakemoney.Tests` :
   - Résultat attendu : 933 tests passants (909 actuels + 24 nouveaux), 0 échec, 1 ignoré (`Draft_PreservesUnknownFields_OnResave`).
2. Compilation Android :
   - `dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home`
   - Résultat attendu : exactement 216 avertissements, 0 erreur.
3. Contrôle d'encodage :
   - `git diff --check main` vierge.

## Risques et points incertains

- **Délai du brouillon (`DraftDelayMs = 1000`)** : le déclenchement normal du brouillon passe par un debounce `Task.Delay(1000, token)`. Pour éviter toute attente réelle dans les tests, les tests déclenchent la sauvegarde immédiate via `await vm.FlushDraftAsync()` (méthode publique déjà prévue pour vider le brouillon avant départ de l'écran ou avant validation).
- **Absence de `Shell.Current` sous xUnit** : `GoBackAsync()` (l.864) et `AddLinePhotoAsync()` (l.563) ainsi que la boîte de dialogue d'action de variantes de réception (l.807) s'appuient sur `Shell.Current`. Les gardes `if (Shell.Current is { } shell)` présentes dans le code empêchent toute exception. Le test de réception avec variantes teste le repli propre (`postponed++`).

## Questions pour Claude ou le propriétaire

1. **Sélection de variante dans `ReceiveAsync`** : dans `ReceiveAsync(commandeId)` (l.807), le choix de variante fait appel à `Shell.Current.DisplayActionSheetAsync`. Sous xUnit, `Shell.Current` étant `null`, les choix de variante ne peuvent pas être sélectionnés et sont systématiquement comptabilisés en `postponed`. Le test 22 vérifie ce comportement de repli. Si Claude souhaite que nous puissions simuler la sélection active d'une variante pour tester le second appel `_reception.ReceiveAsync(commandeId, bodyAvecVariantes)`, une abstraction du dialogue de sélection d'actions (par exemple sur `IDialogService`) serait requise. Est-ce suffisant de tester le comportement de repli pour ce lot ?
2. **Test en `Skip` BUG** : le test 18 est configuré en `[Fact(Skip = "BUG: les champs inconnus du brouillon ne sont pas conservés à la réécriture")]` conformément aux consignes. Est-ce bien ce qui est attendu ?

