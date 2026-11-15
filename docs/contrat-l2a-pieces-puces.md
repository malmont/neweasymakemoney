# Contrat L2-a : pièces et puces (prévention des pertes)

Reçu le 15/11/2026 de « prevention des pertes backend ». Pas encore en ligne. Détail : docs/prevention-pertes.md § 13.9 du dépôt backend.

1. Toutes les routes sont sous /api/gestion/pertes, pour l'administrateur.
   - Il faut la gestion puis le module actifs, sinon 403 MODULE_INACTIVE.
   - Messages selon `?locale=fr|en` ; erreurs `{ error, code, message, … }`.
   - Aucune route n'écrit dans le stock, les ventes ni le catalogue.
2. Fiche d'une pièce : `{ id, number, status (created|retired), statusLabel, zone (null), exercise, variantId, productId, variantMissing, name, variantLabel, barcode, barcodeOf (variant|product|null), symbology, tags [puce], tagged, commandeId, retiredReason, retiredReasonLabel, createdAt, retiredAt }`.
   - Puce : `{ id, identifier, technology (uhf|nfc|simulated), technologyLabel, status (known|associated|retired), statusLabel, pieceId, exercise, associatedAt, retiredAt, retiredReason, retiredReasonLabel }`.
3. `GET /pieces ?variantId&productId&status&tagged=0|1&page&limit` rend une liste de fiches, avec les en-têtes X-Total-Count, X-Page et X-Limit.
   - `GET /pieces/{id}` rend la fiche avec `events [{ id, type, label, at, userId, tagId, data }]` ; 404 PIECE_NOT_FOUND.
4. `POST /pieces { variantId | barcode, count (1 à 500), commandeId? }` rend 201 `{ pieces, count }`.
   - 422 VALIDATION_FAILED.
   - 404 VARIANT_NOT_FOUND, BARCODE_NOT_FOUND ou COMMANDE_NOT_FOUND.
   - 409 BARCODE_AMBIGUOUS `{ productId, variantIds }` ou NO_VARIANT.
5. `POST /puces/associer { pieceId | variantId | barcode, identifier, technology (uhf|nfc) }` rend `{ created, associated, piece, tag }`.
   - C'est le geste unique : lire le code-barres, puis la puce. Rejouer le même geste rend la même pièce, avec `created` et `associated` à false.
   - 422 TAG_INVALID `{ technology }`.
   - 409 TAG_ALREADY_ASSOCIATED `{ holder { id, number, variantId, productId, name, variantLabel } }`, TAG_RETIRED, TAG_IS_BARCODE `{ holder }`, PIECE_RETIRED ou PIECE_TAG_LIMIT `{ limit: 4 }`.
   - 404 PIECE_NOT_FOUND.
6. `GET /puces/recherche?identifier=` rend `{ identifier (forme canonique), found, refusal (TAG_INVALID|null), tag, piece }`.
7. Retraits, définitifs : un retrait ne s'annule pas.
   - `POST /puces/{id}/retirer { reason (detached|destroyed|lost|error), confirm: true }` rend `{ changed, tag, piece }`.
   - `POST /pieces/{id}/retirer { reason (damaged|lost|sold|error), confirm: true }` rend `{ changed, piece }` ; les puces de la pièce sont retirées avec elle.
   - Sans `confirm: true` : 409 CONFIRMATION_REQUIRED.
8. `GET /pieces/synthese ?variantId&productId&page&limit` rend `{ rows [{ variantId, productId, name, variantLabel, barcode, stock, pieces { active, retired, tagged, untagged }, gap }], total, page, limit, totals { variants, pieces, retired, tagged, untagged } }`.
9. Mode exercice, ouvert sans le droit du module, avec les mêmes formes. Les vraies pièces et les exercices ne se croisent sur aucune route.
   - `GET|POST|DELETE /exercices/pieces`. La liste rend `{ pieces, count, limit: 30, remaining, createLimit: 10, tagLimit: 60, daily { limit, remaining, resetAt } }`.
   - `GET /exercices/pieces/{id}` et `POST /exercices/pieces/{id}/retirer`.
   - `POST /exercices/puces/associer { pieceId | variantId | barcode, identifier? }`. Sans `identifier`, c'est « Simuler une lecture » : une puce SIM-… est créée.
   - `GET /exercices/puces/recherche` et `POST /exercices/puces/{id}/retirer`.
   - 409 EXERCISE_LIMIT_REACHED `{ limit }` ou EXERCISE_DAILY_LIMIT `{ limit, resetAt }`.
