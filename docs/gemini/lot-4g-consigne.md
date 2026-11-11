# Lot 4g : tests des ViewModels de location (promotion, grilles, liste des promotions)

Consigne de Claude du 11/11/2026. **Écris d'abord ton plan** dans `docs/gemini/lot-4g-plan.md`, puis arrête-toi. Tu ne codes rien avant que je l'aie validé.

- Branche : `feature/lot-4g-tests`, créée depuis `main` (`git switch -c feature/lot-4g-tests main`).
- Pas de commit.
- Référence : 580 tests sur `main`, **270 avertissements Android** (la référence a baissé depuis le lot 4f, avec la suppression des anciens écrans de statistiques).

Les modèles et services sont déjà testés dans `RentalPromotionTests` et `RentalGridTests` : lis-les d'abord et **ne reteste pas ce qu'ils couvrent**. Ce lot porte sur le **comportement des ViewModels** : requêtes envoyées (URL exacte, corps JSON), requêtes qui ne partent pas, messages et confirmations montrés au gérant. Reprends les faux du lot 4f (`Easymakemoney.Tests/Fakes/`) et ajoute ceux qui manquent, **dans le projet de tests seulement** :
- un faux de `IRentalPromotionService` ;
- un faux de `IRentalGridService`.
Chaque faux enregistre les appels et peut lever une `ApiException` construite par `ApiError.FromHttpResponse`, comme dans `AskComment_429CommentLimit_ShowsInfoDialogInsteadOfError`.

| ViewModel | À vérifier |
|---|---|
| `RentalPromotionViewModel` | `Init` : un article (ScopeText avec son nom), un conseil (`adviceId`), jours et pourcentage repris (« 1,2,3,4 », « 20 ») ; `ComputePreview` : message sans requête si la remise est invalide ou si la fin précède le début ; corps de la requête (un article ou un conseil, jours) ; un changement de remise, de dates ou de jours efface l'aperçu ; `Apply` : rien si l'aperçu bloque (chevauchement, non louable) ; confirmation refusée = aucune requête ; acceptée = `ApplyAsync` avec l'empreinte et le même corps que l'aperçu, `RentalStatisticsViewModel.PromotionsChanged` à vrai, message de confirmation ; 409 `PRICES_CHANGED`, `PROMOTION_OVERLAP` et 422 `NOT_RENTABLE` = message d'information puis nouvel aperçu, sans `ShowErrorAsync`. Remets `PromotionsChanged` à faux avant et après chaque test (il est statique) |
| `RentalGridViewModel` | `InitializeAsync` charge une seule fois les grilles et les changements récents ; `SelectGrid` efface l'aperçu ; `ComputePreview` sans grille ou avec un pourcentage invalide = message sans requête ; « Baisse » + « 15 » = `percent: -15` ; arrondi au dollar ou non ; `Apply` sur une grille partagée envoie `allowShared: true` et la confirmation cite les articles ; non partagée = sans `allowShared` ; 409 `PRICES_CHANGED` et `SHARED_GRID` = nouvel aperçu ; 422 `NOTHING_TO_CHANGE` = message ; `Undo` : rien si `CanUndo` est faux ; refus = aucune requête ; 409 `UNDO_EXPIRED`, `GRID_EDITED_SINCE` et `ALREADY_UNDONE` = message propre à chaque code, sans erreur ; rechargement après l'annulation |
| `RentalStatisticsViewModel` (promotions) | Liste : seules les promotions `active` et `upcoming` apparaissent, y compris une promotion terminée aujourd'hui (`endedAt` renseigné, « Se termine ce soir », sans « Terminer ») ; `EndPromotion` : rien si `CanEnd` est faux ; refus = aucune requête ; accepté = `EndAsync(batchId)` puis relecture ; message différent pour une promotion à venir et une promotion en cours ; `PromotionsChanged` à vrai : `InitializeAsync` relit seulement les promotions, pas les statistiques. Construis le ViewModel avec ses **quatre** dépendances (le service des promotions est le quatrième, facultatif) |
| `AdviceViewModel` | Le conseil `RENTAL_LOW_WEEKDAYS` peut créer une promotion (`CanCreatePromotion`) et mène à la promotion de location, pas à la promotion d'article. Si la navigation (`Shell.Current`) empêche de le tester sans toucher l'application, teste `Advice.IsRentalPromotion` et `CanCreatePromotion` et signale-le |

Mêmes règles que les lots 4 à 4f :
- aucun fichier XAML ni `Views/*.xaml.cs`, aucun fichier de l'application modifié, aucun appel au backend, aucun lancement de l'application ;
- pas de reformatage, encodage intact, aucune clé ni adresse réelle, pas de commit ;
- `SiteCurrency` dans la `[Collection("SiteCurrency")]`, culture fixée puis rétablie ;
- pas de comparaison exacte des montants formatés ;
- les dates « aujourd'hui » viennent de `DateTime.Today`, jamais en dur ;
- aucun accès aux membres privés (ni par réflexion) ;
- un bug trouvé donne un test en `Skip` avec la mention BUG.

Vérification, avec les sorties collées telles quelles dans `docs/gemini/lot-4g-compte-rendu.md` :
1. `dotnet test Easymakemoney.Tests` ;
2. Android : `dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home`. Colle la ligne des avertissements telle quelle (référence 270), sans lui chercher de cause ;
3. en dernier, le contrôle d'encodage contre `main`.
