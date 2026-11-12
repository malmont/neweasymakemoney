# Lot 4i : tests des ViewModels des promotions d'article et des changements de prix

Consigne de Claude du 12/11/2026. Lot de la file d'attente (`GEMINI.md` § 1 ter) : tu peux en écrire le **plan** dès que tu n'as rien
d'autre à faire. **Aucun code avant que Claude ait validé le plan dans la boîte aux lettres.**

- Plan : `docs/gemini/lot-4i-plan.md`. Tu l'écris sur ta branche du moment, sans en créer une autre.
- Branche du code : `feature/lot-4i-tests`, créée depuis `main` à jour (`git switch -c feature/lot-4i-tests main`), **seulement quand
  la boîte aux lettres te le dit**. Claude doit d'abord avoir rangé le lot précédent.
- Pas de commit.
- Référence : lance `dotnet test Easymakemoney.Tests` sur `main` avant de commencer et note le nombre réel dans le plan.
  Référence Android : **216 avertissements**.

## Ce qui est déjà testé : ne le reteste pas

Lis d'abord `Easymakemoney.Tests/Models/PromotionTests.cs` et `Easymakemoney.Tests/Models/PriceChangeTests.cs` (modèles, corps des
requêtes, routes des services). Ton plan cite, pour chaque test prévu, ce qu'il ajoute par rapport à eux.

## Ce que porte ce lot

Le **comportement des ViewModels** de `Easymakemoney/ViewModels/Lists/PromotionViewModels.cs` et
`Easymakemoney/ViewModels/Lists/PriceViewModels.cs` :
- requêtes envoyées (méthode du service appelée, arguments) ;
- requêtes qui ne partent pas ;
- messages et confirmations montrés au gérant.

Ajoute dans `Easymakemoney.Tests/Fakes/`, **dans le projet de tests seulement**, un faux de `IPromotionService` et un faux de
`IPriceService`. Chacun enregistre les appels et peut lever une `ApiException` construite par `ApiError.FromHttpResponse`.

| ViewModel | À vérifier |
|---|---|
| `PromotionCreateViewModel` | Initialisation depuis un article ou depuis un conseil. **Aperçu** : message sans requête quand la saisie est invalide (pourcentage, dates) ; corps de la requête envoyée ; un changement de saisie efface l'aperçu. **`Apply`** : rien sans aperçu applicable ; confirmation refusée = aucune requête ; acceptée = le service reçoit l'empreinte de l'aperçu et le même corps ; message de confirmation. **409 `PRICES_CHANGED`** → message d'information puis nouvel aperçu, sans `ShowErrorAsync`. **Autre erreur** → `ShowErrorAsync` une seule fois. `CanChooseVariant` et `OpenVariantPromotion` : si la navigation (`Shell.Current`) empêche de le tester, vérifie seulement `CanChooseVariant` et signale-le |
| `PromotionListViewModel` | `LoadAsync` : la liste est remplie, l'état vide est juste, une erreur passe par `ShowErrorAsync`. **`End`** : refus de la confirmation = aucune requête ; acceptée = appel du service puis relecture. **409 `PROMOTION_EDITED_SINCE`** → message d'information et relecture, sans erreur. `ShowAdminInfo` : le message affiché |
| `PriceChangeViewModel` | Modes « pourcentage » et « coefficient » : `ValueLabel`, `ValuePlaceholder`, et le message propre à chaque mode quand la valeur est invalide, sans requête. Corps de l'aperçu selon le mode, l'arrondi et la portée. Un changement de saisie efface l'aperçu. **`Apply`** : confirmation refusée = aucune requête ; acceptée = empreinte et même corps. **409 `PRICES_CHANGED`** → message puis nouvel aperçu. **Autre erreur** → `ShowErrorAsync` |
| `PriceBatchListViewModel` | `LoadAsync`. **`Undo`** : rien si le lot n'est plus annulable ; refus = aucune requête ; acceptée = appel puis relecture. **409 `PRICE_EDITED_SINCE`** → message d'information, sans erreur |

Si un comportement décrit ici n'existe pas dans le code, ne l'invente pas : écris-le dans le plan, à la section des questions.

## Règles, les mêmes que pour les lots 4 à 4h

- Aucun fichier XAML ni `Views/*.xaml.cs`, aucun fichier de l'application modifié.
- Aucun appel au backend, aucun lancement de l'application.
- Pas de reformatage. Encodage et fins de ligne intacts (CRLF reste CRLF).
- Aucune clé ni adresse réelle. Pas de commit.
- `SiteCurrency` : les tests qui formatent un montant vont dans la `[Collection("SiteCurrency")]`, avec la culture fixée puis rétablie.
- Pas de comparaison exacte des montants formatés.
- Les dates « aujourd'hui » viennent de `DateTime.Today`, jamais en dur.
- Aucun accès aux membres privés, ni par réflexion.
- Un bug trouvé donne un test en `Skip` avec la mention BUG, et une ligne dans le compte rendu.

## Vérification

Colle les sorties telles quelles dans `docs/gemini/lot-4i-compte-rendu.md`.
1. `dotnet test Easymakemoney.Tests`.
2. Android : `dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home`. Colle la ligne des avertissements telle quelle (référence 216).
3. En dernier, le contrôle d'encodage contre `main`.
