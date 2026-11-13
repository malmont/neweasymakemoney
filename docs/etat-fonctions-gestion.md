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
- **Conformité par pays** (11/11/2026) : la gestion et la comptabilité s'adaptent à la **France, au Canada et aux États-Unis**, tous les
  trois. Un profil pays par site (pays, province ou État, régime de taxes) pilote les taxes (TVA ; TPS, TVQ, TVH, TVP ; sales tax),
  le plan de comptes (PCG en France), les exports (FEC en France, formats QuickBooks et Sage ailleurs), les mentions obligatoires et les
  échéances. Les règles sont des données datées et versionnées, et chaque paquet sera validé par un comptable du pays (que le propriétaire
  trouvera). Points lourds pour la France : facturation électronique et **certification NF525 de la caisse** (procédure du propriétaire
  auprès d'un organisme agréé). Lot PAYS : audit de l'existant d'abord.
- **Module Assistance (support boutique)** (11/11/2026), cadrage backend `docs/assistance.md` (07/11) : boîte de demandes, entrées
  (formulaire du site, courriel, « Signaler un problème » de la caisse), IA assistante du répondeur, puis chat client borné. Placé
  **après MOD**. Choix : adresse de réception fournie par la plateforme (le commerçant y redirige sa boîte) ; module payant à part dont
  l'IA consomme les unités mensuelles du site ; délai de réponse affiché réglable par site (48 h ouvrées par défaut) ; demandes conservées
  3 ans puis anonymisées (Loi 25, RGPD). Onglet « Assistance » dans l'appli de gestion mobile et web (module `/admin/support`).
  **Rôle « Assistance »** (décision du 11/11) : un compte de support n'accède qu'au module Assistance (demandes, commandes et ventes liées,
  actions de support confirmées) ; il ne voit ni la gestion, ni les prix, ni la trésorerie, ni les réglages du site.
- **Promotion par variante** (11/11/2026) : feu vert pour cadrer la règle de prix par variante (variantes à prix propre comprises),
  activable par site et activée seulement quand la caisse, la boutique Next et l'application mobile savent l'appliquer. Contrat backend
  d'abord, puis plans des trois fronts.
- Connexion directe aux banques par un agrégateur : **plus tard**, **depuis l'appli web uniquement** ; elle devra couvrir les banques **canadiennes et américaines**
  (pistes : Plaid pour le Canada et les États-Unis, Flinks pour le Canada, MX ou Finicity pour les États-Unis ; à choisir sur coût,
  couverture des banques des clients, sécurité et consentement). Elle remplacera seulement le fichier importé : même calcul derrière.

## Décision du propriétaire (12/11/2026) : compte Assistance sur le web seulement

Le compte Assistance (ROLE_SUPPORT) ne sert que dans l'**appli web**, où il n'accède qu'à la partie Assistance, jamais à la gestion.
L'**appli de gestion mobile reste réservée aux administrateurs** (et au super administrateur) : elle refuse un compte Assistance à la
connexion (test `MobileRolesTests`).

L'onglet Assistance de l'appli mobile (administrateurs : liste, fiche, réponse, note, état, priorité, création) est **gardé tel quel et
gelé** : aucune nouvelle fonction d'assistance sur le mobile. La suite de l'Assistance (réponses types, ouverture depuis une commande,
IA du répondeur, chat) se fait sur le **web**.

## Revue du 12/11/2026 : améliorations et idées retenues par le propriétaire

Améliorations de l'existant :
- **Essais à l'écran** : les écrans mobiles du 11 et du 12/11 (variantes, « non activée », « Préparer ma gestion », Assistance, fenêtres
  nettoyées) ne sont vérifiés que par les tests. Séance d'essai sur le téléphone à faire, simulateur à réparer.
- **Mode dégradé sans réseau** : garder la dernière réponse de l'Accueil et des statistiques, avec « données de telle heure ».
- **Anglais dans l'appli mobile**, à partir du lexique servi par le backend (LEX).
- **Une seule façon d'afficher une fenêtre** : remplacer les fenêtres natives restantes (saisie d'un nombre, listes de choix) par celles
  de l'appli.
- **Mise en ligne backend par script de contrôle** (cache des interrupteurs compris), toujours sur le go du propriétaire.
- **Ordre backend** : après PAYS-1c et IMGHOST, IA1 et IA2 passent avant PAYS-2 à PAYS-8.
- **Notifications** : toujours en attente de Firebase.

Idées de fonctions retenues (à cadrer une par une, contrat backend d'abord) :

| Idée | Contenu |
|---|---|
| Ma journée | Sur l'Accueil : ventes du jour, commandes à expédier, demandes d'assistance en retard, solde de trésorerie |
| Résumé hebdomadaire | Courriel du lundi : ventes, marge, trésorerie, 3 actions conseillées |
| Assistance depuis une commande (web seulement) | Bouton « Ouvrir une demande » sur une commande, client et commande préremplis |
| Réponses types (web seulement) | Modèles de réponse dans l'Assistance, rédigés plus tard par l'IA (lot 3 du backend) |
| Objectifs | Objectif de ventes ou de marge par période, avance ou retard, ce qu'il faut vendre par jour |
| Réapprovisionnement conseillé | Quantité et date de commande par article, brouillon de commande fournisseur |
| Fiche client | Achats, retours, demandes d'assistance, valeur totale |
| Journal d'activité | Qui a changé un prix, lancé une promotion, annulé une vente |
| Rôle comptable | Lecture seule : statistiques, exports, dossier de fin d'exercice, documents |

Publication sur les stores (Play Store, App Store) : **un lot commun à toutes les applis mobiles de la suite**, plus tard (décision du
12/11/2026). Pas de travail de publication propre à l'appli de gestion d'ici là.

À reparler avec le propriétaire (jugé pertinent, non décidé) : site de démonstration réinitialisé chaque nuit ; parcours d'accueil d'un
nouveau client déroulé de bout en bout.

**Lot IA élargi (décision du propriétaire du 12/11/2026)** : l'IA met à jour les données elle-même en posant ses questions (facture en
pièce jointe enregistrée, commande remplie à partir d'une facture, et le maximum de cette façon) ; un **guide des séquences** montre
l'avancement global (plusieurs séquences, sur le modèle de « Préparer ma gestion » / readiness : activer un module, préparer une période,
clôturer l'exercice…) ; l'IA fait avancer ces séquences étape par étape. Règle gardée : l'IA prépare, le serveur calcule, chaque écriture
passe par un aperçu, la confirmation du gérant, le journal et l'annulation. Constat du backend reçu ; découpage validé par le propriétaire :
**IA-SEQ** (guide des séquences, sans IA : séquences uniques depuis readiness, séquences récurrentes avec instances, résumé global avec la prochaine chose à faire ; affiché sur le web seulement, le mobile garde « Ma journée », « Préparer ma gestion » et le guide de la période), **IA-FACT** (facture par
conversation : questions, aperçu avec empreinte, annulation d'un bloc ; au choix nouvelle commande ou ajout à une commande ouverte du même
fournisseur), **IA-ACT** (autres actions préparables), **IA-AVANCE** (l'IA fait avancer les séquences). Unités : 1 par réponse de l'IA ;
lecture d'un document 1 ; questions calculées gratuites. ERREURS-3 : chaque module dans sa propre session.

**Comptes et applications (décision du propriétaire du 13/11/2026)** : une seule base de comptes, des **rôles d'équipe par application**
(biographe Mémoires Vivantes, conseiller Boussole ; administrateur, caisse et assistance inchangés) et une **appartenance des clients aux
applications** (liste par compte : application, date, source, conditions acceptées). Première connexion d'un compte existant à une autre
application : étape « Rejoindre » avec ses conditions (Loi 25), jamais d'ajout silencieux ; reprise de l'existant d'après l'historique.
Lots backend : MV-BIOGRAPHE (memoiremobile backend), BOUSSOLE-CONSEILLER puis APPS-MEMBRES (appli degestion backend).

À reparler aussi (idée du propriétaire du 12/11/2026, non décidée) : **démarque et vols**. Avis de Claude, par ordre :
1. sans matériel : rapport d'anomalies de caisse calculé par le serveur (remboursements sans ticket, annulations, remises et prix
   manuels, ventes sous le coût, réimpressions, écarts au Z, tiroir ouvert sans vente), résumé rédigé par l'IA, et comptage du stock au
   téléphone qui donne la démarque par article ;
2. RFID UHF sur les vêtements (inventaire en minutes, lecteur à la porte, puce marquée vendue par la caisse), en essai sur une boutique ;
3. caméras avec IA plus tard, par un partenaire (Loi 25, transparence envers les employés).
Toujours « anomalie à vérifier », jamais une accusation. Questions ouvertes : portiques antivol déjà en place ? nombre d'articles par
boutique ?

## Campagne d'essais avant le premier client

Décision du propriétaire (12/11/2026) : tout lot validé part en production sans attendre ; les essais réels se font en une campagne
avant de mettre un premier client en production, le propriétaire se connectant lui-même. Le 13/11/2026 : un **nouveau site dédié aux
essais** sera créé pour cette campagne, avec sa documentation des essais (la démo n'est pas modifiée pour cela). Liste à tenir à jour :
- Caisse (posflutter, main 92ca9db), en lecture seule : LOC3 et VAR (catalogue, panier, récapitulatif, ticket avec promotions de
  location et par variante) ; PAYS-1 et PAYS-1c (ancien reçu avec taux depuis ratePpm, pack v4,v5 → pos-offline-v5) ; PAYS-1b
  (vente annulée « ANNULÉE le … — avoir n° … ») ; REIMP (affichage purpose=display, aucun POST /reprint, 5 reçus au plus, contrôle du
  compteur côté serveur) ; menu latéral en paysage : libellés entiers à l'œil avec la vraie police (« Statistiques des caissiers »,
  « Mon code PIN », « Tiroir-caisse »).
- Appli mobile de gestion : séance sur le téléphone (variantes, « non activée », « Préparer ma gestion », Ma journée, fenêtres de
  l'appli, hors connexion des statistiques et de la trésorerie, formulaires à liaisons compilées).
- Boussole ESG web : passage d'un compte à l'autre et deux onglets, avec deux comptes d'essai, sur le site d'essai (module activé).
- Mémoires Vivantes web : restauration d'un brouillon après reconnexion ; menu « Biographe » de la fiche du livre ; bouton de rôle de la
  console Équipe ; parcours complet avec un compte **biographe pur** (sans ROLE_ADMIN), à créer sur le site d'essai.
- Site web commun : deux onglets (connexion du même compte sans rechargement ; bandeau de l'éditeur avec brouillon), avec une session admin ;
  hydratation de la langue sur un site réglé en anglais.
- Caisse : POS-CLIENT-WEB (compte web trouvé par courriel exact → fiche créée et rattachée), écritures sur un compte de test.
- Web : essais déjà faits lot par lot sur demo ; revue d'ensemble à prévoir. W1i (modifier fournisseur, transporteur, frais de port),
  poussé sans essai : téléphone d'un fournisseur, contact de Nationex, numéro de suivi d'un frais de port, chacun aller-retour ;
  transporteur « ESSAI W1i » créé, modifié puis supprimé ; relevé avant et après. Reprise d'historique (W1g) : appliquer et annuler sur un
  site où elle a un sens, après le lot backend REPRISE-FIGE (l'annulation doit restaurer les résultats figés des périodes fermées).
- Facture par conversation (IA-FACT-1 et 2, mobile main 41dd411, puis web) : envoi d'une vraie facture (1 unité d'IA), réponses
  (fournisseur proposé, cible « nouvelle commande » ou commande ouverte, frais de port ajouté ou non), confirmation, puis annulation d'un
  bloc et relevé avant et après ; ajout de deux factures à une même commande, puis annulation dans l'ordre inverse (LATER_DOCUMENT) ;
  phrase libre (IA-FACT-2-B) avec une réponse refusée par le serveur.

- Clés de connexion (JWT) suivies par le dépôt du backend (`jwt/private.pem`, `jwt/public.pem`), relevé du 13/11 : à retirer du suivi et à remplacer par de nouvelles clés pendant la campagne d'essais globaux (décision du propriétaire). Effet : tout le monde est déconnecté une fois.
- Console IA (W2, en ligne le 13/11) : premières questions réelles faites sur demo le 13/11 ; à refaire sur le site d'essai, avec la mesure des réponses de repli.

**Avant l'ouverture, côté serveur.** Bascule du 13/11 (IA-FACT-2-A) : une requête encore en traitement au moment de l'échange du cache
a cherché un fichier de l'ancien cache (aucune réponse en erreur, c'était la requête de contrôle). Correction demandée au backend : garder
l'ancien cache lisible quelques instants après l'échange ; à vérifier avant le premier client.

**Lot IA, suite (décisions du propriétaire du 13/11/2026)**
- **Périmètre :** l'IA de la console (fil, actions, analyses) sert uniquement la gestion ; EasyAdmin couvre les autres modules. Le prix de vente et la mise en ligne d'un article font partie de la gestion. Restent dehors la description, les photos, les comptes, les rôles, les réservations, les livres et les biographes.
- **En ligne le 13/11 :** IA-FACT-1, 2-A et 2-B (facture par conversation, ajout à une commande ouverte, phrase libre) ; IA-ACT-1 à 3 (note de frais, fournisseur, frais de port, budget, solde et mouvement de trésorerie, voyage, exercice, découpage des périodes, coût d'achat). Écrans : mobile (facture) et web (IA-CONV-WEB : fil, liste « En cours », points d'entrée).
- **Ordre suivant :**
  1. IA-ACT-4 : clôture d'une période avec annulation exacte.
  2. IA-ACT-5 : prix de vente et mise en ligne.
     - Dans la facture : coefficient de la facture ou par ligne, prix d'un article déjà en vente gardé par défaut, mise en ligne « En caisse » et « Sur la boutique en ligne » (non, tout de suite, ou à la réception du stock).
     - L'action price.set : prix en masse par période, collection, catégorie ou liste, par coefficient, pourcentage ou prix saisi.
     - Arrondi psychologique réglable par site.
     - Coefficient proposé d'après les articles semblables de la même saison.
  3. Console IA générale (W2, DOC) : questions libres, analyses d'ensemble et rapports PDF « comme un spécialiste », sur les mêmes analyses nommées calculées par le serveur. Premier cas d'usage : le bilan des nouveaux articles (garder, monter ou baisser le coefficient, avec un pourquoi chiffré), puis price.set sur la sélection. Contrat demandé au backend le 13/11.
- **Après W2, web (souhait du propriétaire du 13/11) : menu par thèmes.** Les 26 onglets à plat deviennent des sections au nom explicite, avec des groupes repliables, une page d'accueil en cartes par thème, et des adresses inchangées.
  - Proposition du web, à valider à l'ouverture du lot : Tableau de bord, Achats et stock, Prix et ventes, Argent, Périodes et résultats, Réglages.
  - Mon avis : « Locations » (et plus tard « Abonnements ») vont dans Prix et ventes, pas dans Périodes et résultats. « Lire un document » sert aussi aux notes de frais : il vaut mieux en faire une action toujours visible (bouton « Lire un document » en haut, comme la console) que de le ranger dans Achats.
  - **Validé par le propriétaire (maquettes du web, 4 pages) :**
    - variante A (thèmes dépliables, accueil en cartes avec un chiffre clé, page par thème) ;
    - menu réductible d'un clic en barre d'icônes pour les grands tableaux ;
    - « Lire un document » et la console toujours en haut ;
    - chiffres des cartes servis par les routes existantes.
  - Plus tard, le mobile reprendra les mêmes 6 thèmes sur sa page « Gestion ».
  - Fait le 13/11, avant W2, à la demande du propriétaire : menu par thèmes en ligne (web 38a2c65, 1b57887) et choix clair / sombre (f214f75). Chiffres des cartes : seulement des totaux ou des champs rendus par les routes existantes. Après W2, une route de résumé des thèmes pourra donner les chiffres manquants (commandes, fournisseurs, locations, notes de frais…).
- **Partage des rôles :**
  - les vignettes du guide font les ajustements précis, un article ou un poste à la fois. Elles ne changent pas : ce qu'elles font est déjà bon, et les anciens articles continuent d'être traités par les promotions proposées ;
  - le coefficient ne concerne que les nouveaux articles. Les ventes des anciens articles semblables (saison terminée) ne servent que de référence pour le choisir, sans toucher à leur prix ;
  - la console fait les ajustements globaux, sur un ensemble (une période, une collection, une catégorie, un fournisseur, les nouveaux articles), par les actions en masse (price.set, puis les promotions de groupe), toujours avec aperçu, confirmation et annulation.

## Lots en cours ou à venir

| Lot | Contenu | État |
|---|---|---|
| P5b | Fuseau des dates de promotion (administration, PATCH) | contrat validé, en code |
| Statistiques B1–B7 | Valeur du stock par variantes en cents, paiements échoués exclus, panier moyen, commandes, transporteurs, caisse | validé, après P5b |
| P6 | Préparer une période (estimation des ventes, achats × coefficient, voyage, trésorerie, avis, valider) | contrat validé, après P5b |
| Variantes en promotion (VAR) | Règle de prix par variante, bascule par site (`shop.variant_promotions`) | **en ligne** (backend 32febeff) ; écrans dans l'appli, la caisse, Next et l'appli e-commerce ; essai réel sur demo à faire (interrupteur à allumer) |
| LOC | Onglet Location : occupation, revenu, jours creux et pleins par article ; conseils (hausse ciblée, promotion en semaine, baisse) ; promotion datée ou changement de grille ; projection et mesure ; jamais une réservation déjà faite | LOC1 à LOC4 **en ligne** et dans l'appli ; LOC5 après S2 |
| ABO | Onglet Abonnements : revenu mensuel récurrent, actifs, nouveaux, départs, revenu par abonné, valeur sur la durée, échecs de paiement ; conseils ; nouveau tarif **pour les nouveaux abonnés seulement** (Stripe), offre de bienvenue ; projection 3, 6, 12 mois | validé le 10/11, après LOC |
| Ensuite | Réservations futures dans la trésorerie et « Préparer une période » ; vignettes sur la dérive de l'occupation ou des départs | à cadrer |
| Plus tard | Forfaits location + vente ; saisonnalité des locations ; relance des anciens abonnés | idées retenues |
| Lexique | Chaque terme technique de l'application (marge, taux de marque, point mort, coefficient, prévu, solde, seuil, occupation…) devient un lien vers sa définition, à partir d'un lexique unique | demandé le 10/11, en cours côté app |
| FF | Frais fixes mensuels : saisie sur les mouvements prévus, équivalent mensuel, résultat de période « après frais fixes », point mort et guide | validé, après RAP (écran Frais fixes déjà dans l'application) |
| AN | Statistiques › Année : mois par mois et cumul, résultat de l'entreprise (avec locations, abonnements et frais fixes), point mort annuel, ce qui manque, leviers chiffrés, bilan de fin d'année et leçons | validé le 10/11, après S1 et FF2 |
| IA1 | Bilan de configuration : ce qui manque ou ne colle pas (solde absent ou ancien, aucun frais fixe, notes qui ressemblent à des frais fixes, articles sans coût, périodes sans budget, écarts bancaires, stock négatif, exercice par défaut…), pourcentage, vignette d'Accueil | validé le 11/11 (remplace DEP), contrat à demander |
| IA2 | Assistant : questions de l'IA, aperçu, application tout ou rien, journal, annulation | validé le 11/11, après IA1 |
| IA3 | Import de relevés bancaires PDF, CSV, OFX, QFX (au moins 3 demandés) : solde, frais fixes, dépenses, rapprochement ; dépôt de plusieurs fichiers sur le web ; **sur le mobile, relevé pris en photo** (appareil photo, galerie, PDF, comme la lecture des factures ; demande du propriétaire du 13/11/2026), la même fonction des deux côtés | validé le 11/11, après CLO |
| CLO | Clôture du mois et de l'exercice ; dossier de fin d'exercice (web, `docs/web/dossier-fin-exercice.md`) | validé le 11/11 |
| IA4 | Connexion directe aux banques (agrégateur, Canada et États-Unis), depuis le web uniquement | plus tard |
| MOD | Module de gestion payant et activable ; garde « management » (403 MODULE_INACTIVE, mode live seulement) ; unités IA (`GET /api/gestion/ia`) | **en ligne** le 11/11 (7fbd8ace) ; transition appliquée (demo accordé) ; appli : écran « non activée » et unités IA sur le guide (3c7ce57) |
| CHE | Cheminement de la gestion : 15 étapes (6 bloquantes, 9 conseillées) dans `/api/site/readiness` (avec `appScreen`, `figures`), page EasyAdmin `/admin/gestion` | livré en développement (e1d80fda), mise en ligne sur go du propriétaire ; appli : écran « Préparer ma gestion » poussé |
| ASSISTANCE lot 1 | Boîte de demandes côté agent (tickets, messages, notes internes, pièces jointes, délai ouvré réglable), ROLE_SUPPORT limité à `/api/support`, module payant `module.support`, anonymisation 3 ans après fermeture, cheminement | contrat validé le 11/11 ; **décisions du propriétaire attendues** : prix et essai de l'offre (Q1), adresse de réception et fournisseur de courriel entrant pour le lot 2 (Q2) ; aucun compte ROLE_SUPPORT avant que l'appli et le web sachent l'afficher |
| Ordre backend | ~~RAP, FF1, FF2, S1, AN, LOC1 à LOC4, VAR, DIR, MOD, B3 bis, ROUTEID, REIMP 1 et 2, langue des estimations~~ (livrés), ERREURS-1 (messages d'exception jamais renvoyés au client, accord du propriétaire le 12/11/2026 ; ERREURS-2 ensuite, ERREURS-3 Boussole et Mémoires à décider), REPRISE-FIGE (l'annulation d'une reprise restaure les résultats figés), constat des dates EasyAdmin en UTC, EDIT (modifier fournisseur, transporteur, frais de port), IA1, IA2, LEX, CAP, ABO, REG, INV, CLO, IA3, S2, S3 | fixé par Claude avec l'accord du propriétaire |
| Mise à niveau | Rapprochement bancaire (livré), assistant IA (lots IA), régularisations, inventaire, clôture du mois | validé |
| Notifications | Inscription du téléphone et réglages | attend Firebase (nom de l'application) |
