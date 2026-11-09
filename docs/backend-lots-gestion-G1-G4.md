# Backend de gestion : lots G1 à G4 annoncés (09/11/2026)

Annonce de la session « appli degestion backend » (app_v2, branche `dev`), rapportant une décision du propriétaire du
09/11/2026 : `/api/commandes` n'est plus gelé, et la gestion passe aux normes des autres applications (centimes, devise du
site, `?locale=`, fuseau du magasin, codes d'erreur).

**Chaque lot sera annoncé par « LIVRÉ : lot Gx » le jour de sa mise en production. On prépare le code, on n'active rien avant.**

## G1 : centimes partout et devise

Tous les montants deviennent des **entiers en centimes**, en entrée et en sortie.

- **Entrées** : `budgetCollection` (`POST /api/createcollections`), `budget` (`POST /api/collections/{id}/commandes`), `price`
  (`POST /api/commandes/{id}/frais-de-port`), `montant` (`POST …/notes-de-frais`, `PUT /api/notes-de-frais/{id}`), `purchasePrice`
  (`POST /api/commandes/{id}/products`, déjà en centimes). `coefficientMultiplier` reste décimal, avec un point.
- **Lectures concernées** : `GET /api/collections`, `GET /api/collections/{id}/commandes`, `GET /api/commandes/{id}/frais-de-port`,
  `GET /api/collections/{id}/notes-de-frais`, `GET /api/commandes/{id}/products`, les trois tableaux de bord,
  `GET /api/statistiques/*`, `GET /api/payments/statistics/*`, `GET /api/frais/total`, `GET /api/taxes/monthly`.
- **Erreur** : montant non entier ou négatif → 422 `{"error":"VALIDATION_FAILED","fields":{"budget":"MUST_BE_INTEGER_CENTS"}}`.
- Chaque réponse avec un montant porte **`currency`** (ISO 4217 du site) : **aucune devise écrite en dur à l'écran**.
- Les données existantes sont converties en base par le backend.

## G2 : tableaux de bord corrigés (mêmes clés JSON) et rattachement

- `totalItemCost` = Σ coût d'achat × quantité ; `usedBudget` = `totalItemCost` + frais de port + notes de frais ;
  `stockValue` = Σ prix de vente × quantité ; `marge` = `stockValue` − `usedBudget` ; `tauxMarge` = marge/stockValue×100 ;
  `tauxMarque` = marge/totalItemCost×100 (2 décimales) ; `averageMultiplier` pondéré ; nouvelles clés `purchaseCost` et `currency`.
- `POST …/close` : déjà fermé → 409 `ALREADY_CLOSED` ; succès 200 avec les chiffres figés.
- **Correctif du 09/11/2026 : le rattachement n'est pas dans G2.** Il est porté par « posflutter backend », codé et déployé sur demo
  (colonne `product.commande_cost_cents`, tableaux de bord en `commande_cost_cents ?? price`). G2 passera ensuite cette lecture à
  `commande_cost_cents ?? purchasePrice`, avec les mêmes clés JSON.
- **Rappel du contrat de rattachement** (voir `lot-rattachement-produits.md`) :
  `GET /api/commandes/products/disponibles` (`q`, `page`, `limit` ≤ 100) ; `POST /api/commandes/{id}/products/rattacher`
  `{products:[{productId, purchasePrice? centimes, coefficientMultiplier?, quantity?}]}`, tout ou rien, 409 `COMMANDE_CLOSED` ou
  `PRODUCT_ALREADY_ATTACHED`, 404 `PRODUCT_NOT_FOUND`, 422 `VALIDATION_FAILED` ; `DELETE /api/commandes/{id}/products/{productId}` → 204,
  détache sans supprimer.

## G3 : langue et fuseau

- `?locale=fr|en` sur `GET /api/commandes/{id}/products`, `GET /api/type-fournisseurs`, `GET /api/type-note-de-frais`.
- Statistiques : `month` devient un numéro de 1 à 12, les jours une date ISO, plus de noms de mois en anglais ; périodes bornées
  dans le fuseau du magasin.
- Les messages de succès (`{"success":…}`) disparaissent.

## G4 : contrat HTTP

- Créations : 201 avec l'objet créé (`POST …/commandes`, `…/frais-de-port`, `…/notes-de-frais`, `/api/transporteurs`).
- Suppressions : 204 sans corps partout (`DELETE /api/collections/{id}` et `/api/products/{id}` passent de 200 à 204).
- **`GET …/frais-de-port` sans frais → 200 `null`** (aujourd'hui 404).
- Erreurs toujours `{"error":"CODE","message"}` : 422 `VALIDATION_FAILED` ; 404 `COLLECTION_NOT_FOUND`, `COMMANDE_NOT_FOUND`,
  `FOURNISSEUR_NOT_FOUND`, `TYPE_FOURNISSEUR_NOT_FOUND`, `TYPE_NOTE_DE_FRAIS_NOT_FOUND`, `TRANSPORTEUR_NOT_FOUND`, `STYLE_NOT_FOUND`,
  `CATEGORY_NOT_FOUND`, `PRODUCT_NOT_FOUND` ; 409 `SHIPPING_COST_ALREADY_SET`, `COLLECTION_NOT_EMPTY`, `FOURNISSEUR_IN_USE`,
  `COMMANDE_CLOSED`, `ALREADY_CLOSED` ; 403 `FORBIDDEN` ; plus de 500 avec message brut.
- `DELETE /api/commandes/{id}` détache les produits au lieu de les supprimer ; `DELETE /api/collections/{id}` devient une suppression
  logique ; `POST /api/createcollections` ignore `userId` du corps.
- `ROLE_ADMIN` sur toutes les routes de gestion.
- `GET /api/commandes` (API Platform) rendra `id, budget, date, name, photo, collectionId, fournisseur, isClosed, currency`.

## Ce que cela change côté application

- le lot 3 doit tolérer **à la fois** le 404 actuel et le futur `200 null` de `GET …/frais-de-port` ;
- la lecture d'erreur doit accepter `fields` en plus de `errors` et `violations` ;
- l'affichage des montants doit passer à la **devise du site** (`currency`), au lieu de la culture de l'appareil ;
- la nouvelle interface ne doit écrire aucune devise en dur.
