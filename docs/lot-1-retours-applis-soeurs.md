# Lot 1 : choix du site dans les applications Flutter (retours du 08/11/2026)

Ce document prépare la consigne du lot 1. Il rassemble ce que les autres applications font réellement pour choisir
le site (tenant). Il ne contient ni clé, ni identifiant, ni adresse de site. Le contrat commun est
`strategie-multitenant-mobile.md`, §1 à 3.

## 1. Application client e-commerce (session « izzpro mobile flutter »)

**Différence de départ : c'est l'application des clients d'une boutique.** Ses utilisateurs n'ont pas de clé : elle
n'appelle jamais `POST /api/app/activate`, seulement `GET /api/tenant/check-mobile`. Elle confirme que, pour une
application de gestion (`app: "admin"`), le bon parcours est : **la clé d'activation d'abord, `check-mobile` en
repli**.

### Premier lancement

- **Un seul champ**, qui accepte un code de site, un code court ou un domaine. Le code court s'écrit sous la forme
  `ABC-D4F`. Le backend accepte la confusion de I ou L avec 1, et de O avec 0, et limite les essais à 20 par minute et
  par adresse IP.
- **Saisie automatique** possible par App Link ou Universal Link (`/b/<CODE>`), par QR code, par le référent
  d'installation Play, ou par « Coller un lien ». Dans ce dernier cas, le presse-papiers n'est lu que sur un geste de
  l'utilisateur.
- **La saisie normalisée est envoyée dans `X-Tenant-Host` pour cette seule requête**, et n'est jamais enregistrée.
- **Confirmation obligatoire** : l'application affiche une carte « C'est bien ça ? » et n'enregistre le site qu'après
  l'accord de l'utilisateur.
- **Règle absolue** : on garde le `tenant.host` renvoyé par le serveur. Jamais la saisie, ni l'hôte lu dans un lien
  ou un QR code.
- **Un lien entrant ne remplace jamais en silence** un site déjà enregistré.

### Ce qui est stocké, et ce qui part à chaque appel

- **`TenantContext {host, code, name, shortCode?}`** est gardé en stockage sécurisé.
- **Toutes les autres données sont rangées par site**, avec le code du site en préfixe : `<code>_TOKEN`,
  `<code>_REFRESH_TOKEN`, `<code>_USER`… Une classe `SiteKeys` refuse de lire ou d'écrire quand aucun site n'est
  actif.
- **Un seul client HTTP**, dont les intercepteurs s'appliquent dans cet ordre :
  1. le site : `X-Tenant-Host = tenant.host` ;
  2. la langue ;
  3. le jeton : `Authorization: Bearer` ;
  4. la session : sur un 401, **un seul** rafraîchissement du jeton, la requête est rejouée **une fois**, et sinon
     c'est la déconnexion, qui **garde le site**.
- **Sans site actif, la requête est rejetée avant l'envoi.** Il n'y a d'exception pour aucune route : même
  `check-mobile` reçoit un hôte explicite.
- **Contrôle du jeton** : il est décodé, et rejeté si son `tenant_code` diffère de `TenantContext.code`.

### Changement de site et revérification

- **Changer de site** :
  1. `POST /api/logout {refresh_token}`, en ignorant les erreurs ;
  2. purge de toutes les clés `<code>_*` ;
  3. effacement du `TenantContext` ;
  4. retour à l'écran de choix du site.
- **Revérification au démarrage, en arrière-plan, par `tenant.code` et non par l'hôte**, pour suivre un changement de
  domaine fait dans le back-office :
  - l'hôte, le nom ou le code court a changé : on met à jour ;
  - 404 : purge et écran « site plus disponible » ;
  - réseau, 429 ou 5xx : on garde le site en mémoire ;
  - la réponse porte un autre code : on ne change jamais de site en silence.
- **iOS : le trousseau survit à la désinstallation.** Au premier lancement après une installation, l'application
  efface tout le stockage sécurisé (`deleteAll`). Elle le repère grâce à un drapeau dans les préférences, et c'est le
  seul endroit où elle fait cet effacement.

### Erreurs et messages

| Cas | Message ou comportement |
|---|---|
| Saisie vide ou invalide | message sur la saisie, aucun appel |
| 404, ou 200 sans `exists`, `host` ou `code` | « Site introuvable », rien n'est enregistré |
| 429 | délai tiré de l'en-tête `Retry-After`, sinon du champ `retryAfter` du corps, sinon 60 s ; le message affiche ce délai |
| Pas de réponse | « Pas de connexion » |
| 5xx | erreur du serveur |
| 403 `MODULE_INACTIVE` | écran « indisponible », sans nouvel essai automatique |

Ce que dit le contrat sur les clés d'activation, puisque cette application ne les gère pas :
- 400 `APP_KEY_MISSING` ;
- 404 `APP_KEY_INVALID` : rien n'est enregistré. Si la clé déjà enregistrée a été révoquée, purge du site et retour à
  l'activation ;
- 403 `APP_KEY_WRONG_APP` ;
- 429, avec `Retry-After`.

**`POST /api/app/activate` part sans `X-Tenant-Host` et sans jeton.**

### Pièges du backend, toujours d'actualité

- **Un hôte inconnu dans `X-Tenant-Host`** (faute de frappe, site supprimé) : le backend sert **la base par défaut,
  sans erreur**.
- **Une requête sans cet en-tête, sur le domaine principal** : même résultat.
- D'où la règle : **aucune requête tant qu'aucun site validé n'est actif**, et **jamais la saisie brute comme hôte**.
- Les dates de réservation partent sans fuseau et reviennent avec le décalage de la boutique : il ne faut pas les
  convertir à l'heure de l'appareil. Cela ne concerne pas MAUI aujourd'hui.

### Code de référence

- **Application e-commerce** :
  - `lib/core/tenant/` : `tenant_context`, `site_repository`, `tenant_interceptor`, `site_keys`, `first_run_reset`,
    `tenant_source_resolver`, `tenant_store` ;
  - `lib/core/session/` : `jwt_claims`, `token_refresher`, `session_interceptor` ;
  - `site_cubit.changeStore()`.
- **Pour le parcours par clé d'activation, le plus proche de notre cas « admin »** : le dépôt POS Flutter,
  dans `lib/core/tenant/`, `lib/core/session/`, `lib/core/config/dio_config.dart` et `lib/features/site/`.

## 2. Caisse Flutter (session « posflutter front »)

C'est le cas le plus proche du nôtre, puisque la caisse s'active par clé. Son code est dans
`~/Desktop/projets/posflutter v2/posflutter` : `lib/core/tenant/`, `lib/features/site/`,
`lib/core/config/dio_config.dart`, `lib/core/session/session_interceptor.dart`, `auth_repository_impl.dart` et
`main.dart`.

- **Voie principale : la clé**, créée par l'administrateur dans EasyAdmin.
  - Elle est saisie à la main, ou lue dans un QR code sur les appareils qui ont une caméra. Pas de lien.
  - Avant l'appel, elle est nettoyée : espaces retirés, majuscules, clé retrouvée au milieu d'un texte de QR code. Un
    format invalide est refusé sur place, sans appel.
  - `POST /api/app/activate {key, app, deviceName}`, sans `X-Tenant-Host`.
- **Voie de secours : `check-mobile`**, avec la saisie nettoyée : minuscules, sans espaces, sans `http(s)://` ni
  `www.`, sans chemin ni port. Le site n'est gardé que si la réponse contient `exists: true` et un objet `tenant`.
- **Ce qui est stocké**, tout en stockage sécurisé :
  - `TENANT_CONTEXT {host, code, name}` (si `name` est vide, il vaut `code`) ;
  - la clé et son libellé, gardés pour pouvoir réactiver ;
  - le profil (`profile` de la réponse : nom, logo, coordonnées, textes légaux).

  Le jeton de connexion est rangé à part.
- **Intercepteur de site** : il ajoute `X-Tenant-Host = tenant.host` à toutes les requêtes, sauf `/app/activate` et
  `/tenant/check-mobile`. Sans site, il bloque la requête (`TenantMissingException`). Les intercepteurs s'appliquent
  dans cet ordre : site, jeton, propre à la caisse, session.
- **Après la connexion**, la connexion est refusée si le `tenant_code` du JWT diffère du site enregistré.
- **« Changer de boutique »** : tout est effacé (stockage sécurisé, préférences, site, clé, profil), puis
  l'application revient à l'activation. **La caisse n'appelle pas `POST /api/logout`.** Si on l'ajoute, il doit
  partir **avant** la purge.
- **Premier lancement après une réinstallation** : le stockage sécurisé est effacé, parce que le trousseau iOS survit
  à la désinstallation.
- **Erreurs** :
  - activation (400, 403, 404) : affichage du `message` du serveur, avec un texte de repli ; rien n'est gardé ;
  - 429 : délai tiré de `Retry-After` ;
  - `TENANT_NOT_FOUND` (404) et `TENANT_HOST_MISSING` (400) : affichage du message du serveur ;
  - `TOO_MANY_ATTEMPTS` (429) pour le code court ;
  - pas de réponse : « pas de connexion ».

  Un site désactivé ou supprimé répond 404 à `check-mobile`, comme un site introuvable.
- **Lecture du code d'erreur** : on prend `code` s'il est en `MAJUSCULES_SOULIGNÉES`, sinon `error`.
- **Pièges** :
  - **un 401 n'est pas toujours une session expirée** : `LOGIN_INVALID` et `OTP_INVALID` doivent afficher leur
    message, pas déconnecter ;
  - **les routes publiques ne déclenchent jamais le rafraîchissement du jeton** : `/login`, `/otp-verify`,
    `/token/refresh`, `/app/activate`, `/tenant/check-mobile`.
- **À ne pas reprendre** : le jeton de terminal (`X-Pos-Terminal`), le poste de caisse et `TERMINAL_REVOKED`.

## 3. Vérifié dans le backend (`origin/dev`, 08/11/2026)

`src/Services/AppKeyService/AppKeyService.php` :
- **Format des clés de gestion : `IZ-ADM-XXXX-XXXX-XXXX`.** L'alphabet n'a pas de caractères ambigus (ni I, ni O, ni
  0, ni 1). Le serveur tolère les minuscules, les espaces et les tirets : il ne garde que les lettres et les chiffres
  avant de comparer.
- **Clé « admin » révoquée ou expirée : 404 `APP_KEY_INVALID`.** `APP_KEY_REVOKED` (403) est réservé à la caisse.
- **Clé destinée à une autre application** : 403 `APP_KEY_WRONG_APP`. **Clé vide** : 400 `APP_KEY_MISSING`.
- **Corps d'erreur** : `{error, code, message, errors?}`, où `code` vaut `error`.
- **Réponse 200** : `tenant {code, name, host}`, `app`, `label`, `expiresAt`, `cashRegister` (vide pour nous) et
  `profile`, qui peut être `null` ; dans ce cas, `GET /api/app-profile/{app}` suffit. Aucun champ `terminal` hors
  caisse.
- **La clé identifie un site et une application, jamais un utilisateur.** L'administrateur se connecte ensuite avec
  son compte et l'OTP. Révoquer une clé ne coupe donc pas une session déjà ouverte.

## 4. Proposition pour le lot 1 MAUI

Les deux applications sont d'accord sur l'essentiel. Là où elles diffèrent, je propose la solution la plus sûre.

1. **Écran d'activation** :
   - un champ pour la clé, avec le contrôle du format `IZ-ADM-…` sur place, puis
     `POST /api/app/activate {key, app: "admin", deviceName}`, sans `X-Tenant-Host` ;
   - un lien « J'ai un code de site », qui passe par `check-mobile` avec la saisie nettoyée comme dans la caisse.
2. **Carte de confirmation « C'est bien ça ? »** avant d'enregistrer, comme dans l'e-commerce : on y montre le nom du
   site.
3. **`TenantContext {host, code, name}`** en `SecureStorage`, ainsi que le profil. On garde le `tenant.host` de la
   réponse. **La clé n'est pas gardée** : elle ne sert qu'à l'arrivée sur un site, puisque l'accès est ensuite protégé
   par `ROLE_ADMIN` et l'OTP, et la voie de secours suffit pour revenir.
4. **`TenantHandler`** (`DelegatingHandler`) : il ajoute `X-Tenant-Host` et **refuse la requête avant l'envoi** quand
   aucun site n'est actif. Les seules requêtes qui portent leur propre hôte sont l'activation (aucun hôte) et
   `check-mobile` (l'hôte saisi).
5. **Changement de site** : `POST /api/logout` d'abord, en ignorant les erreurs ; puis effacement complet du
   stockage, comme dans la caisse ; puis retour à l'activation. Puisqu'on efface tout, on n'a pas besoin de ranger
   les données par site avec un préfixe.
6. **Revérification au démarrage** en arrière-plan par `check-mobile` avec le code du site, comme dans l'e-commerce :
   - hôte ou nom changé : mise à jour ;
   - 404 : purge et écran « site plus disponible » ;
   - réseau, 429 ou 5xx : on garde le site.
7. **Premier lancement après installation** : effacement du stockage sécurisé, nécessaire sur iOS et Mac Catalyst.
8. **Lecture d'erreur commune** : `code`, sinon `error` ; `message` du serveur avec un texte de repli ; 429 avec
   `Retry-After`. C'est la base de l'`ApiError` du lot 3.
9. **Au lot 2** :
   - `tenant_code` du JWT comparé au site actif ;
   - un seul rafraîchissement et une seule relance ;
   - les 401 métier affichent leur message ;
   - les routes publiques sont exclues du rafraîchissement ;
   - la déconnexion garde le site.
10. **Plus tard, en option** : la lecture de la clé par QR code. Le backend sait déjà générer ces QR codes
    (`AppQrCodeGenerator`).

## 5. Décisions du propriétaire (08/11/2026)

- **La proposition du §4 est acceptée**, y compris : ne pas garder la clé, et appeler `POST /api/logout` avant la
  purge.
- **La saisie du code de site doit être bien visible** sur l'écran d'activation, pas cachée derrière un petit lien.
  Elle sert d'abord à entrer le code du site demo pendant les essais. Dans les autres applications, ce code figure dans
  les instructions. On décidera après les essais si on garde cette saisie aussi visible.
- **Identifiant de l'application** : le propriétaire a choisi `com.arkanoa-media.gestion`. Android interdit le tiret
  dans un identifiant : la forme exacte est à confirmer, par exemple `com.arkanoamedia.gestion`.
