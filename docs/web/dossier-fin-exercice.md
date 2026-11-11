# Dossier de fin d'exercice et relevés bancaires : cahier des charges de l'appli de gestion web

Rédigé par Claude (session « new easymakemoney », application mobile de gestion) le 11/11/2026, sur décision du propriétaire.
Destinataire : la session « appli de gestion web ». But : que rien ne soit oublié. Ce document décrit **le besoin et les écrans** ;
les routes viendront des contrats du backend (session « appli degestion backend »), pas encore écrits.

**Ne rien coder avant le contrat backend correspondant.** Envoie d'abord ton plan (écrans, composants, états, tests) à la session
« new easymakemoney », qui le relit. Essais sur le site **demo** uniquement.

---

## 0. Ce que c'est, et ce que ce n'est pas

- **C'est** un dossier qui rassemble et vérifie tout ce dont un comptable a besoin pour valider l'exercice : relevés de tous les comptes,
  rapprochements, soldes d'ouverture, inventaire de clôture, emprunts, taxes. On l'exporte ensuite pour le comptable.
- **Ce n'est pas** un bilan officiel. L'application prépare et contrôle ; **seul le comptable signe**. Cette phrase doit apparaître
  sur l'écran du dossier et en tête de l'export (texte exact au § 9).
- À ne pas confondre avec le **bilan de configuration** (lot IA1), qui vérifie seulement que l'application est bien réglée.

## 1. Décisions du propriétaire à respecter (10 et 11/11/2026)

1. **Exercice réglable par site** : 12 mois à partir du mois choisi. Il se lit par `GET /api/gestion/reglages/exercice`, déjà en ligne.
   Libellé « 2026 » (début en janvier) ou « 2026-2027 » (autre mois). Le dossier porte toujours sur un exercice ; il se choisit en haut de
   l'écran (exercice en cours et précédents).
2. **Relevés bancaires** : import **PDF, CSV et OFX** (aussi QFX, la variante américaine de l'OFX).
   - **Au moins 3 relevés** sont demandés pour une mise à jour fiable. Un seul relevé est accepté, avec l'avertissement du § 9.
   - Pour un dossier de fin d'exercice complet : **les 12 relevés de l'exercice, pour chaque compte**.
3. **Confidentialité** :
   - le PDF est **supprimé après lecture** ; seules les lignes extraites sont gardées ;
   - le numéro de compte est **toujours masqué** (4 derniers chiffres au plus) ;
   - l'accord du gérant est demandé **à chaque import**.
4. **Connexion directe aux banques** (agrégateur, lot IA4) : **plus tard**, et **uniquement depuis l'appli web**. L'application mobile
   n'en affiche que l'état. Elle devra couvrir les banques **canadiennes et américaines** (pistes : Plaid, Flinks, MX, Finicity, à choisir
   plus tard). Elle remplacera seulement l'import de fichier : le même calcul s'applique derrière.
5. **Rien n'est modifié sans l'accord du gérant** : chaque proposition passe par aperçu, application, journal et annulation (§ 5).
6. **L'IA formule, l'application calcule** : l'IA peut lire un PDF et rédiger des explications, mais tout chiffre affiché ou écrit vient
   du calcul du backend, jamais d'un texte de l'IA.

## 2. Règles communes de la gestion (déjà en place, à reprendre telles quelles)

- `ROLE_ADMIN`.
- Montants en **cents** dans la devise du site ; affichage selon la devise du site.
- Dates au **fuseau du magasin**.
- Paramètre `locale` (fr ou en) pour tous les textes du backend.
- Erreurs : `{ code, message, fields }` ; à afficher sans plantage, avec le message du backend.
- Codes à prévoir : 409 (version modifiée entre-temps, empreinte d'aperçu périmée), 422 (validation), 429 (plafond IA).
- Chaque terme technique est un **lien vers sa définition** (lexique unique, comme dans l'application mobile : solde, rapprochement,
  écart, frais fixe, exercice, inventaire…). Reprends les définitions de `Easymakemoney/Utilities/Glossaire.cs` pour que les deux
  applications disent la même chose.
- Rien n'est stocké dans le navigateur (relevés, lignes extraites), en dehors des préférences d'affichage.

## 3. Écran principal : le dossier de l'exercice

En haut :
- le sélecteur d'exercice (libellé, dates, « en cours » ou « terminé ») ;
- **la jauge de complétude**, en pourcentage, avec la liste de ce qui manque ;
- le texte « préparé pour votre comptable, qui seul valide le bilan ».

Liste de contrôle. Chaque ligne a un statut (fait, à faire, attention), un détail chiffré et un bouton qui mène à l'écran concerné :

| Contrôle | Fait quand… |
|---|---|
| Comptes déclarés | Le gérant a listé tous ses comptes (§ 4), y compris Stripe, PayPal, Square ou autre encaissement |
| Relevés | 12 sur 12 pour **chaque** compte, sans trou ni doublon de période |
| Soldes enchaînés | Le solde de fin d'un relevé = le solde de début du suivant, pour chaque compte |
| Rapprochement | Chaque mois rapproché ; aucun écart « à traiter » ; écarts « non justifiés » listés et totalisés |
| Soldes d'ouverture | Bilan de l'exercice précédent déposé, ou soldes saisis (premier exercice : « exercice de création ») |
| Inventaire de clôture | Inventaire du stock à la date de fin d'exercice (lot INV) |
| Emprunts | Capital restant au début et à la fin, intérêts de l'exercice, pour chaque emprunt |
| Taxes | Taxes perçues sur les ventes et payées sur les achats, par taxe (§ 7) |
| Notes de frais | Aucune note sans justificatif ou sans catégorie ; notes qui ressemblent à des frais fixes vérifiées |
| Articles sans coût | Ventes au coût inconnu : nombre et montant ; à corriger pour une marge juste |
| Périodes | Toutes les périodes de l'exercice fermées (clôture du mois, lot CLO) |
| Exercice | Terminé, ou date de fin dépassée |

États à prévoir :
- exercice en cours : le dossier se remplit, l'export reste possible et porte la mention « provisoire » ;
- exercice terminé et dossier incomplet ;
- dossier complet ;
- dossier clôturé, en lecture seule, avec réouverture tracée (§ 8).

## 4. Comptes

- Liste des comptes du site :
  - nom donné par le gérant ;
  - type : banque, carte de crédit, Stripe, PayPal, Square, caisse en espèces, autre ;
  - établissement ;
  - **4 derniers chiffres** au plus ;
  - devise ;
  - date d'ouverture, et de fermeture s'il y a lieu.
- Pour chaque compte, une **grille des 12 mois** de l'exercice. Par mois :
  - relevé importé ou non ;
  - soldes de début et de fin ;
  - rapproché ou non ;
  - écart.
  Un clic sur un mois ouvre le détail.
- **Compte dans une autre devise** que celle du site (par exemple un compte en dollars américains pour un site canadien) : à prévoir
  dans l'écran (affichage dans la devise du compte), mais la règle de conversion reste une **question ouverte** (§ 11).
- Encaissements par Stripe ou PayPal : leurs virements vers la banque ne sont **pas des ventes en plus**. Le dossier doit les montrer comme
  des transferts entre comptes (question ouverte au backend sur la détection, § 11).

## 5. Import des relevés

**Déposer** plusieurs fichiers à la fois par glisser-déposer, ou en les choisissant. Formats acceptés : PDF, CSV, OFX et QFX.

**Avant de commencer**, une fenêtre de consentement :
- ce qui sera lu ;
- ce qui sera gardé (les lignes) et supprimé (le PDF) ;
- le numéro de compte masqué ;
- rien n'est modifié sans validation.

**Pour chaque fichier, afficher :**
- le compte reconnu (ou à choisir) et la période couverte ;
- les soldes de début et de fin ;
- le nombre de lignes ;
- l'état de la lecture.

**Erreurs à gérer, chacune avec un message clair et une action :**
- fichier illisible ou protégé par mot de passe ;
- scan de mauvaise qualité ;
- format inconnu ;
- relevé d'un autre compte ;
- période déjà importée (doublon) ;
- **trou** entre deux relevés ;
- soldes qui ne s'enchaînent pas ;
- relevé à cheval sur deux exercices ;
- relevés hebdomadaires ou de dates irrégulières ;
- fichier trop gros ;
- coupure réseau pendant l'envoi ;
- plafond de lecture IA atteint (429).

**Avertissement quand il y a moins de 3 relevés** sur un compte (texte au § 9).

**Différence de précision à montrer** : un CSV ou un OFX est exact au centime ; un PDF est lu par l'IA. Les lignes d'un PDF qui n'ont pas
pu être lues avec certitude sont signalées et doivent pouvoir être corrigées à la main avant validation.

**Après la lecture, les propositions**, chacune avec un aperçu, puis application en une fois, journal et annulation :
- les **soldes** de fin de mois deviennent les soldes du compte, dans la Trésorerie, et alimentent le rapprochement ;
- les **prélèvements qui reviennent** (au moins 2 fois, mieux 3) sont proposés en **frais fixes** : montant, fréquence, catégorie ;
- les **dépenses absentes** de l'application sont proposées en **notes de frais** (catégorie proposée, modifiable) ;
- les **dépenses déjà saisies** sont reconnues et rattachées, jamais comptées deux fois. Le rattachement doit pouvoir être défait ;
- les **virements entre comptes** (Stripe vers banque, banque vers carte) sont reconnus comme transferts ;
- les lignes **non reconnues** restent dans une liste « à classer ».

Rien n'est écrit tant que le gérant n'a pas validé l'aperçu. L'annulation remet exactement l'état d'avant (même schéma que les prix et
les promotions : aperçu, empreinte, application, journal, annulation).

## 6. Rapprochement et soldes d'ouverture

- **Rapprochement mois par mois et compte par compte** :
  - attendu (solde précédent + mouvements connus), solde réel du relevé, écart ;
  - l'écart doit être expliqué (note) ou marqué non justifié ;
  - le rapprochement de la Trésorerie existe déjà (lot RAP) : le dossier en reprend les règles et les statuts (à traiter, rapproché,
    expliqué, non justifié).
- **Soldes d'ouverture** :
  - dépôt du bilan de l'exercice précédent (PDF), ou saisie des soldes de chaque compte au premier jour de l'exercice ;
  - premier exercice du site : mention « exercice de création », les soldes d'ouverture sont ceux du premier relevé.

## 7. Inventaire, emprunts, taxes

- **Inventaire de clôture** (lot INV) :
  - date (fin d'exercice) ;
  - valeur au coût et à la vente ;
  - écarts avec le stock théorique ;
  - statut.
- **Emprunts** : pour chacun, prêteur, capital emprunté, capital restant au début et à la fin de l'exercice, intérêts payés. Un
  remboursement de capital **n'est pas une charge** ; les intérêts le sont.
- **Taxes** : uniquement des totaux, le comptable décide.
  - **Canada** : TPS, TVQ, TVH par province.
  - **États-Unis** : sales tax par État.
  - Pour chaque taxe : taxes **perçues** sur les ventes (déjà connues par les commandes et la caisse), taxes **payées** sur les achats et les
    frais (factures et notes de frais), solde.
  - Les régimes particuliers (petit fournisseur, méthode rapide) sont une question ouverte (§ 11) : ne rien calculer d'autre que les
    totaux tant que ce n'est pas tranché.

## 8. Clôture de l'exercice

- Bouton « Clôturer l'exercice », actif seulement quand le dossier est complet, ou après confirmation explicite de ce qui manque.
- Une fois clôturé, l'exercice est en lecture seule : périodes fermées, relevés et rapprochements figés.
- Réouverture possible, avec confirmation, motif obligatoire et journal d'audit (qui, quand, pourquoi).

## 9. Textes obligatoires

À reprendre tels quels, ou traduits fidèlement en anglais :

- **Dossier** : « Ce dossier est préparé pour votre comptable. Il rassemble et vérifie vos données, mais seul votre comptable peut
  valider et signer le bilan. »
- **Moins de 3 relevés** : « Pour une mise à jour fiable, importez au moins vos 3 derniers relevés. Avec un seul, le solde et les dépenses
  seront à jour, mais les frais fixes ne pourront pas être détectés avec certitude. »
- **Avant un import** : « Vos relevés sont lus pour mettre à jour le solde, les frais fixes et les dépenses. Le fichier PDF est supprimé
  après lecture, seules les lignes sont gardées, et le numéro de compte est masqué. Rien n'est modifié sans votre validation. »
- **Avant d'appliquer des propositions** : « Ces changements vont mettre à jour vos données. Vérifiez qu'ils vous conviennent : vous
  pourrez les annuler. »
- **Export provisoire** : « Exercice en cours : document provisoire. »

## 10. Export pour le comptable

- **PDF de synthèse** :
  - en-tête : site, exercice, date d'export, mention du § 9 ;
  - état de complétude et ce qui manque ;
  - résultat de l'exercice (ventes, marge, locations et abonnements, frais variables, frais fixes, résultat : mêmes chiffres que
    Statistiques › Année, route `gestion/statistiques/annee`) ;
  - rapprochements par compte et par mois ;
  - écarts non justifiés ;
  - stock de clôture ;
  - emprunts ;
  - taxes.
- **Tableur** (CSV, et XLSX si possible), un onglet ou un fichier par sujet :
  - ventes par mois ;
  - achats ;
  - notes de frais ;
  - frais fixes ;
  - lignes de relevés avec leur rattachement ;
  - rapprochements ;
  - inventaire ;
  - taxes.
- **Pièces jointes** : justificatifs des notes de frais et factures d'achat déjà dans l'application, dans une archive. Les PDF de relevés
  n'y sont pas, puisqu'ils sont supprimés après lecture ; le tableur des lignes les remplace. À signaler dans l'export.
- **Montants** : en devise du site, en cents dans les fichiers ; formatés seulement dans le PDF.
- **Envoi au comptable** (courriel, lien de partage) : question ouverte (§ 11). Au départ, un simple téléchargement.

## 11. Questions ouvertes

Elles sont à trancher avec le propriétaire et le backend avant le contrat. Ne pas les décider seul.

1. Comptes dans une autre devise : taux de conversion (date du relevé, fin de mois, taux moyen) ?
2. Virements Stripe, PayPal ou Square vers la banque : comment le backend les reconnaît-il pour ne pas compter les ventes deux fois ?
3. Immobilisations (gros achats d'équipement) : faut-il les lister à part, au-delà d'un seuil ?
4. Régimes de taxes particuliers (Canada, États-Unis) : seulement des totaux, ou plus ?
5. Envoi au comptable : téléchargement seulement, ou compte « comptable » en lecture seule, ou lien de partage limité dans le temps ?
6. Plusieurs sites pour un même propriétaire : un dossier par site (par défaut) ?
7. Durée de conservation des lignes de relevés et du journal ?

## 12. Lots et dépendances

| Lot | Contenu | Où |
|---|---|---|
| RAP | Rapprochement bancaire de la Trésorerie | livré (backend et application mobile) |
| EX | Exercice réglable par site | livré |
| IA1, IA2 | Bilan de configuration et assistant IA (questions, aperçu, application, annulation) | à venir, application mobile et web |
| INV | Inventaire | à venir |
| CLO | Clôture du mois **et de l'exercice**, dossier de fin d'exercice | à venir ; écrans du dossier : **web** |
| IA3 | Import des relevés PDF, CSV, OFX et QFX, propositions, rapprochement | à venir ; dépôt de plusieurs fichiers : **web** |
| IA4 | Connexion directe aux banques (agrégateur, Canada et États-Unis) | plus tard, **web uniquement** |

## 13. Critères d'acceptation (à cocher avant de dire « livré »)

- [ ] Toutes les lignes du tableau du § 3 existent, avec statut, détail chiffré et lien vers l'écran concerné.
- [ ] Grille des 12 mois par compte, adaptée à l'exercice du site (début en avril : avril → mars).
- [ ] Import multiple PDF, CSV, OFX et QFX ; chaque erreur du § 5 a son message et son action ; essai réel sur demo pour chaque format.
- [ ] Avertissement « moins de 3 relevés » ; consentement avant chaque import ; numéro de compte jamais affiché en entier.
- [ ] Propositions avec aperçu, application tout ou rien, journal, annulation qui remet exactement l'état d'avant.
- [ ] Aucune dépense comptée deux fois (rattachement aux notes et commandes existantes, transferts entre comptes reconnus).
- [ ] Textes du § 9 présents ; termes techniques en lien vers le lexique.
- [ ] Export PDF et tableur conformes au § 10 ; « provisoire » pour un exercice en cours.
- [ ] Clôture, lecture seule et réouverture tracée.
- [ ] Erreurs 409, 422 et 429 et coupure réseau affichées sans plantage.
- [ ] Interface en français et en anglais ; utilisable au clavier ; lisible sur un écran d'ordinateur portable.
- [ ] Les chiffres du résultat sont identiques à ceux de Statistiques › Année de l'application mobile pour le même exercice.
