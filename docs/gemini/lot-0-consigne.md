# Lot 0 : plateforme (.NET MAUI 8 → .NET 10)

Consigne rédigée par Claude le 05/11/2026. Lis d'abord `GEMINI.md` à la racine du dépôt.

## Objectif

Faire compiler l'application en **.NET 10 (LTS)** pour Android, le simulateur iOS et Mac Catalyst sur ce Mac,
**sans changer son comportement**. Aucune modification des appels réseau, de la connexion, des écrans ni des modèles :
ce sont les lots 1 à 3.

Branche de travail : `feature/lot-0-plateforme`, créée depuis `main` (commit `e2b05e7`), **seulement après
validation du plan**.

## Faits relevés dans le code (à vérifier toi-même)

- `Easymakemoney/Easymakemoney.csproj` : cibles `net8.0-android;net8.0-ios;net8.0-maccatalyst` (+ Windows sous
  Windows) ; `ApplicationId` `com.companyname.easymakemoney` (**ne pas le changer** : décision du propriétaire en
  attente) ; OS minimum iOS 11, Mac Catalyst 13.1, Android 21.
- Paquets : CommunityToolkit.Maui 9.0.2, Microcharts.Maui 1.0.0, Microsoft.Extensions.Http 8.0.0,
  Microsoft.Maui.Controls et Microsoft.Maui.Controls.Compatibility `$(MauiVersion)`,
  Microsoft.Extensions.Logging.Debug 8.0.0, Microsoft.Maui.Essentials 8.0.61, **Microsoft.Toolkit.Mvvm 7.1.2**,
  Newtonsoft.Json 13.0.3, SkiaSharp.Views.Maui.Controls 2.88.8, System.IdentityModel.Tokens.Jwt 8.0.1,
  **Xamarin.Essentials 1.8.1**.
- **MVVM** : `Microsoft.Toolkit.Mvvm` n'est importé que dans `GlobalUsings.cs` (`Microsoft.Toolkit.Mvvm.Input` et
  `Microsoft.Toolkit.Mvvm.ComponentModel`). Le code contient 46 attributs `[ICommand]`, 112 `[ObservableProperty]`,
  10 `ObservableObject` et 16 `RelayCommand` utilisés directement.
- **Xamarin.Essentials** et **Microsoft.Maui.Controls.Compatibility** : aucun usage trouvé dans les `.cs` et `.xaml`
  (pas de `UseMauiCompatibility` dans `MauiProgram.cs`).
- **Graphiques** : Microcharts est utilisé par `Utilities/ChartGenerator.cs` et 8 pages de `Views/Statistiques/`.
  `MauiProgram.cs` appelle `.UseSkiaSharp()`.
- Machine : .NET SDK 8.0.204 seul ; workloads `maui`, `maui-ios`, `maui-android`, `maui-maccatalyst` 8.0.61 ;
  **Xcode 26.2** ; JDK installés : OpenJDK 22.0.1 (par défaut) et Corretto 11 ; SDK Android présent dans
  `~/Library/Android/sdk`.

## Ce que ton plan doit couvrir

1. **Prérequis** : version exacte du SDK .NET 10 et des workloads MAUI à installer ; version de Xcode exigée par ces
   workloads, comparée à Xcode 26.2 ; JDK exigé pour Android en .NET 10, comparé aux JDK présents (11 et 22) ;
   niveaux d'API Android requis. Commandes exactes, **sans les lancer**. Cite tes sources (page de documentation ou
   note de version, avec la version).
2. **Projet** : cibles `net10.0-*` ; versions minimales des OS si .NET 10 les impose (dis lesquelles et pourquoi) ;
   `SingleProject`, `EmbedAssembliesIntoApk` et autres propriétés à garder ou à changer.
3. **Paquets** : pour chacun, version cible compatible .NET 10, ou retrait justifié. Points attendus :
   - `Microsoft.Toolkit.Mvvm` 7 → `CommunityToolkit.Mvvm` 8 : `[ICommand]` → `[RelayCommand]` (46 occurrences),
     `using` de `GlobalUsings.cs`, et différences de comportement à surveiller (noms générés des commandes,
     commandes asynchrones et exécution concurrente, `[ObservableProperty]` sur des champs, classes `partial`).
     Garde les `[ObservableProperty]` **sur des champs** : pas de passage aux propriétés partielles dans ce lot.
   - Retrait de `Xamarin.Essentials` et de `Microsoft.Maui.Controls.Compatibility` si tu confirmes qu'ils ne
     servent pas.
   - `Microsoft.Maui.Essentials` : encore nécessaire en paquet séparé avec .NET 10 ?
   - **Microcharts.Maui 1.0.0 et SkiaSharp** : risque principal du lot. Microcharts.Maui 1.0.0 est compilé contre
     SkiaSharp 2.88 ; vérifie s'il fonctionne avec la version de SkiaSharp qu'impose .NET 10 / CommunityToolkit.Maui
     à jour. Si ce n'est pas le cas, propose des options (garder SkiaSharp 2.88 si possible, autre version de
     Microcharts, remplacement par une autre bibliothèque) **sans en choisir une seule** : le choix revient au
     propriétaire.
4. **Avertissements et API obsolètes** de MAUI 10 qui touchent ce code (par exemple `Frame`, `Device`,
   `Application.MainPage`, `DisplayAlert`), à relever avec chemin:ligne. Corrige seulement ceux qui **empêchent la
   compilation** ; liste les autres pour un lot ultérieur.
5. **Vérification** : commandes de compilation par cible (Android, simulateur iOS, Mac Catalyst) et ce que tu
   considères comme un succès. Compiler seulement, **sans lancer l'application** (elle appellerait le backend).

## Hors du lot (ne pas toucher)

URL du backend, `HttpService`, connexion, stockage du jeton, `EncryptionHelper`, services, routes, écrans, textes,
`ApplicationId`, icônes, dossier `Platforms/Tizen`.
