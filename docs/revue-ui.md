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

**Pistes :**
- un en-tête aux couleurs du site : logo de `app-profile` sur fond neutre, couleur `primaryColor`, sinon le violet ;
- un titre en français (« Accueil ») et le nom du site ;
- une **grille de tuiles** (2 colonnes) avec des icônes d'un seul style et des libellés clairs : « Listes »,
  « Tableau de bord », « Statistiques » ;
- le bouton sans action à retirer ou à définir, après décision du propriétaire ;
- les barres et l'élément coupé à corriger.


## 3. Proposition de charte

*(à rédiger après l'audit, puis à faire valider par le propriétaire)*
