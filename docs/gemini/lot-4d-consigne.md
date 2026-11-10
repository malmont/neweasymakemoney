# Lot 4d : tests des périodes, de la réception et de la relecture des factures

Consigne de Claude du 10/11/2026. **Écris d'abord ton plan** dans `docs/gemini/lot-4d-plan.md`, puis arrête-toi. Tu ne codes rien avant que je l'aie validé.

- Branche : `feature/lot-4d-tests`, créée depuis `main` (`git switch -c feature/lot-4d-tests main`).
- Pas de commit.

Les contrats sont dans `docs/periodes-gestion.md` (périodes, résultat, fermeture et réouverture) et dans `docs/perimetre-mobile-web.md` (lecture des documents, lignes d'achat et réception).

| Sujet | À vérifier |
|---|---|
| `PeriodResultService` | URL `periodes/{id}/resultat`. Lecture d'un JSON complet en `PeriodResult` : `result.netRate` nul conservé nul, `cogs.estimatedItems` et `estimatedAmount`, `otherRevenue` |
| `CollectionCloseService` | URLs `dashboard/collection/{id}/close` et `/reopen`. Valeur renvoyée en cas de succès et d'échec. Une erreur 409 `PERIOD_CLOSED` : décris dans le plan ce que fait le code aujourd'hui, et teste-le tel quel |
| `CommandeReceptionService` | URL `commandes/{id}/reception`. Corps transmis tel quel, et corps nul (réception de tout ce qui reste). Lecture de la réponse en `CommandeReception` |
| `ListFraisDePortService` et `ListNoteDeFraisServices` | URLs d'ajout et de suppression. Montants envoyés en centimes (`12,50` saisi donne `1250`, jamais `125000`). Message affiché pour `PERIOD_CLOSED` (409) et pour le champ `date` en `OUTSIDE_PERIOD` |
| `InvoiceLineItemViewModel` | Avec une doublure de `IInvoiceLineHost` : `IsNew` et `IsExisting` selon le lien choisi, `ShowDoubt` (doute présent, ligne non validée), `ToLine()` qui reprend les valeurs saisies, `ApplyGroupValues`, `SetCurrency` (devise du document différente de celle du site, avec et sans taux). `SetPhoto` est exclu, car il dépend de MAUI |
| `InvoiceConfirmationBuilder` | Lis d'abord `InvoiceReviewTests` et liste dans le plan ce qui n'est pas encore couvert. Teste seulement ces manques |

Mêmes règles que les lots 4 à 4c :
- aucun fichier XAML ni `Views/*.xaml.cs`, aucun appel au backend, aucun lancement de l'application ;
- pas de reformatage, encodage intact, aucune clé ni adresse réelle, pas de commit ;
- `SiteCurrency` dans la `[Collection("SiteCurrency")]`, culture fixée puis rétablie ;
- pas de comparaison exacte des montants formatés : vérifie le symbole, l'absence de l'autre symbole et les chiffres ;
- un bug trouvé donne un test en `Skip` avec la mention BUG, sans corriger le code de l'application.

Vérification, avec les sorties collées telles quelles dans `docs/gemini/lot-4d-compte-rendu.md` :
1. `dotnet test Easymakemoney.Tests` ;
2. Android (`-t:Rebuild`), avec pour référence le nombre d'avertissements de `main` au moment où tu crées ta branche ;
3. en dernier, le contrôle d'encodage contre `main`.
