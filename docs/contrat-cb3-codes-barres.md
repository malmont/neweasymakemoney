# Contrat CB-3 : codes-barres dans l'appli de gestion

Reçu le 15/11/2026 de « prevention des pertes backend ». Mise en ligne prévue à l'étape 74 du backend.

Toutes les routes sont sous `/api/gestion/codes-barres`.
- Droits : ROLE_ADMIN à jeton, module de gestion actif (sinon 403 MODULE_INACTIVE).
- Messages selon `?locale=fr|en`.
- Erreurs : `{ error, code, message, … }`.

1. `GET /recherche?code=` rend `{ code (forme canonique), found, refusal, currency, sheet }`.
   - `sheet` : la fiche qui porte le code, ou null.
   - `refusal`, quand rien ne porte le code : null si le code peut être posé, sinon BARCODE_INVALID ou BARCODE_RESERVED.
   - Code absent : 422 VALIDATION_FAILED `{ fields: { code: REQUIRED } }`.
2. `sheet`, rendue partout : `{ type: variant|product, productId, variantId (null pour un article), name, variantLabel, barcode, symbology (ean13|code128|null), isPos, isWeb, stock, priceCents (null si pas de prix), priceIncludesTax }`.
3. `GET /manquants` rend `{ variants, products, total, limit: 500 }`.
4. `POST /manquants/generer` rend `{ written, variants, products, remaining, limit }`. On relance tant que `remaining > 0`.
5. `POST /variantes/{id}/generer` et `POST /articles/{id}/generer` rendent `{ barcode, sheet }`.
   - 404 VARIANT_NOT_FOUND ou PRODUCT_NOT_FOUND.
   - 409 BARCODE_ALREADY_SET, ou BARCODE_HAS_VARIANTS pour un article à variantes.
6. `PUT /variantes/{id}` et `PUT /articles/{id}` reçoivent `{ barcode }` et rendent `{ barcode, changed, warning, sheet }`.
   - `barcode` vide ou null retire le code.
   - `warning` : null ou `{ code: BARCODE_CHECK_DIGIT, message }`.
   - 422 BARCODE_INVALID, BARCODE_RESERVED ou VALIDATION_FAILED.
   - 409 BARCODE_ALREADY_USED `{ holder: { type, id, productId, variantId, label } }`.
   - Garde-fou demandé le 15/11 : sur une fiche qui a déjà un code différent, il faut `replace: true`. Sinon la route rend 409 BARCODE_ALREADY_SET `{ current }`. Retirer un code exige aussi `replace: true`.
7. `POST /etiquettes/pdf` reçoit `{ labels: [{ variantId | productId, copies }], showPrice? }` et rend un PDF, avec l'en-tête X-Labels-Count. 500 étiquettes au plus.
   - 422 VALIDATION_FAILED, LABELS_LIMIT_EXCEEDED `{ max, requested }` ou SHEET_NOT_FOUND `{ variantIds, productIds }`.
   - 409 BARCODE_MISSING `{ variantIds, productIds }`.
   - `productId` vaut seulement pour un article sans variante.

Chaque écriture laisse une ligne au journal du site, avec la source « gestion ». Le stock d'un article est la somme de ses variantes. La génération ne remplace jamais un code ; seul le PUT avec `replace` le fait.
