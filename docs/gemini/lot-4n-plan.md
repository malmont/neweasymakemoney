# Lot 4n : plan de Gemini

## Ce que j'ai lu

- `docs/gemini/lot-4n-consigne.md` : consigne du lot 4n (tests des ViewModels des listes de gestion : `ListNewCollectionViewModel`, `ListNewCommandViewModels`, `ListNewFournisseurViewModel`, `ListNewFraisDePortViewModel`, `ListNewNoteDeFraisViewModel`, `AttachProductsViewModel`, `ToReceiveViewModel` ; au plus 25 tests, priorités sur les suppressions, créations/rattachements, à recevoir, chargements/états vides/erreurs, référence Android 216 avertissements).
- `Easymakemoney/ViewModels/Lists/ListNewCollectionViewModel.cs:33-58` : chargement avec `GetListCollectionAsync`, gestion de `IsBusy` et exception ; lignes 98-128 : suppression avec confirmation `ConfirmAsync`, appel `_deleteCollectionUseCase.ExecuteAsync(collection.id)` et rafraîchissement.
- `Easymakemoney/ViewModels/Lists/ListNewCommandViewModels.cs:43-75` : `GetListCommandAsync()` avec vérification `CollectionId == 0`, gestion de `IsBusy` et exception ; lignes 77-107 : suppression avec confirmation `ConfirmAsync`, appel `_deleteCommandUseCase.ExecuteAsync(command.id)` et rafraîchissement ; lignes 125-170 : `ChooseTrip` conditionné par `CanEdit => !IsClosed` et `Shell.Current`.
- `Easymakemoney/ViewModels/Lists/ListNewFournisseurViewModel.cs:43-68` : `GetListFournisseursAsync()` ; lignes 70-109 : suppression avec confirmation `ConfirmAsync`, appel `_deleteFournisseurUseCase.ExecuteAsync(fournisseur.id)`, suppression locale dans `ListFournisseurs.Remove(fournisseur)` et notification info.
- `Easymakemoney/ViewModels/Lists/ListNewFraisDePortViewModel.cs:65-91` : `SelectCurrentPeriodIfNoneAsync()` utilisant `_periodUseCase.GetCurrentPeriodAsync()` ; lignes 93-139 : `GetListFraisDePort()` chargeant en parallèle avec sémaphore (max 4) les frais de chaque commande et construisant les lignes `FraisDePortCommandeRow` ; lignes 162-199 : suppression avec confirmation `ConfirmAsync`, appel `_deleteFraisDePortUseCase.ExecuteAsync(row.Commande.id)` et rechargement.
- `Easymakemoney/ViewModels/Lists/ListNewNoteDeFraisViewModel.cs:62-79` : sélection période courante ; lignes 117-142 : `GetListNoteDeFraisAsync()` ; lignes 196-235 : suppression avec confirmation `ConfirmAsync`, appel `_deleteNoteDeFraisUseCase.ExecuteAsync(noteDeFrais.id)` et retrait local de la liste ; lignes 264-272 : `ShowBottomSheet()` refusé avec avertissement si `CollectionId == 0`.
- `Easymakemoney/ViewModels/Lists/ListNewProductViewModels.cs:124-158` : `DetachProduct` avec confirmation `ConfirmAsync`, appel `_detachProductUseCase.ExecuteAsync(CommandId, product.id)`, retrait local de la liste et gestion d'exception.
- `Easymakemoney/ViewModels/Lists/AttachProductsViewModel.cs:78-122` : recherche et pagination avec `LoadFirstPageAsync` et `LoadMore` ; lignes 161-177 : sélection avec `ToggleProduct`, `SelectedCount` et `HasSelection` ; lignes 266-310 : `Confirm()` validant le rattachement de la sélection via `_attachProductsUseCase.ExecuteAsync(targetCommandId, _selected.Values)`.
- `Easymakemoney/ViewModels/Lists/ToReceiveViewModel.cs:28-51` : `LoadAsync()` regroupant les lignes par commande via `ToReceiveGroup.Build(lines)` et mettant à jour `IsEmpty` ; lignes 53-111 : `ReceiveAll` avec confirmation, premier appel `TryReceiveAsync(group.CommandeId, null)`, boucle de variantes via `Shell.Current.DisplayActionSheetAsync` (reporté si null ou non sélectionné), éventuel second appel avec les choix, affichage du résumé `CommandeReception.Summary` et rechargement.

## Prérequis à installer ou à vérifier (commandes exactes, rien n'est lancé)

Aucun outil ni paquet à installer.
Vérification des tests existants : `dotnet test Easymakemoney.Tests` (973 passants après 4m).
Vérification Android : compilation avec JDK 21 Microsoft (référence 216 avertissements).

## Étapes

### 1. Faux de services dans `Easymakemoney.Tests/Fakes/`

1. **`FakeListCommandService.cs`** (nouveau) : implémente `IListCommandService` avec listes configurables, traçabilité des appels de suppression `DeleteCalls`, et simulation d'exceptions (`PERIOD_CLOSED`, etc.).
2. **`FakeListFraisDePortService.cs`** (nouveau) : implémente `IListFraisDePortService` avec dictionnaire de frais par commande, traçabilité des suppressions `DeleteCalls`, et levée d'exceptions.
3. **`FakeToReceiveService.cs`** (nouveau) : implémente `IToReceiveService` avec liste configurable de `ToReceiveLine`.
4. **`FakeProductAttachmentService.cs`** (complété) : implémentation de `AttachAsync` (avec traçabilité `AttachCalls`) et `DetachAsync` (avec traçabilité `DetachCalls` et résultat booléen configurable).
5. **`FakeListProductsService.cs`** (nouveau ou complété) : implémente `IListProductService` pour `ListNewProductViewModel`.

### 2. Tests unitaires xUnit dans `Easymakemoney.Tests/ViewModels/` (25 tests au total)

#### Fichier `Easymakemoney.Tests/ViewModels/ManagementListDeletionsTests.cs` (10 tests)
Priorité 1 : Suppressions et refus du serveur (commande, fournisseur, note de frais, frais de port, article détaché).

1. **`Command_Delete_Declined_DoesNotCallService`** : confirmation refusée (`ConfirmAsync` -> false) -> aucun appel de suppression.
2. **`Command_Delete_Confirmed_CallsDelete_AndRefreshes`** : confirmation acceptée -> appel `DeleteCommandes(id)`, rafraîchissement de la liste et info "Commande supprimée avec succès.".
3. **`Command_Delete_Error_PeriodClosed_DisplaysApiErrorMessage`** : erreur 409 `PERIOD_CLOSED` lors de la suppression -> dialogue d'erreur affiché avec le message explicatif, la liste n'est pas modifiée.
4. **`Fournisseur_Delete_Declined_DoesNotCallService`** : confirmation refusée -> aucun appel de suppression.
5. **`Fournisseur_Delete_Confirmed_CallsDelete_AndRemovesFromList`** : acceptation -> appel `DeleteFourniseurs(id)`, suppression locale dans `ListFournisseurs`, notification info.
6. **`Fournisseur_Delete_Error_FournisseurInUse_DisplaysApiErrorMessage`** : erreur 409 `FOURNISSEUR_IN_USE` -> dialogue d'erreur affiché avec le message, le fournisseur reste dans la liste.
7. **`NoteDeFrais_Delete_Declined_DoesNotCallService`** : confirmation refusée -> aucun appel de suppression.
8. **`NoteDeFrais_Delete_Confirmed_CallsDelete_AndRemovesFromList`** : acceptation -> appel `DeleteNoteDeFrais(id)`, suppression locale dans `ListNoteDeFrais`, message info.
9. **`FraisDePort_Delete_Confirmed_CallsDelete_AndRefreshes`** : confirmation acceptée -> appel `DeleteFraisDePort(commandId)`, rafraîchit les lignes de la période.
10. **`Product_DetachProduct_Confirmed_CallsDetach_AndRemovesFromList`** : détachement confirmé d'un article d'une commande -> appel `DetachAsync(commandId, productId)`, article retiré de `ListProducts`. Sur erreur 409 `COMMANDE_CLOSED` ou `LINE_RECEIVED`, dialogue d'erreur affiché.

#### Fichier `Easymakemoney.Tests/ViewModels/AttachProductsViewModelTests.cs` (6 tests)
Priorité 2 : Créations et rattachements (corps envoyé, sélection, ce qui ne part pas).

11. **`ToggleProduct_UpdatesSelectionAndCount`** : cocher puis décocher un article met à jour `SelectedCount` et la propriété `HasSelection`.
12. **`Confirm_WithCommandId_AttachesSelectedProducts`** : sélection de 2 articles, `CommandId = 42`, clic `ConfirmCommand` -> appel `_attachProductsUseCase.ExecuteAsync(42, selected)` avec la liste exacte des articles et notification info.
13. **`Confirm_WithoutSelection_DoesNothing`** : clic `ConfirmCommand` sans aucun article sélectionné -> aucun appel à `_attachProductsUseCase`.
14. **`Confirm_Error_PeriodClosed_DisplaysApiErrorMessage`** : sur erreur 409 `PERIOD_CLOSED`, dialogue d'erreur affiché avec le message de l'exception.
15. **`NoteDeFrais_ShowBottomSheet_WithoutCollection_ShowsWarning`** : dans `ListNewNoteDeFraisViewModel`, appel de `ShowBottomSheet()` avec `CollectionId == 0` -> dialogue info "Vous devez choisir obligatoirement une période" sans ouvrir le popup.
16. **`Command_ChooseTrip_WhenClosed_DoesNotOpenPicker`** : dans `ListNewCommandViewModel`, si `CanEdit == false` (`IsClosed == true`), l'appel à `ChooseTripCommand` s'interrompt immédiatement sans aucun appel réseau.

#### Fichier `Easymakemoney.Tests/ViewModels/ToReceiveViewModelTests.cs` (4 tests)
Priorité 3 : Marchandise à recevoir (`ToReceiveViewModel`).

17. **`Load_PopulatesGroups_AndSetsEmptyState`** : chargement des lignes à recevoir, regroupement par commande via `ToReceiveGroup.Build`, mise à jour de `IsEmpty` (faux si groupes présents, vrai si liste vide).
18. **`ReceiveAll_Declined_DoesNotCallService`** : dialogue de confirmation "Marchandise reçue" refusé -> aucun appel à `ReceiveAsync`.
19. **`ReceiveAll_WithoutNeedsVariant_CompletesAndRefreshes`** : confirmation acceptée, premier appel `ReceiveAsync(commandeId, null)` retourne sans variantes en attente -> affichage du résumé `CommandeReception.Summary(received, last, 0)` et rechargement `LoadAsync`.
20. **`ReceiveAll_WithNeedsVariant_VariantChosen_SendsSecondReceive`** : si des lignes nécessitent une variante, avec le point d'entrée `ChooseAsync` proposé à Claude :
    - variante choisie -> second appel `ReceiveAsync` avec le corps `VariantChoicesBody`, cumul du total reçu et affichage du résumé ;
    - option "Plus tard" choisie -> pas de second appel, ligne comptée comme reportée dans le résumé.

#### Fichier `Easymakemoney.Tests/ViewModels/ManagementListLoadingTests.cs` (5 tests)
Priorité 4 : Chargements, états vides, erreurs, sélection de période.

21. **`ListNewCollection_GetList_Success_PopulatesList`** : chargement initial des périodes, `IsBusy` passe à true puis false, `ListCollections` alimentée.
22. **`ListNewCollection_GetList_Error_DisplaysError`** : échec du use case -> `IsBusy` redevient false, dialogue d'erreur affiché.
23. **`ListNewFraisDePort_SelectCurrentPeriodIfNone_LoadsCurrentPeriodAndRows`** : si `CollectionId == 0`, récupération de la période en cours via `_periodUseCase.GetCurrentPeriodAsync()`, affectation de `CollectionId`, `CollectionName`, `IsClosed` et chargement des frais.
24. **`ListNewFraisDePort_GetList_EmptyCollection_ShowsEmptyState`** : période sans commande -> `Rows` vide, `ShowEmptyState = true` avec titre "Aucune commande".
25. **`ListNewNoteDeFrais_GetList_Success_PopulatesList`** : chargement des notes de frais pour la collection sélectionnée, alimentation de `ListNoteDeFrais`.

## Comment je vérifie

1. `dotnet test Easymakemoney.Tests` : l'ensemble des 25 tests doit être vert, portant le total à 998 tests passants (973 + 25), 0 échec, 0 ignoré.
2. `dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home` : exactement **216 avertissements**, 0 erreur.
3. `git diff --check main` vierge (aucune erreur d'encodage, aucun espace en fin de ligne).

## Risques et points incertains

1. **Choix de variante dans `ToReceiveViewModel`** : à la ligne 83 de `ToReceiveViewModel.cs`, le code appelle directement `Shell.Current.DisplayActionSheetAsync(...)`. Dans un environnement de test unitaire headless où `Shell.Current` est `null`, le choix retourne automatiquement `null` (ce qui simule l'abandon/report "Plus tard", mais ne permet pas de tester le chemin avec sélection d'une variante et émission du second appel de réception).
2. **`AttachProductsViewModel.Confirm()` en mode "À classer" (`CommandId == 0`)** : le picker de commande ouvre un popup UI `BottomSheetPopupListView` via `AppPage.Current.ShowPopupAsync(...)`. Dans ce plan, nous testons le rattachement direct lorsque `CommandId != 0` (ajout à une commande existante) pour rester indépendant de l'UI MAUI.

## Questions pour Claude ou le propriétaire

1. **`ToReceiveViewModel.ChooseAsync`** : pour tester le choix d'une variante reçue et l'envoi du second appel `ReceiveAsync` avec `choices` (exactement comme pour `InvoiceReviewViewModel` au lot 4l), pourrais-tu ajouter sur `ToReceiveViewModel` un délégué public :
   ```csharp
   public Func<string, string[], Task<string?>> ChooseAsync { get; set; }
   ```
   utilisé à la place de `Shell.Current.DisplayActionSheetAsync` lorsque `ChooseAsync != null` ?
2. **Refus serveur `FOURNISSEUR_IN_USE`, `COMMANDE_CLOSED`, `LINE_RECEIVED`** : ces erreurs HTTP (409 Conflict) lèvent une `ApiException`. Dans les tests des ViewModels, nous vérifions que `_dialogService.ShowErrorAsync` est appelé avec l'exception correspondante, dont `ex.Message` contient le texte du serveur ou le message de repli d'`ApiError`. Est-ce conforme à tes attentes ?

