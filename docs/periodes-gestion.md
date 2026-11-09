# Gestion : les périodes remplacent les collections

Décisions du propriétaire du 09/11/2026. Ce document sert de base à la demande au backend et aux écrans de l'application.

## Le principe

Une **période** regroupe tout ce qui s'est passé entre deux dates. À sa clôture, les chiffres sont figés et on voit si elle a été rentable.

Dans l'application et sur le web, le mot « Collection » devient « Période ». Seul l'affichage change : les routes `collections` du backend gardent leur nom.

## Les règles

| Sujet | Règle |
|---|---|
| Nombre | 4 par an par défaut, réglable par site (2 semestres, ou par mois) |
| Dates par défaut | Printemps : 01/03 au 31/05. Été : 01/06 au 31/08. Automne : 01/09 au 30/11. Hiver : 01/12 à fin février |
| Nom | « Été 2026 », « Hiver 2026-2027 » |
| Création | Automatique, quand la période débute. Plus de bouton « Ajouter une collection » |
| Budget | Vide à la création ; l'application propose de reprendre celui de la même période l'année précédente |
| Classement | Par date, automatiquement : commandes, frais de port, notes de frais, ventes |
| Clôture | Automatique, 15 jours après la fin, pour laisser arriver les factures en retard. Réouverture manuelle possible |
| Vente après la fin | Compte dans la période suivante, selon sa date |
| Articles sans commande | Vont dans « À classer » de la période en cours |
| Périodes déjà créées | Les collections existantes aux dates différentes restent telles quelles |

**Pas de numéro continu** (« Période 5 ») : il dépendrait de la date d'activation du site et devrait être recalculé à chaque reprise d'historique.

## La rentabilité

> **Résultat de la période = ventes de la période − coût des articles vendus dans la période − frais de la période**

Les achats s'affichent à part : « Investi dans le stock pendant la période ». Sinon, une période où l'on achète le stock de la suivante paraîtrait déficitaire.

Le prévisionnel d'un stock reste sur la **commande**, qui garde son coût, ses coefficients et ce qu'elle a déjà vendu, même après la clôture de la période.

## Le client qui arrive après quelques mois

Pas de mode à activer dans la boutique ou la caisse : le premier lancement de l'application de gestion sert d'activation. Les ventes et les autres applications ne changent pas.

Accueil en 3 écrans :

1. **Le calendrier** : 4 périodes, 2 semestres ou par mois.
2. **Reprendre l'historique** : oui par défaut, sur 12 mois. Le résultat est affiché avant confirmation (périodes créées, articles classés, articles sans coût).
3. **Compléter les coûts** : facultatif. Un coefficient moyen donne des coûts marqués « estimés ».

**Règles de la reprise :**
- une « commande de reprise » par période, faute de connaître les vrais achats passés ;
- un coût n'est jamais inventé sans le dire : un article sans coût est en état « coût inconnu » et exclu des calculs de marge ;
- la reprise est marquée comme telle et s'annule d'un geste.

## Locations et abonnements

Ils n'entrent pas dans la logique commande : ce sont des sources de revenus de la période, à côté des ventes.

- **Location** : l'article est acheté par une commande normale et marqué « destiné à la location ». On suit ce qu'il a rapporté, par exemple « remboursé à 65 % ».
- **Abonnement** : revenu récurrent, avec le montant mensuel et les départs. Le mobile montre l'encaissé ; l'étalement comptable va sur le web.

## Demande au backend

Contraintes : ajouts uniquement, aucune régression pour les autres applications, routes `collections` conservées, calculs faits côté backend pour que le mobile et le web affichent les mêmes chiffres.

1. **Calendrier par site** et création automatique des périodes, budget facultatif.
2. **Classement automatique par date** des commandes, frais et notes de frais.
3. **Clôture automatique** à la fin + 15 jours, figeant les chiffres, avec réouverture manuelle.
4. **Résultat d'une période** : ventes, coût des articles vendus, frais, montant investi, en devise du site (taux de chaque commande, lot G6).
5. **Reprise d'historique** : simulation, application, annulation, et état « coût inconnu ». Aujourd'hui, le coût retombe sur `commande_cost_cents ?? purchasePrice ?? price` : un article sans coût est compté à son prix de vente, ce qui fausserait la marge.
6. **À confirmer** : comment les locations et les abonnements sont enregistrés aujourd'hui, et de quelles dates et montants on dispose.

## Contrat du lot P1, amendé (backend, 09-10/11/2026, annoncé avant le code)

**Modèle :** une période est une collection avec `kind: "period"` (les autres : `"manual"`). Champs ajoutés sur chaque collection : `kind`, `periodKey`, `status` (`open` ou `closed`), `closedAt`, `reopenedAt` ; `isClosed` reste présent.

**Réglage :** clé `gestion.periods` de `GET/PUT /api/site/features` : `quarterly` (défaut), `semester`, `monthly`. Un changement prend effet à la fin de la période en cours, sans chevauchement.

**Création à la lecture :** toutes les périodes manquantes sont créées, depuis la plus ancienne activité du site (24 mois au plus) jusqu'à la suivante. Création idempotente, index unique sur `period_key`.

**État :** `open` tant qu'aujourd'hui ≤ fin + 15 jours, en jours civils dans le fuseau du site, ou après réouverture. Chiffres figés à la première lecture d'une période fermée.

| Route | Effet |
|---|---|
| `GET /api/periodes`, `GET /api/periodes/courante` | Liste (filtres G5) et période en cours |
| `GET /api/collections?kind=manual\|period` | Sans `kind` : les deux, chacune avec son `kind` |
| `POST /api/commandes` | `{budget, date, name, fournisseur{id}, exchangeRate?}` : 201, classée par sa date |
| `POST /api/notes-de-frais` | `{description, montant, date, typeNoteDeFraisId}` : 201, classée par sa date |
| `PATCH /api/periodes/{id}` | `{budget?}` : 200 ; 409 `PERIOD_CLOSED` |
| `PATCH /api/collections/{id}` | `{budget?, nomCollection?}` : nom pour une manuelle seulement ; 409 `COLLECTION_CLOSED` |
| `POST /api/dashboard/collection/{id}/reopen` | 200 ; 409 `NOT_CLOSED` |
| `POST /api/dashboard/collection/{id}/close` | 409 `ALREADY_CLOSED` si déjà fermée |
| `GET /api/dashboard/collections-combined?kind=` | `manual` par défaut, jamais les deux ensemble |

**Écritures refusées (409 `PERIOD_CLOSED`, avec `periodId` et `periodKey`) :** création de commande et de note ; `POST` et `DELETE` de `frais-de-port` ; `POST` de `products` et `products/rattacher` ; `DELETE` d'un produit de commande ; `PATCH` et `DELETE` de `commandes/{id}` (date déplacée vers ou depuis une période fermée incluse) ; `PUT` et `DELETE` de `notes-de-frais/{id}`.

**Autres refus :** `DELETE /api/collections/{id}` sur une période : 409 `PERIOD_NOT_DELETABLE`.

**À adapter côté application, après « LIVRÉ » :** traduire `PERIOD_CLOSED` et `PERIOD_NOT_DELETABLE`, libellé « Période », lecture seule si `status = closed`, création de commande et de note par la date, combiné avec `kind=period`, modification du budget.

**À suivre :** P2 (résultat de période, locations et abonnements à part si le propriétaire le confirme) et P3 (reprise d'historique), chacun annoncé avant son code.

## LIVRÉ : P1, le 09/11/2026 (`dev` 54a0cdca ; 898 tests verts)

**Adapté dans l'application :** « Période » partout, plus de création ni de suppression d'une période, état affiché (à venir, en cours, clôturée), bannière et lecture seule sur une période clôturée (commandes, articles, notes, frais), réouverture et nouvelle clôture depuis le tableau de bord, messages `PERIOD_CLOSED`, `PERIOD_NOT_DELETABLE`, `OUTSIDE_PERIOD`, `NOT_CLOSED`.

**À venir côté application :** modification du budget (`PATCH /api/periodes/{id}`), création par la date depuis un point d'entrée général (`POST /api/commandes`, `POST /api/notes-de-frais`), période courante présélectionnée.

## Contrat du lot P2 : résultat de période (validé avec corrections le 09/11/2026, en cours)

`GET /api/periodes/{id}/resultat` : `period`, `currency`, `frozen`, `frozenAt`, `sales {revenue, unknownCostRevenue, shippingBilled, orders, items, returns, bySource}`, `otherRevenue {rentals, subscriptions, total}`, `cogs {amount, items, unknownCostItems}`, `expenses {shipping, notes, other: [], total}`, `result {gross, net, tauxMarge, tauxMarque, netRate}`, `investment {purchases, shipping, total}`.

- `gross = (revenue − unknownCostRevenue) − cogs` ; `net = gross − expenses.total` ; `tauxMarge = gross / cogs` ; `tauxMarque` et `netRate` sur `revenue − unknownCostRevenue`. Dénominateur nul : `null`.
- `revenue` net des remises et des retours ; un retour retire aussi son coût de `cogs`, et compte à sa propre date.
- Locations et abonnements exclus de `revenue` et `cogs`, renvoyés dans `otherRevenue`.
- Une période fermée ignore les rattachements et coûts posés après son échéance, quelle que soit la date de première lecture.

Maquette des écrans : https://claude.ai/artifact/8biDay5vzqM3kEKQcz8zU8

## LIVRÉ : P2, le 10/11/2026 (`dev` be08147d ; 899 tests verts)

Résultat branché dans le tableau de bord d'une période. Le contrat P3 (reprise d'historique) est en cours de cadrage : simulation, application, annulation, coûts estimés par un coefficient, aucun effet sur le prix, le stock ni la visibilité des articles.

## LIVRÉ : P3, le 10/11/2026 (`dev` 93124fe4 ; 900 tests verts)

`POST /api/gestion/reprise/simulation`, `POST /api/gestion/reprise/appliquer` (`costs`, `estimateCoefficient`), `POST /api/gestion/reprise/annuler`, `GET /api/gestion/reprise`. Priorité des coûts : saisis, `purchase_price`, estimation par coefficient, sinon coût inconnu. Prix, stock, variantes et visibilité ne changent jamais.

Écran « Reprise d'historique » ajouté dans Gestion. **Application et annulation pas encore vérifiées de bout en bout dans l'application** : les périodes fermées du site demo n'ont pas de ventes ; demande faite au backend.
