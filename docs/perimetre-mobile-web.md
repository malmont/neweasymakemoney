# Gestion : ce qui va sur le mobile (MAUI) et sur le web (Next)

Orientation du propriétaire du 09/11/2026 : **le web contiendra tout**. Le mobile est l'application du terrain : voir vite et faire des choses à la
main. Proposition du chef de projet, à corriger par le propriétaire.

## Répartition

| Sujet | Mobile (MAUI) | Web (Next) |
|---|---|---|
| Pilotage | Écran court : chiffres clés, à traiter, alertes | Complet, avec comparaisons et classements |
| Conseiller IA | Cartes de conseil, résumé de la semaine | Complet, question libre, analyses |
| Collections et commandes | Consulter, créer, modifier | Idem, plus la gestion en masse |
| **Facture fournisseur → articles** | **Oui : photo de la facture** | Oui, avec import de PDF |
| **Photos des articles, améliorées par l'IA** | **Oui : appareil photo** | Oui |
| Stock | Consulter, inventaire au scan | Analyses, réassort, stock dormant |
| Fournisseurs, notes de frais, frais de port | Saisie rapide, photo du justificatif | Complet |
| Statistiques détaillées | Une sélection courte | Toutes |
| Simulateur de commande | Version rapide | Complet |
| Bilan et comptabilité, assistés par l'IA | Non | Oui |
| Support, assisté par l'IA | Notifications et réponses courtes, plus tard | Oui |

**Conséquences :**
- les règles et les calculs vivent dans le backend, pour que les deux applications affichent les mêmes chiffres ;
- les écrans de statistiques détaillées restent sur le mobile tant que le web n'existe pas. On les allégera ensuite.

## Nouvelle fonction : lire une facture et créer les articles

Parcours sur le mobile, depuis une commande :

1. **Photographier la facture** (plusieurs pages possibles) ou choisir un PDF.
2. **Le backend extrait, avec l'IA** : fournisseur, date, numéro, lignes (désignation, quantité, coût unitaire), totaux, taxes, devise.
3. **Écran de vérification** : chaque ligne est modifiable. L'écran **compare le total calculé au total de la facture** et signale tout écart.
4. **Le gérant confirme**, et le backend crée les articles dans la commande, en centimes.
5. **Ajouter les photos** des articles : elles alimentent la fiche produit par IA (`lot-5-fiche-produit-ia.md`), qui propose le nom, la description et les variantes, et améliore l'image quand ce sera branché.

**Règles :**
- rien n'est créé sans confirmation du gérant ;
- la facture d'origine reste attachée à la commande, comme justificatif ;
- un article déjà connu est proposé au rattachement plutôt que recréé (route de rattachement déjà livrée) ;
- crédits IA du backend, avec 402 `AI_CREDITS_EXHAUSTED`.

**À instruire avec le backend :** route d'extraction, format des lignes, tailles et couleurs quand la facture les détaille, taxes, conservation du fichier.

## Choisir où un article est vendu (demande du propriétaire, 09/11/2026)

Après la création d'un article, depuis une commande ou non, le gérant décide où il est en ligne : **site web, application mobile, caisse**.
Disponible sur le mobile comme sur le web, en lot si possible.

**Relevé dans le backend (`origin/dev`) :**
- `Product` porte déjà `isWeb`, `isPos` et `isLandingPage` ;
- `POST /api/products/ai/create` accepte `channels {web, pos, landing}` ;
- `POST /api/commandes/{id}/products` et la route de rattachement ne reçoivent pas ces canaux ;
- `PATCH /api/products/{id}` existe (API Platform) mais sans contrôle dédié : **à ne pas utiliser**.

**À demander au backend :** une route dédiée, par exemple `PATCH /api/products/{id}/channels {web, pos, landing}` et sa version en lot, à ajouter sans
toucher à l'existant. **À confirmer aussi :** l'application e-commerce mobile suit-elle `isWeb`, ou faut-il un canal mobile distinct ?

**Rappel : le web fait tout**, y compris les articles sans commande et la création d'articles à partir d'une commande.

## Ordre recommandé

1. Finir l'interface, le lot 3 et l'adaptation aux livraisons G1 à G4.
2. **Saisie sur le terrain** : facture → articles, justificatifs de voyage, photos et fiche IA (lot 5).
3. **Prévisionnel de collection** et simulateur de coefficients (voir `propositions-pilotage.md`), puis les frais fixes.
4. Pilotage mobile et Conseiller IA.
5. Version web, avec bilan, comptabilité et support.

## Lecture des documents : décisions du propriétaire (10/11/2026)

Trois types de documents, lus par l'IA, jamais créés sans validation :

| Document | Vérification | À la validation |
|---|---|---|
| **Facture fournisseur** | Une carte par produit : photo, nom, quantité, coût hors taxes, prix de vente, taille, couleur, nouvel article ou déjà connu | Commande classée par sa date, articles créés (invisibles en boutique) ou rattachés |
| **Note de frais** | Montant, date, type, description ; la photo du reçu sert de justificatif | Note de frais classée par sa date |
| **Facture de transport** | Transporteur, montant, date, numéro de suivi | Frais de port de **la commande choisie**, une facture pour une commande |

**Règles de l'écran :** lignes douteuses en premier, compteur de lignes validées, bouton « Tout valider », correction possible sur chaque ligne, travail sauvegardé ligne par ligne, photo par article facultative.

**Lecture en arrière-plan :** le téléphone envoie le document, puis récupère le résultat, pour tenir sur un réseau faible. Facture datée dans une période clôturée : « enregistrer dans la période en cours » ou « rouvrir ». Les tailles et couleurs d'un même modèle forment un seul article avec ses variantes.

Ordre des lots backend : L1 lecture des documents, A1 publicité, B1 budgets et simulateur, G1 guide IA, chacun avec son contrat avant le code.

## Lignes d'achat et réception (avis de la session backend de la caisse, retenu le 10/11/2026)

**Constat :** un article n'appartient qu'à une commande fournisseur, avec un seul coût. Un réassort du même article à un autre prix est impossible, et la lecture de factures buterait dessus. Par ailleurs, les achats n'alimentent pas le stock de la caisse et de la boutique.

**Nouvel ordre backend :** L0 lignes d'achat, fin de L1 (confirmation d'une facture), R1 réception, puis A1 publicité, B1 budgets et simulateur, G1 guide IA. Le reste de L1 ne dépend pas de ce changement.

- **L0 :** une ligne par achat (commande, article, variante facultative, quantité commandée et reçue, coût). Coût d'une vente proposé : moyenne pondérée glissante à la date de la vente.
- **R1 :** réception explicite, jamais automatique à la confirmation d'une facture, qui crée les mouvements de stock par variante.

**À confirmer par le propriétaire :** variante facultative sur la ligne ; réception non obligatoire pour figer une période ; sort de `product.quantity`.

**Plus tard :** carte « Gestion » dans le back-office, guide d'écran illustré.

**Livré :** L0 le 10/11/2026 (`dev` 48c1a720, vérification sans écart sur les 10 sites) ; application adaptée : réassort depuis « Ajouter des articles existants » (tout le catalogue, quantité et coût saisis). L1 lit les reçus et factures de transport (première lecture réelle réussie sur demo). R1 et la confirmation des factures fournisseur sont en cours, livrés ensemble : stock à la réception seulement, réception en un geste avec `needsVariant`.
