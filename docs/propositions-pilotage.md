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

## Guide de période par l'IA et volet publicité (demande du propriétaire, 10/11/2026)

Le Conseiller IA devient un **guide qui accompagne le gérant tout au long de la période**, pendant le voyage d'achat comme pendant la vente, pour que la période soit rentable.

### Ce que le guide fait à chaque moment

| Moment | Ce qu'il propose |
|---|---|
| **Voyage d'achat** | Budget restant, marge prévue de chaque achat, coefficient conseillé, alerte si la trésorerie de l'entreprise est tendue |
| **Pendant la vente** | Hausse de prix sur ce qui part vite, promotion sur ce qui ne part pas, relance d'une publicité qui a bien marché |
| **Fin de période** | Écouler le stock restant : quels articles, à quel prix, pour quelle marge |
| **Clôture** | Bilan de la période et leçons à reprendre pour la même période l'année suivante |

### Une vision globale, même là où l'écran ne l'affiche pas

Le guide lit toute l'entreprise, y compris ce que la gestion n'affiche pas ou seulement en lecture : locations, abonnements, frais fixes, trésorerie. Ces données restent hors du résultat de période. Elles servent à ajuster le conseil, par exemple : « la période est bonne, mais la trésorerie ne permet pas de nouvel achat avant le mois prochain ». Les salaires n'y entrent qu'en total, jamais par personne.

### Forme d'un conseil

Chaque conseil donne une action, son effet estimé en argent, son niveau de confiance et ses sources. Après application, le guide mesure ce qui s'est passé : « hausse de 5 % appliquée le 12 : marge +340 $ ». Les règles de `Conseiller IA` restent valables : le backend calcule tous les chiffres, l'IA les explique et propose, rien ne s'applique sans confirmation du gérant, aucune donnée personnelle.

### Volet publicité

**Nouvelle dépense « Campagne publicitaire »** : canal (Meta, Google, affichage, radio, influence…), dates, montant, articles ou commandes visés si le gérant le souhaite. Elle se classe dans sa période par sa date, compte dans les frais du résultat, et suit la règle des périodes clôturées.

**Rentabilité perçue après chaque campagne :** ventes et marge pendant la campagne, comparées à une période de référence de même durée juste avant, et montant gagné pour 1 $ investi.

**À afficher honnêtement :** c'est une **estimation**. La saison ou une autre campagne peuvent expliquer une hausse. Des campagnes qui se chevauchent sont mesurées ensemble.

**Plus tard :** liens de suivi sur l'e-commerce et connexion aux régies publicitaires pour une mesure plus précise. On commence par la saisie à la main.

### Ordre conseillé

1. Périodes : P1, P2, P3.
2. Volet publicité, simple et utile tout de suite.
3. Prévisionnel de commande et simulateur de coefficients et de promotions.
4. Guide IA, qui a besoin de tous ces chiffres pour donner de vrais conseils.

## Budgets de période, simulateur, lecture des factures (décisions du propriétaire, 10/11/2026)

Feu vert donné pour le volet publicité, le prévisionnel avec simulateur, et le guide IA.

### Budgets et simulateur

Avant ou pendant une période, le gérant répartit son budget : **stock, voyage, frais de port, publicité, autres**. L'écran compare ensuite le prévu au réel.

- **À la main**, ou **par un questionnaire court** qui prérempli les mêmes champs : trésorerie disponible, objectif de ventes, voyage prévu ou non.
- **Le simulateur** part des données en cours (stock restant valorisé, ventes de la même période l'an dernier, rythme de vente) et montre, quand on bouge un budget, un coefficient ou une promotion : ventes prévues, marge, résultat, point mort, trésorerie après achats. Rien n'est enregistré tant que le gérant ne valide pas.
- **Le guide IA** commente ces chiffres et propose une répartition. Il ne calcule rien lui-même.

**Point à savoir :** le backend ne connaît pas le solde bancaire. La trésorerie est donc saisie par le gérant, datée, et une liaison bancaire pourra venir plus tard.

### Lire une facture pour créer les articles et les notes de frais

Un seul service de lecture de documents, deux usages :

- **Facture fournisseur → commande et articles** (voir `perimetre-mobile-web.md`) ;
- **Facture ou reçu → note de frais** : montant, date, type proposé, taxes.

Le gérant vérifie toujours avant création, le total calculé est comparé à celui du document, le document reste attaché comme justificatif, et le classement dans la période se fait par la date. La ré-association d'articles reste utile pour les exceptions.

### Ordre retenu

1. Lecture des factures (articles, notes de frais) : le plus grand gain de temps au quotidien.
2. Volet publicité.
3. Budgets et simulateur.
4. Guide IA, qui s'appuie sur tout le reste.

## Vignettes d'accueil (décision du propriétaire, 10/11/2026)

Trois vignettes au plus sur l'écran d'accueil, triées par urgence. Chacune mène au bon écran, et le gérant peut l'écarter.

| Famille | Exemples | Source |
|---|---|---|
| Premiers pas | « Lisez votre première facture : le stock se créera seul », « Photographiez vos reçus » | Règles simples, sans IA |
| Alertes chiffrées | Stock bas sur des articles qui se vendent, fonds de roulement bas, dépenses au-dessus du prévu, budget journalier du voyage qui fond | Calculées par le backend |
| Conseils | Formulés et classés par l'IA à partir des chiffres, avec leurs sources | Guide IA (G1) |

Première version : premiers pas et stock bas. Le reste suit les budgets (B1), qui gagnent une notion de **voyage** (dates, lieu, budget total et journalier), et le guide IA (G1).

**À trancher :** notifications sur le téléphone, réservées aux alertes vraiment urgentes.
