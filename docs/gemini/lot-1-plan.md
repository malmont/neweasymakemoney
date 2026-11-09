# Lot 1 : plan de Gemini

## Ce que j'ai lu

### Documents de référence
- `docs/gemini/lot-1-consigne.md` : objectifs du lot 1 (choix du site, client HTTP unique, `TenantContext`, `TenantStore`, `TenantHandler`, `SiteService`, `ActivationPage`, cycle de démarrage, changement de site, `ApplicationId`, vérifications).
- `docs/strategie-multitenant-mobile.md` (§1 à 3) : contrat multi-tenant commun Izzipro. Pièges du backend (sans en-tête ou hôte inconnu = base de production ou `tenantdefaut` servie sans erreur) ; règle absolue : aucune requête vers l'API sans site actif validé ; en-tête `X-Tenant-Host = tenant.host` ; activation par clé (`POST /api/app/activate {key, app: "admin", deviceName}`) sans `X-Tenant-Host` ; repli `check-mobile` (`GET /api/tenant/check-mobile`) avec la saisie nettoyée dans `X-Tenant-Host`.
- `docs/backend-contrat-gestion.md` : identité `app: "admin"`, format de clé `IZ-ADM-XXXX-XXXX-XXXX`, codes d'erreur `APP_KEY_MISSING` (400), `APP_KEY_INVALID` (404), `APP_KEY_WRONG_APP` (403), `TENANT_NOT_FOUND` (404), `TENANT_HOST_MISSING` (400), `RATE_LIMITED` / `TOO_MANY_ATTEMPTS` (429).
- `docs/lot-1-retours-applis-soeurs.md` (§4 et §5) : retour d'expérience POS et e-commerce. La saisie du code de site doit être bien visible sur le même écran ; carte de confirmation « C'est bien ça ? » avec « Oui » et « Non » ; pas de conservation de la clé d'activation ; effacement complet au changement de site et au premier lancement (trousseau iOS persistant après désinstallation).
- `docs/revue-architecture.md` (§2 A et B, §3 ligne 1) : les 24 constantes d'URL dans les services, `HttpService` et `DefaultRequestHeaders`, capture du client par les singletons (`LoginService`, `ListCollectionService`), ViewModels singletons (`LoginPageViewModel`, `DashboardPageViewModel`, `LoadingPageViewModel`), et décisions du propriétaire du 08/11/2026.

### Code existant (vérifié par les commandes réelles)
- `Easymakemoney/const/Configurations.cs:4` : déclare `public const string BackendSymfonyUrl = "https://backend-strapi.online/api";` (sans barre finale).
- `Easymakemoney/Services/` : exactement 24 fichiers déclarent une constante `XxxUrl = Configurations.BackendSymfonyUrl;` et concatènent leurs chemins avec un slash initial (ex: `CollectionsUrl + "/collections"` dans `ListCollectionService.cs:15`).
- `Easymakemoney/Services/HttpService.cs:8-15` : reçoit `HttpClient` injecté ; lignes 25, 48, 85, 130 modifient `DefaultRequestHeaders.Authorization`.
- `Easymakemoney/MauiProgram.cs` :
  - ligne 34 : `builder.Services.AddHttpClient<IHttpService, HttpService>();`
  - lignes 36-37 : `builder.Services.AddSingleton<ILoginService, LoginService>();`, `builder.Services.AddSingleton<IListCollectionService, ListCollectionService>();`
  - lignes 142-144 : `builder.Services.AddSingleton<LoginPageViewModel>();`, `builder.Services.AddSingleton<DashboardPageViewModel>();`, `builder.Services.AddSingleton<LoadingPageViewModel>();`
- `Easymakemoney/AppShell.xaml` :
  - lignes 13-19 : routes `LoadingPage` et `LoginPage` déclarées hors menu (`FlyoutItemIsVisible="False"`).
  - lignes 42-46 : bouton « Sign Out » dans `Shell.FlyoutFooter` lié à `SignOutCommand`.
- `Easymakemoney/AppShell.xaml.cs:9` : `this.BindingContext = new AppShellViewModel();`.
- `Easymakemoney/ViewModels/AppShellViewModel.cs:7-14` : `SignOut()` supprime uniquement `nameof(App.UserDetails)` dans `Preferences` puis navigue vers `//LoginPage`.
- `Easymakemoney/ViewModels/Startup/LoadingPageViewModel.cs:6-9` : constructeur appelant `checkUserLoginDetails()` en `async void`.
- Exactement 13 accès directs à `Preferences.` dans le code : `AppShellViewModel.cs` (2), `LoadingPageViewModel.cs` (2), `PreferenceService.cs` (9).
- `Easymakemoney/Easymakemoney.csproj:13` : `<ApplicationId>com.companyname.easymakemoney</ApplicationId>`.
- `Easymakemoney/Platforms/MacCatalyst/Entitlements.plist:1-11` : ne contient que `app-sandbox` et `network.client` ; ne contient pas `keychain-access-groups`.
- `Easymakemoney/Platforms/iOS/` : aucun fichier `Entitlements.plist` n'existe.

---

## Prérequis à installer ou à vérifier (commandes exactes, rien n'est lancé)

Aucun outil ni paquet externe n'est à installer pour ce lot.
Les paquets déjà installés au lot 0 (`CommunityToolkit.Mvvm 8.4.2`, `CommunityToolkit.Maui 15.0.1`, `Microsoft.Extensions.Http 10.0.14`) fournissent l'intégralité des fonctionnalités requises (`IHttpClientFactory`, `DelegatingHandler`, `SecureStorage`, `Preferences`).

Vérifications de chaîne d'outils préalables (sans installation) :
```bash
dotnet --version
```
Attendu : `10.0.403`.

---

## Étapes

### Étape 1 : Adresse de l'API en un seul endroit et chemins relatifs dans les 24 services
- **Fichier modifié** : `Easymakemoney/const/Configurations.cs`
  - Conserver la constante en ajoutant la barre finale obligatoire :
    ```csharp
    public static class Configurations
    {
        public const string BackendSymfonyUrl = "https://backend-strapi.online/api/";
    }
    ```
- **Fichier modifié** : `Easymakemoney/MauiProgram.cs`
  - Configurer `HttpClient.BaseAddress` sur cette URL avec barre finale :
    ```csharp
    builder.Services.AddHttpClient<IHttpService, HttpService>(client =>
    {
        client.BaseAddress = new Uri(Configurations.BackendSymfonyUrl);
    });
    ```
- **24 fichiers modifiés dans `Easymakemoney/Services/`** :
  Suppression de la constante locale `private const string XxxUrl = Configurations.BackendSymfonyUrl;` et passage des chemins en relatifs sans barre oblique initiale (pour que `HttpClient` ne supprime pas le segment `/api/`).
  
  Liste exhaustive des 24 fichiers :
  1. `Easymakemoney/Services/AverageOrderValueServices.cs`
  2. `Easymakemoney/Services/ChiffreAffaireService.cs`
  3. `Easymakemoney/Services/CollectionCloseService.cs`
  4. `Easymakemoney/Services/DashBoardCollectionService.cs`
  5. `Easymakemoney/Services/DashboardCommandServices.cs`
  6. `Easymakemoney/Services/FraisClientTotalService.cs`
  7. `Easymakemoney/Services/FraisEvolutionService.cs`
  8. `Easymakemoney/Services/ListCategoryServices.cs`
  9. `Easymakemoney/Services/ListCollectionService.cs`
  10. `Easymakemoney/Services/ListColorsServices.cs`
  11. `Easymakemoney/Services/ListCommandService.cs`
  12. `Easymakemoney/Services/ListFourniseursServices.cs`
  13. `Easymakemoney/Services/ListFraisDePortService.cs`
  14. `Easymakemoney/Services/ListNoteDeFraisServices.cs`
  15. `Easymakemoney/Services/ListProductService.cs`
  16. `Easymakemoney/Services/ListProductVarianstService.cs`
  17. `Easymakemoney/Services/ListSizesServices.cs`
  18. `Easymakemoney/Services/ListStyleService.cs`
  19. `Easymakemoney/Services/ListTransporteurService.cs`
  20. `Easymakemoney/Services/LoginService.cs`
  21. `Easymakemoney/Services/OrderCountServices.cs`
  22. `Easymakemoney/Services/PaiementRemboursementService.cs`
  23. `Easymakemoney/Services/StockEvolutionServices.cs`
  24. `Easymakemoney/Services/TransactionCaisseService.cs`

  **Exemple avant / après (`ListCollectionService.cs`)** :
  *Avant* :
  ```csharp
  private const string CollectionsUrl = Configurations.BackendSymfonyUrl;
  ...
  var collections = await _httpService.GetAsync<ObservableCollection<ListCollection>>(CollectionsUrl +"/collections");
  var result = await _httpService.PostAsyncWithAuth<object>(CollectionsUrl + "/createcollections", newCollection);
  var url = $"{CollectionsUrl}/collections/{id}";
  ```
  *Après* :
  ```csharp
  // Constante supprimée
  ...
  var collections = await _httpService.GetAsync<ObservableCollection<ListCollection>>("collections");
  var result = await _httpService.PostAsyncWithAuth<object>("createcollections", newCollection);
  var url = $"collections/{id}";
  ```
  Aucune route, paramètre ou corps n'est modifié.

### Étape 2 : `TenantContext`, `TenantStore` et gestion des droits Keychain
- **Nouveau fichier** : `Easymakemoney/Models/Tenant/TenantContext.cs`
  - Classe portant `{ Host, Code, Name, Label }`.
  - Règle métier : si `Name` est nul ou blanc, il prend la valeur de `Code`.
  - Ne stocke pas la clé d'activation.
- **Nouveau fichier** : `Easymakemoney/Services/Tenant/ITenantStore.cs` et `TenantStore.cs`
  - Stocke et relit le `TenantContext` en JSON sous la clé unique `"tenant_context"` dans `SecureStorage`.
  - Cache en mémoire `CurrentTenant` pour lecture synchrone immédiate dans `TenantHandler`.
  - Enregistrement de `tenant.host` retourné par le serveur, jamais la saisie utilisateur.
  - Méthode d'effacement `ClearAsync()` (`SecureStorage.Default.Remove("tenant_context")` ou `RemoveAll()`).
  - Méthode de vérification du premier lancement `EnsureFirstRunCleanAsync()` :
    Vérifie la clé booléenne `has_run_before` dans `Preferences`. Si absente : appel `SecureStorage.Default.RemoveAll()`, puis `Preferences.Set("has_run_before", true)`.
- **Fichiers Entitlements (Keychain)** :
  *Source* : Documentation officielle Microsoft Learn (*"Secure storage - .NET MAUI"* et *"Entitlements and capabilities"*).
  - **Fichier modifié** : `Easymakemoney/Platforms/MacCatalyst/Entitlements.plist`
    Ajout du groupe d'accès Keychain indispensable sous Mac Catalyst sandboxé :
    ```xml
    <key>keychain-access-groups</key>
    <array>
        <string>$(AppIdentifierPrefix)$(CFBundleIdentifier)</string>
    </array>
    ```
  - **Nouveau fichier** : `Easymakemoney/Platforms/iOS/Entitlements.plist`
    Création du fichier d'entitlements pour le trousseau iOS (simulateur) :
    ```xml
    <?xml version="1.0" encoding="UTF-8"?>
    <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
    <plist version="1.0">
    <dict>
        <key>keychain-access-groups</key>
        <array>
            <string>$(AppIdentifierPrefix)com.arkanoamedia.gestion</string>
        </array>
    </dict>
    </plist>
    ```
  - **Fichier modifié** : `Easymakemoney/Easymakemoney.csproj`
    Condition pour le simulateur iOS en Debug :
    ```xml
    <PropertyGroup Condition="$(TargetFramework.Contains('-ios')) and '$(Configuration)' == 'Debug'">
        <CodesignEntitlements>Platforms/iOS/Entitlements.plist</CodesignEntitlements>
    </PropertyGroup>
    ```

### Étape 3 : Gestionnaire HTTP de site (`TenantHandler`)
- **Nouveau fichier** : `Easymakemoney/Services/Tenant/TenantMissingException.cs`
  - Exception dédiée levée quand une requête normale tente de partir sans site actif.
- **Nouveau fichier** : `Easymakemoney/Services/Tenant/TenantHandlerOptions.cs`
  - Déclare les clés d'options `HttpRequestOptionsKey` :
    - `SkipTenantHost` (`bool`) : pour l'activation (`POST api/app/activate`).
    - `CustomTenantHost` (`string`) : pour le contrôle de site (`GET api/tenant/check-mobile`).
  - Fournit les méthodes d'extension `request.SetSkipTenantHost()` et `request.SetCustomTenantHost(string host)`.
- **Nouveau fichier** : `Easymakemoney/Services/Tenant/TenantHandler.cs`
  - Hérite de `DelegatingHandler`.
  - Dans `SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)` :
    1. Si `request.Options.TryGetValue(TenantHandlerOptions.SkipTenantHost, out var skip) && skip` : envoi sans en-tête `X-Tenant-Host`.
    2. Sinon si `request.Options.TryGetValue(TenantHandlerOptions.CustomTenantHost, out var customHost) && !string.IsNullOrEmpty(customHost)` : ajoute `X-Tenant-Host = customHost`.
    3. Sinon (cas normal) :
       - Récupère le tenant actif depuis `_tenantStore.CurrentTenant` (ou `await _tenantStore.GetTenantAsync(cancellationToken)`).
       - Si aucun tenant ou hôte vide : **lève immédiatement `TenantMissingException`** sans appeler `base.SendAsync`. La requête ne part pas.
       - Si présent : ajoute `request.Headers.Add("X-Tenant-Host", tenant.Host)`.
    4. Aucune règle d'exclusion n'est basée sur l'URL / chemin.
- **Fichier modifié** : `Easymakemoney/MauiProgram.cs`
  - Enregistrer `TenantHandler` en transient :
    `builder.Services.AddTransient<TenantHandler>();`
  - L'attacher au client HTTP :
    ```csharp
    builder.Services.AddHttpClient<IHttpService, HttpService>(client =>
    {
        client.BaseAddress = new Uri(Configurations.BackendSymfonyUrl);
    })
    .AddHttpMessageHandler<TenantHandler>();
    ```

### Étape 4 : Service de site (`SiteService`) et lecture d'erreurs (`ApiError`)
- **Nouveaux fichiers** :
  - `Easymakemoney/Models/Api/ApiError.cs` :
    Structure portant `Code`, `Message`, `RetryAfterSeconds`, `Errors`.
    Fonction pure de parsing : lit `code` si au format `^[A-Z0-9_]+$`, sinon `error` ; extrait `message` ; extrait `Retry-After` (en-tête HTTP), sinon champ `retryAfterSeconds` / `retryAfter` du JSON, sinon défaut 60s sur un 429. Fournit les textes de repli en français.
  - `Easymakemoney/Services/Site/ISiteService.cs` et `SiteService.cs` :
    Contient les fonctions pures :
    - `ValidateAppKeyFormat(string input, out string cleanedKey)` : élimine tout caractère non alphanumérique, passe en majuscules, teste le motif `^IZ[A-Z]{3}[A-Z0-9]{12}$`.
    - `CleanSiteInput(string input)` : minuscules, trim, retrait des espaces, `http://`, `https://`, `www.`, chemin et port.
    Méthodes asynchrones :
    - `ActivateWithKeyAsync(string rawKey)` :
      Vérifie le format localement. Si invalide, retourne une erreur sans requête.
      Si valide, prépare la requête `POST api/app/activate` avec corps `{ key = rawKey.Trim(), app = "admin", deviceName = $"{DeviceInfo.Manufacturer} {DeviceInfo.Model}" }` et option `SkipTenantHost`.
      En cas de 200, désérialise et retourne le `TenantContext`.
      En cas d'erreur (400, 404, 403, 429, réseau, 5xx), parse l'`ApiError` et la remonte avec son message en français.
    - `CheckSiteCodeAsync(string rawInput)` :
      Nettoie la saisie. Prépare `GET api/tenant/check-mobile` avec option `CustomTenantHost = cleanedInput`.
      En cas de 200, vérifie `exists == true` et la présence de `tenant.host` et `tenant.code`.
      En cas de 404, 400, 429, réseau ou 5xx, parse l'`ApiError`.
- **Fichier modifié** : `Easymakemoney/MauiProgram.cs`
  - Enregistrer `ISiteService, SiteService` en transient avec le même `HttpClient` et `TenantHandler`.

### Étape 5 : Écran d'activation (`ActivationPage` et `ActivationPageViewModel`)
- **Nouveaux fichiers** :
  - `Easymakemoney/Views/Startup/ActivationPage.xaml` et `ActivationPage.xaml.cs`
    - Deux sections distinctes de même importance visuelle :
      1. Zone « Clé d'activation » : label explicatif, champ de saisie (`BorderlessEntry`), bouton « Valider la clé ».
      2. Zone « Code de la boutique » : label explicatif, champ de saisie bien visible (pour `DEM042` / `demo`), bouton « Valider le code ».
    - Carte de confirmation (« C'est bien ça ? ») affichée par-dessus ou sous forme de panneau visible après réponse 200 :
      Affiche le nom du site (`tenant.Name`), et deux boutons d'action : « Oui » et « Non ».
      « Oui » enregistre le site en `TenantStore` et redirige vers `//LoginPage`.
      « Non » réinitialise la confirmation et revient à la saisie sans rien enregistrer.
    - Indicateur de chargement (`ActivityIndicator`) et blocage des boutons (`IsEnabled="{Binding IsNotBusy}"`).
  - `Easymakemoney/ViewModels/Startup/ActivationPageViewModel.cs` :
    - Dérive de `BaseViewModel` (`IsBusy`, `Title`).
    - Propriétés observables : `KeyInput`, `SiteInput`, `IsConfirming`, `CandidateTenantName`, `ErrorMessage`.
    - Commandes `ValidateKeyCommand`, `ValidateSiteCodeCommand`, `ConfirmTenantCommand`, `CancelConfirmationCommand`.
- **Fichier modifié** : `Easymakemoney/AppShell.xaml` et `AppShell.xaml.cs`
  - Ajout de la route Shell `ActivationPage` invisible et sans menu :
    ```xml
    <ShellContent FlyoutItemIsVisible="False" Shell.FlyoutBehavior="Disabled"
        ContentTemplate="{DataTemplate startupPages:ActivationPage}"
        Route="ActivationPage" />
    ```
  - Enregistrement dans `AppShell.xaml.cs` : `Routing.RegisterRoute("ActivationPage", typeof(ActivationPage));`.

### Étape 6 : Démarrage de l'application (`LoadingPageViewModel`)
- **Fichier modifié** : `Easymakemoney/ViewModels/Startup/LoadingPageViewModel.cs`
  - Injecter `ITenantStore` et `ISiteService`.
  - Dans la méthode d'initialisation :
    1. Exécuter l'effacement du premier lancement (`await _tenantStore.EnsureFirstRunCleanAsync()`).
    2. Lire le site enregistré : `var tenant = await _tenantStore.GetTenantAsync();`.
    3. Si aucun site enregistré (`tenant == null`) : navigation immédiate vers `//ActivationPage`.
    4. Si un site est enregistré :
       - Lancer la revérification en tâche de fond non bloquante (`_ = Task.Run(() => RecheckTenantAsync(tenant));`).
       - Poursuivre immédiatement la vérification de session existante (lecture du jeton dans `Preferences`, contrôle d'expiration, redirection vers `//LoginPage` ou `//DashboardPage` sans modification de la logique actuelle).
  - Méthode `RecheckTenantAsync(TenantContext tenant)` :
    - Appelle `_siteService.CheckSiteCodeAsync(tenant.Code)`.
    - Si 200 avec le même code : met à jour `Host` et `Name` dans le `TenantStore` s'ils ont changé.
    - Si 404 : effacement complet (étape 7), alerte visuelle « Ce site n'est plus disponible », puis redirection vers `//ActivationPage`.
    - Si erreur réseau, 429 ou 5xx : aucun changement, le site est conservé.
    - Si 200 avec un autre code : aucun changement, message consigné dans `Debug.WriteLine`.
  - Protection contre les exécutions multiples (guard `_hasExecuted`).

### Étape 7 : Changement de site et durées de vie des composants
- **Fichier modifié** : `Easymakemoney/AppShell.xaml`
  - Dans `Shell.FlyoutFooter`, ajouter le bouton « Changer de site » à côté de « Sign Out » :
    ```xml
    <StackLayout Padding="20" HorizontalOptions="Center" Spacing="10">
        <Button Text="Changer de site" Command="{Binding ChangeSiteCommand}" BackgroundColor="#512BD4" TextColor="White"/>
        <Button Text="Sign Out" Command="{Binding SignOutCommand}"/>
    </StackLayout>
    ```
- **Fichier modifié** : `Easymakemoney/ViewModels/AppShellViewModel.cs`
  - Ajouter la commande `ChangeSiteCommand` :
    1. Demander confirmation par boîte de dialogue :
       `await Shell.Current.DisplayAlert("Changer de site", "Voulez-vous vraiment changer de boutique ? Toutes les données locales et la session actuelle seront effacées.", "Oui", "Non");`
    2. Si confirmé :
       - `SecureStorage.Default.RemoveAll();`
       - `Preferences.Clear();`
       - Restaurer le drapeau de premier lancement : `Preferences.Set("has_run_before", true);`
       - `App.Token = null;`
       - `App.UserDetails = null;`
       - `await Shell.Current.GoToAsync("//ActivationPage");`
    3. `POST /api/logout` n'est pas appelé dans ce lot (reporté au lot 2, en l'absence de refresh token).
    4. `SignOut` reste inchangé et n'efface pas le site.
- **Fichier modifié** : `Easymakemoney/MauiProgram.cs` (durées de vie)
  - Passer en **Transient** :
    - `builder.Services.AddTransient<LoginPageViewModel>();` (au lieu de Singleton)
    - `builder.Services.AddTransient<DashboardPageViewModel>();` (au lieu de Singleton)
    - `builder.Services.AddTransient<LoadingPageViewModel>();` (au lieu de Singleton)
    - `builder.Services.AddTransient<ILoginService, LoginService>();` (au lieu de Singleton)
    - `builder.Services.AddTransient<IListCollectionService, ListCollectionService>();` (au lieu de Singleton)
  - *Conséquences* : les données en mémoire des formulaires et écrans ne survivent plus au changement de site. `LoadingPageViewModel` étant recréé à chaque navigation vers `LoadingPage`, un flag booléen d'instance garantit qu'un même ViewModel n'exécute pas deux fois le démarrage.

### Étape 8 : Identifiant de l'application (`ApplicationId`)
- **Fichier modifié** : `Easymakemoney/Easymakemoney.csproj`
  - Ligne 13 : remplacer `com.companyname.easymakemoney` par `com.arkanoamedia.gestion` (ou valeur confirmée par Claude/le propriétaire).
  - *Conséquences techniques analysées* :
    - **Android** : Le nom de package devient `com.arkanoamedia.gestion`. Android interdisant les tirets dans les composants de package, la suppression du tiret est conforme. L'application ne remplacera pas l'ancienne version sur l'appareil ou l'émulateur mais s'installera à ses côtés comme une nouvelle application distincte (nouvelles données, nouveau Keystore, nouvelles SharedPreferences).
    - **iOS** : Le `CFBundleIdentifier` devient `com.arkanoamedia.gestion`. Sur appareil ou simulateur, iOS la traitera comme une application distincte. Les groupes d'accès Keychain doivent pointer vers `$(AppIdentifierPrefix)com.arkanoamedia.gestion`.
    - **Mac Catalyst** : L'identifiant de bundle change, créant un conteneur d'application sandboxé distinct avec son propre stockage sécurisé et ses propres préférences.

### Étape 9 : Vérification et compilation
- Compilation Android exclusivement avec `-t:Rebuild` et `JavaSdkDirectory`.
- Aucune compilation Apple (en attente de la chaîne Xcode 27.1).
- Décompte exact des avertissements produit par commande shell réelle.
- Rédaction du compte rendu `docs/gemini/lot-1-compte-rendu.md`.

---

## Comment je vérifie

### 1. Compilation Android (commandes exactes)
```bash
dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -c Debug -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home
```
*Résultat attendu* : `0 Error(s)`, génération réussie de l'APK `com.arkanoamedia.gestion-Signed.apk`.

### 2. Décompte réel des avertissements par commande
```bash
grep -E 'warning (CS[0-9]+|XC[0-9]+|MVVMTK[0-9]+|XA[0-9]+)' <build.log> | sed 's/ \[.*//' | sort -u | grep -oE '\b(CS[0-9]+|XC[0-9]+|MVVMTK[0-9]+|XA[0-9]+)\b' | sort | uniq -c | sort -nr
```
*Résultat attendu* : tableau détaillé de chaque code unique extrait sans estimation.

### 3. Contrôle strict des encodages et BOMs
```bash
for f in $(git diff --name-only); do a=$(git show feature/lot-0-plateforme:"$f" | file -b -); b=$(file -b "$f"); [ "$a" != "$b" ] && echo "DIFF $f"; done; echo fin
```
*Résultat attendu* : affiche uniquement `fin`.

### 4. Liste précise des essais fonctionnels (à exécuter ultérieurement sur émulateur par Claude / le propriétaire sur le site `demo`)
1. **Clé de format invalide** : Saisir `IZ-ADM-123` ou une clé avec caractères spéciaux. Vérifier le refus immédiat à l'écran, le message d'erreur en français et l'absence totale de requête sortante.
2. **Clé refusée par le serveur** : Saisir une clé au format valide mais inconnue du backend. Vérifier la réception de 404 `APP_KEY_INVALID`, l'affichage du message d'erreur en français et le non-enregistrement du site.
3. **Code de site inconnu** : Saisir `siteinconnu999`. Vérifier la réponse 404 `TENANT_NOT_FOUND`, l'affichage du message « Boutique introuvable » et l'absence d'enregistrement.
4. **Code demo avec « Non »** : Saisir `DEM042` (ou `demo`). À l'apparition de la carte de confirmation affichant le nom du site (« C'est bien ça ? »), cliquer sur « Non ». Vérifier la disparition de la carte, le retour au formulaire et l'absence de site en `SecureStorage`.
5. **Code demo avec « Oui »** : Saisir `DEM042` (ou `demo`). Cliquer sur « Oui ». Vérifier l'enregistrement de `TenantContext` (`host = "demo.arkanoa-media.com"`, `code = "demo"`) dans `SecureStorage` et la redirection vers `//LoginPage`.
6. **Redémarrage avec site enregistré** : Fermer et relancer l'application. Vérifier que `LoadingPage` lit le site en `SecureStorage`, lance la revérification en arrière-plan sans bloquer l'écran, et navigue directement vers `//LoginPage`.
7. **Changement de site** : Ouvrir le menu latéral, cliquer sur « Changer de site ». Vérifier la demande de confirmation. Si validée, vérifier la purge de `SecureStorage`, de `Preferences` (hors drapeau premier lancement), la mise à null de `App.Token` / `App.UserDetails` et le retour à `//ActivationPage`.
8. **Blocage de requête sans site** : Vérifier par analyse de code et test que toute tentative d'appel HTTP hors activation ou check-mobile lève `TenantMissingException` et ne transmet aucun paquet.
9. **Premier lancement après réinstallation** : Vérifier que l'absence de `has_run_before` dans `Preferences` déclenche un `SecureStorage.RemoveAll()` préventif pour purger les résidus éventuels du trousseau iOS.

---

## Risques et points incertains

1. **`AppShellViewModel` instancié par `new` dans `AppShell.xaml.cs`** :
   Actuellement, `AppShell.xaml.cs` fait `this.BindingContext = new AppShellViewModel();` sans injection de dépendances. Pour que `AppShellViewModel` puisse injecter ou accéder à `ITenantStore`, il faudra soit lui injecter `ITenantStore` via un constructeur DI dans `AppShell`, soit résoudre `ITenantStore` via `IPlatformApplication.Current.Services` / `Handler.MauiContext.Services`.
2. **Double exécution éventuelle lors de la navigation vers `LoadingPage`** :
   En passant `LoadingPageViewModel` en Transient, la navigation vers `//LoadingPage` instanciera un nouveau ViewModel dont le constructeur ou la méthode déclenche le démarrage. Un drapeau de contrôle d'exécution sera mis en place pour garantir l'idempotence.
3. **Format exact de l'`ApplicationId`** :
   `com.arkanoamedia.gestion` est retenu par défaut pour respecter l'interdiction des tirets sur Android. La validation finale de Claude ou du propriétaire confirmera cette chaîne.

---

## Questions pour Claude ou le propriétaire

1. **Résolution de `ITenantStore` dans `AppShellViewModel`** :
   Préfères-tu que `AppShell` reçoive `AppShellViewModel` injecté par son constructeur dans `MauiProgram.cs` (`builder.Services.AddSingleton<AppShell>();` et `MainPage = sp.GetRequiredService<AppShell>();` dans `App.xaml.cs`), ou que `AppShellViewModel` utilise `IPlatformApplication.Current?.Services` pour résoudre `ITenantStore` ?
2. **`ApplicationId`** :
   Confirmes-tu la valeur `com.arkanoamedia.gestion` pour l'`ApplicationId` dans `Easymakemoney.csproj` et les fichiers `Entitlements.plist` ?

