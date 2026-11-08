# Démarrage de la mise à niveau de l'application de gestion (.NET MAUI)

Topo du chef de projet de la suite Izzipro (session « posflutter front »), 05/11/2026. Il complète
[audit-multitenant.md](audit-multitenant.md) (audit du 15/10/2026) et
[strategie-multitenant-mobile.md](strategie-multitenant-mobile.md) (modèle commun, copie identique dans les dépôts
mobiles : la source est celle du dépôt `posflutter`). Les commits cités sont ceux de la branche `dev` du backend.

## 1. L'application dans la suite

- Dépôt : `neweasymakemoney` (dossier local `~/Desktop/projets/Easymakemoney1`). Dernier commit `e2b05e7` du
  31/03/2025.
- Rôle : application de gestion pour l'administrateur d'un site (`ROLE_ADMIN`) : collections, commandes
  fournisseurs, produits et variantes, notes de frais, frais de port, fournisseurs, tableaux de bord, stock,
  statistiques.
- Applications sur le même backend : caisse POS Flutter (`posflutter`), application e-commerce Flutter, site Next.js
  (`Izzipro_next`), Mémoires Vivantes, Boussole.
- Backend : Symfony multi-tenant `izzipro_backend_symfony_v7`, branche `dev`. Il se lit avec
  `git show origin/dev:<fichier>` et ne se modifie jamais depuis cette application. Un besoin côté serveur passe par
  le propriétaire et les sessions backend, avec la clause de non-régression.
- Un site correspond à un client. Il est choisi par l'en-tête `X-Tenant-Host`, obtenu avec une clé d'activation.

## 2. Documentation à lire

| Sujet | Où |
|---|---|
| Audit de l'application, bloquants B1 à B4, plan en 4 lots | `docs/audit-multitenant.md` (ce dépôt) |
| Choix du site, clé d'activation, session, rafraîchissement, règles | `docs/strategie-multitenant-mobile.md` (ce dépôt) |
| Une route : méthode, rôle exigé, contrôleur | backend, `docs/endpoints.md` |
| Multi-tenant, sécurité, rôles d'EasyAdmin | backend, `docs/architecture.md` |
| Codes d'erreur stables, « Prix taxes comprises » et sa liste « MAUI » | backend, `docs/caisse.md` |
| Codes du compte (connexion, OTP), livraison | backend, `docs/boutique.md` |
| Modules activables et garde `MODULE_INACTIVE` | backend, `docs/console-pilotage.md` |
| Exemple déjà fait : client HTTP unique, intercepteurs site → jeton → session, clé d'activation, OTP | dépôt `posflutter` : `lib/core/`, `docs/audit-multitenant.md` |

La documentation du backend change souvent : la lire sur `origin/dev`, ne pas en garder de copie ici.

## 3. Règles du propriétaire

- Répondre en français.
- Un sujet à la fois : plan d'abord, accord du propriétaire, puis code.
- Commit seulement sur demande ; jamais de push sans accord explicite.
- Le backend n'a pas de préproduction : tout appel écrit dans les bases réelles. Essais sur le site `demo`
  uniquement, jamais sur un site client.
- Aucune URL de backend ni aucun site dans le code : l'URL va dans la configuration, le site vient de la clé
  d'activation.
- Aucune clé, aucun identifiant, aucune clé d'activation dans le dépôt ni dans une conversation : le propriétaire
  les saisit lui-même. Aucune valeur de variable d'environnement affichée, même masquée.
- Après un changement notable (route, session, multi-tenant), mettre à jour `docs/audit-multitenant.md` ; si le
  modèle commun change, `docs/strategie-multitenant-mobile.md` dans les trois dépôts mobiles.
- Compilation sur le Mac pour Android, iOS et Mac Catalyst ; Windows plus tard, sur une machine dédiée.

## 4. Ce qui a changé côté backend depuis l'audit du 15/10/2026

À vérifier sur `origin/dev` avant de s'y fier.

- **Connexion**
  - Code par courriel (OTP) exigé pour le super administrateur (`cf656d7`). L'application n'a pas encore d'écran
    OTP (bloquant B3).
  - `POST /api/otp-verify` (B16, `fb304fe`) : un courriel inconnu rend `401 OTP_INVALID`, comme un code faux, et non
    plus `UNAUTHORIZED`. Le message est en français seulement : traduire à partir du code.
  - `POST /api/resend-verification` rend toujours 200 (plus de 404 ni de 400), avec une limite de 3 appels par heure
    et par courriel (`429 RATE_LIMITED`).
- **Codes d'erreur stables** sur toutes les 4xx (`9d1d805`, puis B11 `02f1cf7`) : `code` et `message`, `errors[]`
  par champ.
  - Règle commune aux fronts : en français, message du serveur ; dans une autre langue, traduction du code ; code
    inconnu, message du serveur.
  - Un 401 avec un code métier (`LOGIN_INVALID`, `OTP_INVALID`…) n'est pas une session expirée : ne pas
    déconnecter.
- **Taxes par pays, F18 partie 2** (`0328162`, `e5a3ac7`, `534e624`, `c5396db`)
  - `sub_total` ne comprend plus la livraison, même en HT, pour les commandes depuis le 02/11/2026. `total_amount`
    ne change pas, et une ancienne commande garde l'ancien sens.
  - Site à prix taxes comprises (aucun aujourd'hui) : `pricesIncludeTax: true`, `subTotal` TTC, `priceTax` = taxe
    incluse, à ne pas ajouter. Ne jamais recomposer `total = subTotal + priceTax`.
  - Nouvelle clé `taxes[] {label, rate, base, amount, total}` sur chaque commande lue, `rate` en décimal (0.05).
  - Exports et écritures comptables au format inchangé ; `salesNetHt` et `salesGrossHt` restent HT, le TTC est dans
    `salesTtc`.
  - Pays des nouvelles adresses en code ISO (« CA ») au lieu du nom.
- `POST /api/caisse/close` ajoute le champ `report`. Les listes de sessions et de transactions sont inchangées.
- `GET /api/dashboard/commande/{id}` : une commande sans produit rendait 500 (division par zéro). Elle rend 200 avec
  un taux de marque à 0 (`ae12e9f`).
- **Console de pilotage** (04/11/2026) : le garde `MODULE_INACTIVE` refuse certaines routes quand un module est
  désactivé. Vérifier si des routes de l'application sont concernées.
- **Rôles d'EasyAdmin** (lots 2a, 2b, 2c) : certains réglages sont réservés au super administrateur. Les routes
  d'API sont inchangées.

## 5. Points connus à traiter au démarrage (décision du propriétaire)

- **Commandes fournisseurs** : l'entité `Commande` porte un `#[ApiResource]` sans groupes, donc six routes
  automatiques. Un caissier ou un client connecté peut lire ou modifier les commandes fournisseurs. Un lot backend a
  été préparé puis annulé en attendant cette application : opérations réservées à `ROLE_ADMIN`, groupes bornés,
  `DELETE` gardé (204). Il faut d'abord la liste exacte des opérations et des champs que l'application utilise sur
  `/api/commandes`.
- **Suppression d'une commande** (`DELETE /api/commandes/{id}`) : le backend rend 204, l'application attend 200. Elle
  affiche donc un échec alors que la suppression a eu lieu. À corriger dans l'application.
- **Hôte configuré** : `backend-strapi.online`, qui ne correspond à aucun site (bloquant B1).
- **Suppression d'un frais de port** : 404 (bloquant B4).

## 6. Première tâche : état des lieux et plan, sans code ni commit

1. Relire l'audit du 15/10/2026 face à `origin/dev` : ce qui tient, ce qui a changé, ce qui manque (sections 4 et 5
   ci-dessus comprises).
2. Inventaire de la plateforme : version de .NET MAUI, dépendances, cibles.
3. Taxes (F18 partie 2), fichiers et lignes à l'appui :
   - les endroits où l'application lit `sub_total`, `priceTax` ou `totalAmount`, ou recalcule un total ou une taxe ;
   - les statistiques qui additionnent `sub_total` ;
   - l'affichage des taxes, des exports et du pays d'une adresse.
4. Plan découpé en lots, à partir des 4 lots de l'audit, avec une estimation par lot et, pour chacun, ce qu'il
   demande au backend.
5. Compte rendu complet au chef de projet, qui le résume au propriétaire. Le propriétaire valide avant tout code.
