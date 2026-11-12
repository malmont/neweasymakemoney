# Lot 4o : plan de Gemini

## Ce que j'ai lu

- `docs/gemini/lot-4o-consigne.md` : consigne du lot 4o (tests de `LoadingPageViewModel`, `ActivationPageViewModel`, `LoginPageViewModel`, `OtpPageViewModel` et `AppShellViewModel`, au plus 25 tests, dans l'ordre : démarrage, activation, connexion, code à usage unique, déconnexion et changement de site ; référence 1 020 tests sur `main`, Android 12 avertissements).
- `Easymakemoney/ViewModels/Startup/LoadingPageViewModel.cs:35-65` : le constructeur appelle `InitStartup()` en `async void`. Il exécute `EnsureFirstRunCleanAsync()`, lit `_tenantStore.GetTenantAsync()` (si `null` → `//ActivationPage`), lance en arrière-plan `_ = Task.Run(() => RecheckTenantInBackgroundAsync(tenant))`, puis appelle `CheckSessionAndNavigateAsync()`.
- `Easymakemoney/ViewModels/Startup/LoadingPageViewModel.cs:84-94` : en cas de 404 `TENANT_NOT_FOUND` lors de la revérification, appelle `_siteDataCleaner.ClearAllAsync()` puis `MainThread.InvokeOnMainThreadAsync(...)` (`AppDialogs.InfoAsync` et `Shell.Current.GoToAsync("//ActivationPage")`). En erreur réseau/429/5xx, le site est gardé sans message.
- `Easymakemoney/ViewModels/Startup/LoadingPageViewModel.cs:110-188` : `CheckSessionAndNavigateAsync()` vérifie les jetons via `_sessionService` (`GetTokenAsync`, `GetRefreshTokenAsync`) : si les deux sont vides → `//LoginPage` ; si jeton présent et `_jwtService.GetExpirationUtc(token) > DateTime.UtcNow.AddSeconds(60)` → `//DashboardPage` ; sinon avec refresh token → `_tokenRefresher.RefreshAsync(token)` (`IsSuccess` → `//DashboardPage`, `Rejected` → `//LoginPage`, `NetworkError` → `//DashboardPage` en conservant la session). La méthode privée `NavigateAsync(route)` utilise `Shell.Current`.
- `Easymakemoney/ViewModels/Startup/ActivationPageViewModel.cs:46-96` : `ValidateKeyAsync()` et `ValidateSiteCodeAsync()` appellent `_siteService.ActivateWithKeyAsync` / `CheckSiteCodeAsync`, positionnent `CandidateTenant` et `IsConfirming = true` en cas de succès, ou `ErrorMessage = result.ErrorMessage` en cas d'échec.
- `Easymakemoney/ViewModels/Startup/ActivationPageViewModel.cs:99-124` : `ConfirmTenantAsync()` appelle `_siteDataCleaner.ClearAllAsync()`, `_tenantStore.SaveTenantAsync(CandidateTenant)`, `IsConfirming = false`, puis `await Shell.Current.GoToAsync("//LoginPage")` sans garde `?.` (provoque une `NullReferenceException` attrapée par le `catch` si `Shell.Current` est `null`).
- `Easymakemoney/Services/Site/SiteService.cs:25-178` : `ValidateAppKeyFormat` et `CleanSiteInput` s'exécutent localement avant tout appel HTTP (`ActivateWithKeyAsync` exige `^IZ[A-Z]{3}[A-Z0-9]{12}$` ; `CheckSiteCodeAsync` refuse une saisie vide après nettoyage avec `"Saisissez le code du site."`).
- `Easymakemoney/ViewModels/Startup/LoginPageViewModel.cs:35-89` : le constructeur lance `_ = InitializeAsync()` qui préremplit `Email` depuis `_sessionService.GetLastLoginEmailAsync()` si `Email` est encore vide. `LoginAsync()` appelle `_loginUseCase.ExecuteLoginAsync(Email, Password)` : si `OtpRequired` → `_pendingLoginStore.Set(Email, Password)`, vide `Password`, navigue via `Shell.Current` vers `//OtpPage` ; si `IsSuccess` → vide `_pendingLoginStore` et `Password`, navigue vers `//DashboardPage` ; sinon `ErrorMessage = result.ErrorMessage`. `ChangeSiteAsync()` délègue à `_authFlowService.PromptAndChangeSiteAsync()`.
- `Easymakemoney/UseCase/LoginUseCase/LoginUseCase.cs:30-137` :
  - `ExecuteLoginAsync` refuse champs vides sans appeler `ILoginService` (`"Veuillez saisir votre adresse e-mail et votre mot de passe."`), traduit `PLATFORM_NOT_ALLOWED` en `"Ce compte n'a pas accès à l'application de gestion."`, retourne `RequireOtp` si `OtpRequired`, ou valide les jetons via `_jwtService.TryParseAndValidate` (`ROLE_ADMIN` + `tenant_code`) puis enregistre la session et le dernier courriel.
  - `ExecuteVerifyOtpAsync` refuse uniquement `string.IsNullOrWhiteSpace(otp)` (`"Veuillez saisir le code reçu par courriel."`), appelle `_loginService.VerifyOtpAsync`, puis `ProcessSuccessfulTokensAsync`.
- `Easymakemoney/ViewModels/Startup/OtpPageViewModel.cs:33-139` : `InitializeAsync()` lit `_pendingLoginStore.TryGet(out email, out _)` (sinon retour `//LoginPage`). `VerifyOtpAsync()` appelle `_loginUseCase.ExecuteVerifyOtpAsync(email, OtpCode)` (si succès : vide `_pendingLoginStore` et `OtpCode`, navigue vers `//DashboardPage` ; sinon `ErrorMessage`). `ResendOtpAsync()` appelle `_loginUseCase.ExecuteLoginAsync(email, password)` : si `OtpRequired` → vide `OtpCode`, renouvelle `_pendingLoginStore`, appelle `AppDialogs.InfoAsync("Code envoyé", ...)` ; si erreur 429, affiche le message de délai dans `ErrorMessage` (aucun minuteur côté ViewModel).
- `Easymakemoney/Services/Auth/PendingLoginStore.cs:5-47` : conserve en mémoire l'e-mail et le mot de passe pendant 10 minutes (`DateTime.UtcNow - _storedAt.Value < TimeSpan.FromMinutes(10)`).
- `Easymakemoney/ViewModels/AppShellViewModel.cs:11-23` : `ChangeSiteCommand` → `_authFlowService.PromptAndChangeSiteAsync()` ; `SignOutCommand` → `_authFlowService.LogoutAsync()`.
- `Easymakemoney/Services/Auth/AuthFlowService.cs:26-127` :
  - `LogoutAsync()` révoque le refresh token (`POST logout`), appelle `_sessionService.ClearSessionAsync()` (qui garde `LastLoginEmail` et le site), vide `_pendingLoginStore`, puis exécute `MainThread.InvokeOnMainThreadAsync` pour `FlyoutHeader = null` et `Shell.Current.GoToAsync("//LoginPage")`.
  - `PromptAndChangeSiteAsync()` exécute `MainThread.InvokeOnMainThreadAsync` avec `AppDialogs.ConfirmAsync`, révoque le refresh token si confirmé, appelle `_siteDataCleaner.ClearAllAsync()`, vide `_pendingLoginStore`, puis `MainThread.InvokeOnMainThreadAsync` vers `//ActivationPage`.
- `Easymakemoney/Services/Tenant/SiteDataCleaner.cs:19-38` : `ClearAllAsync()` appelle `SecureStorage.Default.Remove(SessionKeys.LastLoginEmailKey)` et `SecureStorage.Default.RemoveAll()` (dans un `try/catch`), puis `Preferences.Default.Clear()` (sans `try/catch`), `new FileResponseCache().Clear()`, `OfflineState.Online()`, `Preferences.Default.Set(TenantStore.FirstRunKey, true)`, `_sessionService.ClearSessionAsync()`, `_tenantStore.ClearMemory()` et `SiteCurrency.Reset()`.
- `Easymakemoney/Services/Offline/OfflineCacheHandler.cs:17-66` et `HomeSnapshotStore.cs` : les copies hors connexion des statistiques et de la trésorerie sont stockées via `IResponseCache` (`FileResponseCache` sur disque), et le snapshot d'accueil via `Preferences.Default`.
- Tests existants lus : `Easymakemoney.Tests/Models/MobileRolesTests.cs`, `Easymakemoney.Tests/Services/ActivationTests.cs`, `Easymakemoney.Tests/Services/AuthHandlerTests.cs`, `Easymakemoney.Tests/Services/TenantHandlerTests.cs`, `Easymakemoney.Tests/Services/OfflineCacheTests.cs`, et les faux `FakeSessionService`, `FakeTenantStore`, `FakeTokenRefresher`.

## Prérequis à installer ou à vérifier (commandes exactes, rien n'est lancé)

Aucun outil ni paquet à installer.
Commandes de vérification prévues après validation et codage (sur `feature/lot-4o-tests` depuis `main`) :
- `dotnet test Easymakemoney.Tests` (référence de départ sur `main` : 1 020 passants).
- `dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home` (référence attendue : 12 avertissements, 0 erreur).

## Étapes

### 1. Faux de services dans `Easymakemoney.Tests/Fakes/` (uniquement dans le projet de tests)

1. **`FakeLoginService.cs`** (nouveau) : implémente `ILoginService` avec traçabilité de `AuthenticateCalls` et `VerifyOtpCalls`, résultats configurables (`AuthenticateResult`, `VerifyOtpResult`) ou utilisation conjointe de `LoginService` + `TestHttpMessageHandler`.
2. **`FakeSiteService.cs`** / utilisation du vrai `SiteService` avec `TestHttpMessageHandler` + `FakeDeviceInfoService` : permet de tester la vraie validation locale et le parsing des erreurs JSON sans réseau.
3. **`FakeSiteDataCleaner.cs`** (nouveau) : implémente `ISiteDataCleaner` pour les tests de `ActivationPageViewModel` / `LoadingPageViewModel` (compteur d'appels `ClearCalls`), en complément du test direct de `SiteDataCleaner` réel.
4. **`FakeAuthFlowService.cs`** (nouveau) : implémente `IAuthFlowService` si nécessaire pour les tests isolés de `LoginPageViewModel.ChangeSiteCommand`.
5. **`FakePendingLoginStore.cs`** (nouveau) : implémente `IPendingLoginStore` pour simuler simplement une expiration sans attente réelle si `PendingLoginStore` garde `DateTime.UtcNow` interne.
6. Compléter au besoin `FakeTenantStore.cs` (compteur d'appels `EnsureFirstRunCleanCalls`, `SaveCalls`) et `FakeSessionService.cs` (compteur `ClearSessionCalls`) sans casser les tests existants.

### 2. Tests unitaires xUnit dans `Easymakemoney.Tests/ViewModels/` (25 tests)

#### A. Démarrage (`LoadingPageViewModelTests.cs` — 6 tests, sous réserve des points d'entrée demandés en questions)

1. **`Startup_NoTenant_NavigatesToActivationPage_WithoutRefreshOrClear`** : `TenantStore` vide → `EnsureFirstRunCleanAsync` appelé, navigation vers `"//ActivationPage"`, aucun appel à `RefreshAsync` ni `ClearAllAsync`.
2. **`Startup_TenantWithoutSession_NavigatesToLoginPage_AndKeepsTenant`** : site configuré, pas de token ni de refresh token → navigation vers `"//LoginPage"`, site conservé intact dans `TenantStore`.
3. **`Startup_ValidToken_NavigatesToDashboardPage_WithoutRefresh`** : JWT valide (expiration > `UtcNow + 60s`) → navigation vers `"//DashboardPage"`, `FakeTokenRefresher.InvocationCount == 0`.
4. **`Startup_ExpiredToken_RefreshSucceeded_NavigatesToDashboardPage`** : JWT expiré mais refresh token présent, `FakeTokenRefresher` retourne `Success` → `InvocationCount == 1`, navigation vers `"//DashboardPage"`.
5. **`Startup_ExpiredToken_RefreshRejected_NavigatesToLoginPage`** : JWT expiré, `FakeTokenRefresher` retourne `Rejected` (401) → navigation vers `"//LoginPage"`.
6. **`Startup_ExpiredToken_NetworkError_KeepsSessionAndNavigatesToDashboardPage`** : JWT expiré, `FakeTokenRefresher` retourne `NetworkError` (pas de réseau) → navigation vers `"//DashboardPage"`, session et site jamais effacés par erreur.

#### B. Activation d'un site (`ActivationPageViewModelTests.cs` — 5 tests)

7. **`ValidateSiteCode_EmptyOrWhitespace_ShowsError_WithoutHttpCall`** : `SiteCodeInput = "   "` → aucune requête HTTP envoyée, `ErrorMessage == "Saisissez le code du site."`, `IsConfirming == false`.
8. **`ValidateKey_Malformed_ShowsError_WithoutHttpCall`** : `KeyInput = "CLE-INVALIDE"` → aucune requête HTTP envoyée, `ErrorMessage` mentionne le format attendu, `IsConfirming == false`.
9. **`ValidateSiteCode_Server404NotFound_ShowsErrorMessage`** : serveur retourne 404 `TENANT_NOT_FOUND` → `ErrorMessage == "Site introuvable."`, `HasErrorMessage == true`, `IsConfirming == false`.
10. **`ValidateKey_Accepted_SetsCandidateAndConfirmationMode`** : serveur retourne 200 avec `tenant` (`code: "DEM042"`, `host: "demo.example"`, `name: "Démo"`) → `IsConfirming == true`, `CandidateTenant` renseigné, site pas encore enregistré dans `TenantStore`.
11. **`ConfirmTenant_ClearsPreviousData_SavesTenant_AndNavigatesToLogin`** : après validation, `ConfirmTenantCommand` appelle `ClearAllAsync()`, enregistre `CandidateTenant` dans `TenantStore`, remet `IsConfirming = false`, et navigue vers `"//LoginPage"` (via `NavigateAsync` demandé à Claude).

#### C. Connexion (`LoginPageViewModelTests.cs` — 5 tests)

12. **`Login_EmptyFields_ShowsError_WithoutNetworkCall`** : `Email = ""` ou `Password = ""` → aucun appel HTTP/service, `ErrorMessage == "Veuillez saisir votre adresse e-mail et votre mot de passe."`, `HasErrorMessage == true`.
13. **`Login_InvalidCredentials_ShowsServerError_AndNoSessionSaved`** : réponse 401 avec message serveur → `ErrorMessage` contient le texte retourné, `FakeSessionService.Token == null`.
14. **`Login_NonAdminAccount_RefusedWithManagementMessage`** : test des deux cas de refus de rôle : réponse 403 `PLATFORM_NOT_ALLOWED` et réponse 200 avec JWT `ROLE_SUPPORT` / `ROLE_USER` (sans `ROLE_ADMIN`) → `ErrorMessage == "Ce compte n'a pas accès à l'application de gestion."`, aucune session enregistrée.
15. **`Login_OtpRequired_StoresPendingLogin_ClearsPassword_AndNavigatesToOtp`** : réponse `{ otpRequired: true }` → `_pendingLoginStore.TryGet` contient `(email, password)`, `Password == string.Empty`, navigation vers `"//OtpPage"` (via `NavigateAsync`).
16. **`Initialize_PreFillsLastLoginEmail_WithoutOverwritingExistingInput`** : `LastLoginEmail = "admin@example.test"` préremplit `Email` au démarrage ; si `Email` est déjà saisi, il est conservé.

#### D. Code à usage unique (`OtpPageViewModelTests.cs` — 5 tests)

17. **`VerifyOtp_BlankCode_ShowsError_WithoutNetworkCall`** : `OtpCode = "  "` avec identifiants en attente → aucun appel réseau, `ErrorMessage == "Veuillez saisir le code reçu par courriel."`.
18. **`VerifyOtp_IncompleteOrWrongCode_ShowsError_AndPreservesPendingLogin`** : `OtpCode = "12"` (incomplet) ou code faux refusé par le serveur (401/422) → `ErrorMessage` affiche le refus serveur, session non créée, identifiants conservés dans `_pendingLoginStore`.
19. **`VerifyOtp_ExpiredPendingStore_NavigatesBackToLogin_WithoutServiceCall`** : `_pendingLoginStore` expiré (> 10 minutes) ou vide → aucun appel `VerifyOtpAsync`, navigation vers `"//LoginPage"`.
20. **`ResendOtp_SuccessAndRateLimited429_HandlesDelayWithoutWaiting`** :
    - succès (`OtpRequired == true`) : appelle `ExecuteLoginAsync`, vide `OtpCode`, renouvelle `_pendingLoginStore`, notifie `"Code envoyé"` (via délégué demandé) ;
    - 429 `TOO_MANY_ATTEMPTS` (`Retry-After: 45`) : affiche `"Trop de tentatives. Veuillez patienter 45 secondes."` dans `ErrorMessage` sans aucune attente réelle.
21. **`VerifyOtp_ValidCode_SavesSessionAndLastEmail_ClearsPending_NavigatesToDashboard`** : réponse 200 avec JWT `ROLE_ADMIN` du site configuré + `refresh_token` → jetons, utilisateur et `LastLoginEmail` enregistrés, `_pendingLoginStore` et `OtpCode` vidés, navigation vers `"//DashboardPage"`.

#### E. Déconnexion et changement de site (`AppShellAndAuthFlowTests.cs` — 4 tests, sous réserve des points d'entrée sur `AuthFlowService` et `SiteDataCleaner`)

22. **`SignOut_RevokesToken_ClearsSession_KeepsTenantLastEmailAndOfflineCache`** : `AppShellViewModel.SignOutCommand` → `LogoutAsync()` : révoque `refresh_token` (`POST logout`), efface jetons et utilisateur, vide `_pendingLoginStore`, **conserve** `TenantStore.CurrentTenant`, `LastLoginEmail`, et les copies hors connexion (`IResponseCache`), navigue vers `"//LoginPage"`.
23. **`ChangeSite_Declined_DoesNotRevokeOrClearAnything`** : `AppShellViewModel.ChangeSiteCommand` avec confirmation refusée → aucun appel `POST logout`, aucune donnée effacée (session, site, préférences et cache intacts).
24. **`ChangeSite_Confirmed_RevokesToken_ClearsSessionTenantPreferencesAndOfflineCopies`** : `AppShellViewModel.ChangeSiteCommand` confirmé → `POST logout`, `SiteDataCleaner.ClearAllAsync()` vide la session (y compris `LastLoginEmail`), `TenantStore`, `PendingLoginStore`, les préférences (y compris copies `HomeSnapshotStore`), les copies hors connexion de `IResponseCache` (statistiques / trésorerie), remet `OfflineState.Online()` et `SiteCurrency.Reset()`, puis navigue vers `"//ActivationPage"`.
25. **`SiteDataCleaner_ClearAll_DeletesOfflineCacheDirectoryAndResetsOfflineState`** : vérifie qu'après écriture de copies hors connexion (statistiques / trésorerie), l'exécution réelle de `SiteDataCleaner.ClearAllAsync()` supprime les copies dans `IResponseCache` / dossier dédié et remet `OfflineState.ServedAt` à `null`.

## Comment je vérifie

1. `dotnet test Easymakemoney.Tests` : les 25 nouveaux tests verts, portant le total à **1 045 tests passants** (1 020 de référence + 25), 0 échec, 0 ignoré.
2. `dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home` : exactement **12 avertissements**, 0 erreur.
3. `git diff --check main` vierge, et vérification qu'aucun fichier sous `Easymakemoney/` ni aucune vraie adresse/clé n'apparaît.

## Risques et points incertains

1. **Appels statiques MAUI sous `net10.0` (`NotImplementedInReferenceAssemblyException`)** : `MainThread.InvokeOnMainThreadAsync`, `Preferences.Default`, `SecureStorage.Default` et `FileSystem.Current.AppDataDirectory` ne sont pas implémentés dans la cible `net10.0` utilisée par `Easymakemoney.Tests`. Sans point d'entrée dans l'application, `SiteDataCleaner.ClearAllAsync()` (`Preferences.Default.Clear()` ligne 30) et `AuthFlowService` (`MainThread` lignes 47/65/94) lèvent cette exception dans les tests.
2. **`Shell.Current` nul dans les tests** : `ActivationPageViewModel.ConfirmTenantAsync()` ligne 111 appelle `await Shell.Current.GoToAsync("//LoginPage")` sans `?.`, ce qui provoque une `NullReferenceException` attrapée par le `catch` (affichant une erreur générique au lieu de réussir).
3. **`LoadingPageViewModel`** lance `InitStartup()` en `async void` dans le constructeur, sans possibilité actuelle d'injecter un délégué de navigation avant démarrage ni d'`await` la fin du démarrage dans un test sans `Task.Delay`.

## Questions pour Claude ou le propriétaire

Pour tester tous les cas de ce lot sans modifier moi-même les fichiers de l'application ni introduire d'attente réelle, voici les points d'entrée publics souhaités dans l'application :

1. **`LoadingPageViewModel.cs`** :
   - Remplacer `private static Task NavigateAsync(string route)` par `public Func<string, Task> NavigateAsync { get; set; } = route => Shell.Current?.GoToAsync(route) ?? Task.CompletedTask;` (comme dans `DashboardPageViewModel`).
   - Rendre le démarrage testable sans course ni attente réelle : par exemple un paramètre optionnel `bool autoStart = true` dans le constructeur (ou garder le constructeur DI intact et exposer `public Task RunStartupAsync()` / `StartupTask` avec possibilité d'affecter `NavigateAsync` avant son lancement).
2. **`ActivationPageViewModel.cs`, `LoginPageViewModel.cs`, `OtpPageViewModel.cs`** :
   - Ajouter `public Func<string, Task> NavigateAsync { get; set; } = route => Shell.Current?.GoToAsync(route) ?? Task.CompletedTask;` (et l'utiliser notamment à `ActivationPageViewModel.cs:111` où `Shell.Current.GoToAsync` sans `?.` crashe si `Shell.Current == null`).
   - Dans `OtpPageViewModel.cs:109`, exposer un délégué remplaçable pour `AppDialogs.InfoAsync("Code envoyé", ...)` (ex: `ShowInfoAsync`) afin de vérifier le retour visuel du renvoi d'OTP.
   - Dans `PendingLoginStore.cs` (ou via un faux `IPendingLoginStore` côté tests) : confirmes-tu que l'utilisation d'un `Func<DateTime>? utcNow = null` dans `PendingLoginStore` (ou d'un faux `IPendingLoginStore` pour `OtpPageViewModel`) te convient pour tester l'expiration des 10 minutes sans attente ?
3. **`AuthFlowService.cs` et `SiteDataCleaner.cs` (changement de site et cache hors connexion)** :
   - Dans `AuthFlowService.cs` (lignes 47, 65, 94), remplacer `MainThread.InvokeOnMainThreadAsync`, `AppDialogs.ConfirmAsync` et `Shell.Current.GoToAsync` par des propriétés publiques remplaçables (`RunOnMainThreadAsync`, `ConfirmAsync`, `NavigateAsync`).
   - Dans `SiteDataCleaner.cs` :
     - Injecter `IResponseCache? responseCache = null` (déjà enregistré en singleton dans `MauiProgram.cs:54`) à la place de `new FileResponseCache().Clear()` (ligne 31) pour pouvoir prouver en test que les copies hors connexion des statistiques et de la trésorerie sont bien effacées ;
     - Protéger ou rendre remplaçables `Preferences.Default` (lignes 30 et 33) et `SecureStorage.Default` (lignes 23-24) (par ex. via un délégué/action ou paramètre optionnel) afin que `ClearAllAsync()` ne lève pas `NotImplementedInReferenceAssemblyException` sous `net10.0` et permette de vérifier l'effacement des préférences (`HomeSnapshotStore`, `FirstRunKey`) et de `LastLoginEmail` au changement de site (tout en gardant `LastLoginEmail` intact lors de `LogoutAsync`).

