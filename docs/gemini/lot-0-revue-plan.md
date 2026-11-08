# Lot 0 : revue du plan par Claude

Revue du 06/11/2026 de `docs/gemini/lot-0-plan.md` (version du 06/11 08:56). Versions vérifiées sur nuget.org et sur
les métadonnées officielles de .NET le 06/11/2026.

## Verdict : **validé avec corrections**

Le plan est bien construit : le relevé du code est juste (attributs MVVM, classes `partial`, usages de `MainPage`
et `Frame`), le périmètre est respecté et les décisions du propriétaire (installations, OS minimum, graphiques
« le plus moderne ») sont notées. Trois points sont à corriger avant de coder (C1 à C3), dont un changement majeur
que le plan n'a pas vu (C2). Les autres sont des précisions (C4 à C9).

Tu peux créer la branche `feature/lot-0-plateforme` et commencer, **dans l'ordre de la section « Ordre de travail »
ci-dessous**.

## Corrections obligatoires

### C1. Graphiques : prendre le paquet officiel `Microcharts.Maui` 2.0.0.3, pas le fork Lemathieu

- `Lemathieu.Microcharts.Maui` 1.0.4 ne cible **que .NET 9** (`net9.0-android35.0`, `net9.0-ios18.0`…), dépend de
  Microsoft.Maui.Controls 9.0.120 et de SkiaSharp 3.119.1, et vient d'un dépôt personnel
  (`github.com/mrijntjes/Lemathieu.Microcharts.Maui`). L'affirmation « compatibilité native avec .NET 10 » est fausse.
- L'équipe officielle Microcharts publie **`Microcharts.Maui` 2.0.0.3** (stable, juillet 2026) : cibles
  `net10.0-android36.0`, `net10.0-ios26.0`, `net10.0-maccatalyst26.0`, `net10.0` ; dépendances `Microcharts.Core`
  2.0.0.3, Microsoft.Maui.Controls ≥ 10.0.20, **SkiaSharp.Views.Maui.Controls 3.119.4**. Même assemblée
  `Microcharts.Maui`, même espace de noms `Microcharts.Maui` et même type `ChartView` : les 8 pages XAML gardent
  leur `xmlns`. Cela répond mieux au choix du propriétaire (« le plus récent et moderne »).
- Le paquet expose une méthode d'extension **`UseMicrocharts`** : vérifie si elle doit être appelée dans
  `MauiProgram.cs` (en plus ou à la place de `.UseSkiaSharp()`) et dis-le dans le compte rendu.
- SkiaSharp : **3.119.4**, la version qu'exige Microcharts 2.0.0.3. **Ne pas passer à SkiaSharp 4.x**, publié
  depuis, contre lequel Microcharts n'est pas compilé.

### C2. CommunityToolkit.Maui 15 : les popups changent (Popup v2), à traiter explicitement

Le plan passe CommunityToolkit.Maui de 9.0.2 à 15.0.1 sans en mesurer l'effet. Depuis la version 12, le Toolkit
utilise une nouvelle API de popups (« Popup v2 ») avec des changements cassants. L'application en dépend fortement :

- 2 popups XAML : `Components/BottomSheetPopup.xaml` (avec `Color="Transparent"`, propriété de l'ancienne API) et
  `Components/BottomSheetPopupListView.xaml` ;
- `ViewModels/Components/BottomSheetPopupViewModel.cs:33, 167, 215` : instance de `Popup` passée au ViewModel et
  fermeture par `_popup.Close()` ;
- une vingtaine d'appels `ShowPopupAsync(popup)` dans `ViewModels/Lists/`, `ViewModels/Dashboard/` et
  `ViewModels/Statistiques/` (y compris `new Popup()` passé en paramètre, par exemple
  `ListNewCollectionViewModel.cs:62`).

Ce que je demande : **ne touche pas aux popups dans le premier passage.** Fais d'abord les étapes 1 à 5 (voir
« Ordre de travail »), compile, puis écris dans ton plan une section **« Addendum : migration des popups »** :
erreurs de compilation obtenues (copie exacte), changements de l'API v2 qui s'appliquent ici d'après le guide de
migration officiel du Toolkit (avec le lien), et modification prévue fichier par fichier. **Objectif : même
comportement** (feuille en bas d'écran, fond transparent, fermeture par le ViewModel, attente de la fermeture par
`await`). Arrête-toi là : je valide l'addendum avant que tu modifies les popups.

Vérifie aussi que la version de `Microsoft.Maui.Controls` donnée par `$(MauiVersion)` dans le workload installé
est **au moins 10.0.90**, version minimale exigée par CommunityToolkit.Maui 15.0.1 ; sinon NuGet signalera un
conflit de versions. Si c'est le cas, dis-le au lieu de forcer une version.

### C3. Xcode : la compatibilité n'est pas acquise, à vérifier après l'installation

« La machine est donc parfaitement prête » n'est pas démontré. Les paquets de SDK iOS publiés pour .NET 10 existent
pour Xcode 26.0, **26.2**, 26.5 et **27.0** (`Microsoft.iOS.Sdk.net10.0_26.2` 26.2.10233,
`Microsoft.iOS.Sdk.net10.0_27.0` 27.0.10722). Le dernier SDK .NET 10 (10.0.403, 25/10/2026) peut donc installer
des workloads qui exigent **Xcode 27**, alors que ce Mac a Xcode 26.2.

Après l'installation : relève `dotnet workload list` et la version de Xcode exigée par le workload iOS installé.
Si la compilation iOS ou Mac Catalyst échoue sur une exigence de version de Xcode, **arrête-toi et écris-le** avec
l'erreur exacte. Deux options seront alors à soumettre au propriétaire : installer la version de Xcode exigée, ou
fixer un ensemble de workloads (« workload set ») qui vise Xcode 26.2. Ne choisis pas seul.

## Précisions

- **C4. Installation du SDK** : n'utilise pas `curl … dotnet-install.sh | bash`. Ce script installe dans
  `~/.dotnet`, alors que le `dotnet` du `PATH` est `/usr/local/share/dotnet` (SDK 8) : les workloads iraient au
  mauvais endroit. Le propriétaire installe le **paquet officiel macOS (.pkg) du SDK .NET 10** (dernier SDK stable :
  10.0.403), qui se place à côté du SDK 8 dans `/usr/local/share/dotnet`. Les workloads s'installent ensuite avec
  `sudo dotnet workload install maui` (le dossier appartient à root ; `maui` inclut Android, iOS et Mac Catalyst).
  Indique ces commandes au propriétaire au début de ton compte rendu ; ne lance pas `sudo` toi-même.
- **C5. `global.json`** : ajoute à la racine un `global.json` qui fixe le SDK 10 (`"version": "10.0.403"`,
  `"rollForward": "latestFeature"`), pour que `dotnet build` n'utilise jamais le SDK 8 resté installé.
- **C6. JDK** : n'utilise pas OpenJDK 22 (pas pris en charge par .NET pour Android) ni Corretto 11 (trop ancien).
  Reprends la version que donne la documentation officielle « .NET for Android : dépendances » pour .NET 10
  (Microsoft OpenJDK 17 ou 21 : cite la page). Le propriétaire l'installe. Passe-la à la compilation par
  `-p:JavaSdkDirectory=<chemin>` plutôt que de modifier le `JAVA_HOME` global de la machine.
- **C7. Versions des paquets** (dernières versions stables au 06/11/2026) : `CommunityToolkit.Mvvm` **8.4.2** (et non
  8.4.0) ; `Microsoft.Extensions.Http` et `Microsoft.Extensions.Logging.Debug` : dernière **10.0.x stable** (pas les
  11.0 en préversion) ; `System.IdentityModel.Tokens.Jwt` **8.23.0** (et non 8.2.1) ; CommunityToolkit.Maui
  **15.0.1** ; Newtonsoft.Json inchangé. Retraits de `Xamarin.Essentials`, `Microsoft.Maui.Controls.Compatibility` et
  `Microsoft.Maui.Essentials` : d'accord.
- **C8. Commandes asynchrones** : ne pose pas « comportement identique à la v7 » sans preuve. Vérifie dans les notes
  de version de CommunityToolkit.Mvvm 8.0 la valeur par défaut d'`AllowConcurrentExecutions` pour `[RelayCommand]`
  sur une méthode `async Task`, compare-la à celle de `[ICommand]` en 7.1.2, et écris la conclusion dans le compte
  rendu. Si elle diffère, ne change rien au code : signale-le.
- **C9. Vérification au lancement : impossible dans ce lot.** Lancer l'application appellerait l'URL écrite en dur
  (`backend-strapi.online`, site `tenantdefaut`, en production). Le lot 0 se vérifie donc **par compilation
  seulement**, comme prévu. Le contrôle visuel des graphiques et des popups se fera après le lot 1, sur le site
  demo : liste dans ton compte rendu les écrans à contrôler (les 8 pages de statistiques, chaque popup).

## Ordre de travail

1. Créer la branche `feature/lot-0-plateforme` depuis `main`.
2. Prérequis (C4, C6) : indiquer les commandes au propriétaire et attendre qu'il confirme l'installation. Relever
   ensuite `dotnet --info` et `dotnet workload list` dans le compte rendu.
3. `global.json` (C5) et `Easymakemoney.csproj` : cibles `net10.0-*`, OS minimum validés par le propriétaire
   (Android 24.0, iOS 15.0, Mac Catalyst 15.0, Windows inchangé), paquets selon C1 et C7.
4. MVVM : `GlobalUsings.cs` et les 46 `[ICommand]` → `[RelayCommand]` (C8).
5. Graphiques (C1).
6. Compiler les trois cibles. **Les erreurs restantes devraient venir des popups** : écrire l'addendum (C2) et
   s'arrêter. Si d'autres erreurs restent, les corriger seulement si la correction est mécanique et sans effet sur
   le comportement ; sinon les noter.
7. Après ma validation de l'addendum : migration des popups, compilation des trois cibles, compte rendu complet.

Avertissements `Frame`, `MainPage`, `DisplayAlert` : à laisser tels quels dans ce lot, comme tu le proposes, et à
lister dans le compte rendu avec leur nombre.

Le compte rendu (`docs/gemini/lot-0-compte-rendu.md`) doit contenir la sortie **réelle** des commandes (erreurs
copiées telles quelles), le nombre d'avertissements par cible et la liste des fichiers modifiés (`git status`).
