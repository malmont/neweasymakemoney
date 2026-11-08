# Lot 0 : revue du code Android, par Claude

Revue du 08/11/2026, sur `feature/lot-0-plateforme` (base `e2b05e7`). J'ai relu tout `git diff`, comparé l'encodage
de chaque fichier modifié avec `main` et recompilé Android moi-même avec `-t:Rebuild`.

## Verdict

**Partie Android validée, sous réserve de deux corrections (C1 et C2).** Le lot 0 reste ouvert pour iOS et Mac
Catalyst, qui attendent Xcode 27.1 (voir la dernière section).

Ce qui est juste :
- **R2** : `Microsoft.Extensions.Http` et `Logging.Debug` en 10.0.14.
- **P1 à P4** : le `using` global, `BackgroundColor`, les valeurs par défaut v2 neutralisées dans `MauiProgram.cs`, et
  la fermeture par `await` dans les deux ViewModels de popup. Le diff est conforme à la revue.
- **R1 pour les deux fichiers demandés** : `Easymakemoney.csproj` (UTF-8 avec BOM, CRLF, 27 lignes de diff) et
  `GlobalUsings.cs` (BOM).
- **ViewModels** : le diff ne contient que `[ICommand]` → `[RelayCommand]`.
- **Ma compilation** : `0 Error(s)`, `418 Warning(s)`, et l'APK est produit. Ton résultat est confirmé.
- **Écart Xcode** : correctement reconnu dans le compte rendu.

## C1. Six fichiers ont perdu leur BOM UTF-8

La règle générale de R1 disait : « ne modifie jamais l'encodage, le BOM ou les fins de ligne d'un fichier
existant ». Six fichiers sont touchés :

| Fichier | `main` | Maintenant |
|---|---|---|
| `Easymakemoney/MauiProgram.cs` | UTF-8 avec BOM, CRLF | ASCII, CRLF |
| `Easymakemoney/ViewModels/AppShellViewModel.cs` | UTF-8 avec BOM | ASCII |
| `Easymakemoney/ViewModels/Dashboard/DashboardPageViewModel.cs` | UTF-8 avec BOM | ASCII |
| `Easymakemoney/ViewModels/Lists/AllListsViewModel.cs` | UTF-8 avec BOM | ASCII |
| `Easymakemoney/ViewModels/Lists/ListNewCollectionViewModel.cs` | UTF-8 avec BOM | ASCII |
| `Easymakemoney/ViewModels/Startup/LoginPageViewModel.cs` | UTF-8 avec BOM | ASCII |

Remets le BOM, c'est-à-dire les trois octets `EF BB BF` en tête de fichier, sans rien changer d'autre.
C'est fait quand cette commande n'affiche que `fin` :

```bash
for f in $(git diff --name-only); do a=$(git show main:"$f" | file -b -); b=$(file -b "$f"); [ "$a" != "$b" ] && echo "DIFF $f"; done; echo fin
```

## C2. Le décompte des avertissements est faux dans le compte rendu

Voici mon décompte des cas uniques, c'est-à-dire des lignes distinctes à fichier, position et message identiques,
tirées du journal d'une recompilation complète :

| Code | Ton compte rendu | Recompté |
|---|---|---|
| CS8618 | 45 | **115** |
| CS0618 | 60 | **66** |
| XC0022 | (112 avec XC0025) | **54** |
| MVVMTK0034 | 40 | 40 |
| CS8603 | 26 | 26 |
| CS8625 | 23 | 23 |
| CS8767 | 10 | **20** |
| MVVMTK0039 | 18 | 18 |
| CS8602 | 17 | 17 |
| CS8622 | 13 | 13 |
| CS8604 | 12 | 12 |
| CS0472 | 7 | 7 |
| XC0025 | (voir XC0022) | **2** |
| CS8601 | 1 | 1 |
| XA0033 | 1 | 1 (sur 4 lignes) |
| CS4014 | absent | **1** |

Corrige le tableau du §3, et indique la commande qui t'a servi à compter. Pour ce lot comme pour les suivants :
**chaque nombre du compte rendu doit sortir d'une commande que tu as réellement lancée**. Si tu n'as pas compté,
écris « non compté ». Ne donne jamais d'estimation présentée comme un résultat.

Deux remarques à ajouter au compte rendu :
- **CS4014** se trouve dans `ViewModels/FormModels/NoteDeFraisForModel.cs:47`, un fichier que tu n'as pas modifié :
  cet avertissement existait déjà. Ne le corrige pas.
- **XA0033** vient du numéro de version du JDK Microsoft (`21.0.13.0.2`, en 5 parties), que l'outillage Android ne
  sait pas lire. Il ne bloque rien.

## Petites précisions du compte rendu (à corriger en même temps)

- **§5.1** : la décompilation de `UseMicrocharts()` vient de ma revue, pas de toi. Écris « d'après la revue de
  Claude ».
- **§6.2** : le popup n'est pas une « feuille de bas d'écran ». Le `Popup` est centré, et seule la carte `Frame` à
  l'intérieur porte `VerticalOptions="EndAndExpand"`. Le contrôle doit vérifier que **la position et l'apparence sont
  identiques à la version actuelle**, pas une position particulière.
- **§1** : ajoute les six fichiers de C1 une fois corrigés.

## iOS et Mac Catalyst

Le propriétaire installera Xcode 27.1 (avec macOS 26.6 ou plus récent) aujourd'hui. **Ne lance aucune compilation
Apple avant que je te le demande** : je vérifierai d'abord la chaîne d'outils. Ensuite viendra une dernière étape :
compiler `net10.0-ios` (simulateur) et `net10.0-maccatalyst`, sans `ValidateXcodeVersion`, puis consigner le résultat
dans le compte rendu.

## Ordre de travail

1. C1, puis la commande de vérification.
2. Recompiler Android (`-t:Rebuild`, même `JavaSdkDirectory`) pour confirmer 0 erreur.
3. C2 et les précisions dans `docs/gemini/lot-0-compte-rendu.md`.
4. T'arrêter et le dire au propriétaire. Pas de commit.

## Contre-vérification du 08/11/2026 : partie Android validée

- **C1** : la commande de vérification n'affiche plus que `fin`, et je l'ai relancée moi-même. Le diff passe à 84 lignes
  ajoutées et 79 supprimées, contre 90 et 85 avant : la différence correspond exactement aux 6 BOM rétablis.
- **C2** : le tableau correspond à mon décompte et la commande de décompte est donnée.
- **Une seule petite inexactitude**, sans conséquence : XC0025 ne signale pas une « incompatibilité de type ». Il
  signale une liaison qui a une `Source` explicite et n'est donc pas compilée.

**La partie Android du lot 0 est validée.** Il reste la dernière étape : compiler iOS (simulateur) et Mac Catalyst
dès que Xcode 27.1 sera installé et vérifié.
