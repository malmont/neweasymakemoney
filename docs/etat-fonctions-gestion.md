# Gestion : fonctions livrées et décisions (état au 10/11/2026)

Ce document récapitule ce que l'application de gestion (MAUI) sait faire, la route backend qui le porte, et les décisions du
propriétaire qui en fixent les règles. Les contrats détaillés sont dans les fiches du backend (`docs/*-gestion.md` du dépôt Symfony).

## Règles communes

- Montants en cents, dans la devise du site ; devise étrangère avec taux pour les fournisseurs, campagnes et voyages.
- Une période fermée est en lecture seule (409 `PERIOD_CLOSED`).
- Classement par date : une dépense compte dans la période qui contient sa date.
- Toute écriture qui touche la boutique ou la caisse (prix, promotions) passe par un aperçu, une confirmation, un journal et une annulation.
- L'IA ne calcule rien : le backend chiffre, l'IA rédige et classe ; un texte qui cite un chiffre absent des sources est rejeté.
- Les textes du backend sont demandés en français (`locale=fr`), quelle que soit la langue du téléphone.
- Essais sur le site demo seulement ; les articles d'exemple (`is_demo`) ne sont jamais modifiés par un essai.

## Écrans livrés

| Écran (accès) | Ce qu'il fait | Backend |
|---|---|---|
| Accueil : vignettes | 3 vignettes au plus (premiers pas, stock bas, trésorerie, budget, voyage, conseil du guide), écartables | `gestion/accueil/vignettes` |
| Accueil : Guide de la période | Conseils chiffrés, synthèse de l'IA, « C'est fait » avec le pourcentage appliqué, mesure sur 14 jours | G1a, G1b-1 (`gestion/conseils`) |
| Tableau de bord d'une période | Résultat, budget global, Budget par poste, Simuler la période | P2, B1a |
| Budget par poste | Prévu contre réel : stock, voyage, port, publicité, autres ; reprise de l'an dernier | B1a (`periodes/{id}/budget`) |
| Simuler la période | Prix, promotion, publicité ; titres en lien vers leur définition ; Avis du guide et application au budget | B1b-2, G1b-2 |
| Gestion › Trésorerie | Solde du compte saisi, mouvements prévus, projection 90 jours, ventes à venir comptées ou non, seuil | B1b-1 (`gestion/tresorerie`) |
| Gestion › Voyages d'achat | Dates, budget total et par jour, devise ; suivi jour par jour ; notes et commandes rattachées | B1a (`gestion/voyages`) |
| Gestion › Publicité | Campagnes, rentabilité, relance préremplie | A1 |
| Gestion › Marchandise à recevoir | Lignes d'achat non reçues par commande, « Tout recevoir » | R1 (`commandes/lignes/a-recevoir`) |
| Gestion › Promotions | Promotions datées en cours et à venir, « Terminer maintenant » | P5 (`gestion/promotions`) |
| Gestion › Changements de prix | Journal des prix changés, annulation pendant 24 h | P4 (`gestion/prix`) |
| Changer les prix | Pourcentage ou coefficient sur le coût, arrondi, aperçu ; depuis un conseil, une commande ou un article | P4 |
| Créer une promotion | Remise, dates (90 jours au plus), aperçu ; depuis un conseil ou un article | P5 |
| Notes de frais, commandes | Action « Voyage » pour rattacher ou détacher à la main | B1a (`gestion/voyages/rattacher`) |
| Statistiques | Cartes (ventes, marge, taux de marque, commandes, panier, retours, valeur du stock) comparées à la plage précédente et à l'an dernier ; plage aujourd'hui, 7 jours, mois, année (exercice), période, dates ; canal ; graphique avec l'an dernier ; les 5 anciens écrans détaillés restent jusqu'à S2 | S1 (`gestion/statistiques`) |
| Statistiques › L'année de l'entreprise | Résultat de l'entreprise, fin d'exercice estimée, point mort, ce qui manque, mois par mois, leviers chiffrés, périodes ; exercice terminé : bilan, « ce qui aurait suffi », avis du guide | AN (`gestion/statistiques/annee`) |
| Statistiques › Locations | Revenu à la vente et à la date de location, occupation par jour et par article, jours pleins et creux ; écran des conseils de location | LOC1, LOC2 |
| Gestion › Exercice comptable | Mois de début de l'exercice, par site | EX (`gestion/reglages/exercice`) |
| Gestion › Frais fixes | Total par mois et par catégorie, actifs, à venir, terminés ; case « Frais fixe » des mouvements ; résultat après frais fixes (période, simulateur, préparation) | FF1, FF2 |
| Gestion › Trésorerie : rapprochement | Écart entre le solde saisi et l'attendu, explication ou écart non justifié | RAP |
| Boîtes de dialogue | Au style de l'application (carte arrondie, Inter, couleurs du thème, bouton rouge pour supprimer), en surimpression sans recharger la page | application |

## Décisions du propriétaire (10/11/2026)

- Budget d'un voyage : il couvre les frais (déplacement, hôtel, repas) ; les achats de stock restent dans le poste Stock.
- Guide IA : statut fait / écarté porté par le site ; langue du site ; génération au premier affichage du jour.
- Promotions : une seule par article (mécanisme existant) ; le prix normal n'est jamais touché ; variantes à prix propre exclues pour
  l'instant (une règle de prix par variante est à l'étude avec le backend, activée seulement quand toutes les caisses la savent appliquer).
- Fuseau des promotions saisies dans l'administration : à corriger (lot P5b), avec décalage des 3 promotions d'exemple existantes.
- Préparer une période entière : validé (lot P6), y compris les achats et le voyage faits avant la période.
- Relecture du code backend par Fable : en attente de la décision du propriétaire.

## Décisions du propriétaire (11/11/2026)

- Exercice comptable réglable par client (pas seulement l'année civile).
- Anciens écrans de statistiques : retirés dès qu'un écran unifié les remplace (Chiffre d'affaires, Panier, Achats retirés ; Caisse,
  Paiements, Stock, Frais, Total frais remplacés par S2, qui gagne les frais de port par transporteur).
- Assistant IA « Faire le point » validé (lots IA) : bilan de ce qui manque ou ne colle pas, questions posées par l'IA, aperçu, validation
  et annulation ; rien n'est modifié sans l'accord du gérant ; l'IA formule, l'application calcule. Il reprend et remplace l'assistant
  « Point de départ » (DEP). Emplacement : vignette en tête de l'Accueil tant que la configuration n'est pas complète, entrée
  permanente dans Gestion ; une fenêtre explique avant de commencer que des données vont être mises à jour.
- Relevés bancaires : import PDF, CSV et OFX (solde, prélèvements récurrents proposés en frais fixes, dépenses manquantes proposées en
  notes de frais, rapprochement) ; le PDF est supprimé après lecture, seules les lignes extraites sont gardées, numéro de compte masqué.
- Import des relevés : **au moins 3 relevés** demandés (récurrences et enchaînement des soldes) ; un seul accepté, avec un avertissement
  (solde et dépenses à jour, frais fixes non détectés avec certitude).
- Dossier de fin d'exercice pour le comptable (appli **web**) : les 12 relevés de chaque compte, soldes d'ouverture, inventaire de
  clôture, emprunts, taxes ; jauge de complétude, export PDF et tableur, clôture et réouverture tracée ; l'application prépare, seul le
  comptable valide. Cahier des charges : `docs/web/dossier-fin-exercice.md`.
- Module de gestion **payant et activable** par site, **web et mobile**, comme les autres modules ; l'IA de la gestion consomme les
  **unités IA mensuelles** du site comme dans les autres modules.
- Appli de gestion **web** : clone complet de l'application mobile, en plus complet, espace à plusieurs modules (gestion, support boutique,
  autres) ; IA au cœur (console IA, assistant comptable, apprentissage du chef d'entreprise). Vision et lots : `docs/web/appli-web-gestion.md`.
- Connexion directe aux banques par un agrégateur : **plus tard**, **depuis l'appli web uniquement** ; elle devra couvrir les banques **canadiennes et américaines**
  (pistes : Plaid pour le Canada et les États-Unis, Flinks pour le Canada, MX ou Finicity pour les États-Unis ; à choisir sur coût,
  couverture des banques des clients, sécurité et consentement). Elle remplacera seulement le fichier importé : même calcul derrière.

## Lots en cours ou à venir

| Lot | Contenu | État |
|---|---|---|
| P5b | Fuseau des dates de promotion (administration, PATCH) | contrat validé, en code |
| Statistiques B1–B7 | Valeur du stock par variantes en cents, paiements échoués exclus, panier moyen, commandes, transporteurs, caisse | validé, après P5b |
| P6 | Préparer une période (estimation des ventes, achats × coefficient, voyage, trésorerie, avis, valider) | contrat validé, après P5b |
| Variantes en promotion | Règle de prix par variante, bascule par site | à cadrer |
| LOC | Onglet Location : occupation, revenu, jours creux et pleins par article ; conseils (hausse ciblée, promotion en semaine, baisse) ; promotion datée ou changement de grille ; projection et mesure ; jamais une réservation déjà faite | validé le 10/11, contrat demandé (après P6) |
| ABO | Onglet Abonnements : revenu mensuel récurrent, actifs, nouveaux, départs, revenu par abonné, valeur sur la durée, échecs de paiement ; conseils ; nouveau tarif **pour les nouveaux abonnés seulement** (Stripe), offre de bienvenue ; projection 3, 6, 12 mois | validé le 10/11, après LOC |
| Ensuite | Réservations futures dans la trésorerie et « Préparer une période » ; vignettes sur la dérive de l'occupation ou des départs | à cadrer |
| Plus tard | Forfaits location + vente ; saisonnalité des locations ; relance des anciens abonnés | idées retenues |
| Lexique | Chaque terme technique de l'application (marge, taux de marque, point mort, coefficient, prévu, solde, seuil, occupation…) devient un lien vers sa définition, à partir d'un lexique unique | demandé le 10/11, en cours côté app |
| FF | Frais fixes mensuels : saisie sur les mouvements prévus, équivalent mensuel, résultat de période « après frais fixes », point mort et guide | validé, après RAP (écran Frais fixes déjà dans l'application) |
| AN | Statistiques › Année : mois par mois et cumul, résultat de l'entreprise (avec locations, abonnements et frais fixes), point mort annuel, ce qui manque, leviers chiffrés, bilan de fin d'année et leçons | validé le 10/11, après S1 et FF2 |
| IA1 | Bilan de configuration : ce qui manque ou ne colle pas (solde absent ou ancien, aucun frais fixe, notes qui ressemblent à des frais fixes, articles sans coût, périodes sans budget, écarts bancaires, stock négatif, exercice par défaut…), pourcentage, vignette d'Accueil | validé le 11/11 (remplace DEP), contrat à demander |
| IA2 | Assistant : questions de l'IA, aperçu, application tout ou rien, journal, annulation | validé le 11/11, après IA1 |
| IA3 | Import de relevés bancaires PDF, CSV, OFX, QFX (au moins 3 demandés) : solde, frais fixes, dépenses, rapprochement ; dépôt de plusieurs fichiers sur le web | validé le 11/11, après CLO |
| CLO | Clôture du mois et de l'exercice ; dossier de fin d'exercice (web, `docs/web/dossier-fin-exercice.md`) | validé le 11/11 |
| IA4 | Connexion directe aux banques (agrégateur, Canada et États-Unis), depuis le web uniquement | plus tard |
| Ordre backend | ~~RAP, FF1, FF2, S1, AN~~ (livrés), LOC (LOC1 et LOC2 livrés, LOC3 en développement, LOC3 en ligne sur feu vert du propriétaire), IA1, IA2, ABO, REG, INV, CLO, IA3, S2, S3 | fixé par Claude avec l'accord du propriétaire |
| Mise à niveau | Rapprochement bancaire (livré), assistant IA (lots IA), régularisations, inventaire, clôture du mois | validé |
| Notifications | Inscription du téléphone et réglages | attend Firebase (nom de l'application) |
