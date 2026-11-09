# Lot 2 : plan de Gemini (connexion administrateur, code OTP et session)

## Ce que j'ai lu
Fichiers et constats, avec chemin:ligne :

1. `Easymakemoney/UseCase/LoginUseCase/LoginUseCase.cs:17-40` :
   - Envoie `LoginRequest` sans champ `platform` (par défaut le serveur attend `web`).
   - Lit uniquement `response.Token`.
   - Décode le jeton avec `_jwtService.ParseToken(response.Token)` sans validation de rôle ni de `tenant_code`.
   - Sauvegarde les détails de session via `_preferenceService.SaveUserDetails(...)`.
   - Effectue directement des actions UI et de navigation : `AppShell.Current.FlyoutHeader = new FlyoutHeaderControl();` et `await Shell.Current.GoToAsync("//DashboardPage");`. Rend un simple booléen `true`/`false`.
2. `Easymakemoney/Services/JwtService.cs:6-16` :
   - Lit uniquement le premier claim `roles` via `jsonToken.Claims.First(claim => claim.Type == "roles").Value;`. Un administrateur ayant plusieurs rôles peut être mal reconnu.
   - N'extrait ni `firstName`, ni `lastName`, ni `email`, ni `tenant_code`.
   - Ne vérifie pas la présence obligatoire de `ROLE_ADMIN` ni la concordance de `tenant_code` avec le site actif.
3. `Easymakemoney/Services/PreferenceService.cs:7-25` et `Easymakemoney/Services/IPreferenceService.cs:3-10` :
   - Chiffre le jeton avec `EncryptionHelper.Encrypt` et sauvegarde dans `Preferences` sous `userToken`, `userId`, `userEmail`, `userRole` et `UserDetails`.
   - Écrit également dans les champs statiques globaux `App.Token` et `App.UserDetails`.
4. `Easymakemoney/Services/EncryptionHelper.cs:5-63` :
   - Chiffrement AES avec une clé écrite dans le code. Sécurité illusoire.
5. `Easymakemoney/App.xaml.cs:11-12` :
   - Champs statiques globaux `public static UserBasicInfo? UserDetails;` et `public static string? Token;`.
6. `Easymakemoney/Controls/FlyoutHeaderControl.xaml.cs:8-12` :
   - Lit `App.UserDetails.Role` et `App.UserDetails.Email` au chargement.
7. `Easymakemoney/UseCase/ListCollectionUseCase/SaveCollectionUseCase.cs:20` :
   - Lit l'identifiant utilisateur via `_preferenceService.GetUserId()`.
8. `Easymakemoney/Services/HttpService.cs:19-25, 42-48, 79-85, 124-130` :
   - Vérifie manuellement la présence du jeton via `_preferenceService.GetUserToken()` et lève `InvalidOperationException("Token is not available.")`.
   - Écrit le jeton dans `_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);`.
   - Lignes 63, 109, 166 : logue l'intégralité du corps des réponses HTTP avec `Debug.WriteLine($"Response Content: {responseJson}");`, ce qui expose les jetons dans le journal lors du login.
9. `Easymakemoney/ViewModels/Startup/LoadingPageViewModel.cs:97-127` :
   - Lit `Preferences.Get(nameof(App.UserDetails), "")` et `Preferences.Get("userToken", string.Empty)`.
   - Déchiffre avec `EncryptionHelper.Decrypt`.
   - Compare `jsonToken.ValidTo < DateTime.Now` (comparaison incorrecte entre une date UTC et l'heure locale de l'appareil).
   - En cas d'expiration, affiche une boîte d'alerte en anglais (`DisplayAlert("User session expired", ...)`) sans tenter de rafraîchir la session avec le refresh token.
10. `Easymakemoney/ViewModels/AppShellViewModel.cs:21-28` :
    - `SignOut` ne supprime que `Preferences.Remove(nameof(App.UserDetails));` sans supprimer le jeton ni appeler `POST /api/logout`.
11. `Easymakemoney/Views/Startup/LoginPage.xaml:16-43` :
    - Textes en anglais (« Welcome! », « Login to your account », « User Name », « Password », « Sign In », « Don't have account? », « Sign Up here » sans action).
    - `LoginPageViewModel.cs:28` affiche une alerte brute en anglais : `DisplayAlert("Invalid User", "Invalid User", "OK")`.
12. `Easymakemoney/Services/Tenant/SiteDataCleaner.cs:26-27` :
    - Réinitialise `App.Token = null;` et `App.UserDetails = null;`.
13. `Easymakemoney/MauiProgram.cs:54-57` :
    - Enregistre `ILoginService`, `IJwtService`, `IPreferenceService`.

---

## Prérequis à installer ou à vérifier (commandes exactes, rien n'est lancé)

Aucun paquet à installer. Le paquet `System.IdentityModel.Tokens.Jwt` est déjà présent dans le projet (`Easymakemoney.csproj`).

Commandes de vérification d'état (informatif, non lancé avant feu vert) :
- `git status` pour vérifier la propreté de la branche `feature/lot-2-session`.

---

## Étapes

### Étape 1 : Modèles de session et clés de stockage
1. **Création de `Easymakemoney/Models/Session/UserInfo.cs`** :
   - Propriétés : `int Id`, `string Email`, `string FirstName`, `string LastName`, `List<string> Roles`, `string TenantCode`, `string Username`.
2. **Création de `Easymakemoney/Services/Session/SessionKeys.cs`** :
   - Classe statique centralisant les clés `SecureStorage` : `SessionTokenKey = "session_token"`, `SessionRefreshTokenKey = "session_refresh_token"`, `SessionUserKey = "session_user"`.
3. **Création de `Easymakemoney/Services/Auth/AuthenticationMissingException.cs`** :
   - Exception dédiée levée par `AuthHandler` lorsqu'une requête authentifiée est émise sans jeton en session.
4. **Mise à jour des modèles de requêtes/réponses d'authentification** :
   - `Easymakemoney/Models/LoginRequest.cs` : ajout de `public string platform { get; set; } = "admin";`.
   - `Easymakemoney/Models/LoginResponse.cs` : ajout des propriétés `RefreshToken` (`refresh_token`), `OtpRequired` (`otp_required`), `Message` (`message`).
   - `Easymakemoney/Models/Login/OtpVerifyRequest.cs` : `{ username, otp, platform = "admin" }`.
   - `Easymakemoney/Models/Login/RefreshTokenRequest.cs` : `{ refresh_token, platform = "mobile" }`.
   - `Easymakemoney/Models/Login/LogoutRequest.cs` : `{ refresh_token }`.
   - `Easymakemoney/Models/Login/LoginResult.cs` : résultat riche typé rendu par `LoginUseCase` (`IsSuccess`, `OtpRequired`, `Email`, `ErrorMessage`, `ApiError`).

### Étape 2 : Service de session sécurisé (`ISessionService` & `SessionService`)
1. **Création de `Easymakemoney/Services/Session/ISessionService.cs`** :
   - Interface enregistrée en singleton, **sans aucune dépendance HTTP**.
   - Méthodes :
     - `Task SaveSessionAsync(string token, string refreshToken, UserInfo user);`
     - `Task<string?> GetTokenAsync();`
     - `Task<string?> GetRefreshTokenAsync();`
     - `Task UpdateTokenAsync(string newToken);` (remplace le seul JWT après rafraîchissement)
     - `Task<UserInfo?> GetUserAsync();` (avec propriété cache mémoire `UserInfo? CurrentUser { get; }`)
     - `Task ClearSessionAsync();` (efface la session sans toucher au site : ni `TenantContext`, ni drapeau de premier lancement).
2. **Création de `Easymakemoney/Services/Session/SessionService.cs`** :
   - Implémentation utilisant `SecureStorage.Default` et les clés de `SessionKeys`.
   - Maintient un cache mémoire pour `_token`, `_refreshToken` et `_currentUser` avec synchronisation thread-safe.
   - Capture et journalise toute exception de `SecureStorage`.
   - `ClearSessionAsync()` supprime les clés de session dans `SecureStorage` et vide le cache mémoire.

### Étape 3 : Décodage, validation et fonctions pures JWT (`IJwtService` & `JwtService`)
1. **Mise à jour de `Easymakemoney/Services/IJwtService.cs` et `JwtService.cs`** :
   - Fonction pure `bool TryParseAndValidate(string token, string expectedTenantCode, out UserInfo? user, out string? errorMessage)` :
     1. Décodage du JWT avec `JwtSecurityTokenHandler`.
     2. Extraction de l'ensemble des claims `roles` (lecture de tous les claims de type `roles` ou du tableau JSON dans le payload).
     3. Refus si `ROLE_ADMIN` n'est pas présent : message exact `« Ce compte n'a pas accès à l'application de gestion. »`.
     4. Extraction du claim `tenant_code`.
     5. Refus si `tenant_code` est différent de `expectedTenantCode` (comparaison insensible à la casse `OrdinalIgnoreCase`) : message exact `« Ce compte n'appartient pas à ce site. »`.
     6. Extraction de `id`, `email`, `firstName`, `lastName`, `username`.
     7. Construction de l'instance `UserInfo`.
   - Fonction pure `DateTime? GetExpirationUtc(string token)` : extraction de la date d'expiration en UTC (`token.ValidTo`).
   - Fonctions 100% pures et déterministes, prêtes pour les tests unitaires du lot 4.

### Étape 4 : Gestionnaire HTTP d'authentification (`AuthHandler`)
1. **Création de `Easymakemoney/Services/Auth/AuthHandlerOptions.cs`** :
   - Déclaration de `HttpRequestOptionsKey<bool> SkipAuthKey = new("SkipAuth")`.
   - Méthodes d'extension sur `HttpRequestMessage` : `SetSkipAuth(bool skip)` et `GetSkipAuth()`.
2. **Création de `Easymakemoney/Services/Auth/AuthHandler.cs`** (hérite de `DelegatingHandler`) :
   - Placé dans la chaîne HTTP **après** `TenantHandler` (l'en-tête du site `X-Tenant-Host` est posé d'abord, le jeton Bearer ensuite).
   - Si `request.GetSkipAuth() == true` : laisse passer la requête sans ajouter d'en-tête `Authorization` et sans intercepter les erreurs 401 (les 401 de `login` ou `otp-verify` remontent directement au service appelant).
   - Si `request.GetSkipAuth() == false` (requête authentifiée) :
     1. Lecture du jeton via `await _sessionService.GetTokenAsync()`.
     2. Si nul ou vide : lever immédiatement `AuthenticationMissingException` avant tout envoi réseau.
     3. Injecter `request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);`.
     4. Envoyer `var response = await base.SendAsync(request, cancellationToken);`.
     5. Sur retour HTTP 401 :
        - Synchronisation via `SemaphoreSlim(1, 1)` pour garantir **un seul rafraîchissement à la fois**.
        - Si une autre requête a déjà rafraîchi le jeton pendant l'attente du sémaphore, cloner la requête, mettre à jour le Bearer et rejouer directement.
        - Sinon, effectuer l'appel `POST api/token/refresh {refresh_token, platform: "mobile"}` (marqué avec `SkipAuth = true`).
        - **Si succès 200** : mettre à jour le jeton en session (`_sessionService.UpdateTokenAsync(newToken)`), cloner la requête initiale via fonction utilitaire `CloneHttpRequestMessageAsync` (méthode, URL, en-têtes, options, corps), y poser le nouveau Bearer et la **rejouer une seule fois**.
        - **Si échec 400 ou 401 (refus du serveur)** : effacer la session (`_sessionService.ClearSessionAsync()`), **garder le site**, afficher sur le fil principal l'alerte « Votre session a expiré, reconnectez-vous. » via `DisplayAlertAsync`, et naviguer vers `//LoginPage`.
        - **Si serveur injoignable (réseau / timeout)** : **garder la session**, laisser échouer la requête d'origine normalement.

### Étape 5 : Nettoyage de `HttpService`
1. **Mise à jour de `Easymakemoney/Services/HttpService.cs`** :
   - Retirer le constructeur avec `IPreferenceService`.
   - Supprimer toutes les écritures directes dans `_httpClient.DefaultRequestHeaders.Authorization`.
   - Supprimer les vérifications de jeton dupliquées (déléguées au gestionnaire `AuthHandler`).
   - Supprimer les 3 `Debug.WriteLine($"Response Content: {responseJson}");` des méthodes `PostAsyncWithAuth`, `PostAsyncWithoutAuth`, `PostMultipartAsyncWithAuth`.
   - Dans `PostAsyncWithoutAuth`, poser systématiquement `request.SetSkipAuth(true);`.

### Étape 6 : Service d'authentification et flux de session (`IAuthFlowService` & `AuthFlowService`)
1. **Création de `Easymakemoney/Services/Auth/IAuthFlowService.cs` et `AuthFlowService.cs`** :
   - Regroupe en un seul service clairement nommé les parcours de connexion, déconnexion, rafraîchissement et changement de site :
     - `Task<LoginResult> LoginAsync(string email, string password);`
     - `Task<LoginResult> VerifyOtpAsync(string email, string otp);`
     - `Task LogoutAsync();` :
       1. Lit `refresh_token` dans `_sessionService`.
       2. Si présent, envoie `POST api/logout {refresh_token}` via requête avec `SkipAuth = true`, en ignorant toute erreur/exception réseau.
       3. Efface la session via `_sessionService.ClearSessionAsync()` (**sans toucher au site**).
       4. Réinitialise l'en-tête Shell (`AppShell.Current.FlyoutHeader = null;`).
       5. Navigue vers `//LoginPage` sur le fil principal.
     - `Task<bool> PromptAndChangeSiteAsync();` :
       1. Demande confirmation via `DisplayAlertAsync` (« Changer de site ? Vous serez déconnecté et les données de ce site seront effacées de l'appareil. »).
       2. Si validé, envoie `POST api/logout {refresh_token}` (en ignorant les erreurs) tant que le refresh token existe encore.
       3. Appelle `_siteDataCleaner.ClearAllAsync()` (qui efface `SecureStorage`, `Preferences`, remet `has_run_before = true`, efface la mémoire de `TenantStore` et de `SessionService`).
       4. Réinitialise le header Shell et navigue vers `//ActivationPage`.
       5. Retourne `true` (ou `false` si annulé).
2. **Mise à jour de `ISiteDataCleaner` et `SiteDataCleaner.cs`** :
   - `ClearAllAsync()` intègre l'appel à `_sessionService.ClearSessionAsync()`.
   - Suppression du code UI (`DisplayAlertAsync` et navigation) de `SiteDataCleaner`, désormais pris en charge par `AuthFlowService`.

### Étape 7 : Refonte de `LoginService` et `LoginUseCase`
1. **Mise à jour de `Easymakemoney/Services/ILoginService.cs` et `LoginService.cs`** :
   - Envoi de `POST api/login` avec `{username, password, platform: "admin"}` et `SkipAuth = true`.
   - Envoi de `POST api/otp-verify` avec `{username, otp, platform: "admin"}` et `SkipAuth = true`.
   - Envoi de `POST api/token/refresh` avec `{refresh_token, platform: "mobile"}` et `SkipAuth = true`.
   - Envoi de `POST api/logout` avec `{refresh_token}` et `SkipAuth = true`.
   - Lecture des erreurs via `ApiError.FromHttpResponse`.
2. **Mise à jour de `Easymakemoney/UseCase/LoginUseCase/LoginUseCase.cs`** :
   - Retrait de toute navigation (`GoToAsync`) et de toute modification d'interface (`FlyoutHeader`).
   - Injection de `ILoginService`, `IJwtService`, `ITenantStore`, `ISessionService`.
   - Méthodes `ExecuteLoginAsync` et `ExecuteVerifyOtpAsync`.
   - En cas de tokens reçus :
     1. Récupère le code du site actif via `_tenantStore.GetTenantAsync()`.
     2. Valide le JWT via `_jwtService.TryParseAndValidate`.
     3. Si refus (rôle admin absent ou `tenant_code` différent) : renvoie `LoginResult.Failure(errorMessage)` sans rien enregistrer.
     4. Si validation réussie : enregistre la session via `_sessionService.SaveSessionAsync` et renvoie `LoginResult.Success()`.
   - En cas d'`otp_required == true` : renvoie `LoginResult.RequireOtp(email)`.

### Étape 8 : Écran de connexion (`LoginPage`) et nouvel écran OTP (`OtpPage`)
1. **Mise à jour de `Views/Startup/LoginPage.xaml` et `LoginPageViewModel.cs`** :
   - Traduction intégrale en français : « Bienvenue », « Connexion à votre compte », « Adresse e-mail », « Mot de passe », « Se connecter ».
   - Suppression du bloc « Don't have account? Sign Up here ».
   - Conservation du bouton transparent « Changer de site » sous « Se connecter ».
   - Ajout d'un bandeau d'erreur élégant (`Border` moderne, conforme K6) lié à `ErrorMessage`.
   - `LoginPageViewModel` appelle `LoginUseCase` :
     - Si `OtpRequired == true` : transmet temporairement les identifiants en mémoire au ViewModel OTP (ou via service de transition mémoire) et navigue vers `//OtpPage`.
     - Si succès direct : met à jour l'en-tête `FlyoutHeaderControl` et navigue vers `//DashboardPage`.
     - Si erreur : affiche l'erreur en français sur la page via `ApiError`.
   - La commande `ChangeSiteCommand` appelle `_authFlowService.PromptAndChangeSiteAsync()`.
2. **Création de l'écran OTP (`Views/Startup/OtpPage.xaml`, `OtpPage.xaml.cs`, `ViewModels/Startup/OtpPageViewModel.cs`)** :
   - **Type de navigation choisi** : Route Shell absolue déclarée dans `AppShell.xaml` via :
     `<ShellContent Route="OtpPage" ContentTemplate="{DataTemplate startupPages:OtpPage}" FlyoutItemIsVisible="False" />`.
     *Pourquoi ce choix* : Comme pour `ActivationPage`, une route Shell absolue isole nettement l'étape d'authentification sans laisser de pile de retour historique une fois arrivé sur le Dashboard. L'utilisateur ne peut pas faire « Retour » depuis le Dashboard pour retomber sur l'écran OTP.
   - Interface (composée exclusivement de `Border` et contrôles modernes MAUI, 0 API obsolète) :
     - Rappel de l'adresse courriel ayant reçu le code.
     - Champ de saisie numérique (`Keyboard="Numeric"`), sans limite artificielle de longueur.
     - Bouton « Valider ».
     - Bouton « Renvoyer le code » (relance la demande de login avec les identifiants conservés en mémoire uniquement).
     - Bouton « Retour » (retourne vers `//LoginPage` et purge les identifiants en mémoire).
     - Bandeau d'erreur pour les messages `ApiError` (`OTP_INVALID`, `RATE_LIMITED`, etc.).
   - Sécurité mémoire : les identifiants ne sont gardés qu'en mémoire vive dans le ViewModel/service de transition durant la session d'OTP, et sont immédiatement écrasés/remis à null à la validation ou à l'annulation.

### Étape 9 : Séquence de démarrage dans `LoadingPageViewModel`
1. **Mise à jour de `Easymakemoney/ViewModels/Startup/LoadingPageViewModel.cs`** :
   - Suppression complète de `EncryptionHelper.Decrypt` et des lectures directes de `Preferences`.
   - Injection de `ISessionService` et `IAuthFlowService`.
   - Après les vérifications de site du lot 1 (premier lancement, lecture du site, revérification arrière-plan) :
     1. Lecture de la session via `_sessionService.GetTokenAsync()` et `_sessionService.GetRefreshTokenAsync()`.
     2. **Pas de session** : navigation vers `//LoginPage`.
     3. **Session avec jeton valide** :
        - Comparaison UTC : `_jwtService.GetExpirationUtc(token) > DateTime.UtcNow.AddSeconds(60)`.
        - Si valide : instanciation de `FlyoutHeaderControl` avec le courriel et `ROLE_ADMIN`, puis navigation vers `//DashboardPage`.
     4. **Jeton expiré mais refresh token présent** :
        - Tentative de rafraîchissement via `_authFlowService.RefreshTokenAsync()` :
          - Succès : mise à jour de `FlyoutHeaderControl`, navigation vers `//DashboardPage`.
          - Refus serveur (400 ou 401) : purge session, navigation vers `//LoginPage`.
          - Serveur injoignable : navigation vers `//DashboardPage` quand même (session conservée, erreurs réseau habituelles affichées sur les listes).

### Étape 10 : Migration finale et suppression des classes obsolètes
1. **Suppression des classes obsolètes** :
   - `Easymakemoney/Services/EncryptionHelper.cs`
   - `Easymakemoney/Services/PreferenceService.cs`
   - `Easymakemoney/Services/IPreferenceService.cs`
2. **Mise à jour des utilisateurs restants** :
   - `Easymakemoney/App.xaml.cs` : suppression des champs statiques `Token` et `UserDetails`.
   - `Easymakemoney/Controls/FlyoutHeaderControl.xaml.cs` : lit les informations depuis `ISessionService` ou reçoit `UserInfo` en paramètre. Rôle affiché fixé à `ROLE_ADMIN`.
   - `Easymakemoney/UseCase/ListCollectionUseCase/SaveCollectionUseCase.cs` : injecte `ISessionService` au lieu de `IPreferenceService` pour lire `user.Id`.
   - `Easymakemoney/ViewModels/AppShellViewModel.cs` :
     - Bouton « Se déconnecter » (remplaçant « Sign Out » dans `AppShell.xaml`) appelle `_authFlowService.LogoutAsync()`.
     - « Changer de site » appelle `_authFlowService.PromptAndChangeSiteAsync()`.
3. **Mise à jour de `Easymakemoney/MauiProgram.cs`** :
   - Enregistrement de `ISessionService` / `SessionService` en singleton.
   - Enregistrement de `AuthHandler` en transient.
   - Enregistrement de `IAuthFlowService` / `AuthFlowService` en singleton.
   - Enregistrement de `OtpPage` et `OtpPageViewModel` en transient.
   - Insertion de `AuthHandler` dans la configuration du client HTTP partagé (`ConfigureBackendHttpClient`), placé après `TenantHandler`.
   - Retrait de `IPreferenceService`.

---

## Comment je vérifie
Commandes de compilation et de contrôle (à lancer après le code) :

1. **Recompilation complète Android (`-t:Rebuild`)** :
   ```bash
   dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -c Debug -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home
   ```
   *Résultat attendu* : `0 Error(s)`, `418 Warning(s)` (aucun avertissement supplémentaire introduit).

2. **Décompte des avertissements par type** :
   ```bash
   grep -E 'warning (CS[0-9]+|XC[0-9]+|MVVMTK[0-9]+|XA[0-9]+)' <build.log> | sed 's/ \[.*//' | sort -u | grep -oE '\b(CS[0-9]+|XC[0-9]+|MVVMTK[0-9]+|XA[0-9]+)\b' | sort | uniq -c | sort -nr
   ```
   *Résultat attendu* : Même décompte qu'au lot 1 (115 CS8618, 66 CS0618, 54 XC0022, 40 MVVMTK0034, etc.).

3. **Contrôle d'encodage, du BOM UTF-8 et des fins de ligne** :
   ```bash
   for f in $(git diff --name-only); do a=$(git show feature/lot-0-plateforme:"$f" | file -b -); b=$(file -b "$f"); [ "$a" != "$b" ] && echo "DIFF $f"; done; echo fin
   ```
   *Résultat attendu* : Affichage de `fin` uniquement.

4. **Liste des 11 essais fonctionnels à mener sur émulateur/téléphone (par Claude et le propriétaire sur le site `demo`)** :
   1. **Mauvais mot de passe** : Saisir un e-mail valide et un mauvais mot de passe sur `LoginPage`. Vérifier l'affichage du message « Identifiants invalides » sur place, sans alerte popup ni crash.
   2. **Compte sans `ROLE_ADMIN`** : Se connecter avec un compte client ou non-admin. Vérifier le refus immédiat avec le message « Ce compte n'a pas accès à l'application de gestion. » et l'absence de session enregistrée.
   3. **Bon mot de passe, puis code OTP faux** : Saisir les identifiants d'un admin. Vérifier la réception de `{otp_required: true}` et l'arrivée sur `OtpPage`. Saisir un code erroné (`000000`). Vérifier l'affichage de « Code invalide ou expiré » sur l'écran OTP.
   4. **Bon mot de passe, puis bon code OTP** : Saisir le bon code OTP reçu par courriel. Vérifier le décodage du JWT, la présence de `ROLE_ADMIN`, la concordance du `tenant_code` avec `demo`, l'enregistrement sécurisé de la session et la redirection vers `DashboardPage`. Vérifier l'affichage du courriel et de `ROLE_ADMIN` dans le menu.
   5. **« Renvoyer le code »** : Sur `OtpPage`, cliquer sur « Renvoyer le code ». Vérifier le nouvel appel à `login` avec les identifiants mémoire, la confirmation visuelle et la réception d'un nouveau code.
   6. **Redémarrage de l'application en étant connecté** : Fermer complètement et relancer l'application. Vérifier que `LoadingPage` lit la session valide en `SecureStorage`, effectue la revérification de site en arrière-plan et navigue directement vers `DashboardPage` sans repasser par `LoginPage`.
   7. **Jeton expiré** : Simuler un jeton expiré (par exemple via une date d'expiration passée ou jeton altéré). Vérifier que `AuthHandler` intercepte le 401, déclenche `POST api/token/refresh {refresh_token, platform: "mobile"}`, enregistre le nouveau JWT et rejoue la requête initiale de manière totalement transparente pour l'utilisateur.
   8. **Refresh token révoqué** : Simuler un refus de rafraîchissement (400 ou 401). Vérifier l'effacement de la session, la conservation du site, l'affichage de l'alerte « Votre session a expiré, reconnectez-vous. » et la redirection propre vers `LoginPage`.
   9. **« Se déconnecter »** : Ouvrir le menu, cliquer sur « Se déconnecter ». Vérifier l'envoi de `POST api/logout {refresh_token}`, la purge de la session, la conservation du site dans `SecureStorage` et le retour à `LoginPage`.
   10. **« Changer de site » en étant connecté** : Depuis le menu ou `LoginPage`, cliquer sur « Changer de site ». Confirmer par « Oui ». Vérifier l'envoi préalable de `POST api/logout`, l'effacement complet des données et de la session, et le retour à `ActivationPage`.
   11. **Aucun jeton dans le journal** : Inspecter les logs de sortie (`Debug.WriteLine`). Vérifier qu'aucun JWT ni refresh token n'apparaît dans la console.

---

## Risques et points incertains

1. **Format des rôles dans le JWT Symfony** :
   Selon la configuration de LexikJWTAuthenticationBundle, la clé `roles` peut apparaître soit comme un tableau JSON (`["ROLE_ADMIN", "ROLE_USER"]`), soit comme une chaîne unique. Le parser implémenté devra prendre en charge les deux cas de manière tolérante.
2. **Clonage de `HttpRequestMessage` pour rejouer la requête sur 401** :
   En .NET MAUI / .NET 10, un `HttpRequestMessage` ayant déjà été envoyé ne peut pas être renvoyé directement sans lever une exception. Une fonction de clonage robuste (`CloneHttpRequestMessageAsync`) recopiant les en-têtes, la méthode, l'URI, les options et le flux de contenu (`HttpContent`) est indispensable.
3. **Transmission sécurisée des identifiants entre `LoginPage` et `OtpPage`** :
   Pour permettre à l'utilisateur de cliquer sur « Renvoyer le code », l'adresse e-mail et le mot de passe doivent être disponibles pour relancer l'appel `POST api/login`. Ces données doivent impérativement rester en mémoire vive et ne jamais être écrites sur le disque ou dans `Preferences`/`SecureStorage`.

---

## Questions pour Claude ou le propriétaire

1. **Navigation vers `OtpPage`** : Nous proposons une route Shell absolue `<ShellContent Route="OtpPage" ContentTemplate="{DataTemplate startupPages:OtpPage}" FlyoutItemIsVisible="False" />` dans `AppShell.xaml` (naviguée par `//OtpPage`), identique à `ActivationPage`. Le bouton Retour de `OtpPage` navigue vers `//LoginPage`. Cela évite toute superposition d'historique Shell lors de l'accès au Dashboard. Validez-vous ce choix ?
2. **Libellé du bouton de déconnexion dans le menu** : Remplacement de « Sign Out » par « Se déconnecter » dans `AppShell.xaml`. Confirmez-vous ?
3. **Conservation en mémoire vive des identifiants pendant l'étape OTP** : Nous les conservons dans un champ privé de `OtpPageViewModel` (ou un service de transition éphémère en mémoire vive), remis à `null` dès la sortie de l'écran. Cela convient-il ?

