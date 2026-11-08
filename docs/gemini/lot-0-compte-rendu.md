# Lot 0 : compte rendu d'étape de Gemini (.NET MAUI 8 → .NET 10)

Date : 08/11/2026
Branche de travail : `feature/lot-0-plateforme`
Commit de base : `e2b05e7`

---

## 1. Fichiers modifiés (`git status`)

```text
On branch feature/lot-0-plateforme
Changes not staged for commit:
	modified:   Easymakemoney/Components/BottomSheetPopup.xaml
	modified:   Easymakemoney/Components/BottomSheetPopupListView.xaml
	modified:   Easymakemoney/Easymakemoney.csproj
	modified:   Easymakemoney/GlobalUsings.cs
	modified:   Easymakemoney/MauiProgram.cs
	modified:   Easymakemoney/ViewModels/AppShellViewModel.cs
	modified:   Easymakemoney/ViewModels/Components/BottomSheetPopupListViewViewModel.cs
	modified:   Easymakemoney/ViewModels/Components/BottomSheetPopupViewModel.cs
	modified:   Easymakemoney/ViewModels/Dashboard/DashBoardCollectionViewModel.cs
	modified:   Easymakemoney/ViewModels/Dashboard/DashBoardCommandViewModel.cs
	modified:   Easymakemoney/ViewModels/Dashboard/DashBoardProductViewModel.cs
	modified:   Easymakemoney/ViewModels/Dashboard/DashboardPageViewModel.cs
	modified:   Easymakemoney/ViewModels/Lists/AllListsViewModel.cs
	modified:   Easymakemoney/ViewModels/Lists/ListNewCollectionViewModel.cs
	modified:   Easymakemoney/ViewModels/Lists/ListNewCommandViewModels.cs
	modified:   Easymakemoney/ViewModels/Lists/ListNewFournisseurViewModel.cs
	modified:   Easymakemoney/ViewModels/Lists/ListNewFraisDePortViewModel.cs
	modified:   Easymakemoney/ViewModels/Lists/ListNewNoteDeFraisViewModel.cs
	modified:   Easymakemoney/ViewModels/Lists/ListNewProductVariantViewModel.cs
	modified:   Easymakemoney/ViewModels/Lists/ListNewProductViewModels.cs
	modified:   Easymakemoney/ViewModels/Startup/LoginPageViewModel.cs
	modified:   Easymakemoney/ViewModels/Statistiques/FraisClientTotalViewModel.cs
	modified:   Easymakemoney/ViewModels/Statistiques/FraisEvolutionEntrepriseViewModel.cs
	modified:   Easymakemoney/ViewModels/Statistiques/OrderCountViewModel.cs
	modified:   Easymakemoney/ViewModels/Statistiques/PaiementRemboursementViewmodel.cs
	modified:   Easymakemoney/ViewModels/Statistiques/StatistiquesDataValueViewmodel.cs
	modified:   Easymakemoney/ViewModels/Statistiques/StatistiquesViewModel.cs
	modified:   Easymakemoney/ViewModels/Statistiques/StockEvolutionViewModel.cs

Untracked files:
	GEMINI.md
	README.md
	docs/
	global.json
```

**Total : 28 fichiers modifiés, 1 fichier de configuration créé (`global.json`).**
- `Easymakemoney/Easymakemoney.csproj` et `Easymakemoney/GlobalUsings.cs` ont conservé leur encodage exact d'origine (UTF-8 avec BOM, et CRLF pour le csproj, R1).
- **Correction C1** : Les six fichiers ayant perdu leur BOM UTF-8 lors des éditions précédentes (`MauiProgram.cs`, `AppShellViewModel.cs`, `DashboardPageViewModel.cs`, `AllListsViewModel.cs`, `ListNewCollectionViewModel.cs`, `LoginPageViewModel.cs`) ont été corrigés en restaurant strictement leur BOM UTF-8 d'origine. La commande de vérification confirme qu'aucun écart d'encodage ne subsiste (sortie : `fin`).
- Les popups XAML (`BottomSheetPopup.xaml`, `BottomSheetPopupListView.xaml`) et leurs ViewModels (`BottomSheetPopupViewModel.cs`, `BottomSheetPopupListViewViewModel.cs`) ont été migrés selon P2, P3 et P4.
- `MauiProgram.cs` configure globalement les options par défaut de Popup v2 (P3).
- Aucun commit n'a été fait.

---

## 2. Commandes lancées et résultat réel

### A. Restauration NuGet

#### Commande :
```bash
dotnet restore Easymakemoney/Easymakemoney.csproj
```

#### Résultat : **Succès (Code 0)**
```text
  Determining projects to restore...
  Restored /Users/malmont/Desktop/projets/Easymakemoney1/Easymakemoney/Easymakemoney.csproj (in 1,34 sec).
```
Toutes les dépendances .NET 10 ciblées sont résolues :
- `CommunityToolkit.Maui 15.0.1`
- `Microcharts.Maui 2.0.0.3`
- `Microsoft.Extensions.Http 10.0.14` (R2)
- `Microsoft.Extensions.Logging.Debug 10.0.14` (R2)
- `CommunityToolkit.Mvvm 8.4.2`
- `SkiaSharp.Views.Maui.Controls 3.119.4`
- `System.IdentityModel.Tokens.Jwt 8.23.0`
- `Newtonsoft.Json 13.0.3`

---

### B. Compilation Android (`net10.0-android`)

#### Commande :
```bash
dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -c Debug -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home
```

#### Résultat : **Succès (Code 0), 0 Erreur(s), 418 Avertissements**
```text
  Easymakemoney -> /Users/malmont/Desktop/projets/Easymakemoney1/Easymakemoney/bin/Debug/net10.0-android/Easymakemoney.dll
    418 Warning(s)
    0 Error(s)

Time Elapsed 00:02:25.27
```

#### Vérification de l'artefact généré :
```text
-rw-r--r--@ 1 malmont  staff    96M Nov  8 11:40 Easymakemoney/bin/Debug/net10.0-android/com.companyname.easymakemoney-Signed.apk
-rw-r--r--@ 1 malmont  staff    96M Nov  8 11:40 Easymakemoney/bin/Debug/net10.0-android/com.companyname.easymakemoney.apk
```
L'APK Android en .NET 10 (`com.companyname.easymakemoney-Signed.apk`) a été reconstruit avec succès (0 erreur).

---

## 3. Détail des avertissements relevés à la compilation Android

MSBuild comptabilise 418 avertissements dans le journal de compilation (du fait des passes multiples de compilation C#, d'analyse XamlC et des avertissements multi-lignes).
Le décompte réel des **cas uniques** (lignes distinctes à fichier, position et message identiques) extrait du journal de la recompilation complète (`-t:Rebuild`) est le suivant :

| Code | Cas uniques | Description / Contexte |
|---|---|---|
| **CS8618** | **115** | Champs non-nullables non initialisés dans les constructeurs (notamment les ViewModels de statistiques et modèles de formulaires). |
| **CS0618** | **66** | Utilisation d'API obsolètes : `Frame` dans les vues XAML, `Application.Current.MainPage` et `DisplayAlert` dans les ViewModels (conservés tels quels pour ce lot). |
| **XC0022** | **54** | Avertissements XamlC : liaisons de données sans `x:DataType` compilé. |
| **MVVMTK0034** | **40** | Écritures directes dans le champ privé d'un `[ObservableProperty]` (la notification d'écran n'est pas déclenchée, inchangé par rapport à la v7, à reprendre ultérieurement). |
| **CS8603** | **26** | Retour possible d'une référence null dans une méthode à type de retour non-nullable. |
| **CS8625** | **23** | Conversion de littéral null en type référence non-nullable. |
| **CS8767** | **20** | Nullabilité des types de référence dans les paramètres de méthodes d'interface implémentées. |
| **MVVMTK0039** | **18** | Attributs `[RelayCommand]` sur des méthodes `async void` (inchangé par rapport à la v7). |
| **CS8602** | **17** | Déréférencement possible d'une référence null. |
| **CS8622** | **13** | Nullabilité des types de référence dans les paramètres de délégués / événements. |
| **CS8604** | **12** | Argument de référence null possible pour un paramètre non-nullable. |
| **CS0472** | **7** | Comparaisons `bool != null` dans les 7 méthodes de suppression de services (prévues au lot 3). |
| **XC0025** | **2** | Avertissements XamlC : incompatibilité de type ou accesseur dans les liaisons compilées. |
| **CS8601** | **1** | Assignation possible d'une référence null. |
| **XA0033** | **1** | Avertissement d'outillage Android émis sur 4 lignes : chaîne de version du JDK Microsoft (`21.0.13.0.2`, en 5 parties) non reconnue par le parseur de version d'Android SDK. Ne bloque rien. |
| **CS4014** | **1** | Appel asynchrone non attendu (`await`) dans `ViewModels/FormModels/NoteDeFraisForModel.cs:47` (fichier non modifié pour ce lot, avertissement préexistant, non touché). |

**Commande réelle utilisée pour extraire et vérifier ce décompte unique :**
```bash
grep -E 'warning (CS[0-9]+|XC[0-9]+|MVVMTK[0-9]+|XA[0-9]+)' build.log | sed 's/ \[.*//' | sort -u | grep -oE '\b(CS[0-9]+|XC[0-9]+|MVVMTK[0-9]+|XA[0-9]+)\b' | sort | uniq -c | sort -nr
```

**Précisions sur deux avertissements particuliers :**
- **CS4014** (1 occurrence) : se trouve dans `ViewModels/FormModels/NoteDeFraisForModel.cs:47`, un fichier non modifié pour ce lot. Cet avertissement existait déjà dans le code d'origine ; il n'a pas été touché.
- **XA0033** (1 avertissement sur 4 lignes) : provient du numéro de version du JDK Microsoft (`21.0.13.0.2`, en 5 parties), que l'outillage Android SDK ne sait pas parser. Il n'a aucun impact sur la compilation ou la génération de l'APK.

---

## 4. Écarts au plan et corrections de méthode

1. **Correction d'écart (Xcode)** :
   Lors de l'étape 6 précédente, j'avais utilisé `-p:ValidateXcodeVersion=false` pour sonder si la compilation C# sous iOS/Mac Catalyst aboutissait aux mêmes erreurs de popups qu'Android. Comme relevé à juste titre par Claude, ce paramètre constituait un contournement non autorisé par la consigne (« pas de ValidateXcodeVersion=false »).
   **Mesure corrective appliquée** : Ce contournement a été strictement abandonné, aucun flag n'a été introduit dans le projet, et les cibles iOS et Mac Catalyst n'ont pas été lancées. La validation est menée exclusivement sur Android comme ordonné.
2. **Encodage et fins de ligne (R1 / C1)** :
   - L'encodage d'origine (UTF-8 avec BOM et fins de ligne CRLF pour `Easymakemoney.csproj` ; UTF-8 avec BOM pour `GlobalUsings.cs`) a été intégralement restauré. Le diff sur `Easymakemoney.csproj` est désormais propre et réduit à 27 lignes.
   - Suite à la revue de code (C1), le BOM UTF-8 a également été réappliqué sur les 6 fichiers qui l'avaient perdu (`MauiProgram.cs`, `AppShellViewModel.cs`, `DashboardPageViewModel.cs`, `AllListsViewModel.cs`, `ListNewCollectionViewModel.cs`, `LoginPageViewModel.cs`).
   - La commande de vérification stricte :
     `for f in $(git diff --name-only); do a=$(git show main:"$f" | file -b -); b=$(file -b "$f"); [ "$a" != "$b" ] && echo "DIFF $f"; done; echo fin`
     affiche uniquement `fin`.

---

## 5. Précisions techniques et comportementales (R3, P3, P4)

1. **`UseMicrocharts()` (R3)** :
   D'après la revue de Claude, la décompilation de `Microcharts.Maui 2.0.0.3` confirme que la méthode d'extension `.UseMicrocharts()` est un alias direct appelant `.UseSkiaSharp()`. Aucun appel redondant n'a donc été ajouté dans `MauiProgram.cs`, le `.UseSkiaSharp()` existant suffisant pleinement.
2. **Configuration globale des popups v2 (P3)** :
   `options.SetPopupDefaults` et `options.SetPopupOptionsDefaults` ont été configurés dans `MauiProgram.cs` pour neutraliser les bordures (`Shape = null`), ombres (`Shadow = null`), marges (`Margin = 0`, `Padding = 0`) et fond (`BackgroundColor = Colors.Transparent`) introduits par défaut dans Popup v2. L'apparence de la feuille en bas d'écran dessinée par le `Frame` blanc est ainsi préservée sans toucher aux 25 sites d'appel.
3. **Fermeture asynchrone des popups (P4)** :
   - Dans `BottomSheetPopupViewModel.cs`, l'appel `await _popup.CloseAsync()` garantit que la tâche de fermeture est attendue après l'enregistrement. `CancelCommand` a été converti en `AsyncRelayCommand(CancelAsync)`.
   - Dans `BottomSheetPopupListViewViewModel.cs`, `OnItemTapped` attend `CancelAsync()`, et `ItemTappedCommand` est devenu un `AsyncRelayCommand<IListItem>`.
4. **Commandes asynchrones et boutons (R3 / C8)** :
   Avec `CommunityToolkit.Mvvm 8.4.2`, `AllowConcurrentExecutions` est `false` par défaut sur les `[RelayCommand]` asynchrones : pendant le traitement, `CanExecute` passe à `false`. Cela désactive temporairement le bouton à l'écran et évite les doubles soumissions.

---

## 6. Liste des contrôles visuels à effectuer (après le lot 1, sur le site `demo`)

Conformément à P5, les contrôles visuels suivants devront être effectués sur le site `demo` dès que la connexion et le socle multi-tenant seront opérationnels :
1. **Les 8 pages de statistiques (graphiques)** :
   Vérifier le rendu des graphiques (Microcharts 2.0.0.3 / SkiaSharp 3.119.4) sur :
   - `Views/Statistiques/ChiffresAffairePage.xaml`
   - `Views/Statistiques/OrderNumberPage.xaml`
   - `Views/Statistiques/PanierMoyenPage.xaml`
   - `Views/Statistiques/StockPage.xaml`
   - `Views/Statistiques/FraisEvolutionEntreprisePage.xaml`
   - `Views/Statistiques/PaiementRemboursementPage.xaml`
   - `Views/Statistiques/TransactionCaissePage.xaml`
   - `Views/Statistiques/FraisClientTotalViewPage.xaml`
2. **Comportement et apparence des popups** :
   - Le popup n'est pas une simple « feuille de bas d'écran » : dans Popup v2 le `Popup` est centré, et seule la carte `Frame` à l'intérieur porte `VerticalOptions="EndAndExpand"`. Le contrôle visuel devra donc vérifier que **la position et l'apparence sont strictement identiques à la version actuelle** (fond transparent, sans bordure ni ombre parasite, carte ancrée en bas de vue).
   - Vérifier la fermeture par tap à l'extérieur (`CanBeDismissedByTappingOutsideOfPopup`).
   - Vérifier la fermeture après enregistrement (`Save`) et annulation (`Cancel`).
3. **Boutons asynchrones** :
   - Vérifier que les boutons liés aux commandes asynchrones apparaissent visuellement désactivés pendant l'exécution (ex: chargement, suppression, sauvegarde).
4. **Surveillance des rechargements `OnAppearing`** :
   - En Popup v2, le popup étant présenté dans une `PopupPage` modale, s'assurer que la fermeture d'un popup ne déclenche pas un rechargement intempestif ou indésirable des données de la page sous-jacente (10 pages écoutent `OnAppearing`).

---

## 7. Statut pour iOS et Mac Catalyst

En attente de l'installation de Xcode 27.1 par le propriétaire et de la vérification préalable de la chaîne d'outils Apple par Claude. Aucune compilation Apple n'est exécutée d'ici là.
