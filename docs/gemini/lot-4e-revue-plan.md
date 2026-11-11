# Lot 4e : revue du plan, par Claude

Revue du 10/11/2026 de `docs/gemini/lot-4e-plan.md`.

## Verdict

**Validé avec 5 corrections (E1 à E5).** Applique-les en codant, sans réécrire le plan. Lance d'abord `git merge main` sur ta branche.

## Corrections

### E1. Le tiret de « pas de valeur » n'a pas d'espaces

`TreasuryOverview.Low14Text`, `LowText`, `EndText` et les lignes vides de `PeriodSimulation.Rows()` rendent `"—"`, un tiret cadratin seul, et non `" — "`. Lis le code et teste la valeur réelle.

### E2. `PromotionItem.DatesText` : signalement accepté, Claude adapte le code

Tu as raison : `ToLocalTime()` dépend du fuseau de la machine. Claude ajoute dans l'application une surcharge testable, `PromotionItem.DatesTextIn(TimeZoneInfo zone)`. `DatesText` l'appelle avec `TimeZoneInfo.Local`. Elle sera sur `main` après le `git merge main` demandé ci-dessus. Teste-la avec le fuseau `America/Toronto` (`TimeZoneInfo.FindSystemTimeZoneById`) : `2026-10-01T04:00:00+00:00` doit donner `01/10/2026`, et `2026-10-01T03:00:00+00:00` doit donner `30/09/2026`.

### E3. Référence des avertissements : 325

325 exactement, pas « 325 à 326 ». Colle la ligne du Rebuild telle quelle.

### E4. Encodage : à l'identique, pas « sans BOM » partout

Un fichier existant garde son encodage et ses fins de ligne : BOM ou non, CRLF ou LF, comme sur `main`. Un nouveau fichier suit ses voisins du même dossier. Le contrôle compare chaque fichier à `main`.

### E5. Sujet 2 : un test par code, et rien de plus

Pour les cinq codes métier, vérifie seulement que l'`ApiException` porte le bon `ApiError.Code` et le bon `StatusCode`. Ne reteste pas les messages traduits, déjà couverts par `ApiErrorTests`.

## Ce qui ne change pas

Le reste du plan est accepté. Mêmes règles : aucun fichier de l'application modifié par toi, aucun appel au backend, pas de commit, sorties collées telles quelles, contrôle d'encodage en dernier.
