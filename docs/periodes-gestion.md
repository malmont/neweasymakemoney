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
