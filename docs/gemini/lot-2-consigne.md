# Lot 2 : connexion administrateur, code OTP et session

Consigne de Claude du 08/11/2026. Comme pour les lots précédents : **tu écris d'abord ton plan** dans
`docs/gemini/lot-2-plan.md` (modèle de `GEMINI.md` §5), puis tu t'arrêtes. Tu ne codes rien avant que je l'aie
validé.

- Branche : `feature/lot-2-session`, créée depuis `feature/lot-1-multitenant` **une fois les corrections K1 à K8 du
  lot 1 validées**.
- Pas de commit.

## Objectif

À la fin du lot 2, un administrateur du site choisi au lot 1 peut :
1. **se connecter**, avec `platform: "admin"` ;
2. **saisir le code OTP** reçu par courriel ;
3. **rester connecté** : le jeton est renouvelé automatiquement, et la session est gardée en `SecureStorage` ;
4. **se déconnecter proprement**.

Le jeton est refusé s'il n'est pas celui d'un administrateur, ou s'il appartient à un autre site. C'est ce lot qui
permettra au propriétaire de tester l'application sur son téléphone.

## À lire avant d'écrire le plan

1. `docs/strategie-multitenant-mobile.md` §1 : lignes « Connexion », « Réponse de connexion », « Double facteur »,
   « Rafraîchissement », « Jeton lié au site » et « Contenu du JWT ».
2. `docs/backend-contrat-gestion.md` : « Connexion et session » et « Codes d'erreur ».
3. `docs/lot-1-retours-applis-soeurs.md` : §1 (session de l'application e-commerce), §2 (pièges de la caisse sur les
   401) et §4, point 9.
4. `docs/revue-architecture.md` : §2 A5, B1 à B5, et les lignes « 2 » du tableau du §3.

## Contrat du backend (relevé dans le code `origin/dev` le 08/11/2026)

Toutes ces routes passent par le client du lot 1 et portent donc `X-Tenant-Host`. Les routes publiques ne portent
**pas** de jeton : `login`, `otp-verify`, `token/refresh` et `logout`.

| Route | Corps | Réponses |
|---|---|---|
| `POST api/login` | `{username, password, platform: "admin"}` | **200 `{otp_required: true, message}`, sans jeton** : c'est le cas normal pour un administrateur (OTP actif sur le serveur) ; 200 `{token, refresh_token}` si l'OTP n'est pas exigé ; 401 `LOGIN_INVALID` ; 401 `ACCOUNT_NOT_VERIFIED` ; 403 `PLATFORM_NOT_ALLOWED` (compte sans `ROLE_ADMIN`) ; 400 `PLATFORM_INVALID` ; 429 `RATE_LIMITED`, avec `retryAfterSeconds` |
| `POST api/otp-verify` | `{username, otp, platform: "admin"}` | 200 `{token, refresh_token}` ; 401 `OTP_INVALID` (code faux ou expiré, et aussi courriel inconnu) ; 403 `PLATFORM_NOT_ALLOWED` ; 429 (5 essais par 10 minutes et par compte) |
| `POST api/token/refresh` | `{refresh_token, platform: "mobile"}` | 200 `{message, csrf_token, token}` : `token` est le nouveau JWT. Le refresh token **ne change pas**, et il est prolongé de 7 jours à chaque appel ; 401 `REFRESH_TOKEN_MISSING`, `REFRESH_TOKEN_INVALID` ou `USER_NOT_FOUND` |
| `POST api/logout` | `{refresh_token}` | 200 : le refresh token est révoqué sur le serveur |

- **`platform: "mobile"` sur le rafraîchissement** est voulu : c'est la seule valeur qui donne la session glissante
  de 7 jours. Ce n'est pas une erreur.
- **JWT** : valable 3 jours. Il porte `id`, `email`, `firstName`, `lastName`, `roles` (une **liste**), `tenant_code`,
  `username` et `exp`.
- **Un 401 de Lexik** (jeton expiré ou invalide) porte `code: 401`, qui est **un nombre**. `ApiError` le lit déjà
  correctement (L1 du lot 1).

## Faits relevés dans le code (vérifie-les toi-même)

- **`LoginUseCase`** :
  - il envoie `{username, password}` sans `platform` ;
  - il lit `response.Token` ;
  - il décode le JWT avec `JwtService` ;
  - il enregistre la session par `PreferenceService` ;
  - **puis il navigue lui-même** (`FlyoutHeader`, puis `//DashboardPage`).
- **`JwtService.ParseToken`** ne lit que le **premier** claim `roles` (`First`). Un administrateur qui a plusieurs
  rôles pourrait donc être mal reconnu.
- **`PreferenceService`** garde le jeton dans `Preferences`, « chiffré » par `EncryptionHelper` avec une clé écrite
  dans le code. Il garde aussi `userId`, `userEmail`, `userRole` et `UserDetails`, et remplit `App.Token` et
  `App.UserDetails`.
- **Utilisateurs de `IPreferenceService`** :
  - `HttpService` : `GetUserToken`, 4 fois ;
  - `LoginUseCase` ;
  - `SaveCollectionUseCase` : `GetUserId`.
- **Utilisateurs de `App.UserDetails`** : `Controls/FlyoutHeaderControl.xaml.cs` (rôle et courriel affichés dans le
  menu), `LoadingPageViewModel` et `SiteDataCleaner`.
- **`HttpService`**, à chaque appel authentifié :
  - il lève `InvalidOperationException("Token is not available.")` s'il n'y a pas de jeton ;
  - il écrit `DefaultRequestHeaders.Authorization` ;
  - il écrit le corps des réponses dans le journal (`Debug.WriteLine`, 3 fois).
- **`LoadingPageViewModel.checkUserLoginDetails`** décode le jeton et le compare à `DateTime.Now`. S'il a expiré, il
  affiche « User session expired » et renvoie à la connexion.
- **`AppShellViewModel.SignOut`** ne retire que `UserDetails`.
- **`LoginPage.xaml`** :
  - ses textes sont en anglais ;
  - « Sign Up here » n'a **aucune action** ;
  - le seul message d'erreur est « Invalid User ».

## Ce que ton plan doit couvrir

### 1. Session : `SessionService`, en `SecureStorage`

- Interface `ISessionService`, enregistrée en **singleton**, sans aucune dépendance HTTP.
- **Ce qu'il garde** :
  - `token` et `refresh_token` ;
  - l'utilisateur, tiré du JWT : `Id`, `Email`, `FirstName`, `LastName`, `Roles` (liste), `TenantCode`.
- **Ce qu'il offre** :
  - enregistrer une session ;
  - lire le jeton et le refresh token ;
  - remplacer le seul jeton après un rafraîchissement ;
  - lire l'utilisateur ;
  - effacer la session **sans toucher au site**.
- **Ce qui disparaît** : `EncryptionHelper`, `PreferenceService`, `IPreferenceService`, `App.Token` et
  `App.UserDetails`. Chaque utilisateur actuel de ces éléments passe à `ISessionService`. Donne la liste des fichiers
  touchés.
  - `FlyoutHeaderControl` continue d'afficher le rôle et le courriel. Choisis le rôle à montrer : `ROLE_ADMIN`.
  - `SaveCollectionUseCase` lit l'identifiant de l'utilisateur dans la session.
- **Clés de stockage** : écris toutes les clés dans une seule classe, pas en texte libre réparti dans le code.
  `ISiteDataCleaner` (lot 1) continue de tout effacer, session comprise.

### 2. Lecture et contrôle du JWT

- Lecture de **tous** les claims `roles`. La session est **refusée** si `ROLE_ADMIN` n'y figure pas. Message :
  « Ce compte n'a pas accès à l'application de gestion. »
- `tenant_code` doit être égal à `TenantContext.Code`, sans tenir compte de la casse. **Sinon, la session est
  refusée** et rien n'est enregistré. Message : « Ce compte n'appartient pas à ce site. »
- Ce sont des fonctions pures, testables au lot 4.

### 3. Connexion et écran OTP

- `LoginService` envoie `platform: "admin"`. Le modèle de réponse lit `otp_required`, `token`, `refresh_token` et
  `message`.
- **`LoginUseCase` ne navigue plus.** Il rend un résultat : connecté, code OTP demandé (avec le courriel), ou erreur
  (`ApiError`). Le ViewModel décide de la navigation et de l'en-tête du menu.
- **Nouvel écran `OtpPage`** :
  - il rappelle le courriel ;
  - il propose un champ pour le code (clavier numérique, sans limite de longueur imposée), puis « Valider » ;
  - « Renvoyer le code » relance `login` avec les mêmes identifiants, que le ViewModel garde **en mémoire seulement**,
    **jamais stockés** ;
  - « Retour » revient à la connexion.

  Prévois une route Shell, comme pour `ActivationPage`, ou une navigation relative : dis laquelle et pourquoi.
- **Les erreurs** passent par `ApiError` et s'affichent sur l'écran, pas dans une boîte d'alerte en anglais :
  - `LOGIN_INVALID` : « Identifiants invalides » ;
  - `ACCOUNT_NOT_VERIFIED` ;
  - `PLATFORM_NOT_ALLOWED` : « Ce compte n'a pas accès à l'application de gestion. » ;
  - `OTP_INVALID` : « Code invalide ou expiré » ;
  - `RATE_LIMITED`, avec le délai ;
  - pas de connexion.
- **`LoginPage`** :
  - textes en français : « Connexion », « Adresse e-mail », « Mot de passe », « Se connecter » ;
  - **retrait de « Sign Up here »**, qui ne fait rien ;
  - le lien « Changer de site » de K3 reste.

  Ne change rien d'autre à la mise en page.

### 4. Gestionnaire HTTP d'authentification : `AuthHandler`

- C'est un `DelegatingHandler`, placé **après** `TenantHandler` : l'en-tête du site d'abord, le jeton ensuite.
- Il ajoute `Authorization: Bearer <token>` lu dans la session, **sauf** sur les requêtes marquées « sans
  authentification ». Ce marquage passe par une option de requête, comme au lot 1, et **jamais par le chemin**.
  Concernées : `login`, `otp-verify`, `token/refresh`, `logout`. `PostAsyncWithoutAuth` pose cette option.
- **Sur un 401 d'une requête authentifiée** :
  1. **un seul** rafraîchissement à la fois, partagé par toutes les requêtes en attente (`SemaphoreSlim`). Une
     requête qui arrive pendant le rafraîchissement attend son résultat, sans en lancer un second ;
  2. si le rafraîchissement réussit : le nouveau jeton est enregistré, et la requête d'origine est **rejouée une seule
     fois**. Pense à cloner la requête, puisqu'un `HttpRequestMessage` ne peut pas être renvoyé tel quel ;
  3. si le serveur refuse le rafraîchissement (400 ou 401) : la session est effacée, **le site est gardé**, un
     message « Votre session a expiré, reconnectez-vous » s'affiche, et l'application va vers `//LoginPage` sur le
     fil principal ;
  4. si le serveur est injoignable : **on garde la session**, et la requête d'origine échoue normalement.
- **Une requête publique n'est jamais rafraîchie.** Ses 401 (`LOGIN_INVALID`, `OTP_INVALID`) remontent tels quels.
- **`HttpService`** :
  - suppression des écritures dans `DefaultRequestHeaders` et de la lecture du jeton ;
  - sans session, la requête authentifiée est refusée **par `AuthHandler`**, avec une exception dédiée et sans envoi.
    Le comportement reste ainsi proche de l'actuel ;
  - **suppression des 3 `Debug.WriteLine` du corps des réponses**, qui écrivent les jetons dans le journal.
- **Ne touche pas encore aux codes de succès ni à la gestion d'erreur des services** (2xx, 204, `default`) : c'est le
  lot 3.

### 5. Démarrage

Après la partie site du lot 1 :
- **Pas de session** : on va vers `//LoginPage`.
- **Une session dont le jeton est valide** : on va vers `//DashboardPage`.
- **Un jeton expiré, avec un refresh token** : on tente un rafraîchissement.
  - Réussite : `//DashboardPage`.
  - Refus : session effacée, puis `//LoginPage`.
  - Serveur injoignable : `//DashboardPage` quand même, puisque la session est gardée ; les écrans afficheront leurs
    erreurs habituelles.
- **Comparaison de l'expiration** : en UTC (`ValidTo` est en UTC, alors que le code actuel compare à
  `DateTime.Now`), avec une marge de 60 secondes.

### 6. Déconnexion et changement de site

- **« Se déconnecter »**, qui remplace « Sign Out » dans le menu :
  1. `POST api/logout {refresh_token}`, en ignorant toute erreur ;
  2. effacement de la session, **sans toucher au site** ;
  3. retour à `//LoginPage`.
- **« Changer de site »** (menu et écran de connexion) : le même `POST api/logout` part **avant**
  `ISiteDataCleaner.ClearAllAsync()`, tant que le refresh token existe encore.
- Écris cette déconnexion **une seule fois**, et appelle-la depuis les deux parcours.

### 7. Vérification

- Compiler Android (`-t:Rebuild`), compter les avertissements, et contrôler l'encodage. Chaque sortie doit être
  collée telle quelle, après ta dernière modification.
- **Aucun lancement de l'application et aucun appel au backend.** Le propriétaire et moi ferons la recette sur son
  téléphone, avec son compte administrateur du site demo. Il saisira lui-même ses identifiants et le code OTP.
- **Liste des essais à mener**, à mettre dans le plan :
  1. mauvais mot de passe ;
  2. compte sans `ROLE_ADMIN` ;
  3. bon mot de passe, puis code OTP faux ;
  4. bon mot de passe, puis bon code OTP ;
  5. « Renvoyer le code » ;
  6. redémarrage de l'application en étant connecté ;
  7. jeton expiré : simulation à décrire, par exemple en remplaçant le jeton enregistré par un jeton expiré dans une
     version de débogage ;
  8. refresh token révoqué ;
  9. « Se déconnecter » ;
  10. « Changer de site » en étant connecté ;
  11. aucun jeton dans le journal.

## Hors du lot (ne pas toucher)

- **Les codes de succès et les erreurs des services**, ainsi que les 7 suppressions qui rendent toujours `true` et
  B4 : c'est le lot 3.
- **`PATCH /api/profile`**, que MAUI n'appelle pas.
- **La session glissante avec `platform: "admin"`** : elle n'est pas décidée, on garde `"mobile"`.
- **Les autres écrans** et la refonte des popups.
- **Pas de nouveau paquet** sans le demander. `System.IdentityModel.Tokens.Jwt` est déjà présent.
- **Mêmes règles que d'habitude** :
  - ni l'encodage, ni le BOM, ni les fins de ligne d'un fichier existant ne changent ;
  - aucun reformatage ;
  - aucune clé, aucun identifiant ni aucune adresse de site ;
  - pas de commit.
