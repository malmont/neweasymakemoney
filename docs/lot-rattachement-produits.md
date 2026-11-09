# Lot à venir : rattacher des produits existants au processus de gestion

Demande du propriétaire du 09/11/2026. Un produit peut exister sans collection ni commande : les autres applications de
la suite fonctionnent ainsi. Quand un site lance le processus de gestion (collection → commande → produits), il doit
pouvoir y intégrer, s'il le souhaite, des produits déjà créés.

Contrat **proposé** par la session « posflutter backend » le 09/11/2026. **Lot accepté le 09/11/2026** : le propriétaire
m'a délégué le choix, et le contrat ci-dessous est transmis au backend, à charge pour lui de confirmer l'unité de
`purchasePrice`. Côté application, le lot commence après la livraison du backend.

## État actuel (`origin/dev`)

- `Product::$commande` est **nullable** : un produit a au plus une commande.
- Aucune route dédiée au rattachement ou au détachement. `POST /api/commandes/{id}/products` crée un produit neuf.
- `PATCH /api/products/{id}` accepterait techniquement `{"commande": …}`, mais sans contrôle ni test : **à ne pas
  utiliser**.
- Aucune route ne liste les produits sans commande.

## Routes proposées (ajouts, routes existantes intactes, `ROLE_ADMIN`)

| Route | Corps ou paramètres | Réponses |
|---|---|---|
| `GET /api/commandes/products/disponibles` | `?q=&page=1&limit=30` ; recherche sur nom, code, code-barres ; tri par nom | `{items: [{id, name, code, barcode, image, price, purchasePrice, coefficientMultiplier, quantity, categories: [{id, name}]}], total, page, limit}` |
| `POST /api/commandes/{id}/products/rattacher` | `{products: [{productId, purchasePrice?, coefficientMultiplier?, quantity?}]}` | 200 `{attached: […], commande: {id, usedBudget, remainingBudget}}` ; 409 `COMMANDE_CLOSED` ; 404 `PRODUCT_NOT_FOUND` ; 409 `PRODUCT_ALREADY_ATTACHED {commandeId}` ; 422 `VALIDATION_FAILED` |
| `DELETE /api/commandes/{id}/products/{productId}` | aucun | 204 ; 404 si le produit n'est pas dans la commande ; 409 `COMMANDE_CLOSED` |

**Règles :**
- l'opération est **tout ou rien** ;
- les champs facultatifs remplacent la valeur du produit s'ils sont donnés ;
- le **prix de vente et les variantes ne changent jamais** ;
- au détachement, `commande` repasse à `null`, et le prix d'achat et le coefficient restent sur le produit ;
- canaux de vente, stock des variantes, caisse et site ne sont pas modifiés ;
- les opérations sont journalisées (`content_audit_log`).

**Charge backend estimée : une demi-journée.**

## Calcul du budget : aucune régression (corrigé le 09/11/2026)

`CommandeDashboardService` calcule `usedBudget = Σ product.price × product.quantity`, plus les frais de port. Pour un produit
de boutique, `price` est le **prix de vente**, et le rattacher gonflerait le budget utilisé.

La règle d'abord envisagée (« `purchasePrice` s'il est renseigné, sinon `price` ») est **écartée** : `createProductByCommande`
pose toujours `purchasePrice`, donc elle changerait les chiffres de toutes les commandes existantes.

**Exigence du propriétaire : aucune régression sur les autres services et projets.**
- le calcul ne change pour aucun produit existant ni pour aucun produit créé dans une commande ;
- seuls les produits rattachés par la nouvelle route peuvent être comptés sur `purchasePrice`, avec une marque additive
  (colonne nullable) et l'unité convertie ;
- uniquement des ajouts : routes et formats existants intacts, migration sans réécriture des données ;
- test prouvant que le tableau de bord d'une commande existante rend les mêmes valeurs avant et après.

## Côté application (première analyse)

- depuis une commande, un écran « Ajouter des produits existants » : recherche, sélection multiple, puis saisie du prix
  d'achat, du coefficient et de la quantité ;
- un geste « Retirer de la commande », distinct de la suppression du produit ;
- à placer après le lot 3 (erreurs) et avec la nouvelle interface.
