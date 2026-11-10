# Lot 4c : revue du plan, par Claude

Revue du 10/11/2026 de `docs/gemini/lot-4c-plan.md`.

## Verdict

**Validé avec 3 corrections (C1 à C3).** Applique-les en codant, sans réécrire le plan.

## Corrections

### C1. Pas de comparaison exacte des montants

Tes cas « `1 500,00 €` » comparent un texte exact. Le formatage dépend de la version d'ICU et des espaces insécables : c'est la règle T6 du lot 4. Vérifie que le résultat contient le bon symbole (`$` ou `US$` pour USD, `€` pour EUR), ne contient pas l'autre, et contient les chiffres attendus.

### C2. `IsNoConnection` n'existe pas

`ApiException.NoConnection` pose `ApiError.Code = "NO_CONNECTION"`, sans propriété dédiée et sans `StatusCode`. Teste `ex.ApiError.Code == "NO_CONNECTION"`. N'ajoute rien à `ApiException`.

### C3. `CommandFormModel`

- Le constructeur lance un chargement `async void` : ta doublure doit rendre une liste vide immédiatement, et aucun test ne doit dépendre de ce chargement.
- `ExchangeRateLabel` a deux formes : `Taux : 1 USD en CAD` quand la devise du site est connue, `Taux : 1 USD en devise du site` quand `SiteCurrency.CurrentCurrency` est nul. Teste les deux.

## Ce qui ne change pas

Le reste du plan est accepté. Mêmes règles : aucun fichier XAML ni `Views/*.xaml.cs`, aucun appel au backend, aucun lancement de l'application, pas de reformatage, encodage intact, pas de commit. Sorties collées telles quelles dans `docs/gemini/lot-4c-compte-rendu.md`, contrôle d'encodage contre `main` en dernier. Référence Android : 324 avertissements.
