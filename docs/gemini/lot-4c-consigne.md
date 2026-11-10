# Lot 4c : tests de la lecture de documents

Consigne de Claude du 10/11/2026. **Écris d'abord ton plan** dans `docs/gemini/lot-4c-plan.md`, puis arrête-toi. Tu ne codes rien avant que je l'aie validé.

- Branche : `feature/lot-4c-tests`, créée depuis `main` (`git switch -c feature/lot-4c-tests main`).
- Pas de commit.

Le contrat est dans `docs/perimetre-mobile-web.md` (lecture des documents) ; la route est `gestion/documents`.

| Sujet | À vérifier |
|---|---|
| `HttpService.PostFilesAsync` | Avec `TestHttpMessageHandler` : requête `POST` multipart, champs texte présents (`type`, `locale`), une partie `files[]` par fichier avec son nom et son type de contenu, gestion d'erreur comme `POST` (422, 402, coupure réseau) |
| `DocumentReaderService` | URLs : envoi, lecture `gestion/documents/{id}`, relecture `/{id}/relire`, confirmation `/{id}/confirmer` avec le corps transmis tel quel, suppression |
| `GestionDocument` | Lecture du JSON : `status`, `error`, `extraction` conservée en objet JSON |
| `ListCommand.BudgetText` et `FraisDePortCommandeRow.PriceText` | Montant formaté dans la devise de la commande (`currency`), et non dans celle du site |
| `CommandFormModel` | `NeedsExchangeRate` faux si le fournisseur n'a pas de `purchaseCurrency` ou a celle du site, vrai sinon ; `BudgetLabel` |

Mêmes règles que les lots 4 et 4b : aucun fichier XAML ni `Views/*.xaml.cs`, aucun appel au backend, aucun lancement de l'application, pas de reformatage, encodage intact, aucune clé ni adresse réelle, pas de commit. `SiteCurrency` dans la `[Collection("SiteCurrency")]`, culture fixée et rétablie. Un bug trouvé : test en `Skip` avec la mention BUG.

Vérification, sorties collées telles quelles dans `docs/gemini/lot-4c-compte-rendu.md` : `dotnet test Easymakemoney.Tests`, Android (`-t:Rebuild`, référence 324 avertissements), puis en dernier le contrôle d'encodage contre `main`.
