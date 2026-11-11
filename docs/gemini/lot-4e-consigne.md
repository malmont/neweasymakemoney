# Lot 4e : tests des services et des données des derniers écrans de gestion

Consigne de Claude du 10/11/2026. **Écris d'abord ton plan** dans `docs/gemini/lot-4e-plan.md`, puis arrête-toi. Tu ne codes rien avant que je l'aie validé.

- Branche : `feature/lot-4e-tests`, créée depuis `main` (`git switch -c feature/lot-4e-tests main`).
- Pas de commit.
- Référence : 443 tests sur `main`, 325 avertissements Android.

Les contrats sont dans les fiches du backend, résumées dans `docs/etat-fonctions-gestion.md`. Les tests existants sont dans `Easymakemoney.Tests/Models/*Tests.cs` : lis-les d'abord et **ne reteste pas ce qu'ils couvrent déjà**. Dans ton plan, liste pour chaque sujet ce qui existe et ce que tu ajoutes.

| Sujet | À vérifier |
|---|---|
| `HttpService.PutAsyncWithAuth` | Avec `TestHttpMessageHandler` : méthode `PUT`, corps JSON, en-têtes comme `POST` ; erreurs 409 et 422 transformées en `ApiException` (`ApiError.Code`, champs `fields`) ; coupure réseau = `NO_CONNECTION` |
| Erreurs métier attendues par les écrans | Avec le vrai `HttpService` : 409 `PRICES_CHANGED` (prix et promotions), 409 `PROMOTION_EDITED_SINCE`, 409 `PRICE_EDITED_SINCE`, 429 `COMMENT_LIMIT`, 409 `TRIP_OVERLAP` : l'`ApiException` porte le bon `ApiError.Code`, que les écrans testent pour réagir |
| `TreasuryService` | URLs et corps non encore testés : `GetOverviewAsync` (`days`, `locale=fr`), `GetBalancesAsync` (`limit`), `DeleteBalanceAsync`, `GetMovementsAsync` |
| `TripService` | `GetTripsAsync` (`page`, `limit`), `UpdateAsync` (PATCH, corps complet), `DeleteAsync` |
| `PriceService` et `PromotionService` | `PreviewAsync` (corps, un seul périmètre), `PromotionService.ApplyAsync` (`previewHash` et les 3 accords) |
| `PreparationService` | `SimulateAsync` et `AdviseAsync` (route, `locale=fr`, `Prepare()` qui marque la répartition conseillée) |
| `ToReceiveService`, `IToReceiveService` | Lecture d'une réponse vide (`[]`) |
| Données tolérantes | `PreparationValues.Cents` (nombre, objet, null, texte) ; `TreasuryOverview` sans solde (séries nulles) ; `PeriodSimulation.Rows()` avec des scénarios vides ; `PromotionItem.DatesText` avec une date ISO `+00:00` qui tombe la veille en heure locale (culture et fuseau fixés, pas de dépendance au fuseau de la machine : si ce n'est pas possible sans toucher au code, signale-le au lieu de tester) |

Mêmes règles que les lots 4 à 4d :
- aucun fichier XAML ni `Views/*.xaml.cs`, aucun fichier de l'application modifié, aucun appel au backend, aucun lancement de l'application ;
- pas de reformatage, encodage intact, aucune clé ni adresse réelle, pas de commit ;
- `SiteCurrency` dans la `[Collection("SiteCurrency")]`, culture fixée puis rétablie ;
- pas de comparaison exacte des montants formatés ;
- un bug trouvé donne un test en `Skip` avec la mention BUG.

Vérification, avec les sorties collées telles quelles dans `docs/gemini/lot-4e-compte-rendu.md` :
1. `dotnet test Easymakemoney.Tests` ;
2. Android (`-t:Rebuild`) : colle la ligne des avertissements telle quelle, sans lui chercher de cause ;
3. en dernier, le contrôle d'encodage contre `main`.
