# Lot 4h : tests des ViewModels des promotions par variante

Consigne de Claude du 11/11/2026. À faire **après le lot 4g**, une fois 4g validé.

**Écris d'abord ton plan** dans `docs/gemini/lot-4h-plan.md`, puis arrête-toi. Tu ne codes rien avant que je l'aie validé.

- Branche : `feature/lot-4h-tests`, créée depuis `main` à jour (`git switch -c feature/lot-4h-tests main`).
- Pas de commit.
- Référence : lis le nombre de tests sur `main` avec `dotnet test Easymakemoney.Tests` avant de commencer, et note-le dans le plan. Référence Android : **216 avertissements** (270 avant le nettoyage du 11/11 au soir).

## Ce qui est déjà testé : ne le reteste pas

Lis d'abord ces fichiers. Ton plan doit citer, pour chaque test prévu, ce qu'il ajoute par rapport à eux :
- `Easymakemoney.Tests/Models/VariantPromotionTests.cs` :
  - modèles de l'aperçu, textes des lignes et des promotions ;
  - corps des requêtes et routes du service ;
  - pourcentage invalide et portée limitée aux variantes cochées.
- `Easymakemoney.Tests/Models/ManagementModuleTests.cs` et `ManagementReadinessTests.cs` : lots MOD et CHE. **Aucun test de plus sur ces deux lots dans 4h.**

## Ce que porte ce lot

Ce lot porte sur le **comportement des ViewModels** de `Easymakemoney/ViewModels/Lists/VariantPromotionViewModels.cs` :
- requêtes envoyées (méthode du service appelée, arguments) ;
- requêtes qui ne partent pas ;
- messages et confirmations montrés au gérant.

Ajoute un faux de `IVariantPromotionService` dans `Easymakemoney.Tests/Fakes/`, **dans le projet de tests seulement**. Il enregistre les appels, avec tous les drapeaux de `ApplyAsync`, et peut lever une `ApiException` construite par `ApiError.FromHttpResponse`.

| ViewModel | À vérifier |
|---|---|
| `VariantPromotionViewModel` | **`Init`** : l'article est repris. **Aperçu effacé** par un changement de dates, de pourcentage ou d'arrondi. **Date de début** : la date de fin suit si elle passe avant. **`Apply`**, chaque cas sans `ApplyAsync` : interrupteur éteint (`enabled: false` dans l'aperçu) → message « pas encore activées » ; aucune variante cochée → message ; aucune ligne remisable avec les réglages → message ; confirmation refusée. **Confirmation acceptée** : `ApplyAsync` reçoit l'empreinte de l'aperçu et les drapeaux `allowBelowCost`, `allowBigChange`, `allowReplace`, `allowAboveProductPromotion` tels que cochés ; `VariantPromotionListViewModel.PromotionsChanged` passe à vrai ; message de confirmation, différent quand des variantes sont écartées. **409 `PRICES_CHANGED`** et **`NOTHING_TO_APPLY`** → message d'information, sans `ShowErrorAsync`. **`VARIANT_PROMOTIONS_OFF`** → message propre. **Autre erreur** → `ShowErrorAsync` une seule fois |
| `VariantPromotionListViewModel` | `InitializeAsync` ne charge qu'une fois, sauf si `PromotionsChanged` est vrai ; il le remet alors à faux. `End` : confirmation refusée = aucun appel ; acceptée = `EndAsync(batchId)` puis relecture de la liste ; erreur = `ShowErrorAsync`. Remets `PromotionsChanged` à faux avant et après chaque test : c'est un champ statique |

## Règles, les mêmes que pour les lots 4 à 4g

- Aucun fichier XAML ni `Views/*.xaml.cs`, aucun fichier de l'application modifié.
- Aucun appel au backend, aucun lancement de l'application.
- Pas de reformatage. Encodage et fins de ligne intacts (CRLF reste CRLF).
- Aucune clé ni adresse réelle. Pas de commit.
- `SiteCurrency` : les tests qui formatent un montant vont dans la `[Collection("SiteCurrency")]`, avec la culture fixée puis rétablie.
- Pas de comparaison exacte des montants formatés.
- Les dates « aujourd'hui » viennent de `DateTime.Today`, jamais en dur.
- Aucun accès aux membres privés, ni par réflexion.
- Si une boîte de dialogue ne peut pas être testée sans `Shell.Current`, signale-le dans le plan au lieu de contourner.
- Un bug trouvé donne un test en `Skip` avec la mention BUG, et une ligne dans le compte rendu.

## Vérification

Colle les sorties telles quelles dans `docs/gemini/lot-4h-compte-rendu.md`.
1. `dotnet test Easymakemoney.Tests`.
2. Android : `dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home`. Colle la ligne des avertissements telle quelle (référence 216), sans lui chercher de cause.
3. En dernier, le contrôle d'encodage contre `main`.
