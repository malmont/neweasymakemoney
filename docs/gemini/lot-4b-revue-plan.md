# Lot 4b : revue du plan, par Claude

Revue du 10/11/2026 de `docs/gemini/lot-4b-plan.md`.

## Verdict

**Validé avec 5 corrections (R1 à R5).** Applique-les en codant, sans réécrire le plan.

**Avant de coder :** `main` a avancé. Lance `git merge main` sur ta branche.

## Corrections

### R1. Le test de déterminisme de `GetCurrencySymbol` ne prouve rien

Deux appels consécutifs renvoient le même résultat grâce au cache, même si le choix du symbole dépendait de l'ordre des cultures. Ce test passerait avec l'ancien bug.

- Garde un test simple sur le résultat : `GetCurrencySymbol("CAD")` contient `$`, `GetCurrencySymbol("EUR")` contient `€`.
- Retire la mention « déterminisme » du nom du test.
- `FindSymbol` lit `CultureInfo.GetCultures` directement, donc l'ordre n'est pas testable sans modifier le code : signale-le dans le compte rendu, sans le changer.

### R2. Culture et devise dans `PeriodResult`

`NetRateText` et tous les textes de montant dépendent de la culture et de `SiteCurrency`. Mets `PeriodResultTests` dans la `[Collection("SiteCurrency")]`, fixe la culture dans chaque test (`NetRateText` : `15.2` en `en-US`, `15,2` en `fr-CA`) et rétablis-la ensuite. Pas de comparaison exacte des montants (T6 du lot 4).

### R3. Tests du code ajouté depuis ta consigne

Ajoute, après le `git merge main` :

| Sujet | À vérifier |
|---|---|
| `ListCommandService.PostCommandes` | `id = 0` : `POST commandes` ; `id > 0` : `POST collections/{id}/commandes` |
| `PeriodService` | `GET periodes/courante` ; `PATCH periodes/{id}` avec `budget` en centimes |
| `PeriodUseCase.UpdateBudgetAsync` | `12.345` envoie `1235` |
| `HttpService.PatchAsyncWithAuth` | Méthode `PATCH`, corps JSON, gestion d'erreur comme `POST` |

### R4. `DeviceInfo` : abstraction acceptée, à appliquer

Applique-la dans ce lot et retire le `Skip` des 2 tests d'activation.

- Paramètre **obligatoire** dans le constructeur de `SiteService`, sans valeur par défaut : `new MauiDeviceInfoService()` dans le constructeur cacherait un oubli d'enregistrement.
- Enregistre `IDeviceInfoService` dans `MauiProgram.cs`, et rien d'autre dans ce fichier.

### R5. `OUTSIDE_PERIOD` est un code de champ

Il passe par `TranslateValidationCode`, pas par les messages de repli. Teste-le avec `{"fields": {"date": "OUTSIDE_PERIOD"}}` et vérifie le message « La date doit être comprise dans cette période. ».

## Ce qui ne change pas

Le reste du plan est accepté. Rappels : aucun fichier XAML ni `Views/*.xaml.cs`, aucun appel au backend, aucun lancement de l'application, sorties collées telles quelles après ta dernière modification, contrôle d'encodage contre `main` en dernier, pas de commit. Référence Android : prends le nombre d'avertissements de `main` après le `git merge`.
