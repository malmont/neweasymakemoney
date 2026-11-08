# Lot 5 (à venir) : fiche produit par IA

**À traiter seulement après les lots 0 à 4.** Demande reçue le 07/11/2026 pour l'application de gestion MAUI,
précisée le même jour (variantes, aide intégrée). Contrat backend : `docs/boutique.md` du backend (app_v2), sections
« Fiche produit par IA » et « IA en lot » ; aide intégrée : `docs/console-pilotage.md`, section « Aide intégrée ». Le texte ci-dessous reprend la demande telle que reçue. **À revérifier sur `origin/dev` au démarrage du lot** :
le contrat a pu changer d'ici là. Essais sur le site **demo** uniquement.

Toutes les routes exigent un administrateur connecté (jeton Bearer, `ROLE_ADMIN`) et `X-Tenant-Host`. Elles
reposent donc sur les lots 1 (site) et 2 (session).

## 1. Fonctions demandées

### Entrer un article depuis une photo

`POST /api/products/ai/sheet`, en multipart :
- `photos[]` : 1 à 4 photos prises avec l'appareil ;
- `comment` : texte libre (matière, origine, coût d'achat…) ;
- `categoryId` ;
- `locale` : `fr` ou `en`.

Réponse :
- `draft` : `name`, `shortDescription`, `description`, `moreInformations`, `categoryId`,
  `attributes {material, colors, sizes, gender, season}`, `tags`, `suggestedPriceCents`, `currency`,
  `shippingClass`, `confidence`, `note`. `attributes.sizes` et `attributes.colors` sont deux listes de chaînes,
  relevées par l'IA sur la photo ;
- `pictures` : photos déjà déposées, dont il faut garder les `url` ;
- `credits`.

**Écran de correction** :
- nom ;
- description ;
- prix : obligatoire, prérempli avec `suggestedPriceCents` ;
- stock ;
- catégorie ;
- « Tailles » et « Couleurs » : deux champs modifiables, préremplis avec `attributes.sizes` et
  `attributes.colors` ;
- case « Créer les variantes » : cochée d'office quand l'une des deux listes n'est pas vide ;
- où vendre : site, caisse, landing ;
- modes : vente, location, abonnement ;
- case « traduire automatiquement ».

### Créer le produit

`POST /api/products/ai/create`, en JSON. Le corps contient :
- les champs corrigés ;
- `priceCents`, `stockQuantity` et `categoryIds` ;
- `pictures` : les `url` reçues de `/sheet` ;
- `channels {web, pos, landing}` ;
- `modes {sale, rental, subscription}` ;
- `barcode` (facultatif) ;
- `translate: true` ;
- `variants: {sizes: string[], colors: string[]}`, seulement si « Créer les variantes » est cochée.

**Variantes.**
- Le backend crée une variante par combinaison taille × couleur, avec le prix et le stock donnés.
- Les tailles et couleurs déjà connues du site sont réutilisées, sans tenir compte de la casse ; les autres sont
  créées.
- Limites : 20 tailles, 20 couleurs et 100 combinaisons. Au-delà : **422** `VARIANTS_TOO_MANY`.
- Sans `variants`, le backend crée une seule variante, comme avant.

Réponse **201** : `productId`, `variantId` (la première variante), `slug`, `code`, `languages`, `pictures`,
`translated`, ainsi que `variants` (nombre de variantes créées), `sizes` et `colors` (noms retenus). Le produit est
aussitôt visible sur les canaux cochés.

### Traduire une fiche existante

`POST /api/products/ai/translate {name, description, moreInformations, tags, source}`.

### IA en lot (écran « IA en lot »)

1. `GET /api/products/ai/bulk/plan?kind=sheets|translations|images` donne :
   - les produits concernés ;
   - `creditsNeeded`, `creditsRemaining`, `enough`, `missingCredits` ;
   - `doableNow`, `available`.
2. `POST /api/products/ai/bulk {kind, partial?}` répond **202** `{jobId}`. Deux refus possibles :
   - **402** `AI_CREDITS_EXHAUSTED`, avec le plan : proposer le lancement partiel ;
   - **409** `IMAGE_PROVIDER_MISSING` : réponse tant que la génération d'images n'est pas branchée.
3. `GET /api/products/ai/bulk/{jobId}` toutes les 4 s. Champs suivis : `status`, `done`, `failed`, `skipped`,
   `creditsUsed`, `stoppedForCredits`, `current`.

### Images mannequin (plus tard)

Ces images montrent une personne portant le vêtement. Elles arriveront par les mêmes routes, dès que la plateforme
aura sa clé de génération d'images. **Prévoir leur emplacement dans l'écran dès ce lot.**

## 2. Erreurs à afficher

| Code | Cas | Affichage |
|---|---|---|
| 402 `AI_CREDITS_EXHAUSTED` | crédits IA épuisés | afficher `credits.remaining` ; en lot, proposer le lancement partiel |
| 422 `PHOTO_REQUIRED`, `PHOTO_INVALID` | photo absente ou refusée | message du serveur, rester sur la prise de photo |
| 422 `PRICE_INVALID`, `NAME_REQUIRED`, `CATEGORY_UNKNOWN`, `PICTURE_INVALID` | correction incomplète ou invalide (`/create`) | message sur le champ concerné |
| 422 `VARIANTS_TOO_MANY` | plus de 20 tailles, 20 couleurs ou 100 combinaisons | message près des champs Tailles et Couleurs |
| 409 `IMAGE_PROVIDER_MISSING` | images pas encore branchées | désactiver l'option images, sans erreur bloquante |
| 503 `AI_UNAVAILABLE` | service IA indisponible | réessayer plus tard, rien n'est perdu |

La lecture des erreurs suit la règle commune du lot 3 : `code` stable, message du serveur en français.

## 3. Ce que ce lot demandera côté MAUI (première analyse, à détailler dans la consigne)

- **Prise de photo** : `MediaPicker`, avec les permissions appareil photo et photothèque (`Info.plist` iOS et Mac
  Catalyst, `AndroidManifest.xml`). Limiter à 4 photos et réduire leur taille avant l'envoi.
- **Envoi multipart** à travers le client HTTP unique du lot 1 (en-têtes de site et de jeton).
- **Montants en centimes** (`priceCents`, `suggestedPriceCents`), affichés dans la devise du site (`currency`).
- **Suivi d'un traitement en lot** toutes les 4 s, arrêté quand l'écran se ferme ou quand le traitement est terminé.
- **Nouveaux écrans** : prise de photo et correction de la fiche, puis « IA en lot ». Leur place dans la navigation
  reste à décider.

## 4. Aide intégrée (07/11/2026, indépendante de la fiche IA)

À placer dans ce lot ou dans un lot à part : à décider au démarrage. Routes `ROLE_ADMIN`.

- `GET /api/site/help` rend `{pages: [{key, title, summary, screen, steps, settings}]}`. Le paramètre `?q=mot`
  filtre les pages.
- `GET /api/site/help/{key}` rend la même fiche, avec en plus `html` (la page rendue). Clé inconnue :
  **404** `HELP_NOT_FOUND`.
- Les étapes de `GET /api/site/readiness` et les entrées de `GET /api/site/features` portent un champ `helpKey`.
  L'application affiche alors un bouton « ? » qui ouvre la page d'aide dans un panneau ou une feuille. Les pages ne
  contiennent pas encore d'images.
- MAUI n'appelle aujourd'hui ni `readiness` ni `features` : ces écrans sont donc à créer avant de pouvoir afficher
  l'aide. Pour le HTML, une `WebView` locale suffit, sans navigation externe.

## 5. Questions à poser au backend au démarrage du lot

1. Qui répond sur ces routes : « appli degestion backend » (routes de gestion) ou « posflutter backend »
   (boutique) ?
2. ~~`attributes.colors` et `attributes.sizes` : simples textes, ou rattachés aux couleurs et tailles existantes ?~~
   **Réglée le 07/11** : ce sont des chaînes. Le backend les rapproche des couleurs et tailles du site sans tenir
   compte de la casse et crée les variantes via `variants` (§1).
3. Taille et format maximum des photos acceptées par `/sheet`.
4. Lien avec les produits des commandes fournisseurs (`/api/commandes/{id}/products`) : un produit créé par l'IA
   peut-il être rattaché à une commande ?
