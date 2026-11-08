# État des lieux de l'application de gestion (.NET MAUI), 05/11/2026

Rédigé par la session Claude « .net Mauiu », en lecture seule. Aucun code n'a été modifié, rien n'a été commité.
Ce document répond à la section 6 de [topo-demarrage.md](topo-demarrage.md) : audit face au backend, inventaire,
taxes F18 et plan en lots. Il sert aussi de point de départ au prochain agent.

Références vérifiées :

- application : ce dépôt (`~/Desktop/projets/Easymakemoney1`, remote `malmont/neweasymakemoney`), branche `main`,
  commit `e2b05e7` du 31/03/2025 ;
- backend : `izzipro_backend_symfony_v7`, branche `origin/dev`, commit `1ebffd2` du 05/11/2026, soit 141 commits
  après celui de l'audit (`152567d`).

## 0. À savoir avant de commencer

- **Le bon dépôt est celui-ci.** `~/Desktop/projets/easymakemoneyMaui/easymakemoneyMaui` (remote
  `malmont/easymakemoneyMaui`, .NET 7, 2023) est l'**ancêtre** : routes `/jeesign/api/…`, qui n'existent plus.
  `~/Desktop/projets/projetmaui` (MauiApp1) n'est qu'un gabarit. Ouvrir VS Code ou l'agent sur **Easymakemoney1**.
- **Jeton GitHub en clair dans l'adresse du remote** : ne jamais lancer `git remote -v` et ne jamais recopier l'adresse.
  Le propriétaire révoque le jeton sur GitHub. Ensuite seulement, lui proposer de nettoyer l'adresse
  (`git remote set-url origin https://github.com/malmont/neweasymakemoney.git`).
- **Fichiers non suivis par git** : `README.md` et `docs/` (`audit-multitenant.md`, `strategie-multitenant-mobile.md`,
  `topo-demarrage.md` et ce fichier). Ils ne se commitent qu'avec l'accord du propriétaire, donné par lui-même.
- `docs/strategie-multitenant-mobile.md` est identique à la copie de la caisse
  (`~/Desktop/projets/posflutter v2/posflutter/docs/`), vérifié le 05/11. **Il n'existe pas dans le dépôt backend**
  (`git show origin/dev:docs/strategie-multitenant-mobile.md` échoue). La source de vérité est donc la copie de la
  caisse, contrairement à ce qu'a indiqué la session « posflutter backend ».
- Règles : répondre en français ; un sujet à la fois (plan, accord, puis code) ; essais sur le site demo uniquement
  (`demo.arkanoa-media.com`, code court `DEM042`), car le backend n'a pas de préproduction ; le backend se lit
  (`git fetch origin` puis `git show origin/dev:<fichier>`) et ne se modifie jamais ; aucune clé, aucun identifiant ni
  aucune URL de site dans le code.

## 1. Inventaire de la plateforme

| Élément | Valeur | Remarque |
|---|---|---|
| Cibles | `net8.0-android`, `net8.0-ios`, `net8.0-maccatalyst` (+ `net8.0-windows10.0.19041.0` sous Windows) | `Easymakemoney.csproj:4-5` |
| Identité | `ApplicationId` `com.companyname.easymakemoney`, titre « Easymakemoney », version 1.0 (1) | identifiant d'exemple, à remplacer avant les stores |
| OS minimum | iOS 11, Mac Catalyst 13.1, Android 21, Windows 10.0.17763 | à relever avec la version de MAUI |
| SDK sur ce Mac | .NET **8.0.204**, workloads `maui`, `maui-android`, `maui-ios`, `maui-maccatalyst` en **8.0.61** | aucun SDK .NET 9 ou 10 installé |
| Xcode | **26.2** (17C52) | les workloads MAUI 8 visent des SDK Apple bien plus anciens : la compilation iOS et Mac Catalyst a peu de chances d'aboutir (**non essayé**, rien n'a été compilé) |
| Paquets | CommunityToolkit.Maui 9.0.2, Microcharts.Maui 1.0.0, Microsoft.Extensions.Http 8.0.0, Microsoft.Maui.Controls (+ Compatibility) `$(MauiVersion)`, Microsoft.Extensions.Logging.Debug 8.0.0, Microsoft.Maui.Essentials 8.0.61, **Microsoft.Toolkit.Mvvm 7.1.2** (ancien nom de CommunityToolkit.Mvvm), Newtonsoft.Json 13.0.3, SkiaSharp.Views.Maui.Controls 2.88.8, System.IdentityModel.Tokens.Jwt 8.0.1, **Xamarin.Essentials 1.8.1** (reste de Xamarin) | `Easymakemoney.csproj:45-56` |
| Taille | 286 fichiers `.cs` et `.xaml` ; Services → UseCase → ViewModels → Views ; aucun test | |

MAUI 8 n'est plus maintenu. La cible conseillée est **.NET 10 (LTS)**, à confirmer au moment du lot : SDK et workloads à
installer, Xcode compatible. Risques : Microcharts.Maui 1.0.0 (paquet peu suivi), et le passage de
`Microsoft.Toolkit.Mvvm` 7 à CommunityToolkit.Mvvm 8, qui change les attributs (`[ICommand]` → `[RelayCommand]`,
utilisé par exemple dans `ViewModels/Lists/ListNewCommandViewModels.cs`).

## 2. L'audit du 15/10 face au backend du 05/11

### Ce qui tient (vérifié dans le code)

- **B1** : URL en dur `https://backend-strapi.online/api` (`Easymakemoney/const/Configurations.cs:4`), recopiée en
  constante dans chaque service. Aucun `X-Tenant-Host` n'est envoyé, donc l'application sert le site `tenantdefaut`.
- **B2** : `Models/LoginRequest.cs` ne contient que `{username, password}`, sans `platform`. Le backend suppose `web` et
  exige alors `ROLE_USER_INTERNET`. La plateforme `admin` **existe désormais** côté backend
  (`src/Services/AuthenticationService.php:23`, elle exige `ROLE_ADMIN`, sinon 403 `PLATFORM_NOT_ALLOWED`) : le
  palliatif « `platform: web` + `ROLE_USER_INTERNET` » de l'audit est **obsolète**, il faut envoyer `platform: "admin"`.
- **B3** : il n'y a pas d'écran OTP. `Models/LoginResponse.cs` ne lit que `Token` : pas de `refresh_token`, pas
  d'`otp_required`. Une réponse `{otp_required: true}` donne un `Token` nul.
- **B4** : `Services/ListFraisDePortService.cs:25` appelle `DELETE /api/frais-de-port/{id}`, qui n'existe pas (404). La
  bonne route est `DELETE /api/commandes/{id}/frais-de-port` (endpoints.md, ROLE_ADMIN).
- Points importants : `HttpService` modifie `DefaultRequestHeaders` à chaque appel et rend `default` pour toute réponse
  autre que 200/201 (`Services/HttpService.cs`) ; le jeton est dans `Preferences`, chiffré AES avec une clé écrite en
  dur (`Services/EncryptionHelper.cs`) ; `JwtService.ParseToken` ne garde que le premier claim `roles`
  (`Services/JwtService.cs`) ; il n'y a aucun renouvellement de jeton ni aucun traitement du 401.

### Ce qui a changé ou que l'audit n'avait pas vu

1. **Les suppressions disent toujours « succès »** (nouveau, plus grave que ce que pensait le backend). Chaque service
   fait `var result = await _httpService.DeleAsyncWithAuth<object>(url); return result != null;`. Or `result` est un
   `bool`, jamais nul : le service rend **toujours `true`**, quel que soit le statut HTTP. Fichiers concernés :
   `ListCommandService.cs:29-30`, `ListFraisDePortService.cs:26-27`, `ListCollectionService.cs:28`,
   `ListProductService.cs:32`, `ListProductVarianstService.cs:31`, `ListFourniseursServices.cs:36`,
   `ListNoteDeFraisServices.cs:35`.
   - Le backend croit que MAUI « affiche un échec sur le 204 de `DELETE /api/commandes/{id}` ». En réalité,
     `DeleAsyncWithAuth` rend bien `false` sur un 204 (`HttpService.cs:86`, il n'accepte que 200), mais ce `false` est
     perdu. L'écran affiche donc « Success » même sur un 403, un 404 ou un 500
     (`ViewModels/Lists/ListNewCommandViewModels.cs:58-72`).
   - Frais de port : `ListNewFraisDePortViewModel.cs:113-131` ignore aussi le résultat et retire la ligne de la liste.
     Le 404 de B4 est donc invisible, et la ligne réapparaît au rechargement.
   - Correction : accepter tous les 2xx (dont 204) dans `HttpService` et propager le vrai résultat.
2. **OTP** (cf656d7, fb304fe) : `ADMIN_OTP_REQUIRED=1` est actif. Tout `ROLE_ADMIN` reçoit `{otp_required: true}`,
   puis doit passer par `POST /api/otp-verify` en envoyant **`platform: "admin"`** (sans cette valeur, le backend prend
   `mobile`, et le contrôle `ROLE_ADMIN` d'`OtpApiController.php:76` ne s'applique qu'avec `admin`). Un code faux ou un
   courriel inconnu donnent **401 `OTP_INVALID`**. Limite : 5 essais par 10 minutes et par compte. Le super
   administrateur passe toujours par l'OTP.
3. **Codes d'erreur stables** (9d1d805, 02f1cf7) : toute 4xx porte `code`. Règle de lecture : prendre `code` si c'est une
   chaîne en MAJUSCULES_SOULIGNÉES, sinon `error` (les 401 de Lexik gardent un `code: 401` entier : `JWT_EXPIRED`,
   `JWT_INVALID`… sont dans `error`). `VALIDATION_FAILED` est accompagné de `errors[{path, message}]` ; le 429 de
   `RATE_LIMITED`, de `retryAfterSeconds`. Un 401 métier (`LOGIN_INVALID`, `OTP_INVALID`, `ACCOUNT_NOT_VERIFIED`) **n'est
   pas** une session expirée. Table complète : backend `docs/caisse.md`, « Codes d'erreur de la caisse ».
4. **Renouvellement du jeton** : `POST /api/token/refresh {refresh_token, platform: "mobile"}` rend un nouveau `token`.
   Avec `platform: "mobile"`, le jeton de renouvellement est prolongé à chaque appel (session glissante). Un refus du
   serveur entraîne la déconnexion, en gardant le site ; un serveur injoignable laisse la session en place.
5. **Activation par clé** : `POST /api/app/activate {key, app: "admin", deviceName?}` (public, sans `X-Tenant-Host`)
   rend `{tenant: {code, name, host}, app, label, expiresAt, cashRegister, profile}`. Clé au format
   `IZ-ADM-XXXX-XXXX-XXXX`. Désactiver `isManagementAppActive` sur le site révoque les clés : l'application reçoit
   alors 404 `APP_KEY_INVALID`. Repli : `GET /api/tenant/check-mobile` (code, domaine ou code court `DEM042`).
6. **Variantes** (point « à vérifier » de l'audit) : **compatible**. MAUI envoie l'objet `ProductVariant`
   `{id, color{…}, size{…}, stockQuantity}` (`ListProductVarianstService.cs:23`) ; le backend lit `color.id` et `size.id`
   quand `colorId` et `sizeId` sont absents (`src/Services/LandingContentService/BoutiqueCatalogEditor.php:57-62`). Il
   rend 201, que `PostAsyncWithAuth` accepte.
7. **Dashboard d'une commande sans produit** (ae12e9f) : rendait 500, rend maintenant 200 avec `tauxMarque` à 0.
   Rien à changer côté MAUI.
8. **MODULE_INACTIVE** (console de pilotage du 04/11) : le garde ne vise que Mémoires, Boussole, l'assistant Landing et
   le début du parcours Boutique ; la caisse est exclue. **Aucune route de MAUI n'est concernée.** Le verrou effectif de
   l'application de gestion est la révocation de la clé.
9. **Statistiques** : la semaine va désormais du lundi 0 h au dimanche 23 h 59 ; les bornes du mois et de l'année sont
   corrigées. C'est un changement visible dans MAUI, voulu, sans rien à modifier.
10. **Rôles EasyAdmin** (lots 2a à 2c) : réservations au super administrateur dans EasyAdmin ; les routes d'API de MAUI
    sont inchangées.
11. **`POST /api/caisse/close`** ajoute `report`. MAUI n'appelle pas cette route : seulement `GET /api/caisse/transactions`.

## 3. Taxes F18 partie 2 : ce que MAUI lit vraiment

Recherche dans tout le code (`sub_total`, `subTotal`, `priceTax`, `totalAmount`, `taxes`, `TTC`, `country`, `pays`,
`.Sum(`, `.Average(`, `+=`) :

- **MAUI n'appelle ni `GET /api/orders` ni les exports comptables.** Aucun modèle ne contient `subTotal`, `priceTax`,
  `totalAmount`, `pricesIncludeTax` ou `taxes[]`. La liste « Ce que MAUI doit changer » du backend (`docs/caisse.md`,
  vers la ligne 2073) vise donc un lecteur qui **n'existe pas** dans ce code. Rien à corriger aujourd'hui.
- **Aucun total ni aucune taxe n'est recalculé côté client** (pas de `.Sum`, d'`.Average` ni de cumul dans les
  ViewModels, UseCases, Services et Models). Tous les montants affichés viennent du serveur :
  - chiffre d'affaires et panier moyen (`ChiffreAffaireService.cs:15`, `AverageOrderValueServices.cs:17`) : le backend
    fait `SUM` et `AVG` de `o.totalAmount` (`src/Repository/OrderRepository.php:30, 93`), et non de `sub_total`. Le
    changement de `sub_total` n'a donc pas d'effet ;
  - taxes mensuelles (`FraisClientTotalService.cs:23`, `Models/FraisClientTotal.cs:39`, affichage
    `ViewModels/Statistiques/FraisClientTotalViewModel.cs:30-59`, `Views/Statistiques/FraisClientTotalViewPage.xaml:108-113`) :
    `monthly_taxes_for_current_year[].total_tax`, calculé par le backend depuis `order_tax`
    (`src/Services/StatistiqueService/TaxService.php:46`). C'est le seul affichage de taxes ; il n'est pas ventilé par
    taux.
- **Pays** : seulement `ListFournisseur.pays` (texte libre du fournisseur, `Models/ListFournisseur.cs:10`, saisi dans
  `ViewModels/FormModels/FournisseurFormModel.cs:33` et affiché dans `Views/Lists/ListNewFournisseurPage.xaml:163`). Ce
  n'est pas une adresse de commande : le passage au code ISO ne le touche pas.
- **Exports** : aucun dans MAUI.
- **Conséquence** : il suffit que les lecteurs JSON tolèrent les champs inconnus. C'est le cas avec Newtonsoft par
  défaut, ce qui répond aussi à la question « MAUI tolère-t-il un champ inconnu dans `app-profile` ? ». Une ventilation
  des taxes par taux serait une **nouvelle fonctionnalité**, pas une correction.

## 4. Points connus (section 5 du topo)

### Commandes fournisseurs (`#[ApiResource]` nu sur `Commande`)

**Ce que MAUI utilise de l'API Platform `/api/commandes` : uniquement `DELETE /api/commandes/{id}`**
(`ListCommandService.cs:28`), dont il n'attend aucun corps. La liste et la création passent par les contrôleurs dédiés
`GET` et `POST /api/collections/{id}/commandes` (`ListCommandService.cs:15, 21`). Les produits, les frais de port et le
tableau de bord passent par `/api/commandes/{id}/products`, `/frais-de-port` et `/api/dashboard/commande/{id}`, eux
aussi des contrôleurs dédiés. **MAUI ne lit aucun champ sérialisé par l'API Platform.** Le lot backend peut donc ne garder
que l'opération `Delete` (ROLE_ADMIN, 204), avec des groupes bornés, sans rien casser côté MAUI.

Correction de fait pour le chef de projet : sur `origin/dev`, `config/packages/security.yaml:152`
(`^/api/(collections|…|commandes|…)` → `ROLE_ADMIN`) réserve **déjà** les six routes automatiques à `ROLE_ADMIN`
(endpoints.md, lignes 646-651). Un caissier ou un client ne peut pas les atteindre par ce chemin. Reste un risque
d'exposition de champs et de relations sans groupes, mais seulement pour un administrateur.

Champs JSON lus par MAUI sur les routes dédiées des commandes (`Models/ListCommand.cs`) : `id`, `name`, `photo`,
`budget`, `date`, `collectionId`, `fournisseur` (objet `ListFournisseur`). Le POST envoie ce même objet. À garder tels
quels si le backend touche à ces contrôleurs.

### Suppression d'une commande et code 204

Voir 2.1 : à corriger dans `HttpService` (accepter les 2xx) **et** dans les services (cesser le `bool != null`).

### Hôte configuré (B1) et frais de port (B4)

Confirmés, voir la section 2.

## 5. Routes appelées par MAUI (relevé complet, `Services/*.cs`)

`login` ; `collections` (GET, DELETE `{id}`) ; `createcollections` (POST, `ListCollectionService.cs:21`) ; `collections/{id}/commandes` (GET, POST) ; `commandes/{id}` (DELETE,
API Platform) ; `commandes/{id}/products` (GET, POST multipart) ; `products/{id}` (DELETE) ; `products/{id}/variants`
(GET, POST) ; `product-variants/{id}` (DELETE) ; `commandes/{id}/frais-de-port` (GET, POST) ; `frais-de-port/{id}`
(DELETE, **404**) ; `collections/{id}/notes-de-frais` (GET, POST) ; `notes-de-frais/{id}` (DELETE) ;
`type-note-de-frais` ; `fournisseurs` (GET, POST, DELETE `{id}`) ; `type-fournisseurs` ; `dashboard/collection/{id}`
et `…/close` ; `dashboard/commande/{id}` ; `stock-evolution` ; `stock-value` ; `frais/total` ; `taxes/monthly` ;
`carrier/statistics` ; `transporteurs` ; `caisse/transactions` ; `statistiques/nombre-commandes`,
`/chiffre-affaires` et `/panier-moyen` (sans `{source}`, donc toutes sources) ; `payments/statistics` ; `colors` ;
`sizes` ; `styles` ; `category`.

Toutes existent sur `origin/dev` et sont réservées à `ROLE_ADMIN` (ou plus larges), sauf `DELETE frais-de-port/{id}`.

## 6. Plan proposé, en lots (estimations en jours de travail d'agent, recette sur demo comprise)

L'ordre change par rapport à l'audit : **la plateforme passe en premier**, parce qu'avec les workloads .NET 8 et
Xcode 26.2, rien ne garantit qu'on puisse compiler pour iOS ou Mac Catalyst, donc rien tester.

| Lot | Contenu | Estimation | Demande au backend |
|---|---|---|---|
| **0. Plateforme** | Installer le SDK et les workloads .NET 10 (avec l'accord du propriétaire) ; passer les cibles en `net10.0-*` ; CommunityToolkit.Maui à jour ; `Microsoft.Toolkit.Mvvm` → CommunityToolkit.Mvvm 8 (`[ICommand]` → `[RelayCommand]`) ; retirer Xamarin.Essentials ; vérifier Microcharts ; compiler Android, iOS (simulateur) et Mac Catalyst sur ce Mac, sans changer le comportement | 2 à 3 j | aucune |
| **1. Socle multi-tenant** | URL de l'API dans la configuration (plus de constante par service) ; écran « Clé d'activation » (`POST /api/app/activate`, `app: "admin"`, nom de l'appareil) et repli `check-mobile` (code court) ; `TenantContext` en `SecureStorage` ; `DelegatingHandler` qui ajoute `X-Tenant-Host = tenant.host` et bloque tout appel sans site ; purge au changement de site ; messages pour `APP_KEY_MISSING`, `APP_KEY_INVALID`, `APP_KEY_WRONG_APP` et 429 | 3 à 4 j | aucune (livré) ; confirmer seulement le nom définitif de l'hôte d'API (`BACKEND_REST_URI`) |
| **2. Connexion et session** | `platform: "admin"` sur `login` **et** `otp-verify` ; écran OTP (`OTP_INVALID`, limite de 5 essais) ; lire `token` et `refresh_token` ; renouvellement (`token/refresh`) dans le handler ; contrôle de `ROLE_ADMIN` (tous les rôles du JWT) et de `tenant_code` ; `SecureStorage` à la place de `Preferences` et d'`EncryptionHelper` ; déconnexion propre sur un 401 de session, distinct des 401 métier | 2 à 3 j | aucune (livré) ; question : quelle `platform` envoyer à `token/refresh` pour `admin` (la stratégie dit `mobile`) ? |
| **3. Routes et erreurs** | `HttpService` : accepter les 2xx (204), ne plus modifier `DefaultRequestHeaders`, rendre une erreur typée (`code`, `message`, `errors[]`, `retryAfterSeconds`) au lieu de `default` ; corriger les 7 `bool != null` ; B4 (`DELETE /api/commandes/{id}/frais-de-port`) ; messages d'erreur à l'écran (français : message du serveur ; autre langue : traduction du code) | 2 à 3 j | aucune ; confirmer ensuite le lot `Commande` (garder `Delete` seul, 204) |
| **4. Tests et recette** | Tests des services (handler, lecture des erreurs, session) ; recette complète sur demo avec un compte ROLE_ADMIN de demo (OTP saisi par le propriétaire) ; mise à jour d'`audit-multitenant.md` | 1 à 2 j | aucune |
| F18 | Rien d'obligatoire (section 3). Option : ventiler les taxes par taux dans les statistiques, ce qui demanderait au backend une route de statistiques par taux | 0 j (option à chiffrer) | seulement si l'option est retenue |

Total : environ **10 à 15 jours** pour les lots 0 à 4.

**Lot 5, à venir après le lot 4 (ajout du 07/11/2026)** : fiche produit par IA (photo → fiche, création, traduction,
IA en lot). Contrat et analyse : [lot-5-fiche-produit-ia.md](lot-5-fiche-produit-ia.md).

**Module « Assistance », plus tard (décision du 07/11/2026)** : onglet de tickets avec réponses proposées par l'IA,
après les lots du backend. Cadrage : [futur-module-assistance.md](futur-module-assistance.md).

**Ajouts du propriétaire du 08/11/2026 :**

1. **Revue d'architecture, avant la consigne du lot 1.** Il s'agit de vérifier l'organisation de l'application :
   services, cas d'usage, ViewModels, injection de dépendances, client HTTP, navigation et gestion des erreurs.
   Les améliorations nécessaires sont à proposer au propriétaire. La revue se place avant le lot 1, qui réorganise
   déjà le client HTTP, la configuration et l'injection : les améliorations retenues seront intégrées aux lots 1 à 3,
   ou à un lot à part si elles sont trop lourdes. Rapport : `docs/revue-architecture.md`.
2. **Installation sur le téléphone Android du propriétaire, sur le site demo.** Elle vient au plus tôt après le
   lot 2 : sans le lot 1, l'application appelle le site de production par défaut, et sans le lot 2, elle ne sait
   pas se connecter. Conditions préalables :
   - `ApplicationId` décidé, puisque le changer ensuite oblige à réinstaller et efface la clé enregistrée ;
   - le débogage USB activé sur le téléphone ;
   - la clé d'activation demo, le compte et le code OTP saisis par le propriétaire lui-même.
3. **Revue des fonctionnalités, une par une, avec le propriétaire.** Elle se fait à partir de l'application
   installée : connexion, tableau de bord, les listes (collections, commandes, fournisseurs, frais de port, notes de
   frais, produits, variantes) et les 8 pages de statistiques. Pour chaque fonctionnalité : ce qu'elle fait, ce qui
   gêne, et les améliorations possibles. Support de discussion : `docs/revue-fonctionnalites.md`. Les améliorations
   retenues formeront de nouveaux lots, après le lot 4.

## 7. Questions ouvertes (à trancher par le propriétaire ou le backend)

1. Version cible : .NET 10 LTS ? Le propriétaire autorise-t-il l'installation du SDK et des workloads sur ce Mac ?
2. Identité de l'application (`ApplicationId`, nom affiché) avant toute publication.
3. `token/refresh` pour l'application de gestion : `platform: "mobile"` ou `"admin"` ? (backend
   `SecurityController::refreshToken`)
4. Lot `Commande` côté backend : d'accord pour ne garder que `Delete` (MAUI n'utilise rien d'autre) ?
5. Images de produits (retour de l'audit, §5) : toujours écrites dans `public/assets/uploads/products/`, un dossier commun
   à tous les sites ? Non revérifié le 05/11.
