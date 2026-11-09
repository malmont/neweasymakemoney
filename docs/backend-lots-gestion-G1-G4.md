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

## Précisions du backend (09/11/2026)

- **G1** : `1250` et `1250.0` valent 1250 centimes ; refusés : `12.5`, `"1250"` en chaîne, négatif, absent quand il est requis.
  Passer les modèles en entiers (`long`) : les réponses rendront des entiers. Les ÷100 en lecture et ×100 en écriture restent valables.
- **`currency`** figure dans chaque réponse portant un montant, y compris `stock-value` et `stock-evolution`. On formate avec cette
  devise, et la culture de l'appareil pour la langue seulement.
- **Inchangées** : `products/{id}/variants`, `product-variants/{id}`, `sizes`, `colors`, `category`, `styles`, `caisse/transactions`.
- **G2** : `close` commande et collection répondent 200 avec un objet JSON (le tableau de bord figé). L'application lit aujourd'hui une
  chaîne : à adapter à la livraison.
- **G3** : `month` devient un **entier JSON 1..12** dans `chiffre-affaires`, `panier-moyen`, `nombre-commandes`, `frais/total`, `taxes/monthly`
  et `carrier/statistics`. L'application lit une chaîne : à adapter. `date` reste une chaîne ISO, `week` un entier. Clés accentuées de
  `nombre-commandes` inchangées.
- **G4** : les 4 `POST` répondent 201 avec l'objet créé ; les champs en trop restent ignorés.
- **`carrier_count` est bien un montant en centimes** (correctif du backend du 09/11, somme des prix de transport du mois) : garder le ÷100. G3 ajoutera `shippingAmount` (même valeur) à côté, sans retirer la clé.
- **Quantité produit** : `product.quantity` est recalculée par le backend (Σ `stockQuantity` des variantes) à chaque création, modification ou suppression d'une variante. Rien à changer côté application. Un produit sans variante reste à 0. En G4, `quantity` devient facultatif sur `POST commandes/{id}/products` : pas de champ à l'écran tant qu'on passe par les variantes.

## LIVRÉ : G1, le 09/11/2026 (en production, tous les sites ; 885 tests verts)

- Conforme à l'annonce. Acceptés : `1250`, `1250.0`, et la chaîne `"1250"` (multipart). Refusés : `12.5`, `"12.50"`, texte, négatif.
- Erreur : 422 `{"error":"VALIDATION_FAILED","code":"VALIDATION_FAILED","message":…,"fields":{"budget":"MUST_BE_INTEGER_CENTS"}}`. Autres codes de `fields` : `REQUIRED`, `INVALID_DATE`, `INVALID_ID`, `MUST_BE_NUMBER`.
- `currency` : sur chaque élément de `collections`, `…/commandes`, `…/notes-de-frais`, sur l'objet `frais-de-port`, à la racine des tableaux de bord, `chiffre-affaires`, `panier-moyen`, `payments/statistics`, `frais/total`, `taxes/monthly`, `stock-value`, `stock-evolution`.
- Défauts corrigés : `DELETE commandes/{id}/frais-de-port` ne supprime plus la commande ; `POST dashboard/collection/{id}/close` fonctionne (réponse `{"message":…}` jusqu'à G2).

**À faire côté application** (après le lot 3) :
1. arrondir à l'entier les 4 écritures ×100 (`Save*UseCase`), sinon `12,345` est refusé ;
2. lire `fields` dans `ApiError` ;
3. afficher la devise du site au lieu de la culture de l'appareil ;
4. `POST …/close` : voir le point de vigilance de G2.

## G2 : précisions avant livraison (09/11/2026)

- `tauxMarge` = marge / `stockValue` × 100 ; `tauxMarque` = marge / `usedBudget` × 100 (`usedBudget` = coût d'achat + frais de port + notes de frais). Arrondis à 2 décimales, comme `averageMultiplier`.
- Ajoutés : `purchaseCost` (= `totalItemCost`) et `currency` dans les tableaux de bord.
- `stockValue` = Σ coût × quantité × coefficient, pour la commande comme pour la collection.
- `close` commande ou collection → 200 avec le tableau de bord figé ; 409 `ALREADY_CLOSED`.

**Point de vigilance côté application :** `CollectionCloseService` lit la réponse en `string`. Un objet JSON (déjà `{"message":…}` depuis G1) fait échouer la lecture alors que la clôture a réussi. À corriger avec le lot d'adaptation G1 : ne plus lire le corps, afficher « Collection clôturée », et recharger le tableau de bord.

## LIVRÉ : G2, le 09/11/2026 (en production ; 885 tests verts)

- Tableaux de bord : mêmes clés, plus `purchaseCost` et `currency`. Coût = `commande_cost_cents ?? purchasePrice ?? price`.
- `POST dashboard/commande/{id}/close` et `dashboard/collection/{id}/close` : 200 avec l'objet du tableau de bord figé ; déjà fermé → 409 `ALREADY_CLOSED`.

**À adapter côté application, en priorité :** `CollectionCloseService` lit encore une chaîne (voir le point de vigilance de G2).
