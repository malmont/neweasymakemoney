# Audit de l'application de gestion (.NET MAUI) face au backend multi-tenant

Audit du 15/10/2026. Application de gestion de la suite Izzipro : collections, commandes fournisseurs, produits et
variantes, notes de frais, frais de port, fournisseurs, tableaux de bord et statistiques. Dépôt `neweasymakemoney`,
dernier commit `e2b05e7` du 31/03/2025, soit environ 18 mois sans mise à jour. Backend de référence :
`izzipro_backend_symfony_v7`, branche `dev`, commit `152567d` du 15/10/2026. Les chemins `src/…` et `config/…` sont
ceux du dépôt backend.

Modèle cible pour toutes les applications mobiles : [strategie-multitenant-mobile.md](strategie-multitenant-mobile.md).

## Verdict

**L'application gère aujourd'hui uniquement le site `tenantdefaut`.** L'URL du backend est écrite en dur sur le domaine
principal (`https://backend-strapi.online/api`, `Easymakemoney/const/Configurations.cs:4`) et aucun site n'est
transmis. Les routes de gestion existent toujours, réservées à `ROLE_ADMIN`, sauf la suppression d'un frais de port
(404).
La connexion est fragile : sans `platform`, il faut `ROLE_USER_INTERNET`, et l'écran de code (OTP) des administrateurs
n'existe pas.

## 1. Les routes de l'application confrontées au backend

| Appel (`Services/*.cs`) | Route backend | Accès | État |
|---|---|---|---|
| `POST /login` `{username, password}` | `/api/login` | public | Sans `platform`, le backend suppose `web` : le compte doit avoir `ROLE_USER_INTERNET` (sinon 403) et reçoit un jeton à durée web. Si `ADMIN_OTP_REQUIRED=1` : `{otp_required: true}` sans jeton, que l'application ne sait pas traiter |
| `GET /collections`, `POST /createcollections`, `DELETE /collections/{id}` | idem | `ROLE_ADMIN` | OK |
| `GET/POST /collections/{id}/commandes` | idem | `ROLE_ADMIN` | OK |
| `DELETE /commandes/{id}` | API Platform `/api/commandes/{id}` | `ROLE_ADMIN` | OK |
| `GET/POST /commandes/{id}/products` (multipart `name, description, purchasePrice, coefficientMultiplier, category_ids[], style_id, image`) | idem | `ROLE_ADMIN` | Existe. Image écrite dans `public/assets/uploads/products/`, dossier commun à tous les sites et hors du stockage des médias (`src/Services/ProductService/ProductService.php`) |
| `DELETE /products/{id}` | idem | `ROLE_ADMIN` | OK |
| `GET/POST /products/{id}/variants`, `DELETE /product-variants/{id}` | idem | lecture publique, écriture `ROLE_ADMIN` | La création passe désormais par `BoutiqueCatalogController::createVariant` (09/10/2026) : **corps attendu à vérifier** |
| `GET/POST /commandes/{id}/frais-de-port` | idem | `ROLE_ADMIN` | OK |
| `DELETE /frais-de-port/{id}` (`ListFraisDePortService.cs:25`) | **absente** | — | **404** ; le backend supprime par commande : `DELETE /api/commandes/{id}/frais-de-port` |
| `GET/POST /collections/{id}/notes-de-frais`, `DELETE /notes-de-frais/{id}`, `GET /type-note-de-frais` | idem | `ROLE_ADMIN` / connecté | OK |
| `GET/POST /fournisseurs`, `DELETE /fournisseurs/{id}`, `GET /type-fournisseurs` | idem | `ROLE_ADMIN` / connecté | OK |
| `GET /dashboard/collection/{id}`, `POST …/close`, `GET /dashboard/commande/{id}` | idem | `ROLE_ADMIN` | OK |
| `GET /stock-evolution`, `/stock-value`, `/frais/total`, `/taxes/monthly`, `/carrier/statistics`, `/transporteurs`, `/caisse/transactions` | idem | `ROLE_ADMIN` | OK (`/caisse/transactions` répond 404 quand la caisse ouverte n'a aucune transaction) |
| `GET /statistiques/nombre-commandes`, `/statistiques/chiffre-affaires`, `/statistiques/panier-moyen`, `/payments/statistics` | `…/{source}` | `ROLE_ADMIN` | OK : `{source}` est facultatif (argument `?string $source = null`) ; sans source, toutes les ventes sont comptées. Valeurs : `pos`, `ecommerce` (+ `mobile_app` pour les paiements) |
| `GET /colors`, `/sizes`, `/styles`, `/category` | idem | connecté / public | OK |

## 2. Bloquants

| # | Problème | Où | Correction |
|---|---|---|---|
| B1 | URL du domaine principal en dur, aucun site transmis → `tenantdefaut` pour tout le monde | `const/Configurations.cs:4`, tous les services | `TenantContext` + `DelegatingHandler` qui ajoute `X-Tenant-Host` ([stratégie §2-3](strategie-multitenant-mobile.md)) ; URL dans la configuration |
| B2 | Connexion sans `platform` : exige `ROLE_USER_INTERNET` d'un administrateur | `Models/LoginRequest.cs`, `Services/LoginService.cs` | En attendant une plateforme `admin` côté backend (stratégie §4), envoyer `platform: "web"` en connaissance de cause et donner `ROLE_USER_INTERNET` aux comptes de gestion ; contrôler `ROLE_ADMIN` et `tenant_code` dans le JWT |
| B3 | Pas d'écran OTP : connexion impossible dès que `ADMIN_OTP_REQUIRED=1` | `Services/LoginService.cs` | Traiter `otp_required`, écran du code, `POST /api/otp-verify` |
| B4 | Suppression d'un frais de port en 404 | `ListFraisDePortService.cs:25` | `DELETE /api/commandes/{commandId}/frais-de-port` (le backend supprime le frais de port de la commande) |

## 3. Problèmes importants

- **Erreurs silencieuses** : `HttpService` renvoie `default` pour toute réponse autre que 200/201 (`Services/HttpService.cs`).
  Un 401, un 403 ou un 404 s'affichent comme une liste vide.
- **Pas de gestion de l'expiration du jeton** ni du 401 : l'application doit déconnecter proprement.
- **Jeton stocké dans `Preferences`**, chiffré par AES avec une **clé et un vecteur d'initialisation écrits en dur**
  (`Services/EncryptionHelper.cs`) : n'importe qui ayant le binaire peut déchiffrer. Utiliser `SecureStorage` (Keychain
  / Keystore) et supprimer `EncryptionHelper`.
- **Rôle tronqué** : `JwtService.ParseToken` ne garde que le premier claim `roles` ; un compte avec plusieurs rôles
  peut être mal classé.
- **`HttpClient` partagé modifié à chaque appel** (`DefaultRequestHeaders.Authorization`) : à remplacer par un
  `DelegatingHandler` (jeton et site), plus sûr quand plusieurs appels partent en même temps.
- **.NET MAUI 8** : plus maintenu, passer à la version supportée actuelle ; `Xamarin.Essentials` est un reste de
  Xamarin à retirer ; `Microsoft.Toolkit.Mvvm` 7 → `CommunityToolkit.Mvvm`.
- **Aucun README ni test** dans le dépôt.

## 4. Plan de mise à jour

| Lot | Contenu |
|---|---|
| 1. Socle multi-tenant | Configuration (`BACKEND_REST_URI` seulement), écran « Clé d'activation » à la place de « Votre boutique » : `POST /api/app/activate` **sans `X-Tenant-Host`** `{key (saisie, trim), app: "admin", deviceName? = nom de l'appareil}` → `TenantContext {host, code, name, label?}` en `SecureStorage` ; messages `400 APP_KEY_MISSING`, `404 APP_KEY_INVALID` (ressaisir, rien n'est enregistré), `403 APP_KEY_WRONG_APP`, `429` (attendre `Retry-After`) ; clé gardée pour se réactiver si le `TenantContext` est perdu (`404` → purge du site, retour à l'activation) ; repli faute de clé : saisie du code ou du domaine + `GET /api/tenant/check-mobile` ; `DelegatingHandler` site + jeton (`X-Tenant-Host` = `tenant.host` sur toutes les autres requêtes, bloquées sans site) ; données rangées par site, purgées au changement de site (`POST /api/logout`, `TenantContext` et clé compris). Backend : livré |
| 2. Connexion | `platform`, OTP, contrôle `ROLE_ADMIN` et `tenant_code`, `SecureStorage`, suppression d'`EncryptionHelper` |
| 3. Routes | Suppression des frais de port, contrat de création des variantes, erreurs remontées à l'écran |
| 4. Plateforme | .NET MAUI à jour, dépendances, tests des services |

Recette : site `demo` uniquement, avec un compte `ROLE_ADMIN` du site `demo`.

## 5. Retours pour le backend propres à cette application

- **Plateforme `admin` à la connexion** (exige `ROLE_ADMIN`), au lieu d'imposer `ROLE_USER_INTERNET` aux
  administrateurs ; refuser les plateformes inconnues (`src/Services/AuthenticationService.php:57-71`).
- **Documenter les valeurs de `{source}`** des statistiques (`pos`, `ecommerce`, `mobile_app` seulement pour les
  paiements ; une valeur inconnue est traitée comme « toutes sources ») dans `docs/endpoints.md`.
- **Images de produits** : passer par le stockage des médias (`SharedMediaStorage`, contrôle du type réel) et un
  dossier par site.
- Rafraîchissement natif, route mobile `GET /api/tenant/check-mobile`, hôte inconnu : voir [stratégie §4](strategie-multitenant-mobile.md).
- Liste complète des retours backend : `docs/audit-multitenant.md` du dépôt `posflutter`, section 7.
