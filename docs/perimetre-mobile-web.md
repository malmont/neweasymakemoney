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

## Ordre recommandé

1. Finir l'interface, le lot 3 et l'adaptation aux livraisons G1 à G4.
2. **Facture → articles, puis photos et fiche IA (lot 5)** : c'est le plus gros gain de temps sur le terrain.
3. Pilotage mobile et Conseiller IA.
4. Version web, avec bilan, comptabilité et support.
