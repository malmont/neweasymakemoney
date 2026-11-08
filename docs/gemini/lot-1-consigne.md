# Lot 1 : socle multi-tenant (choix du site, client HTTP unique)

Consigne de Claude du 08/11/2026. Comme pour le lot 0 : **tu écris d'abord ton plan** dans
`docs/gemini/lot-1-plan.md`, en suivant le modèle de `GEMINI.md` §5, puis tu t'arrêtes. Tu ne codes rien avant que
je l'aie validé.

## Objectif

Aujourd'hui, l'application appelle une adresse d'API écrite dans le code, sans dire de quel site elle parle. Le
backend lui sert alors le site par défaut, c'est-à-dire la production. À la fin du lot 1 :

1. **Au premier lancement**, l'application demande le site, soit par une clé d'activation, soit par un code de site.
   Elle confirme le site avec l'utilisateur, puis l'enregistre en `SecureStorage`.
2. **Chaque requête porte `X-Tenant-Host = tenant.host`.** Sans site enregistré, **aucune requête ne part**.
3. **On peut changer de site**, et ce changement efface tout ce qui appartient à l'ancien.
4. **L'adresse de l'API n'existe plus qu'à un seul endroit.**

La connexion elle-même (`platform: "admin"`, OTP, rafraîchissement du jeton, jeton en `SecureStorage`) relève du
lot 2 : **ne la touche pas**.

- Branche : `feature/lot-1-multitenant`, créée depuis `feature/lot-0-plateforme`. Ne travaille pas sur
  `feature/lot-0-plateforme`.
- Pas de commit.

## À lire avant d'écrire le plan

1. `docs/strategie-multitenant-mobile.md` §1 à 3 : le contrat commun.
2. `docs/backend-contrat-gestion.md` : « Identité de l'application » et « Codes d'erreur ».
3. `docs/lot-1-retours-applis-soeurs.md` en entier, **en particulier le §4, qui est la proposition retenue, et le §5,
   qui donne les décisions du propriétaire**.
4. `docs/revue-architecture.md` §2 A et B, puis §3, lignes « 1 » du tableau.

## Faits relevés dans le code (vérifie-les toi-même)

- **Adresse de l'API** : `const/Configurations.cs` (`BackendSymfonyUrl`, qui finit par `/api`), recopiée en
  constante dans 24 fichiers de `Services/`, qui construisent leurs URL par concaténation, par exemple
  `CollectionsUrl + "/collections"`.
- **Client HTTP** : `MauiProgram.cs` enregistre `AddHttpClient<IHttpService, HttpService>()`. `LoginService` et
  `ListCollectionService` sont des singletons qui gardent donc leur `HttpService` à vie, alors que les autres services
  sont transient.
- **`HttpService`** écrit `DefaultRequestHeaders.Authorization` à chaque appel. Dans ce lot, on n'y touche pas : c'est
  le lot 2.
- **Démarrage** : `AppShell.xaml` déclare les routes `LoadingPage`, `LoginPage` et `DashboardPage`.
  `LoadingPageViewModel`, appelé depuis son constructeur, lit `Preferences` et le jeton, puis va vers `//LoginPage` ou
  `//DashboardPage`.
- **ViewModels singletons** : `LoginPageViewModel`, `DashboardPageViewModel` et `LoadingPageViewModel`.
- **Déconnexion** : `AppShellViewModel.SignOut` (bouton « Sign Out » du menu) n'efface que `UserDetails`.
- **13 accès directs à `Preferences.`** dans le code.
- **`ApplicationId`** : `com.companyname.easymakemoney` dans `Easymakemoney.csproj`.
- **Entitlements** : `Platforms/MacCatalyst/Entitlements.plist` existe ; il n'y en a aucun pour iOS.

## Ce que ton plan doit couvrir

### 1. Adresse de l'API, en un seul endroit

- Une seule source pour l'adresse de l'API : tu peux garder `Configurations`, mais réduit à cette seule valeur.
- `HttpClient.BaseAddress` est réglée sur cette adresse **avec la barre finale** (`…/api/`).
- Les 24 services passent en **chemins relatifs sans barre initiale** (`"collections"`, et non `"/collections"`). Sinon,
  `HttpClient` fait sauter le `/api`.
- Les chemins, les paramètres et les corps restent identiques : **aucun changement de route**.
- Donne la liste des 24 fichiers et un exemple avant et après.

### 2. Site enregistré : `TenantContext` et `TenantStore`

- **`TenantContext {Host, Code, Name}`**, plus `Label`, facultatif. `Name` vaut `Code` s'il est vide.
- **`TenantStore`** lit et écrit ce contexte en `SecureStorage` (JSON sous une seule clé).
  - On garde le **`tenant.host` de la réponse du serveur**, jamais la saisie de l'utilisateur.
  - **La clé d'activation n'est pas gardée** (décision du propriétaire).
- **Effacement au premier lancement** : si un drapeau `Preferences` est absent, on appelle `SecureStorage.RemoveAll()`,
  puis on pose le drapeau. C'est nécessaire parce que le trousseau iOS survit à la désinstallation.
- **Entitlements** : vérifie dans la documentation Microsoft de `SecureStorage` ce qu'exigent iOS, en particulier
  sur le simulateur, et Mac Catalyst (`keychain-access-groups`). Propose la modification exacte des fichiers
  `Entitlements.plist`, et donne ta source.

### 3. Gestionnaire HTTP de site : `TenantHandler`

- C'est un `DelegatingHandler`, branché sur le client de `HttpService` par `AddHttpMessageHandler`.
- **Cas normal** : il ajoute `X-Tenant-Host = TenantContext.Host`.
- **Sans site enregistré** : il **lève une exception dédiée (`TenantMissingException`) avant l'envoi**, et la
  requête ne part pas.
- **Deux exceptions seulement**, signalées par une option de la requête (`HttpRequestMessage.Options`) :
  - l'**activation** part **sans** `X-Tenant-Host` ;
  - **`check-mobile`** part avec l'hôte **donné par l'appelant**, c'est-à-dire la saisie nettoyée.
- **Aucune exception fondée sur le chemin** de la requête.

### 4. Service de site : `SiteService` (activation et code de site)

1. **Activation par clé.**
   - Avant l'appel, on contrôle le format sur place : retirer tout ce qui n'est ni lettre ni chiffre, passer en
     majuscules, puis vérifier le motif `^IZ[A-Z]{3}[A-Z0-9]{12}$`. Si le format est faux, on affiche un message et
     aucun appel ne part.
   - Si le format est bon : `POST api/app/activate {key, app: "admin", deviceName}`. `key` est la saisie après `Trim`
     seulement, puisque le serveur la normalise lui-même.
   - `deviceName` : fabricant et modèle de l'appareil (`DeviceInfo`). **Pas `DeviceInfo.Name`**, qui contient souvent
     le nom de la personne.
2. **Code de site.**
   - Nettoyage : minuscules ; on retire les espaces, `http://`, `https://`, `www.`, le chemin et le port.
   - Appel : `GET api/tenant/check-mobile`, avec cette saisie nettoyée dans `X-Tenant-Host`.
   - On accepte la réponse seulement si elle contient `exists: true` et un `tenant` qui a un `host` et un `code`.
3. **Erreurs, avec le message du serveur et un texte de repli en français** :
   - 400 `APP_KEY_MISSING` ;
   - 404 `APP_KEY_INVALID` (inconnue, révoquée ou expirée) ;
   - 403 `APP_KEY_WRONG_APP` ;
   - 404 `TENANT_NOT_FOUND` ;
   - 400 `TENANT_HOST_MISSING` ;
   - 429 `TOO_MANY_ATTEMPTS` ou `RATE_LIMITED`, avec le délai tiré de `Retry-After`, sinon de `retryAfterSeconds`,
     sinon 60 s ;
   - pas de réponse : « Pas de connexion » ;
   - 5xx : erreur du serveur.

   **Lecture du code d'erreur** : on prend `code` si c'est une chaîne au format `MAJUSCULES_SOULIGNÉES`, sinon `error`.
   Écris cette lecture dans une petite classe à part, `ApiError` : le lot 3 la généralisera. **Ne modifie pas encore
   la gestion d'erreur des autres services.**
4. **Fonctions pures et testables** : le nettoyage de la saisie, le contrôle du format de la clé et la lecture d'une
   erreur doivent être des fonctions sans dépendance à MAUI. Le lot 4 les testera.

### 5. Écran d'activation : `ActivationPage` et son ViewModel

- **Deux zones de même importance**, sur le même écran : « Clé d'activation » et « Code du site ». **La saisie du code
  doit être bien visible**, puisqu'elle sert à entrer le code du site demo pendant les essais (décision du
  propriétaire).
- **Après une réponse positive**, une **carte de confirmation** affiche le nom du site (« C'est bien ça ? »), avec
  « Oui » et « Non ». On n'enregistre qu'après « Oui ». « Non » revient à la saisie sans rien garder.
- **Textes en français.** Pendant l'appel, le bouton reste désactivé (`IsBusy`).
- Une nouvelle route Shell `ActivationPage`, comme `LoginPage` : sans menu, et invisible dans le menu.

### 6. Démarrage

- **Ordre** : effacement du premier lancement, puis lecture du `TenantStore`.
  - Pas de site : on va vers `//ActivationPage`.
  - Un site : on suit la logique actuelle (`LoginPage` ou `DashboardPage`), **sans la modifier**.
- **Revérification en arrière-plan** quand un site est enregistré : `check-mobile` avec `TenantContext.Code`.
  - 200, avec le même code : mise à jour de `Host` et `Name` s'ils ont changé.
  - 404 : effacement complet (point 7), message « Ce site n'est plus disponible », puis retour à l'activation.
  - Réseau, 429 ou 5xx : on garde le site, sans message.
  - 200 avec un autre code : **on ne change rien**, et on l'écrit dans le journal de débogage.
- La revérification ne doit **ni retarder ni bloquer** l'affichage.

### 7. Changement de site et effacement

- Dans le menu, une entrée **« Changer de site »**, à côté de « Sign Out ». Elle demande d'abord une confirmation.
- **L'effacement complet** comprend :
  - `SecureStorage.RemoveAll()` ;
  - l'effacement de `Preferences` (sauf le drapeau du premier lancement) ;
  - `App.Token` et `App.UserDetails` mis à `null` ;
  - le retour à `//ActivationPage`.
- **`POST /api/logout` n'est pas appelé dans ce lot.** L'application n'a pas encore de refresh token : cet appel
  viendra au lot 2, **avant** l'effacement.
- **Les trois ViewModels singletons passent en transient**, pour qu'aucune donnée d'un site ne reste affichée. Dis
  ce que cela change : `LoadingPageViewModel` fait son travail dans son constructeur, vérifie que le démarrage ne
  s'exécute pas deux fois. Si cela ne suffit pas, propose une remise à zéro explicite.
- **Durées de vie** : `LoginService` et `ListCollectionService` passent en transient, comme les autres services.
- `SignOut` reste tel quel, puisqu'il relève du lot 2. Note seulement qu'il ne doit pas effacer le site.

### 8. Identifiant de l'application

`ApplicationId` passe à **`com.arkanoamedia.gestion`**. C'est le choix du propriétaire, sans le tiret qu'Android
interdit ; **sa forme exacte est encore à confirmer**. Prévois l'étape, mais je te confirmerai la valeur avec la
validation du plan. Dis aussi ce que ce changement implique pour Android, iOS et Mac Catalyst. En particulier,
l'application déjà installée n'est pas remplacée : la nouvelle s'installe à côté.

### 9. Vérification

- **Compiler Android**, avec `-t:Rebuild` et le même `JavaSdkDirectory` qu'au lot 0 : 0 erreur, et le nombre
  d'avertissements compté **par une commande** (règle de la revue du lot 0).
- **iOS et Mac Catalyst** : pas de compilation tant que je ne l'ai pas demandée (Xcode 27.1 en cours d'installation).
- **Aucun lancement de l'application et aucun appel au backend.** Je ferai l'essai moi-même sur l'émulateur, avec le
  code du site demo, après la revue du code.
- **Liste précise des essais à mener**, à mettre dans le plan :
  - clé de format invalide ;
  - clé refusée ;
  - code inconnu ;
  - code demo, avec « Oui », puis avec « Non » ;
  - redémarrage avec un site enregistré ;
  - « Changer de site » ;
  - aucune requête sans site ;
  - premier lancement après une réinstallation.

## Hors du lot (ne pas toucher)

- **La connexion** : `platform`, OTP, refresh token, `SecureStorage` du jeton, `EncryptionHelper`, `SignOut`,
  l'en-tête `Authorization`. C'est le lot 2.
- **Les erreurs des autres services**, les codes 2xx et 204, les 7 suppressions qui rendent toujours `true`, et B4.
  C'est le lot 3.
- **Le profil de l'application** (`GET /api/app-profile/admin`), le thème, le logo et la lecture par QR code. Plus
  tard.
- **La refonte des popups, `INavigationService`, les avertissements** et les fautes dans les noms de fichiers ou de
  classes.
- **README et docs** : tu ne modifies que ton plan, puis ton compte rendu.
- **Aucune clé, aucun identifiant ni aucune adresse de site** dans le code, le plan ou le compte rendu, à l'exception
  de l'adresse de l'API, qui existe déjà.
