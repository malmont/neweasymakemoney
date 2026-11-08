# Stratégie multi-tenant des applications mobiles Izzipro

Document commun aux applications clientes du backend Izzipro : **POS** (Flutter, dépôt `posflutter`),
**e-commerce mobile** (Flutter, dépôt `EcommerceFlutter`) et **gestion** (.NET MAUI, dépôt `neweasymakemoney`).
La même copie est posée dans ces trois dépôts (`docs/strategie-multitenant-mobile.md`) : la modifier dans les trois.
L'application Mémoires Vivantes mobile (`memoiremobile`) en garde une copie adaptée, tenue par son propre agent.

Rédigé le 15/10/2026 à partir du backend `izzipro_backend_symfony_v7` (branche `dev`, commit `152567d`) ; **mis à jour
le 15/10/2026** avec ce que le backend a livré (branche `dev`, pas encore commitée ni poussée par le propriétaire),
puis le même jour avec le contrat de la **clé d'activation** (`POST /api/app/activate`), livrée et en service.
Audit propre à chaque application : `docs/audit-multitenant.md` du dépôt. Contrat détaillé de la caisse :
`docs/caisse.md` du backend ; routes et rôles exacts : `docs/endpoints.md` du backend.

## 1. Ce que le backend attend (le contrat)

| Sujet | Règle | Où, côté backend |
|---|---|---|
| Hôte de l'API | **un seul pour tous les clients** (`BACKEND_REST_URI`, ex. `https://v2.backend-strapi.online`) ; le client n'est **pas** choisi par l'URL de l'API | `docs/architecture.md` |
| Choix du site (tenant) | en-tête **`X-Tenant-Host`** sur chaque requête = `tenant.host` (domaine propre du client, sinon `<code>.<domaine de base du frontend>`, ex. `demo.arkanoa-media.com`) | `EventListener/TenantDoctrineSwitcherListener`, `Services/TenantConnectionManager::findTenantConfigByHost` |
| Obtenir le site — **cible, livré** | **clé d'activation** : `POST /api/app/activate`, public, **sans `X-Tenant-Host`** (c'est la réponse qui donne le site) et sans jeton ; limité en débit par IP (`429` avec `Retry-After` : afficher le message et attendre). Corps `{key, app, deviceName?}` : `key` telle que saisie (juste `trim` ; le backend tolère minuscules, espaces et tirets manquants) ; `app` **toujours envoyé** (`pos`, `mobile` = e-commerce, `admin` = gestion : une clé d'une autre application est refusée) ; `deviceName` facultatif (modèle ou nom donné à l'appareil, pour l'administrateur). `200 {tenant: {code, name, host}, app, label, expiresAt}` ; `400 {error: "APP_KEY_MISSING"}` (clé vide) ; `404 {error: "APP_KEY_INVALID", message}` (inconnue, révoquée ou expirée) ; `403 {error: "APP_KEY_WRONG_APP"}` ; `429` | backend, contrat transmis le 15/10/2026 (`docs/architecture.md`, « Authentification ») |
| Obtenir le site — **repli, livré** | **`GET /api/tenant/check-mobile`**, public, `X-Tenant-Host` = saisie nettoyée par l'application (§2) : code (`demo`) ou domaine (`boutique.ca`, `demo.arkanoa-media.com`) → `200 {exists: true, tenant: {code, name, host}}` (même objet `tenant` que l'activation, même usage) ; `404 {exists: false, error: "TENANT_NOT_FOUND", message: "Boutique introuvable"}` ; `400 {error: "TENANT_HOST_MISSING"}` (en-tête absent ou vide). **Ne pas utiliser `/api/tenant/check`** : route du frontend web (elle refuse un code nu et renvoie vers la création d'un site) | `Controller/TenantSetupController/TenantController` |
| Code court du site — **livré le 31/10/2026** | Le client tape 6 caractères (affichés `ABC-D4F`, tiret facultatif, majuscules indifférentes, I et L lus 1, O lu 0, jamais de U) dans l'écran de choix ; l'application envoie la saisie telle quelle dans `X-Tenant-Host` à `GET /api/tenant/check-mobile`. Le backend cherche d'abord le code de site ou l'hôte exact (inchangé, sans limite), puis seulement le code court (20 essais par minute et par IP, sinon `429 TOO_MANY_ATTEMPTS` avec `Retry-After`). Réponse 200 inchangée plus `tenant.shortCode` (« DEM042 », sans tiret, à afficher `DEM-042`) ; code inconnu, invalide ou site supprimé : `404 TENANT_NOT_FOUND`. Site de test : `DEM042` (permanent, relecteurs des stores). Le marchand voit son code dans EasyAdmin (fiche Entreprise, onglet Applications, et page « Code court de la boutique ») ; seul le super-administrateur le régénère. L'activation par clé ne renvoie pas `shortCode` | `Controller/TenantSetupController/TenantController::checkMobile`, `Services/TenantShortCodeService` |
| Valeur de `X-Tenant-Host` | toujours **`tenant.host`**, l'hôte complet, jamais le code seul ni la saisie : le backend s'en sert aussi pour construire les liens des e-mails (inscription, mot de passe oublié, réservations, abonnements, Mémoires Vivantes) | `RegistrationController`, `ResetPasswordController`… |
| Connexion | `POST /api/login {username, password, platform}` ; valeurs connues `web`, `mobile`, `pos`, `admin` (comparées après trim et minuscules) ; `pos` exige `ROLE_USER_POS`, `mobile` et `web` exigent `ROLE_USER_INTERNET`, **`admin`** (application de gestion) exige `ROLE_ADMIN` ; toute autre valeur → `400 {error: "PLATFORM_INVALID"}` ; sans `platform` : `web`. **Les applications envoient toujours `pos`, `mobile` ou `admin`** | `Services/AuthenticationService::login` |
| Réponse de connexion (natif) | `{token, refresh_token}` dans le corps pour `mobile`, `pos` et `admin` (JWT valable 3 jours, `token_ttl: 259200` ; refresh token 7 jours) ; pas de cookie | `Services/TokenService` |
| Double facteur | si `ADMIN_OTP_REQUIRED=1`, un `ROLE_ADMIN` reçoit `{otp_required: true}` **sans jeton**, puis `POST /api/otp-verify` (même règle `platform` ; sans `platform` : `mobile`) rend les jetons | `Security/TwoFactorPolicy`, `Controller/Account/OtpApiController` |
| Rafraîchissement (natif) | `POST /api/token/refresh {refresh_token, platform: "mobile"}` → `{message, csrf_token, token}` : `token` = nouveau JWT à utiliser en Bearer. Avec `platform: "mobile"` (B8, 01/11/2026), le refresh token est **prolongé de 7 jours à chaque appel** (même jeton, session glissante) ; sans ce champ, il n'est pas renouvelé (7 jours). `password-reset/confirm` et `DELETE /api/account` révoquent tous les refresh tokens du compte. Refus du serveur (400 ou 401) → déconnexion propre, **le `TenantContext` est conservé** ; serveur injoignable → session gardée | `Controller/Account/SecurityController::refreshToken` |
| Jeton lié au site | le JWT porte `tenant_code` ; présenté sur un autre site, il est **refusé** (401) ; un jeton sans `tenant_code` est refusé sur tout site | `EventListener/JWTCreatedListener`, `JWTDecodedListener` |
| Contenu du JWT | `id`, `firstName`, `lastName`, `email`, `roles`, `tenant_code` (+ `username`, `exp`) | `JWTCreatedListener` |
| CSRF | uniquement quand le cookie `auth_token_<code>` est présent (navigateur) : en-tête `X-XSRF-TOKEN` = cookie `XSRF-TOKEN_<code>` ; **les apps natives (Bearer, sans cookie) ne sont pas concernées** | `EventListener/CsrfValidationListener` |
| Autres paramètres du site | lus par les routes habituelles une fois `X-Tenant-Host` en place : `GET /api/boutique-settings` (réglages boutique), `GET /api/entreprise` (devise, langue, coordonnées), `GET /api/cash-registers` (POS : postes de caisse, à proposer au caissier), `GET /api/category`… Rien de tout cela n'est écrit dans l'application | routes existantes |
| Format des erreurs | nouvelles routes et nouveaux cas : `{error: "<CODE>", message: "<texte en français>", errors?: [{path, message}]}` ; `422 VALIDATION_FAILED` pour un contenu refusé. Codes HTTP de succès existants inchangés (ex. `/api/caisse/close` → 200, autres opérations de caisse → 201) | `docs/architecture.md` |

### Les deux pièges qui font lire les données d'un autre client sans aucune erreur

Toujours d'actualité (correction reportée par le backend) :

1. **Hôte inconnu** (`X-Tenant-Host: mobile-app`, faute de frappe, site supprimé) : le listener ne bascule sur aucune
   base et la requête est servie par la **base par défaut du backend**. Aucune erreur n'est renvoyée.
2. **Pas d'en-tête et URL = domaine principal du backend** (`https://backend-strapi.online/api`) : le backend sert le
   site **`tenantdefaut`**.

Conséquence : **une application ne doit jamais appeler l'API tant qu'un site validé (clé d'activation, ou
`/api/tenant/check-mobile` en repli) n'est pas actif.**

## 2. Modèle : un « site actif », obtenu par une clé d'activation

| Mode | Usage | Comment |
|---|---|---|
| **A. Clé d'activation** (cible, livré) | une seule application publiée pour tous les clients | écran « Clé d'activation » au premier lancement : l'utilisateur saisit la clé reçue (`IZ-POS-A7K2-M9QX-4RTB`), `POST /api/app/activate` |
| A bis. Saisie du site (repli, livré) | faute de clé (essais, dépannage) | accès depuis l'écran « Clé d'activation » (par exemple « Je n'ai pas de clé ») : code (`demo`) ou domaine (`boutique.ca`), vérifié par `check-mobile` |
| **B. Marque blanche** | une application par client sur les stores | `TENANT_HOST` (hôte complet) fixé à la compilation : site imposé, écran de choix absent, vérifié par `check-mobile` au premier lancement |

Choix retenu le 15/10/2026 : **mode A** pour les trois applications : écran « Clé d'activation » à la place de
« Votre boutique », `check-mobile` en repli.

### La clé d'activation (livrée le 15/10/2026)

- L'administrateur du site crée et révoque les clés dans EasyAdmin (« Clés des applications ») ou par l'API
  `ROLE_ADMIN` `/api/app-keys` : une clé **par application** (`pos`, `mobile`, `admin`) **et par appareil**. Elle lui
  est affichée une seule fois et envoyée par e-mail au contact du site. Format lisible : `IZ-POS-A7K2-M9QX-4RTB`.
- **La clé identifie le site et l'appareil, jamais l'utilisateur** : après l'activation, l'écran de connexion habituel
  (`POST /api/login`, avec `X-Tenant-Host`) reste obligatoire.
- L'application garde le même `TenantContext {host, code, name}` qu'avec `check-mobile` : seul l'écran d'entrée
  change. `host` est l'hôte **complet** (domaine propre du client, ou `<code>.arkanoa-media.com`), jamais le code seul.
- La clé peut être **gardée en stockage sécurisé** pour se réactiver si le `TenantContext` est perdu. Si le backend
  répond `404 APP_KEY_INVALID` à cette réactivation, la clé a été révoquée : purger les données du site et revenir à
  l'écran « Clé d'activation ».
- `expiresAt` : s'il est renseigné et dépassé, l'activation est refusée (`404 APP_KEY_INVALID`) ; l'application peut
  l'afficher ou l'ignorer.

### Le contexte de site (`TenantContext`)

```
TenantContext {
  host  : "demo.arkanoa-media.com"   // tenant.host renvoyé par /api/app/activate (ou check-mobile), envoyé dans X-Tenant-Host
  code  : "demo"                     // tenant.code, comparé au tenant_code du JWT
  name  : "Démo Arkanoa"             // tenant.name, affichage
  label : "Tablette comptoir 1"      // label renvoyé par /api/app/activate (appareil), facultatif
}
appKey  : "IZ-POS-A7K2-M9QX-4RTB"    // facultatif, à part, pour se réactiver (purgé au changement de site)
```

Gardé en **stockage sécurisé** (Keychain / Keystore : `flutter_secure_storage`, `SecureStorage` de MAUI), relu au
démarrage avant tout appel réseau. Rien de propre à un client n'est écrit dans le code ni dans la configuration : seul
`BACKEND_REST_URI` y figure. `TENANT_BASE_DOMAIN` n'est plus nécessaire.

### Saisies

Clé d'activation : envoyée telle que saisie, après `trim` (le backend tolère minuscules, espaces et tirets manquants).

Repli `check-mobile` :

```
saisie → minuscules, sans espaces
       → retirer "http://", "https://", le chemin, le port, le préfixe "www."
       → envoyée telle quelle dans X-Tenant-Host à GET /api/tenant/check-mobile
       → l'application garde ensuite tenant.host de la réponse, jamais la saisie
```

### Déroulé

```
Démarrage
  ├─ TENANT_HOST défini au build ? ──── oui ──► check-mobile → TenantContext imposé (mode B)
  ├─ TenantContext en stockage ? ────── oui ──► écran de connexion du site (ou session reprise)
  ├─ clé gardée, TenantContext perdu ? ─ oui ──► réactivation (POST /api/app/activate)
  │                                              404 APP_KEY_INVALID → clé révoquée : purge du site, écran d'activation
  └─ sinon ──► écran « Clé d'activation »
                 POST /api/app/activate {key, app: "pos"|"mobile"|"admin", deviceName?}   (sans X-Tenant-Host)
                   200   → enregistrer tenant {host, code, name} (+ label, + clé) → connexion
                   400   → « Saisissez la clé »                 (APP_KEY_MISSING)
                   404   → « Clé invalide », proposer de ressaisir (APP_KEY_INVALID ; rien n'est enregistré)
                   403   → « Clé d'une autre application »      (APP_KEY_WRONG_APP)
                   429   → message, attendre Retry-After
               repli « Je n'ai pas de clé » : saisie du site → GET /api/tenant/check-mobile (X-Tenant-Host: saisie)
                   200 exists → enregistrer tenant {host, code, name} → connexion
                   404 / 400  → « Boutique introuvable », ne rien enregistrer

Connexion
  POST /api/login {username, password, platform: "pos"|"mobile"|"admin"}   (X-Tenant-Host toujours présent)
    otp_required → écran du code → POST /api/otp-verify (même platform)
    {token, refresh_token} → décoder le JWT → tenant_code == TenantContext.code ? sinon rejeter
    → enregistrer token_<code>, refresh_<code>, user_<code>

Chaque requête : X-Tenant-Host: tenant.host + Authorization: Bearer <token_<code>>
401 → POST /api/token/refresh {refresh_token} → nouveau token, requête rejouée une fois ;
      échec → déconnexion propre (TenantContext conservé) → écran de connexion

Changer de site : POST /api/logout {refresh_token} → purge des données du site (jetons, panier, caches,
                  TenantContext, clé) → écran « Clé d'activation »
```

## 3. Règles d'implémentation (les trois applications)

1. **Un seul client HTTP** par application (Dio en Flutter ; `HttpClient` via `IHttpClientFactory` + `DelegatingHandler`
   en MAUI). Interdit : un second client (`package:http`, `new HttpClient()`), une URL de backend en dur dans le code.
2. **Intercepteur de site** : ajoute `X-Tenant-Host` à **toutes** les requêtes, y compris la connexion et les routes
   publiques (produits, catégories). Sans `TenantContext`, il bloque la requête (sauf `/api/app/activate` et
   `/api/tenant/check-mobile`).
3. **Intercepteur d'authentification** : ajoute `Authorization: Bearer` si un jeton existe pour le site actif ; ne
   lève pas d'exception quand il n'y en a pas.
4. **Données rangées par site** : jetons, utilisateur, panier, caches (produits, catégories, commandes) sous une clé
   préfixée par le code du site, ou purgés au changement de site (avec le `TenantContext` et la clé d'activation).
5. **Session** : au démarrage, lire `exp` du JWT (expiré → rafraîchissement, sinon écran de connexion). Réponse `401`
   → un seul rafraîchissement (`POST /api/token/refresh`), puis la requête rejouée ; nouvel échec → déconnexion propre
   et écran de connexion, **sans** effacer le `TenantContext`. Réponse `403` → message « droits insuffisants »,
   **sans** déconnecter.
6. **Erreurs lisibles** : remonter `error` / `message` (et `errors`) du backend jusqu'à l'écran au lieu d'un échec
   générique. Accepter `200` **et** `201` pour un succès.
7. **Build web (Flutter web uniquement)** : `host = Uri.base.host` si l'application est servie depuis le domaine du
   site ; cookies (`withCredentials`) et en-tête `X-XSRF-TOKEN` (valeur `csrf_token` du corps de la connexion et du
   rafraîchissement) sur les requêtes autres que GET.
8. **Essais** : uniquement sur le site **`demo`** (`demo.arkanoa-media.com`). Le backend n'a pas d'instance de
   préproduction : le code tourne en direct en production, chaque essai écrit dans de vraies bases.
9. **Tests automatiques** : la clé d'activation (200, 400, 403, 404, 429, réactivation refusée → purge) et la saisie
   du repli, les intercepteurs (en-tête présent, absent sur `activate`, blocage sans site), le rafraîchissement unique
   sur 401 et le contrôle `tenant_code` du JWT.

## 4. Côté backend : livré, reporté, à venir

Règle du propriétaire (15/10/2026) : le backend ne change que par des **ajouts**, ou sur ce qui ne sert qu'aux
applications mobiles ; les projets déjà en production (frontend web, EasyAdmin…) ne voient aucune différence ; le
mécanisme multi-tenant (reconnaissance du site, bascule de base, isolation des jetons et des caches) n'est pas modifié.
Prompt envoyé au backend : `docs/prompt-backend-corrections.md` du dépôt `posflutter`.

**Livré le 15/10/2026** (branche `dev` du backend, pas encore commitée ni poussée) :

| Demande | Résultat |
|---|---|
| Jeton dans le corps du rafraîchissement natif | `POST /api/token/refresh {refresh_token, platform: "mobile"}` → `{message, csrf_token, token}` (refresh token prolongé de 7 jours avec `platform: "mobile"`, inchangé sans) |
| `platform: "admin"` et refus des valeurs inconnues | `admin` exige `ROLE_ADMIN` (jetons dans le corps) ; autre valeur → `400 PLATFORM_INVALID` ; même règle à l'OTP |
| `GET /api/payments`, écritures de `/api/categories` | `GET /api/payments` réservé admin + caissier (source 2), client → `403 FORBIDDEN` ; écritures de catégories réservées admin |
| Choix du site par une application | **clé d'activation** `POST /api/app/activate` (§1, §2), clés gérées dans EasyAdmin et `/api/app-keys` ; repli `GET /api/tenant/check-mobile` ; `TENANT_BASE_DOMAIN` inutile |
| Thème de l'application e-commerce mobile (30/10/2026) | `GET /api/mobile/app-theme?app=` : public, `X-Tenant-Host`, `app` facultatif (défaut `mobile`, code inconnu → `404 APP_UNKNOWN`) → `{app, theme, sources}`. `theme` a toujours ses neuf clés (`primaryColor`, `accentColor`, `mode`, `font`, `radius`, `buttonStyle`, `productCard`, `homeHero`, `updatedAt`) ; `null` = défaut de l'application. Repli par champ : profil d'application (EasyAdmin, panneau Apparence), puis charte du site web, puis `null` (`sources` : `app`, `charter` ou `null`). Cache public 60 s, `ETag`, `Vary: X-Tenant-Host`. Profil et activation inchangés |
| Droits du caissier, plusieurs postes de caisse, vente au comptoir | voir l'audit du POS (`docs/audit-multitenant.md`) et `docs/caisse.md` du backend |

**Reporté** (décision plus tard) : hôte inconnu servi par la base par défaut (les applications valident toujours le
site avant tout appel, §1) ; refresh token non prolongé sans `platform: "mobile"` (prolongé avec, depuis B8 du 01/11/2026) ; `login-web` encore dans `access_control` sans route ;
clé JWT versionnée.

**À venir** : rien pour le choix du site. Mémoires Vivantes mobile (`memoiremobile`) n'a pas encore de valeur `app`
propre (seuls `pos`, `mobile` et `admin` existent) : demande dans son `docs/05-besoins-backend.md`.

Côté applications : **écran du code à usage unique** à prévoir si `ADMIN_OTP_REQUIRED` est activé (application de
gestion, et POS pour un compte admin).
