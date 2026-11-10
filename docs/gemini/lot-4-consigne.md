# Lot 4 : tests automatiques

Consigne de Claude du 09/11/2026. **Écris d'abord ton plan** dans `docs/gemini/lot-4-plan.md`, puis arrête-toi. Tu ne codes rien avant que je l'aie validé.

- Branche : `feature/lot-4-tests`, créée depuis `main` (`git switch -c feature/lot-4-tests main`).
- Pas de commit.

L'application n'a aucun test. Ce lot protège ce qui a déjà cassé ou ce qui coûterait cher s'il cassait : montants, erreurs, session, choix du site.

## Ce que ton plan doit couvrir

### 1. Mise en place

Décision déjà prise (`docs/revue-architecture.md`, §D) : ajouter une cible `net10.0` au projet de l'application, pour qu'un projet de tests classique puisse le référencer.

- Nouveau projet `Easymakemoney.Tests` à la racine du dépôt, xUnit, ajouté à `Easymakemoney1.sln`.
- Montre dans le plan **le changement exact** de `Easymakemoney.csproj`. `OutputType` ne doit rester `Exe` que pour les cibles Android, iOS, Mac Catalyst et Windows.
- Le fichier `Platforms/` ne doit pas être compilé en `net10.0`. Dis comment tu le vérifies.
- Les API MAUI de l'appareil (`SecureStorage`, `Preferences`, `Connectivity`…) lèvent une exception en `net10.0` : les tests n'y touchent jamais, ils passent par des doublures.
- Pas de bibliothèque de simulation sauf nécessité : un `HttpMessageHandler` de test écrit à la main suffit. Donne les versions exactes des paquets.

### 2. Tests à écrire

Pour chaque groupe, liste les cas dans le plan.

| Groupe | À vérifier |
|---|---|
| `MonthHelper` | 1 et 12, 0 et 13, texte, vide, `null`, espaces |
| `SiteCurrency` et formatage | centimes vers montant, devise du site indépendante de la culture de l'appareil, code inconnu, code absent, remise à zéro |
| Montants envoyés | les 4 cas d'usage `Save…` envoient un **entier arrondi** (`12,345` → `1235` ou `1234` : écris la valeur réellement attendue et justifie-la) |
| `ApiError.FromHttpResponse` | `errors`, `violations`, `fields`, `{"error":"CODE","message"}`, corps vide, corps non JSON, codes 404 et 409 du contrat, champ inconnu sans nom technique |
| `TenantHandler` | en-tête `X-Tenant-Host` présent, absent quand aucun site n'est choisi |
| `AuthHandler` | jeton ajouté, 401 puis rafraîchissement puis rejeu, un seul rafraîchissement pour plusieurs 401 simultanés, échec du rafraîchissement qui prévient `ISessionExpiryNotifier`, rejeu d'un envoi multipart, routes publiques sans jeton |
| Activation et code de site | normalisation de la saisie, 404 `APP_KEY_INVALID` |
| `HttpService` | 200, 201 avec objet, 204 sans corps, `null` de `GET frais-de-port`, relevé de `currency` à la racine et sur le premier élément d'un tableau |
| Lecture des modèles | `ListProduct` avec `purchasePrice: null` et un coefficient décimal (bug réel corrigé), `month` entier dans les 6 modèles mensuels, montants en `long?` |

Le contrat est dans `docs/backend-lots-gestion-G1-G4.md`. Les tests partent du contrat, pas du code actuel.

### 3. Si un test révèle un bug

**Ne corrige pas le code de l'application.** Laisse le test en échec, marque-le `[Fact(Skip = "BUG : …")]`, et décris le bug dans le compte rendu. Je déciderai. Même règle si du code est impossible à tester sans le modifier : propose le changement minimal dans le plan, sans l'appliquer.

### 4. Vérification

Dans `docs/gemini/lot-4-compte-rendu.md`, colle telles quelles les sorties de ces commandes, lancées après ta dernière modification :

1. `dotnet test Easymakemoney.Tests` avec le nombre de tests réussis, échoués et ignorés ;
2. Android (`-t:Rebuild`) et décompte des avertissements, qui doit montrer que la cible `net10.0` n'a rien dégradé ;
3. en dernier, le contrôle d'encodage contre `main`.

## Hors du lot

- **Aucun fichier XAML ni `Views/*.xaml.cs`** : l'interface est de mon ressort.
- La recette complète sur le site demo : je la fais avec le propriétaire.
- **Mêmes règles** : pas d'appel au backend, y compris depuis les tests ; pas de lancement de l'application ; pas de reformatage ; encodage intact ; aucune clé, adresse de site ou identifiant réel, utilise `exemple.test` et des valeurs fictives ; pas de commit.
