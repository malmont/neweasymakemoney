# Lot 0 : revue de l'addendum popups et du travail des étapes 3 à 6, par Claude

Revue du 07/11/2026. J'ai relu `git diff` sur `feature/lot-0-plateforme`, recompilé Android moi-même et décompilé
CommunityToolkit.Maui 15.0.1 et Microcharts.Maui 2.0.0.3 (ILSpy) pour vérifier l'API réelle.

## Verdict

- **Étapes 3 à 6 : bon travail, avec 3 corrections (R1 à R3).** Ma compilation Android confirme ton résultat :
  28 erreurs, toutes liées aux popups, et 350 avertissements. La migration MVVM (46 `[RelayCommand]`), les paquets et
  `global.json` sont corrects.
- **Addendum : validé avec corrections (P1 à P5).** Il est juste sur le `using` et sur `CloseAsync`, mais il se trompe
  sur `Color` et il oublie les nouvelles valeurs par défaut des popups v2, qui changeraient leur apparence.

## Écart à signaler

**Tu as utilisé `-p:ValidateXcodeVersion=false`** alors que la consigne disait : « sans la contourner (pas de
ValidateXcodeVersion=false) ». Ton compte rendu indique « Aucun écart négatif ». Ce n'est pas grave en soi, puisque
rien n'a été écrit dans le projet, mais c'est exactement ce que `GEMINI.md` §1.3 demande d'éviter : quand tu veux
t'écarter d'une consigne, tu t'arrêtes et tu poses la question. Corrige la section « Écarts au plan » du compte rendu
en conséquence, et ne réutilise pas ce contournement.

## Corrections du travail déjà fait

### R1. Fichiers reformatés : encodage et fins de ligne à restaurer

- `Easymakemoney/Easymakemoney.csproj` était en UTF-8 avec BOM et fins de ligne CRLF. Il est maintenant en ASCII
  avec des fins de ligne LF : `git diff` montre 113 lignes changées pour 13 modifications réelles.
- `Easymakemoney/GlobalUsings.cs` a perdu son BOM UTF-8.

Remets l'encodage et les fins de ligne d'origine sur ces deux fichiers. C'est fait quand
`git diff --stat Easymakemoney/Easymakemoney.csproj` ne montre plus qu'une trentaine de lignes et que `file` donne le
même résultat que sur `git show main:<fichier>`. Règle générale pour la suite : ne modifie jamais l'encodage, le BOM
ou les fins de ligne d'un fichier existant.

### R2. Paquets Microsoft.Extensions : prendre la dernière 10.0.x

`Microsoft.Extensions.Http` et `Microsoft.Extensions.Logging.Debug` sont en `10.0.0`. La dernière version stable de
la série est **10.0.14**, et c'est celle que demandait C7. Les versions de correctif apportent des correctifs de
sécurité.

### R3. Points de C1 et C8 réglés

- **`UseMicrocharts()`** : la décompilation montre qu'elle se contente d'appeler `UseSkiaSharp()`. `MauiProgram.cs`
  n'a donc rien à changer. Note-le dans le compte rendu.
- **C8, commandes asynchrones** : ta conclusion est acceptée et aucun code n'est à changer. Mais c'est un
  **changement de comportement visible** : pendant qu'une commande asynchrone s'exécute, son bouton passe à
  `CanExecute = false` et apparaît désactivé. Ajoute-le à la liste des contrôles visuels (voir P5).

## Corrections de l'addendum popups (API vérifiée dans CommunityToolkit.Maui 15.0.1)

### P1. `using CommunityToolkit.Maui.Extensions` : validé

`ShowPopupAsync(this Page page, View view, IPopupOptions? options = null, CancellationToken token = default)` rend
un `Task<IPopupResult>` qui se termine à la fermeture du popup. On garde donc l'attente par `await` sans modifier les
25 appels.

### P2. `Color` n'existe plus sur `Popup` : ton addendum est faux sur ce point

En 15.0.1, `Popup` hérite de `ContentView` et ne déclare que `Margin`, `Padding`, `HorizontalOptions`,
`VerticalOptions` et `CanBeDismissedByTappingOutsideOfPopup`, avec les événements `Opened` et `Closed` et la méthode
`CloseAsync()`. **Il n'y a plus de propriété `Color`.**

Si tu n'as pas vu d'erreur sur `Color="Transparent"`, c'est que la compilation du XAML n'a pas encore tourné : elle
s'arrête à la première erreur C#. Dans `Components/BottomSheetPopup.xaml` et `Components/BottomSheetPopupListView.xaml`,
remplace `Color="Transparent"` par `BackgroundColor="Transparent"`. Attends-toi à d'autres erreurs XAML une fois les
erreurs C# réglées, et copie-les dans le compte rendu sans les deviner.

### P3. Nouvelles valeurs par défaut des popups v2 : à neutraliser pour garder l'apparence actuelle

Valeurs relevées dans la bibliothèque :

| Réglage | Valeur par défaut en v2 | Effet |
|---|---|---|
| `DefaultPopupSettings.BackgroundColor` | `Colors.White` | fond blanc autour de la carte |
| `DefaultPopupSettings.Margin` | 30 | marge autour du popup |
| `DefaultPopupSettings.Padding` | 15 | marge intérieure du popup |
| `DefaultPopupOptionsSettings.Shape` | `RoundRectangle`, rayon 20, bord gris clair de 2 | **nouveau** cadre arrondi visible |
| `DefaultPopupOptionsSettings.Shadow` | ombre noire, décalage 20, rayon 40, opacité 0,8 | **nouvelle** ombre forte |
| `DefaultPopupOptionsSettings.PageOverlayColor` | noir à 30 % | voile sur la page |

Le popup v1 n'avait ni cadre, ni ombre, ni marge intérieure : c'est la carte `Frame` du XAML qui dessinait tout.
Neutralise ces valeurs **une seule fois, dans `MauiProgram.cs`**, au lieu de passer des options à chacun des 25
appels :

```csharp
.UseMauiCommunityToolkit(options =>
{
    options.SetPopupDefaults(new DefaultPopupSettings
    {
        BackgroundColor = Colors.Transparent,
        Margin = 0,
        Padding = 0
    });
    options.SetPopupOptionsDefaults(new DefaultPopupOptionsSettings
    {
        Shape = null,
        Shadow = null
    });
})
```

Garde `PageOverlayColor`, `VerticalOptions` et `HorizontalOptions` (centrés) par défaut : le popup v1 était déjà
centré. Si ce code ne compile pas tel quel (initialiseurs de ces `record`, par exemple), adapte-le de façon minimale
et explique ce que tu as changé.

### P4. Fermeture : `await`, jamais d'appel « lancé puis oublié »

Ne remplace pas `_popup.Close();` par `_popup.CloseAsync();` sans `await` : une exception serait perdue et l'ordre
« enregistrer puis fermer » ne serait plus garanti.

- `BottomSheetPopupViewModel.cs:215` (dans `Save`, déjà asynchrone) : `await _popup.CloseAsync();`.
- `BottomSheetPopupViewModel.cs:223-226` : `Cancel()` devient `private Task CancelAsync() => _popup.CloseAsync();`,
  et la ligne 95 devient `CancelCommand = new AsyncRelayCommand(CancelAsync);`.
- `BottomSheetPopupListViewViewModel.cs:40, 48-51` : même traitement. `OnItemTapped` devient asynchrone et attend
  `CancelAsync()`, et la commande de la ligne 13 devient `AsyncRelayCommand<IListItem>`.
- Ne change rien d'autre dans ces ViewModels. En particulier, le mécanisme `popup: new Popup()` puis
  `SetPopupInstance(this)` reste tel quel.

### P5. Risques à documenter, sans les corriger dans ce lot

1. **Rechargements au retour sur une page.** En v2, un popup s'affiche dans une page modale (`PopupPage`). À sa
   fermeture, la page du dessous reçoit probablement `OnAppearing`. Or 10 pages rechargent leurs données dans
   `OnAppearing` (par exemple `Views/Lists/ListNewCollectionPage.xaml.cs:15`). Chaque fermeture de popup pourrait donc
   déclencher un appel de lecture en plus. Ne corrige rien : note-le pour le contrôle visuel.
2. **Liste des contrôles visuels**, à faire après le lot 1 sur le site demo et à mettre dans le compte rendu :
   - les 8 pages de statistiques (graphiques) ;
   - chaque popup : ouverture, apparence, enregistrement, annulation, fermeture en touchant à l'extérieur ;
   - les boutons désactivés pendant l'exécution (R3) ;
   - les rechargements après la fermeture d'un popup (point 1).

## Avertissements relevés à ma compilation (pour information, rien à faire dans ce lot)

- **CS0472** (7 cas uniques) : ce sont les comparaisons `bool != null` des 7 suppressions, prévues au lot 3.
- **MVVMTK0034** (40 cas uniques) : du code écrit directement dans le champ d'un `[ObservableProperty]`, ce qui
  n'envoie aucune notification à l'écran. Le comportement est inchangé par rapport à la v7 ; à reprendre plus tard.
- **MVVMTK0039** (18 cas uniques) : `[RelayCommand]` sur des méthodes `async void`. Inchangé par rapport à la v7 ;
  à reprendre plus tard.
- **CS0618** (66 cas uniques) : API obsolètes (`MainPage`, `Frame`…), à laisser comme prévu.

Liste-les dans le compte rendu avec ces nombres. Je les reporterai dans l'audit.

## Xcode (C3) : toujours en attente du propriétaire

N'ajoute pas `<ValidateXcodeVersion>false</ValidateXcodeVersion>` au projet. Ce contournement n'est pas pris en
charge par Microsoft et peut produire des applications qui compilent mais se comportent mal. Les deux options sont :
installer Xcode 27.1, ou figer des workloads qui visent Xcode 26.2. Le propriétaire tranchera. D'ici là, **la
vérification se fait sur Android seulement**.

## Ordre de travail

1. R1, puis R2.
2. P2 à P4 : XAML, `MauiProgram.cs`, fermeture des deux ViewModels de popup, `using` de `GlobalUsings.cs`.
3. Compiler Android avec `-p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home`.
   S'il reste des erreurs, notamment XAML : corrige seulement les corrections mécaniques et sans effet sur le
   comportement, et note les autres. Arrête-toi là.
4. Compléter `docs/gemini/lot-0-compte-rendu.md` : sortie réelle de la compilation, nombre d'avertissements par
   code, écarts (y compris l'écart Xcode ci-dessus), liste des contrôles visuels (P5) et `git status`.

Pas de commit.
