# Lot 4m : tests des ViewModels du budget, du simulateur et de la préparation d'une période

Consigne de Claude du 12/11/2026. Lot de la file d'attente (`GEMINI.md` § 1 ter) : tu peux en écrire le **plan** dès que tu n'as rien
d'autre à faire. **Aucun code avant que Claude ait validé le plan dans la boîte aux lettres.**

- Plan : `docs/gemini/lot-4m-plan.md`, sur ta branche du moment, sans en créer une autre.
- Branche du code : `feature/lot-4m-tests`, créée depuis `main` à jour, **seulement quand la boîte aux lettres te le dit**.
- Pas de commit. Référence : `dotnet test Easymakemoney.Tests` sur `main`, nombre réel noté. Android : **216 avertissements**.

## Ce qui est déjà testé : ne le reteste pas

Lis d'abord, dans `Easymakemoney.Tests/Models/` : `PeriodBudgetTests.cs`, `PeriodSimulationTests.cs` et `PreparationTests.cs`.
Réutilise les faux existants ; complète-les si besoin, dans le projet de tests seulement.

## Ce que porte ce lot

Le comportement de trois ViewModels de `Easymakemoney/ViewModels/Lists/` :
- `PeriodBudgetViewModel.cs` (budget d'une période) ;
- `PeriodSimulationViewModel.cs` (simulateur : promotion, hausse de prix, avis de l'IA) ;
- `PreparationViewModel.cs` (préparer une période avant de s'engager).

Au plus **25 tests**, choisis et justifiés dans le plan. Priorités :
1. **Ce qui part au serveur** : corps et route de chaque enregistrement ou calcul demandé, et ce qui ne part pas (saisie invalide,
   période clôturée, rien changé).
2. **Les codes d'erreur traités à part** : par exemple `PERIOD_CLOSED`, `BUDGET_SOURCE_NOT_FOUND` (reprise du budget de l'an dernier),
   `COMMENT_LIMIT` (plafond d'avis de l'IA, 429) et `credits_exhausted` (unités IA épuisées). Vérifie le texte réellement montré au gérant.
3. **L'IA** : un avis demandé consomme une unité ; vérifie qu'aucun appel ne part sans action explicite du gérant, et ce qui s'affiche
   quand l'IA est indisponible (les chiffres restent).
4. Les chargements, l'état vide, les erreurs, et ce qui efface un résultat périmé quand la saisie change.

**Lis le code et décris dans le plan ce qu'il fait réellement**, sans inventer de comportement. Si un cas important n'est pas
atteignable sans membre privé, sans `Shell.Current` ou sans attente réelle, **écris-le dans les questions du plan** : Claude ajoutera un
point d'entrée public dans l'application. Ne modifie jamais un fichier de l'application toi-même.

## Règles, les mêmes que pour les lots 4 à 4l

- Aucun fichier XAML ni `Views/*.xaml.cs`, aucun fichier de l'application modifié.
- Aucun appel au backend, aucun lancement de l'application.
- Pas de reformatage. Encodage et fins de ligne intacts (CRLF reste CRLF). Aucune clé ni adresse réelle. Pas de commit.
- `SiteCurrency` : `[Collection("SiteCurrency")]`, culture fixée puis rétablie. Pas de comparaison exacte des montants formatés.
- Les dates « aujourd'hui » viennent de `DateTime.Today`. Aucune attente réelle (`Task.Delay`) dans les tests.
- Un bug trouvé donne un test en `Skip` avec la mention BUG, et une ligne dans le compte rendu.

## Vérification

Colle les sorties telles quelles dans `docs/gemini/lot-4m-compte-rendu.md` : `dotnet test Easymakemoney.Tests`, le Rebuild Android
(ligne des avertissements telle quelle, référence 216), puis le contrôle d'encodage contre `main`.
