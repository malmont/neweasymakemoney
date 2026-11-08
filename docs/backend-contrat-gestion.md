# Contrat backend de l'application de gestion (.NET MAUI)

Relevé du 05/11/2026 par la session backend « posflutter backend », à partir du backend `izzipro_backend_symfony_v7`
(branche `dev`). Complète `docs/strategie-multitenant-mobile.md` (contrat commun) avec ce qui ne concerne que la gestion.
Rien de ce qui suit n'a été vérifié dans le code MAUI : c'est la cible, à confronter au code dans `docs/audit-multitenant.md`.

## État des trois demandes de la section 4 de la stratégie

Toutes livrées par le backend le 15/10/2026 : `token` dans le corps de `POST /api/token/refresh` (refresh token prolongé de
7 jours à chaque rafraîchissement si le corps porte `platform: "mobile"` (B8, 01/11/2026), sinon non renouvelé) ; `platform: "admin"` à la connexion (exige `ROLE_ADMIN`, 400 `PLATFORM_INVALID` pour une valeur inconnue) ;
`GET /api/tenant/check-mobile` (rend aussi `shortCode`, le code court du site, exemple `DEM042` pour demo).

## Identité de l'application

- Code d'application : `admin`. Clés d'activation au format `IZ-ADM-XXXX-XXXX-XXXX`, générées quand l'indicateur
  `isManagementAppActive` de la fiche Entreprise est activé (EasyAdmin), envoyées au contact du site avec un code QR.
- `POST /api/app/activate {key, app: "admin", deviceName?}` (public, 20 essais par IP et 15 min) →
  `{tenant: {code, name, host}, app, label, expiresAt, profile}` ; 400 `APP_KEY_MISSING`, 404 `APP_KEY_INVALID`,
  403 `APP_KEY_WRONG_APP`. Aucune limite d'appareils. Puis connexion normale avec `X-Tenant-Host = tenant.host`.
- `GET /api/app-profile/admin` (public, `X-Tenant-Host`, `?locale=fr|en`) : nom, logo, icône, accroche, couleur principale,
  coordonnées, liens et textes légaux propres à l'application, fusionnés avec la fiche Entreprise (`sources` dit d'où vient
  chaque champ). Le thème `GET /api/mobile/app-theme` n'est pas prévu pour `admin` : ses champs restent `null`.

## Connexion et session

- `POST /api/login {username, password, platform: "admin"}`. Les routes de gestion exigent `ROLE_ADMIN`.
- `ADMIN_OTP_REQUIRED=1` est actif sur le serveur : tout administrateur reçoit `{otp_required: true}` sans jeton, puis
  `POST /api/otp-verify` rend `{token, refresh_token}`. Code envoyé par courriel ; 5 essais par 10 min par compte.
  L'application doit donc avoir l'écran du code à usage unique.
- Limites : connexion 10 par 15 min par compte, 50 par 15 min par site et IP, 200 par 15 min par IP ; réponse 429
  `{code: "RATE_LIMITED", message, retryAfterSeconds}`.
- `PATCH /api/profile` : si l'adresse courriel change réellement, confirmation par `currentPassword` (ou code OTP à
  l'ancienne adresse pour un compte OTP) et la réponse rend `{message, user, token, refresh_token}` : enregistrer les
  nouveaux jetons à la place des anciens.

## Codes d'erreur

Toute réponse 4xx porte une clé `code` stable en MAJUSCULES_SOULIGNÉES. Règle : lire `code` si c'est une chaîne de ce
format, sinon `error` (les 401 de Lexik gardent `code: 401` en entier). `VALIDATION_FAILED` + `errors[{path, message}]`
pour les validations champ par champ. Table complète : `docs/caisse.md` du backend, « Codes d'erreur de la caisse ».

## Routes propres à la gestion (toutes `ROLE_ADMIN`)

- Collections : `GET /api/collections`, `POST /api/createcollections`, `DELETE /api/collections/{id}`,
  `GET /api/dashboard/collections-combined`.
- Commandes fournisseurs (achats) : API Platform `GET/POST /api/commandes`, `GET/PUT/PATCH/DELETE /api/commandes/{id}` ;
  contrôleurs dédiés `GET/POST /api/collections/{id}/commandes`, `GET/POST /api/commandes/{id}/products`,
  `GET/POST/DELETE /api/commandes/{id}/frais-de-port`, `GET /api/dashboard/commande/{id}`, `POST /api/dashboard/commande/{id}/close`.
  **L'application de gestion est le seul client de ces routes** : leur surface est gelée jusqu'à décision du propriétaire.
- Fournisseurs : `GET/POST /api/fournisseurs`, `DELETE /api/fournisseurs/{id}`, `GET /api/type-fournisseurs`.
- Notes de frais : `GET/POST /api/collections/{id}/notes-de-frais`, `PUT/DELETE /api/notes-de-frais/{id}`
  (`ROLE_ADMIN` ou propriétaire de la collection depuis l'audit du 31/10/2026).
- Commandes de vente : `GET /api/orders…` ; statistiques `GET /api/statistiques/*` ; paiements `GET /api/payments`
  (réservé `ROLE_ADMIN` et caisse), `POST /api/payment` (le paiement doit appartenir à l'appelant, l'administrateur voit tout,
  sinon 404 `PAYMENT_NOT_FOUND`).
- Liste exhaustive avec rôle et contrôleur : `docs/endpoints.md` du backend.

## Écarts connus à corriger ou à vérifier dans l'application

1. `DELETE /api/commandes/{id}` (API Platform) répond **204**, pas 200 : l'application afficherait un échec alors que la
   suppression a eu lieu. Accepter 200, 201 et 204.
2. `GET /api/dashboard/commande/{id}` : division par zéro corrigée côté backend le 03/11/2026 (`tauxMarque = 0` quand le
   budget utilisé est 0) ; vérifier l'affichage d'une commande sans produit.
3. Lecteur des commandes de vente (`GET /api/orders`) : nouveaux champs à tolérer et à afficher : `pricesIncludeTax`,
   `taxes[] {label, rate, base, amount, total}`, `giftCardApplied`, `creditApplied`, remises (`discountAmount`,
   `discountPercentBp`, `discountReasonCode`), lignes libres (article personnalisé) de même structure qu'une ligne produit.
   Règles (docs/caisse.md, « Ce que MAUI doit changer ») : ne plus recomposer `total = subTotal + priceTax` (en TTC, `priceTax`
   est inclus : lire `totalAmount` tel quel) ; `subTotal` = lignes seules, la livraison est dans `priceShipping` ; afficher les
   taxes par taux depuis `taxes[]` ; écritures comptables : format inchangé, `salesByTax.base`, `salesNetHt`, `salesGrossHt`
   toujours HT, TTC dans `salesTtc`.
4. Statistiques : la semaine commence le **lundi** dans le fuseau de la boutique ; statistiques mois et année corrigées ;
   `otherPayments` inclus. Changement d'affichage voulu.
5. Devise : clé `currency` (ISO 4217) dans le profil `pos` ; la devise du site se règle dans la fiche Entreprise tant que le
   site n'a aucun montant enregistré.
6. Quand la plateforme passera en mode `live` (aujourd'hui `demo`, aucune restriction), les routes d'un module non accordé
   au site répondront **403 `{code: "MODULE_INACTIVE", module, message}`**. Les routes de gestion ne sont pas gardées ; prévoir
   le code si l'application lit Mémoires Vivantes, Boussole ESG ou le début du parcours Boutique.

## Règles de travail

- Essais uniquement sur le site `demo` (`demo.arkanoa-media.com`) ; jamais sur un site client. Le code tourne en direct.
- Toute demande au backend passe par le propriétaire ou par un message à la session « posflutter backend », avec la route
  concernée ; le backend ne change les routes de gestion que sur accord explicite du propriétaire.
- Si cette fiche ou la stratégie change dans ce dépôt, prévenir le backend pour aligner sa copie.
