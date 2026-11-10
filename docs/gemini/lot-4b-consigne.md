# Lot 4b : tests des nouveaux écrans de gestion

Consigne de Claude du 10/11/2026. **Écris d'abord ton plan** dans `docs/gemini/lot-4b-plan.md`, puis arrête-toi. Tu ne codes rien avant que je l'aie validé.

- Branche : `feature/lot-4b-tests`, créée depuis `main` (`git switch -c feature/lot-4b-tests main`).
- Pas de commit.

Complète `Easymakemoney.Tests` pour le code ajouté depuis le lot 4. Le contrat est dans `docs/periodes-gestion.md` et `docs/lot-rattachement-produits.md`.

| Sujet | À vérifier |
|---|---|
| `HttpService` : devise | `siteCurrency` passe avant `currency` ; un objet avec `purchaseCurrency` ne change pas la devise du site |
| `SiteCurrency.GetCurrencySymbol` | Résultat identique à chaque appel ; repli sur le code ISO |
| `ProductAttachmentService` | URL de recherche échappée ; `purchasePrice` envoyé seulement s'il est saisi ; route de retrait |
| `RepriseService` | Corps de simulation et d'application ; `estimateCoefficient` absent si non fourni ; annulation |
| `PeriodResult` | Lecture du JSON du contrat P2 ; `netRate` nul ; articles sans coût ; perte (`IsLoss`) ; part estimée |
| `ListCollection` | `kind`, `IsPeriod`, `CanDelete`, `StatusText` (à venir, en cours, clôturée) |
| `FraisDePortCommandeRow` | Ajout et suppression impossibles si la période est clôturée |
| `ApiError` | `PERIOD_CLOSED`, `PERIOD_NOT_DELETABLE`, `NOT_CLOSED`, `OUTSIDE_PERIOD` : jamais le code brut à l'écran |

**Activation :** les 2 tests ignorés du lot 4 restent ignorés. Si tu proposes une petite abstraction de `DeviceInfo`, montre-la dans le plan sans l'appliquer.

Vérification, sorties collées telles quelles dans `docs/gemini/lot-4b-compte-rendu.md` : `dotnet test Easymakemoney.Tests`, Android (`-t:Rebuild`, référence 315 avertissements), puis en dernier le contrôle d'encodage contre `main`.

**Mêmes règles :** aucun fichier XAML ni `Views/*.xaml.cs`, aucun appel au backend, aucun lancement de l'application, pas de reformatage, encodage intact, aucune clé ni adresse réelle, pas de commit. Un bug trouvé : test en `Skip` avec la mention BUG, sans corriger l'application.
