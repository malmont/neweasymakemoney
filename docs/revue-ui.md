# Revue de l'interface (audit visuel et modernisation)

Demande du propriétaire du 08/11/2026 : moderniser l'interface en gardant la vision de l'application. Ce document
sert de support à l'audit visuel, mené écran par écran sur le téléphone du propriétaire, puis à la charte à faire
valider. Plan : `etat-des-lieux-2026-11-05.md` §6, point 4.

## 1. Identité de l'application : fournie par le backend, jamais écrite en dur

**Décision du propriétaire (08/11/2026).** Le logo, le nom et la couleur affichés sont ceux **du site**, fournis par
la route dédiée aux applications. Le logo IIZI manager écrit en dur dans l'application disparaît.

**Contrat**, relevé dans le code (`AppProfileService::resolve`, `origin/dev`) et confirmé par la session
« appli degestion backend » le 09/11/2026 :

- **Route** : `GET /api/app-profile/admin`, publique (aucun jeton), avec `X-Tenant-Host` et `?locale=fr|en`.
  - Un code d'application inconnu reçoit 404 `APP_UNKNOWN`.
  - La réponse `profile` de `POST /api/app/activate` est **le même objet**. Elle peut valoir `null` : il faut alors
    rappeler la route.
- **Champs utiles** :
  - `name`, `tagline`, `logo`, `icon`, `primaryColor` ;
  - `email`, `phone`, `website`, `supportUrl`, `address` ;
  - les textes légaux : `apropos`, `privacyPolicy`, `conditionOfUse`, `legalNotice` ;
  - `hasProfile`, `sources` (indique, champ par champ, si la valeur vient de l'application ou de l'entreprise),
    `updatedAt`.
- **Replis du backend** : quand l'application n'a pas de profil `admin`, `name`, `logo` et `icon` viennent de la fiche
  Entreprise. `icon` vient alors du favicon de l'entreprise.
- **`logo` et `icon`** :
  - ce sont des URL absolues publiques, sans jeton ;
  - le backend n'impose ni format ni taille ;
  - le logo est **souvent rectangulaire**, l'icône **carrée** ;
  - **ils peuvent valoir `null` à tout moment**, par exemple si le fichier a disparu du serveur ;
  - **règle d'affichage** : un fond neutre derrière le logo, et aucun ratio supposé.
- **`primaryColor`** :
  - format `#rrggbb` en minuscules ;
  - il vient **uniquement** du profil `admin`, sans repli sur l'entreprise ;
  - il est conçu pour colorer l'application (« couleur des boutons et des éléments mis en avant ») ;
  - **s'il vaut `null` : couleur par défaut de l'application**, c'est-à-dire le violet `#512BD4` actuel.
- **Thème complet, en option** : `GET /api/mobile/app-theme?app=admin` renvoie `primaryColor`, `accentColor`, `mode`
  (`light`, `dark` ou `system`), `font` et `radius`. Il est public, avec un cache HTTP de 60 s et un `ETag`. À
  considérer si l'on veut aller plus loin que logo, nom et couleur.
- **Mise en cache** :
  - aucun `ETag` n'existe sur `app-profile` ;
  - `updatedAt` ne change **pas** quand la fiche Entreprise change, et vaut `null` sans profil `admin`.

  Stratégie retenue :
  1. appeler la route au démarrage et après l'activation ;
  2. garder la réponse en local ;
  3. rafraîchir l'affichage si `name`, `logo`, `icon` ou `primaryColor` ont changé.
- **Identité par défaut** : si `name` et `logo` valent `null`, ce qui n'arrive pas sur un site configuré, on affiche
  IIZI manager **en dernier recours seulement**. Proposition du backend, **à confirmer par le propriétaire**.

**État du site demo (09/11/2026)** : il n'a **aucun profil `admin`**, seulement un profil `mobile`.
- La route renverra donc `hasProfile: false`, avec le nom, le logo et le favicon de la fiche Entreprise.
- `primaryColor` vaudra `null`, et l'application utilisera son violet par défaut.
- Pour essayer la couleur, il faut créer la fiche `admin` du site demo dans EasyAdmin (Entreprise → Profils des
  applications → « Gestion (application) »).

**Questions au propriétaire :**
1. Créer la fiche `admin` du site demo, avec un logo et une couleur, pour les essais ?
2. IIZI manager en dernier recours seulement : d'accord ?
3. Demander au backend un `ETag`, ou un `updatedAt` qui tienne aussi compte de la fiche Entreprise ? Ce n'est pas
   nécessaire au départ : comparer la réponse suffit.

## 2. Audit visuel, écran par écran

Les captures sont prises sur le téléphone du propriétaire, avec la version du lot 2.

### Écran 1 : accueil (`Views/Dasboard/DashboardPage.xaml`)

**Constats :**
- **Titre technique** : « DashboardPage » s'affiche dans la barre du haut.
- **Trois couleurs qui ne vont pas ensemble** : la barre système est violette, la barre de titre bleu ardoise
  foncé, et l'en-tête du logo bleu pétrole. Le violet `#512BD4` de la charte n'apparaît que dans la barre système.
- **Logo IIZI manager écrit en dur** dans l'en-tête : il doit venir de `app-profile` (§1).
- **Boutons de menu** :
  - en anglais et en majuscules, avec une faute : « ITEMS CALCULTOR » ;
  - **deux boutons portent le même nom, « DASHBOARD »** : le premier ouvre le tableau de bord des collections, le
    second les statistiques ;
  - « GESTIONS » ouvre les listes ;
  - « ITEMS CALCULTOR » n'a **aucune action** ;
  - un cinquième bouton, « COLLECTIONS », est masqué.
- **Lisibilité** : le texte doré `#C4AB4D` sur fond blanc, encadré de doré, est peu contrasté.
- **Icônes** : des illustrations de styles différents.
- **Défauts de mise en page** :
  - deux barres verticales bleu foncé sur les bords de l'écran ;
  - un élément coupé sur le bord droit ;
  - un grand vide dans le bas de l'écran.

**Sur iPhone (simulateur iOS 27.2) :** les boutons et l'en-tête sont identiques. Les barres verticales bleues et l'élément coupé à droite n'y apparaissent pas : ces deux défauts sont propres à Android. La barre de titre est blanche et le menu s'ouvre par un bouton rond.

**Pistes :**
- un en-tête aux couleurs du site : logo de `app-profile` sur fond neutre, couleur `primaryColor`, sinon le violet ;
- un titre en français (« Accueil ») et le nom du site ;
- une **grille de tuiles** (2 colonnes) avec des icônes d'un seul style et des libellés clairs : « Listes »,
  « Tableau de bord », « Statistiques » ;
- le bouton sans action à retirer ou à définir, après décision du propriétaire ;
- les barres et l'élément coupé à corriger.


### Écran 2 : « Gestions », menu des listes (`Views/Lists/AllListsPage.xaml`)

**Constats (iPhone, iOS 27.2) :**
- **Titre technique** : « AllListsPage ».
- **Le logo prend environ un tiers de l'écran**, plus grand encore que sur l'accueil.
- **Texte doré sur bouton bleu pétrole** : contraste très faible, c'est l'écran le moins lisible jusqu'ici.
- **Boutons d'un autre style que l'accueil** : pleins et sans icône, alors que l'accueil a des cartes blanches avec
  icône. Les deux écrans ne semblent pas appartenir à la même application.
- **Grille irrégulière** : 2 × 2, puis un bouton « SIMULATEUR » seul, de largeur différente.
- **Texte en majuscules**, et moitié basse de l'écran vide.

**Pistes :**
- le même composant de tuile qu'à l'accueil, avec une icône par liste ;
- un en-tête réduit, ou le logo seulement sur l'accueil ;
- le titre « Gestion » ;
- une couleur de texte qui passe les seuils de contraste.

### Écran 3 : listes (collections, fournisseurs)

**Constats (iPhone, iOS 27.2) :**
- **Le logo occupe l'écran** et le titre reste technique (« ListNewCollectionPage »).
- **Liste vide sans aucun message.** Sur le site demo, c'est normal : il n'a aucune collection (confirmé par le
  propriétaire). Mais rien ne distingue « aucune donnée » d'un appel échoué : `HttpService` rend une liste vide dans les
  deux cas (corrigé au lot 3). Il faut un vrai état vide, avec une invitation à créer.
- **Bouton « + » en dégradé brillant**, au style daté.
- **Listes remplies pas encore vues** : le site demo n'en montre aucune, et l'audit n'y crée rien.

### Écran 4 : formulaire d'ajout (popup, collection)

**Constats :**
- L'ouverture, l'apparence et la fermeture du popup v2 fonctionnent (contrôle du lot 0).
- **Tout est en anglais** : « Collection Name », « Budget », « Start Date », « End Date », « Save », « Cancel ».
- **Interrupteur rouge sans libellé**, dont on ignore la signification.
- **Champs à bordure noire épaisse** et dates dans des cases trop hautes.
- « Save » et « Cancel » sont de simples liens.

### Écran 5 : tableau de bord (`DashBoardCollectionPage`)

**Constats :**
- **Grands blocs dorés** à texte noir, qui écrasent les valeurs.
- **Un carré noir et un carré doré vides** : les valeurs s'affichent mal quand il n'y a pas de collection.
- **« COLLECTIO / N » coupé** sur deux lignes, faute « Coeeficient moyen », bouton « Listes » bleu sur doré.
- Barre d'outils au-dessus du logo, qui repousse le contenu vers le bas.

### Écran 6 : statistiques (`StatistiquesPage`, `ChiffresAffairePage`)

**Constats :**
- **La grille de tuiles avec icônes est le meilleur écran** : c'est le modèle à généraliser, avec des icônes
  d'un seul style.
- **Détail d'une statistique** : le logo et la barre « Période » prennent la moitié de l'écran.
- **Montants en « $ » écrits en dur** : la devise doit être celle du site.
- **Ombres floues sur le texte** dans le tableau journalier, et or sur gris peu lisible.

### Synthèse

| Problème | Présent sur | Remède proposé |
|---|---|---|
| Titres techniques | tous les écrans | titres français courts |
| Logo surdimensionné, en dur | tous les écrans | identité `app-profile`, en-tête compact |
| Or sur blanc ou sur bleu | tous les écrans | couleur principale du site, contraste vérifié |
| Trois styles de boutons | accueil, listes, tableau de bord | un seul composant de tuile et de bouton |
| Anglais | accueil, formulaires | français partout |
| Écrans vides sans message | listes, tableau de bord | états vide, chargement et erreur |
| Boutons sans action | « ITEMS CALCULTOR », « SIMULATEUR » | **à retirer** (décision du propriétaire, 09/11/2026) |

## 3. Proposition de charte

*(à rédiger après l'audit, puis à faire valider par le propriétaire)*
