# Lot 0 : plan de Gemini (mis à jour après revue de Claude - Validé avec corrections)

## Ce que j'ai lu

### Documentation de référence
- `GEMINI.md` : circuit de validation (plan sans code -> revue Claude -> code -> compte rendu -> revue Claude), interdiction stricte de toucher au remote, de commiter/pusher, de modifier le backend ou d'appeler l'API sans accord.
- `docs/etat-des-lieux-2026-11-05.md` : inventaire de la plateforme, Xcode 26.2, SDK .NET 8.0.204 seul, identification des risques (Microcharts, Mvvm 7 -> 8), découpage en 5 lots (0 à 4).
- `docs/topo-demarrage.md` : cadrage du projet Izzipro, non-régression, essais exclusivement sur le site `demo`.
- `docs/backend-contrat-gestion.md` : routes ROLE_ADMIN, double facteur OTP, codes d'erreur stables, activation par clé.
- `docs/strategie-multitenant-mobile.md` : modèle commun multi-tenant, en-tête `X-Tenant-Host`, cycle de vie des sessions et jetons.
- `docs/audit-multitenant.md` : audit initial du 15/10/2026.
- `docs/gemini/lot-0-consigne.md` : objectifs du lot 0, contrainte de non-modification du comportement applicatif, périmètre strict (plateforme, cibles .NET 10, MVVM, Microcharts).
- `docs/gemini/lot-0-revue-plan.md` : revue de Claude validant le plan avec corrections C1 à C9 et fixant l'ordre de travail.

### Code source et inventaire
- `Easymakemoney/Easymakemoney.csproj:1-58` :
  - Cibles : `net8.0-android`, `net8.0-ios`, `net8.0-maccatalyst` (+ Windows conditionnel `net8.0-windows10.0.19041.0`).
  - Propriétés : `ApplicationId` (`com.companyname.easymakemoney`), `SupportedOSPlatformVersion` (iOS 11.0, Mac Catalyst 13.1, Android 21.0, Windows 10.0.17763.0), `SingleProject=true`, `EmbedAssembliesIntoApk=true`.
  - 11 dépendances NuGet : `CommunityToolkit.Maui` 9.0.2, `Microcharts.Maui` 1.0.0, `Microsoft.Extensions.Http` 8.0.0, `Microsoft.Maui.Controls` & `Compatibility` `$(MauiVersion)`, `Microsoft.Extensions.Logging.Debug` 8.0.0, `Microsoft.Maui.Essentials` 8.0.61, `Microsoft.Toolkit.Mvvm` 7.1.2, `Newtonsoft.Json` 13.0.3, `SkiaSharp.Views.Maui.Controls` 2.88.8, `System.IdentityModel.Tokens.Jwt` 8.0.1, `Xamarin.Essentials` 1.8.1.
- `Easymakemoney/GlobalUsings.cs:1-57` :
  - Usings MVVM obsolètes : `global using Microsoft.Toolkit.Mvvm.Input;` (l.2) et `global using Microsoft.Toolkit.Mvvm.ComponentModel;` (l.9).
  - Graphiques : `global using Microcharts;` (l.45) et `global using SkiaSharp.Views.Maui.Controls.Hosting;` (l.56).
- `Easymakemoney/MauiProgram.cs:1-173` :
  - Initialisation MAUI : `.UseMauiCommunityToolkit()`, `.UseSkiaSharp()`.
  - Aucun appel à `.UseMauiCompatibility()`.
- `Easymakemoney/Utilities/ChartGenerator.cs:1-48` :
  - Générateur de graphiques utilisant `Microcharts` (`PointChart`, `ChartEntry`) et `SkiaSharp` (`SKColor`, `SKColors`).
- `Easymakemoney/Views/Statistiques/*.xaml` :
  - 8 pages XAML utilisant `xmlns:microcharts="clr-namespace:Microcharts.Maui;assembly=Microcharts.Maui"` et `<microcharts:ChartView ... />` (`PaiementRemboursementPage.xaml`, `FraisClientTotalViewPage.xaml`, `StockPage.xaml`, `TransactionCaissePage.xaml`, `ChiffresAffairePage.xaml`, `OrderNumberPage.xaml`, `PanierMoyenPage.xaml`, `FraisEvolutionEntreprisePage.xaml`).
- MVVM dans `Easymakemoney/ViewModels/` :
  - 46 occurrences de `[ICommand]`, 112 occurrences de `[ObservableProperty]`, 10 `ObservableObject`, 16 `RelayCommand`.
  - Vérification des classes : toutes les classes de ViewModels utilisant `[ICommand]` ou `[ObservableProperty]` sont déjà déclarées avec le mot-clé `partial`.
  - Analyse du nommage des commandes : les méthodes asynchrones se terminant par `Async` (ex. `LoginAsync()` dans `LoginPageViewModel.cs:20`, `GetListCollectionAsync()` dans `ListNewCollectionViewModel.cs:29`, `ShowBottomSheetAsync()` dans `ListNewCollectionViewModel.cs:57`) généraient déjà un nom tronqué sans suffixe (`LoginCommand` dans `LoginPage.xaml:38`, `ShowBottomSheetCommand` dans `ListNewCollectionPage.xaml:164`). Le générateur CommunityToolkit.Mvvm 8.x applique la même règle de troncature.
- Popups XAML et CommunityToolkit.Maui (C2) :
  - `Components/BottomSheetPopup.xaml` (avec `Color="Transparent"`) et `Components/BottomSheetPopupListView.xaml`.
  - `ViewModels/Components/BottomSheetPopupViewModel.cs:33, 167, 215` (`_popup.Close()`).
  - Une vingtaine d'appels `ShowPopupAsync(popup)` dans `ViewModels/Lists/`, `ViewModels/Dashboard/` et `ViewModels/Statistiques/`.
- API obsolètes et avertissements :
  - `Frame` : 157 occurrences dans les fichiers XAML (`Easymakemoney/Views/**/*.xaml`). Avertissement MAUI, conservé tel quel dans ce lot.
  - `Application.Current.MainPage` : 35 occurrences dans les ViewModels et `App.xaml.cs:44`. Avertissement CS0618, conservé tel quel dans ce lot.
- Environnement machine actuel :
  - SDK .NET : `8.0.204` uniquement (`/usr/local/share/dotnet/sdk`).
  - Workloads : `maui`, `maui-android`, `maui-ios`, `maui-maccatalyst` en version `8.0.61/8.0.100`.
  - Xcode : `Xcode 26.2` (Build 17C52) avec SDK iOS 26.2 et simulateurs iOS 26.2 et 17.4.
  - Java : `OpenJDK 22.0.1` et `Amazon Corretto 11.0.23`.
  - Android SDK : `~/Library/Android/sdk` contenant les API platforms 31 à 37.0 et build-tools 30 à 36.0.0.

---

## Prérequis à installer ou à vérifier (commandes exactes, rien n'est lancé par Gemini)

### 1. SDK .NET 10 (macOS .pkg officiel) (C4)
Pour éviter que l'outillage ne s'installe dans `~/.dotnet` en conflit avec `/usr/local/share/dotnet`, le propriétaire installe le paquet officiel macOS (.pkg) du SDK .NET 10 (version stable `10.0.403`) dans `/usr/local/share/dotnet` :
- Téléchargement et installation du `.pkg` (ou depuis https://dotnet.microsoft.com/download/dotnet/10.0) :
  ```bash
  curl -o /tmp/dotnet-sdk-10.0.403-osx-arm64.pkg https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.403/dotnet-sdk-10.0.403-osx-arm64.pkg
  sudo installer -pkg /tmp/dotnet-sdk-10.0.403-osx-arm64.pkg -target /
  ```

### 2. Workloads MAUI .NET 10 (C4)
Une fois le SDK 10 installé, installation des workloads MAUI avec `sudo` (car `/usr/local/share/dotnet` appartient à `root`) :
```bash
sudo dotnet workload install maui
```

### 3. JDK pour Android (C6)
Conformément à la documentation officielle Microsoft [.NET for Android : dépendances](https://learn.microsoft.com/en-us/dotnet/android/getting-started/installation/dependencies), Microsoft OpenJDK 21 LTS est requis pour .NET 10 :
```bash
brew install --cask microsoft-openjdk@21
```
Le chemin d'installation est `/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home`. Il sera passé directement à MSBuild via `-p:JavaSdkDirectory="/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home"`.

### 4. Vérification Xcode 26.2 vs Xcode 27 (C3)
Après l'installation des workloads, relever `dotnet workload list` et vérifier la version de Xcode exigée par le workload iOS :
```bash
dotnet workload list
xcodebuild -version
```
Si le workload exige Xcode 27 alors que la machine a Xcode 26.2, consigner l'erreur exacte dans le compte rendu et soumettre les deux options (installation Xcode 27 ou workload set ciblant Xcode 26.2) au propriétaire.

---

## Ordre de travail et étapes

### Étape 1 : Branche de travail
- Créer et basculer sur la branche `feature/lot-0-plateforme` depuis `main` :
  ```bash
  git switch -c feature/lot-0-plateforme
  ```
  *(Branche créée le 06/11/2026).*

### Étape 2 : Confirmation des prérequis
- Indiquer les commandes au propriétaire et attendre sa confirmation d'installation.
- Relever `dotnet --info` et `dotnet workload list`.

### Étape 3 : Fichiers de configuration du projet (`global.json` et `Easymakemoney.csproj`)
1. **Création de `global.json` à la racine (C5)** :
   ```json
   {
     "sdk": {
       "version": "10.0.403",
       "rollForward": "latestFeature"
     }
   }
   ```
2. **Mise à jour de `Easymakemoney/Easymakemoney.csproj`** :
   - Cibles : `net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst` (+ `net10.0-windows10.0.19041.0`).
   - OS minimal :
     - Android : `24.0`
     - iOS : `15.0`
     - Mac Catalyst : `15.0`
     - Windows : `10.0.17763.0` (inchangé)
   - Paquets NuGet (C1, C7) :
     - Remplacer `Microcharts.Maui 1.0.0` par **`Microcharts.Maui 2.0.0.3`** (officiel, cible .NET 10).
     - Remplacer `SkiaSharp.Views.Maui.Controls 2.88.8` par **`SkiaSharp.Views.Maui.Controls 3.119.4`**.
     - Remplacer `Microsoft.Toolkit.Mvvm 7.1.2` par **`CommunityToolkit.Mvvm 8.4.2`**.
     - Mettre à jour `CommunityToolkit.Maui` vers **`15.0.1`**.
     - Mettre à jour `Microsoft.Extensions.Http` vers **`10.0.0`** (dernière 10.0.x stable).
     - Mettre à jour `Microsoft.Extensions.Logging.Debug` vers **`10.0.0`** (dernière 10.0.x stable).
     - Mettre à jour `System.IdentityModel.Tokens.Jwt` vers **`8.23.0`**.
     - Conserver `Newtonsoft.Json 13.0.3`.
     - Retirer `Xamarin.Essentials 1.8.1`.
     - Retirer `Microsoft.Maui.Controls.Compatibility`.
     - Retirer `Microsoft.Maui.Essentials 8.0.61`.
   - Vérifier que `$(MauiVersion)` est ≥ 10.0.90 (requis par CommunityToolkit.Maui 15.0.1).

### Étape 4 : Migration MVVM (C8)
1. **`Easymakemoney/GlobalUsings.cs`** :
   - Ligne 2 : `global using CommunityToolkit.Mvvm.Input;`
   - Ligne 9 : `global using CommunityToolkit.Mvvm.ComponentModel;`
2. **Remplacement des 46 occurrences de `[ICommand]` par `[RelayCommand]`** dans les ViewModels.
3. **Surveillance des commandes asynchrones (C8)** :
   - Dans CommunityToolkit.Mvvm 8.0+, `AllowConcurrentExecutions` vaut `false` par défaut sur une méthode `async Task` (générant un `AsyncRelayCommand` qui désactive `CanExecute` pendant l'exécution). En Toolkit 7.1.2, `[ICommand]` autorisait par défaut l'exécution concurrente.
   - Ne rien modifier dans le code, mais consigner cette différence de comportement protectrice (évite les doubles clics) dans le compte rendu.

### Étape 5 : Graphiques (C1)
1. Conserver les types `ChartView`, `ChartEntry`, `PointChart` et `SKColor` dans `ChartGenerator.cs` et les 8 pages de `Views/Statistiques/`.
2. Vérifier si `builder.UseMicrocharts()` doit être ajouté dans `MauiProgram.cs` en plus ou à la place de `.UseSkiaSharp()`.

### Étape 6 : Première compilation & Addendum Popups (C2)
1. **Ne pas toucher aux popups** dans ce premier passage.
2. Compiler les trois cibles :
   - Android : `dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -c Debug -p:JavaSdkDirectory="/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home"`
   - iOS : `dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-ios -c Debug -p:RuntimeIdentifier=iossimulator-arm64`
   - Mac Catalyst : `dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-maccatalyst -c Debug`
3. Relever les erreurs de compilation exactes (qui doivent se limiter aux popups du Toolkit).
4. Rédiger la section **« Addendum : migration des popups »** dans ce document `lot-0-plan.md` avec :
   - Erreurs exactes obtenues.
   - Changements de l'API Popup v2 de CommunityToolkit.Maui qui s'appliquent.
   - Modifications prévues fichier par fichier.
5. Commencer `docs/gemini/lot-0-compte-rendu.md` avec les sorties réelles.
6. **S'arrêter et attendre la validation de Claude** sur l'addendum avant de modifier les popups.

### Étape 7 : Migration des popups (après validation de l'addendum)
1. Appliquer les modifications validées sur les popups et leurs appels.
2. Recompiler les trois cibles.
3. Compléter `docs/gemini/lot-0-compte-rendu.md`.

---

## Addendum : migration des popups (C2)
## Addendum : migration des popups (C2)

### 1. Constat et erreurs de compilation réelles obtenues (Étape 6)

Lors de la compilation de la cible `net10.0-android`, **28 erreurs CS1061** ont été relevées, toutes causées par l'évolution de l'API des Popups dans `CommunityToolkit.Maui` v15.0.1 (Popup v2). Aucune autre erreur n'a été rencontrée dans le reste du code de l'application (MVVM, SkiaSharp, Microcharts, JWT, etc. compilent sans aucune erreur).

#### A. Erreurs d'extension `ShowPopupAsync` sur `Page` (26 occurrences) :
Exemple exact de sortie MSBuild :
```text
/Users/malmont/Desktop/projets/Easymakemoney1/Easymakemoney/ViewModels/Lists/ListNewCollectionViewModel.cs(73,36): error CS1061: 'Page' does not contain a definition for 'ShowPopupAsync' and no accessible extension method 'ShowPopupAsync' accepting a first argument of type 'Page' could be found (are you missing a using directive or an assembly reference?) [/Users/malmont/Desktop/projets/Easymakemoney1/Easymakemoney/Easymakemoney.csproj::TargetFramework=net10.0-android]
/Users/malmont/Desktop/projets/Easymakemoney1/Easymakemoney/ViewModels/Dashboard/DashBoardCollectionViewModel.cs(128,48): error CS1061: 'Page' does not contain a definition for 'ShowPopupAsync' and no accessible extension method 'ShowPopupAsync' accepting a first argument of type 'Page' could be found (are you missing a using directive or an assembly reference?) [/Users/malmont/Desktop/projets/Easymakemoney1/Easymakemoney/Easymakemoney.csproj::TargetFramework=net10.0-android]
/Users/malmont/Desktop/projets/Easymakemoney1/Easymakemoney/ViewModels/Statistiques/StockEvolutionViewModel.cs(104,48): error CS1061: 'Page' does not contain a definition for 'ShowPopupAsync' and no accessible extension method 'ShowPopupAsync' accepting a first argument of type 'Page' could be found (are you missing a using directive or an assembly reference?) [/Users/malmont/Desktop/projets/Easymakemoney1/Easymakemoney/Easymakemoney.csproj::TargetFramework=net10.0-android]
```
Fichiers touchés :
- `ViewModels/Lists/ListNewCollectionViewModel.cs` (l. 73)
- `ViewModels/Lists/ListNewCommandViewModels.cs` (l. 100)
- `ViewModels/Lists/ListNewFournisseurViewModel.cs` (l. 111)
- `ViewModels/Lists/ListNewFraisDePortViewModel.cs` (l. 158, 183, 218, 239, 272)
- `ViewModels/Lists/ListNewNoteDeFraisViewModel.cs` (l. 119, 142, 163)
- `ViewModels/Lists/ListNewProductViewModels.cs` (l. 73)
- `ViewModels/Lists/ListNewProductVariantViewModel.cs` (l. 72)
- `ViewModels/Dashboard/DashBoardCollectionViewModel.cs` (l. 128, 148)
- `ViewModels/Dashboard/DashBoardCommandViewModel.cs` (l. 87, 113)
- `ViewModels/Dashboard/DashBoardProductViewModel.cs` (l. 62, 89)
- `ViewModels/Statistiques/FraisClientTotalViewModel.cs` (l. 88)
- `ViewModels/Statistiques/FraisEvolutionEntrepriseViewModel.cs` (l. 95)
- `ViewModels/Statistiques/OrderCountViewModel.cs` (l. 179)
- `ViewModels/Statistiques/PaiementRemboursementViewmodel.cs` (l. 110)
- `ViewModels/Statistiques/StatistiquesDataValueViewmodel.cs` (l. 131)
- `ViewModels/Statistiques/StockEvolutionViewModel.cs` (l. 104)

#### B. Erreurs de fermeture `Popup.Close()` (2 occurrences) :
Exemple exact de sortie MSBuild :
```text
/Users/malmont/Desktop/projets/Easymakemoney1/Easymakemoney/ViewModels/Components/BottomSheetPopupViewModel.cs(215,24): error CS1061: 'Popup' does not contain a definition for 'Close' and no accessible extension method 'Close' accepting a first argument of type 'Popup' could be found (are you missing a using directive or an assembly reference?) [/Users/malmont/Desktop/projets/Easymakemoney1/Easymakemoney/Easymakemoney.csproj::TargetFramework=net10.0-android]
/Users/malmont/Desktop/projets/Easymakemoney1/Easymakemoney/ViewModels/Components/BottomSheetPopupViewModel.cs(225,20): error CS1061: 'Popup' does not contain a definition for 'Close' and no accessible extension method 'Close' accepting a first argument of type 'Popup' could be found (are you missing a using directive or an assembly reference?) [/Users/malmont/Desktop/projets/Easymakemoney1/Easymakemoney/Easymakemoney.csproj::TargetFramework=net10.0-android]
```
Et dans `BottomSheetPopupListViewViewModel.cs(50,16)` :
```text
/Users/malmont/Desktop/projets/Easymakemoney1/Easymakemoney/ViewModels/Components/BottomSheetPopupListViewViewModel.cs(50,16): error CS1061: 'Popup' does not contain a definition for 'Close' and no accessible extension method 'Close' accepting a first argument of type 'Popup' could be found (are you missing a using directive or an assembly reference?)
```

---

### 2. Changements de l'API Popup v2 de CommunityToolkit.Maui

*Source officielle :* [Documentation CommunityToolkit.Maui Popup v2](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/maui/views/popup) et [Guide de migration Popup v2 (GitHub pull request #2308 / release notes v12+)](https://github.com/CommunityToolkit/Maui/wiki).

Les ruptures de compatibilité identifiées sont :
1. **Namespace de `PopupExtensions`** :
   Dans les versions antérieures (v9), `ShowPopupAsync` résidait dans `CommunityToolkit.Maui.Views`. Dans l'architecture Popup v2 (v12+ / v15+), les méthodes d'extension pour les popups ont été déplacées vers l'espace de noms `CommunityToolkit.Maui.Extensions`. L'absence de ce `using` empêche la résolution de `ShowPopupAsync`.
2. **Méthode de fermeture de Popup** :
   La méthode synchrone `Close()` sur la classe `Popup` a été remplacée par la méthode asynchrone `CloseAsync()` (ou `ClosePopupAsync`).
3. **Propriétés visuelles de `Popup`** :
   Dans `Popup v2`, `Popup` hérite désormais de `ContentView`. La couleur d'arrière-plan du canevas popup (`Color="Transparent"`) reste supportée (via `Color` ou `BackgroundColor`), préservant la feuille de style transparente en bas d'écran.

---

### 3. Plan de modification fichier par fichier (pour l'étape 7)

L'objectif strict est de **conserver exactement le même comportement applicatif** :
- Affichage de la feuille en bas d'écran avec fond transparent ;
- Déclenchement de l'affichage via `ShowPopupAsync` ;
- Fermeture initiée depuis le ViewModel ;
- Attente de la fermeture par `await`.

#### Fichiers à modifier :

1. **`Easymakemoney/GlobalUsings.cs`** :
   - Ajouter : `global using CommunityToolkit.Maui.Extensions;`
   - *Effet* : Rend disponible `ShowPopupAsync` sur l'ensemble des pages sans toucher aux 16 fichiers ViewModels appelants.

2. **`Easymakemoney/ViewModels/Components/BottomSheetPopupViewModel.cs`** :
   - Ligne 215 : Remplacer `_popup.Close();` par `_popup.CloseAsync();` (ou `await _popup.CloseAsync();`).
   - Ligne 225 : Remplacer `_popup.Close();` par `_popup.CloseAsync();`.

3. **`Easymakemoney/ViewModels/Components/BottomSheetPopupListViewViewModel.cs`** :
   - Ligne 50 : Remplacer `_popup.Close();` par `_popup.CloseAsync();`.

4. **XAML des popups (`Components/BottomSheetPopup.xaml` et `Components/BottomSheetPopupListView.xaml`)** :
   - Vérifier que la propriété `Color="Transparent"` compile sans avertissement et préserve la transparence du fond. Si `Color` déclenche un avertissement XAML, utiliser `Color="Transparent"` ou `BackgroundColor="Transparent"`.

---

### 4. Situation Xcode sur iOS et Mac Catalyst (C3)

La tentative de compilation sur les cibles Apple a immédiatement rencontré la vérification d'outillage :
```text
/usr/local/share/dotnet/packs/Microsoft.iOS.Sdk.net10.0_27.1/27.1.10018/targets/Xamarin.Shared.Sdk.targets(2998,3): error : This version of .NET for iOS (27.1.10018) requires Xcode 27.1. The current version of Xcode is 26.2. Either install Xcode 27.1, or use a different version of .NET for iOS. See https://aka.ms/xcode-requirement for more information.
```
```text
/usr/local/share/dotnet/packs/Microsoft.MacCatalyst.Sdk.net10.0_27.0/27.0.10742/targets/Xamarin.Shared.Sdk.targets(2998,3): error : This version of .NET for MacCatalyst (27.0.10742) requires Xcode 27.1. The current version of Xcode is 26.2. Either install Xcode 27.1, or use a different version of .NET for MacCatalyst. See https://aka.ms/xcode-requirement for more information.
```

**Preuve d'alignement du code C#** :
En passant `-p:ValidateXcodeVersion=false` à MSBuild, la compilation iOS et Mac Catalyst franchit la vérification d'outillage Apple et aboutit **exactement aux mêmes 28 erreurs CS1061 de popups** qu'Android, sans aucune autre erreur dans le code.

**Deux options soumises à Claude et au propriétaire pour iOS / Mac Catalyst** :
- **Option 1 (Conserver Xcode 26.2 avec contournement MSBuild ou workload set)** :
  Ajouter `<ValidateXcodeVersion>false</ValidateXcodeVersion>` dans le `.csproj` (ou en argument de build) pour autoriser la compilation avec Xcode 26.2.
- **Option 2 (Mettre à jour Xcode)** :
  Installer Xcode 27.1 sur le Mac si le propriétaire dispose de la mise à jour système.

*(Arrêt conformément à l'étape 6 de l'ordre de travail : attente de la validation de Claude sur cet addendum).*
