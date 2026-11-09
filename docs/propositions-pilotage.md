# Propositions : faire de la gestion un vrai outil d'aide à la décision

Demande du propriétaire du 09/11/2026 : aller au-delà des chiffres actuels, côté application et backend. Ces propositions partent de
l'audit des écrans. **Ce qui demande des données de vente par produit reste à confirmer avec le backend** (colonne « Backend »).

## Le cœur de l'application (précision du propriétaire, 09/11/2026)

L'application fait **les deux** : les **statistiques de l'activité**, et la **gestion des collections avec la vision de l'avenir de ses investissements**. Exemple type : en voyage d'affaires, on saisit au fil de l'eau ses
achats, ses factures et ses frais de voyage, puis tous les frais du mois au retour. On évalue alors si le stock acheté sera rentable avec les
coefficients de revente, et on ajuste en direct les coefficients et les promotions.

**Priorité n° 1 : le prévisionnel de collection**, une page qui répond à « cette collection sera-t-elle rentable ? » :

| Élément | Contenu |
|---|---|
| Investissement total | Achats, frais de port, notes de frais, frais de voyage, **part des frais fixes** |
| Revenu prévisionnel | Σ quantité × prix de vente prévu |
| Résultat prévisionnel | Revenu prévisionnel − investissement total, et marge en % |
| Coefficient minimum | Le coefficient moyen à partir duquel la collection devient rentable |
| Simulateur en direct | Modifier un coefficient ou une promotion et voir le résultat changer aussitôt, sans rien enregistrer |
| Alertes | « Pas assez de stock pour que cette collection soit rentable », « Trop de dépenses », « Cette promotion vous fait passer en perte » |

**À ajouter :**
- **Frais fixes** (confirmé par le propriétaire le 09/11/2026) : salaires des employés, loyer, assurances, abonnements, électricité… Pour chacun : libellé, catégorie,
  montant en centimes, périodicité (mensuelle, annuelle…), date de début et de fin éventuelle. Saisis une fois, répartis automatiquement sur les
  collections (règle de départ proposée : au prorata de la durée de chaque collection dans le mois). Rien n'existe aujourd'hui côté backend.
  **Les salaires sont des données sensibles** : `ROLE_ADMIN` uniquement, et l'IA ne reçoit que des totaux par catégorie, jamais de nom ni de salaire individuel.
- **Frais de voyage** : une catégorie de note de frais, avec **photo du justificatif lue par l'IA** (même moteur que les factures fournisseur).
- **Appliquer le coefficient** à un ou plusieurs articles après simulation : ici le prix de vente doit bien être recalculé, contrairement au rattachement. Route
  dédiée à demander, et le prix de vente ne change qu'après confirmation.
- **Promotions** : simuler une remise sur une partie du stock, et voir son effet sur le résultat.
- **Écoulement réel** : à mesure que les ventes arrivent, le prévisionnel se corrige (« au rythme actuel, il restera X articles à la fin de la collection »).

**Le rôle de l'IA** : commenter ce prévisionnel, jamais le calculer, et proposer un conseil chiffré : « Avec un coefficient de 2,4 au lieu de 2,1, vous couvrez vos frais de voyage. »

## Le constat

Aujourd'hui, l'application **montre des chiffres** : huit pages de statistiques séparées, sans comparaison, sans objectif et sans alerte.
Un gérant doit tout parcourir et faire ses calculs lui-même. Elle ne répond pas aux questions qu'il se pose vraiment :

1. Est-ce que je gagne de l'argent, et où ?
2. Qu'est-ce qui se vend, et qu'est-ce qui dort ?
3. Dois-je racheter, solder ou arrêter quelque chose ?
4. Ai-je assez de trésorerie pour ma prochaine commande ?

## Les propositions, par priorité

### 1. Un écran « Pilotage » à la place de l'accueil actuel

Un seul écran, lisible en dix secondes, avec un choix de période (semaine, mois, année).

| Bloc | Contenu | Décision aidée | Backend |
|---|---|---|---|
| Chiffres clés | CA, marge, panier moyen, nombre de commandes, **avec la variation par rapport à la période précédente** | Ça va mieux ou moins bien ? | Route agrégée à créer |
| À traiter | Budget dépassé, stock bas, stock dormant, commande à clôturer | Quoi faire aujourd'hui ? | Route à créer |
| Canaux | Répartition site / caisse | Où concentrer l'effort ? | À confirmer |

### 2. Rentabilité par collection et par commande

| Indicateur | Définition | Décision aidée | Backend |
|---|---|---|---|
| Taux d'écoulement | Quantité vendue / quantité achetée | Racheter ou solder ? | Ventes par produit à relier à la commande |
| Marge réalisée | Sur ce qui est vendu, à côté de la marge prévue | La collection tient-elle ses promesses ? | À créer |
| Seuil de rentabilité | Ce qu'il reste à vendre pour rembourser l'achat | Quand commence-t-on à gagner ? | Calcul à partir de G2 |
| Comparaison | Collections classées par marge et par écoulement | Quoi reconduire ? | À créer |

### 3. Décisions sur le stock

| Indicateur | Définition | Décision aidée | Backend |
|---|---|---|---|
| Meilleures et pires ventes | Par produit et par variante (taille, couleur) | Quelles tailles racheter ? | À créer |
| Stock dormant | Produits sans vente depuis N jours, avec leur valeur | Quoi solder ? | À créer |
| Couverture de stock | Jours restants au rythme de vente actuel | Quand réapprovisionner ? | À créer |
| Valeur du stock | Coût et valeur de vente | Combien d'argent est immobilisé ? | Existe en partie (`stock-value`) |

### 4. Trésorerie

| Indicateur | Décision aidée | Backend |
|---|---|---|
| Encaissé / dépensé (achats, frais de port, notes de frais) sur la période | Puis-je passer cette commande ? | À créer, données existantes |
| Remboursements : montant et taux | Y a-t-il un problème de qualité ? | Existe en partie (`payments/statistics`) |
| Taxes à reverser | Combien mettre de côté ? | Existe (`taxes/monthly`), à ventiler par taux (F18) |
| Projection de fin de mois | Serai-je à l'objectif ? | À créer |

### 5. Plus tard

- **Objectifs** : le gérant fixe un objectif de CA ou de marge, et l'écran montre l'avancement.
- **Alertes** par notification : stock bas, budget dépassé.
- **Résumé de la semaine rédigé par l'IA** : ce qui a changé et ce qu'il faudrait faire. Le backend a déjà un système de crédits IA.

### 6. Ce qui peut en faire l'application phare de la suite

Seule cette application peut réunir **les achats, la boutique et la caisse**. C'est son vrai avantage : les autres ne voient qu'une partie de
l'activité. Propositions à instruire avec le propriétaire :

- **« Cette semaine, je fais quoi ? »** : une liste courte d'actions classées par gain estimé (solder tel stock dormant, racheter telle taille,
  relancer une collection en retard), au lieu de graphiques à interpréter.
- **Simulateur de commande** avant d'acheter : prix d'achat, coefficient, quantités et frais de port donnent la marge prévue, le seuil de
  rentabilité et l'effet sur la trésorerie. C'est sans doute ce que devaient être les boutons sans action retirés de l'accueil.
- **Prix conseillé** : à partir de la marge visée et des ventes passées d'articles comparables.
- **Note des fournisseurs** : marge dégagée, taux d'écoulement et retours par fournisseur, pour savoir avec qui continuer.
- **Détection d'anomalies** : baisse inhabituelle des ventes, pic de remboursements, écart de caisse.
- **Saisonnalité** : comparaison avec la même période de l'année précédente, et préparation de la prochaine collection.
- **Rapport mensuel** : un PDF pour le comptable, généré en un geste.
- **Vue multi-sites** pour le futur espace partenaire : comparer plusieurs boutiques.
- **Inventaire au scan** : compter le stock avec l'appareil photo du téléphone.

### 7. Le Conseiller IA (idée du propriétaire, 09/11/2026)

Un conseiller qui commente les chiffres et propose des actions. **Présentation recommandée, en trois étages :**

1. **Cartes de conseil sur l'écran Pilotage** : « Cette collection s'écoule lentement : envisager une remise sur ces articles », chacune avec le
   bouton « Pourquoi ? » (les chiffres utilisés) et, si possible, une action.
2. **Une question en langage naturel** : « Pourquoi ma marge a baissé ce mois-ci ? », avec une réponse qui cite ses chiffres.
3. **Le résumé de la semaine**, à l'ouverture de l'application le lundi.

**Règles, à ne pas négocier :**
- **L'IA n'invente et ne calcule aucun chiffre.** Le backend calcule, en SQL, à partir des routes de pilotage ; l'IA ne fait que commenter ce qu'il lui donne.
- **Chaque conseil montre ses sources**, et l'IA dit quand les données sont insuffisantes.
- **L'IA ne fait rien seule** : toute action demande une confirmation du gérant.
- **Aucune donnée personnelle de client** n'est envoyée au modèle : seulement des agrégats et des données produits.
- **Consommation de crédits IA**, avec les crédits existants du backend (402 `AI_CREDITS_EXHAUSTED`), et résultats mis en cache.

**Dépend de** l'écran Pilotage : sans indicateurs fiables, il n'y a rien de sérieux à commenter.

## Corrections relevées pendant l'audit (à faire de toute façon)

- Page « Nombre de commandes » : le graphique des commandes livrées affiche les données des annulations.
- Page « Panier moyen » : le titre indique « Valeur du chiffre d'affaires ».
- Page « Paiements » : les totaux du mois et de l'année sont affichés sans la conversion des centimes.
- Icône `revenue.png` absente du projet : icône vide sur 6 pages.

## Ma recommandation

1. **Finir d'abord** l'interface, le lot 3 et l'adaptation à G1-G4.
2. **Commencer par l'écran Pilotage** (proposition 1), avec **une route agrégée dédiée** côté backend, sans toucher aux routes existantes.
3. **Puis la rentabilité et le stock** (propositions 2 et 3), qui apportent le plus de valeur à un gérant.
4. **Puis la trésorerie.**
