# Lot 4 : revue du plan, par Claude

Revue du 09/11/2026 de `docs/gemini/lot-4-plan.md`.

## Verdict

**Validé avec 9 corrections (T1 à T9).** Applique-les en codant, sans réécrire le plan.

## Réponses à tes questions

1. **Restauration :** oui, tu peux lancer `dotnet restore`, pour les paquets du plan uniquement (voir T2).
2. **Arrondi :** ni l'un ni l'autre : c'est un bug, et je t'autorise à le corriger dans ce lot (voir T3).

## Corrections

### T1. `net10.0` en dernier

Le premier élément de `TargetFrameworks` est la cible par défaut des outils. Écris `net10.0-android;net10.0-ios;net10.0-maccatalyst;net10.0`.

### T2. Paquets et commande MSBuild

- Retire `coverlet.collector` : personne n'a demandé de mesure de couverture.
- `dotnet msbuild` ne connaît pas `-f`. La commande de contrôle est `dotnet msbuild Easymakemoney/Easymakemoney.csproj -p:TargetFramework=net10.0 -getItem:Compile`, filtrée sur `Platforms`.
- Après `dotnet sln add`, vérifie que le diff de `Easymakemoney1.sln` ne contient que l'ajout du projet.

### T3. Montants : correction autorisée, seule exception à la règle des bugs

Les saisies sont des `double?`. `Math.Round` sans option donne `12,345` → `1234`, et `1,005 * 100` vaut `100.49999999999999` en `double`. Le gérant qui saisit `12,345` attend `12,35`.

- Crée une fonction pure `MoneyHelper.ToCents(double? valeur)` dans `Utilities/` : `null` → `0`, conversion en `decimal` **avant** la multiplication, arrondi `MidpointRounding.AwayFromZero`, résultat `long`.
- Utilise-la dans les 4 cas d'usage `Save…`, à la place du calcul actuel, et rien d'autre.
- Tests : `12.345` → `1235`, `1.005` → `101`, `2.675` → `268`, `-12.345` → `-1235`, `19.99` → `1999`, `100.50` → `10050`, `null` → `0`, puis les 4 cas d'usage avec une doublure qui capture l'objet envoyé.

### T4. Doublures manquantes

- `HttpService` a besoin d'un `IHttpClientFactory` de test, qui rend aussi le client nommé `auth`.
- Les cas d'usage `Save…` dépendent d'autres classes : `SaveCollectionUseCase` prend un `CreateCollectionUseCase` et un `ISessionService`. Passe par une doublure du service le plus bas (`IHttpService` si possible) et vérifie le corps réellement envoyé. Si ce n'est pas faisable sans modifier le code, signale-le au lieu de contourner.

### T5. `SiteCurrency` est un état statique

xUnit exécute les classes de tests en parallèle : les tests qui lisent ou écrivent cette valeur se perturberaient au hasard.

- Mets dans une même `[Collection]` tout test qui touche `SiteCurrency`, y compris `HttpService` et `CurrencyConverter`, et appelle `Reset()` avant et après chaque test.
- Rétablis la culture après chaque test qui la change.

### T6. Pas de comparaison de texte exacte sur les montants

Le formatage dépend de la version d'ICU, avec des espaces insécables. Vérifie que le résultat contient le symbole attendu, ne contient pas l'autre symbole, et contient les bons chiffres. Pas d'assertion « ex. » : chaque cas a un résultat vérifiable.

### T7. `AuthHandler` : le test de concurrence doit vraiment être concurrent

- Bloque le premier rafraîchissement avec un `TaskCompletionSource` jusqu'à ce que les 3 requêtes aient reçu leur 401. Sans cela, le test passe sans rien prouver.
- La doublure répond 401 à l'ancien jeton et 200 au nouveau.
- Ajoute : un 401 après le rejeu ne déclenche **pas** de boucle ; une route publique qui reçoit 401 (mauvais mot de passe) ne rafraîchit rien ; pas de jeton → `AuthenticationMissingException`.

### T8. `ApiError` : tous les codes du contrat

Tes cas utilisent `TENANT_NOT_FOUND`, qui n'est pas dans le contrat G4. Ajoute une `[Theory]` avec **tous** les codes 404 et 409 de `docs/backend-lots-gestion-G1-G4.md`, y compris `SHIPPING_COST_ALREADY_SET`, `COMMANDE_CLOSED`, `COLLECTION_CLOSED`, `COLLECTION_NOT_EMPTY`, `FOURNISSEUR_IN_USE`. Vérifie que le code est conservé et que le message affiché ne contient jamais le code brut. Un code sans traduction est un bug à signaler, pas à corriger.

### T9. Activation : suivre le vrai code

La normalisation est dans `SiteService` : elle retire tout caractère non alphanumérique, passe en majuscules, puis valide le format avec `AppKeyFormatRegex`. Ton cas `"  dem042  "` ne correspond probablement pas à ce format.

- Utilise des clés **fictives** qui respectent le format, sans reprendre aucune vraie clé ni le code du site demo. Remplace `DEM042`.
- Cas à couvrir : espaces et tirets retirés, minuscules, format invalide → `false` et clé vide, `null` et chaîne d'espaces.
- La réponse simulée de `app/activate` suit le contrat de `docs/lot-1-retours-applis-soeurs.md`, pas une forme inventée.

## Petits ajustements

- `MonthHelper` : ajoute une chaîne d'espaces seuls → `""`.
- Lecture des modèles : utilise les mêmes réglages Newtonsoft que `HttpService`. `ListProduct` : `quantity` et indicateurs à `null` en plus.
- Référence Android : `main` est mesurée à **319** avertissements. Compare à 319, pas à 318.

## Ce qui ne change pas

Le reste du plan est accepté. Rappels : aucun fichier XAML ni `Views/*.xaml.cs`, aucun lancement de l'application, aucun appel au backend, sorties collées telles quelles après ta dernière modification, contrôle d'encodage contre `main` en dernier, pas de commit.
