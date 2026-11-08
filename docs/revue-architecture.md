# Revue d'architecture de l'application de gestion (.NET MAUI), 08/11/2026

Demandée par le propriétaire le 08/11/2026, avant la consigne du lot 1. J'ai lu le code de la branche
`feature/lot-0-plateforme`, soit environ 8 000 lignes de C# hors XAML. Cette revue ne modifie rien. Les numéros
renvoient au plan en lots de `etat-des-lieux-2026-11-05.md` §6.

## 1. Organisation actuelle

| Couche | Contenu | Remarque |
|---|---|---|
| Vues (`Views/`, `Components/`, `DataTemplates/`) | 40 fichiers XAML (23 pages, 9 composants, 8 modèles de données), navigation Shell | 22 routes déclarées dans `AppShell.xaml.cs` |
| ViewModels (`ViewModels/`) | 33 ViewModels et modèles de formulaire, CommunityToolkit.Mvvm | `BaseViewModel` avec `IsBusy` et `Title` |
| Cas d'usage (`UseCase/`) | 49 classes, une par action (lire, créer, enregistrer, supprimer) | beaucoup ne font que relayer le service |
| Services (`Services/`) | 24 services d’accès à l’API, chacun avec son interface, plus `HttpService` | un service par ressource |
| Modèles (`Models/`) | 43 classes JSON (Newtonsoft) | |
| Démarrage (`MauiProgram.cs`) | environ 150 enregistrements d'injection de dépendances | |

**Ce qui est bon et doit être gardé :**
- le découpage en couches, avec un sens clair : vue → ViewModel → cas d'usage → service → `HttpService` ;
- les interfaces sur tous les services, ce qui les rend remplaçables dans les tests ;
- l'injection de dépendances, utilisée partout ;
- un seul point d'accès HTTP.

Il ne faut pas tout réécrire. Les problèmes se concentrent à trois endroits : l'accès HTTP, la session et le popup
des formulaires.

## 2. Problèmes relevés

### A. Accès HTTP et configuration

1. **L'adresse de l'API est copiée dans 24 services.** Chacun déclare
   `private const string XxxUrl = Configurations.BackendSymfonyUrl;` et construit ses URL à la main. Il est donc
   impossible de changer de site sans recompiler.
2. **`HttpService` modifie `DefaultRequestHeaders.Authorization` à chaque appel.** Ces en-têtes sont partagés par tous
   les appels du même client : deux appels simultanés peuvent se gêner.
3. **Un client HTTP capturé pour toujours.** `AddHttpClient<IHttpService, HttpService>()` crée un client à durée
   courte. Mais `LoginService` et `ListCollectionService` sont enregistrés en singletons et gardent le leur jusqu'à
   la fermeture de l'application, ce qui va à l'encontre de la gestion prévue par `IHttpClientFactory`.
4. **Toutes les erreurs deviennent `default`.** Seuls 200 et 201 sont acceptés. Le reste rend `null`, ou `false`
   pour les suppressions, sans le code ni le message du serveur. Le cas des 7 suppressions qui rendent toujours
   `true` est déjà prévu au lot 3.
5. **Le corps des réponses est écrit dans le journal de débogage** (3 `Debug.WriteLine`), y compris la réponse de
   `/login`, qui contient le jeton.

### B. Session et état global

1. **Une clé de chiffrement AES écrite en dur dans le code** (`Services/EncryptionHelper.cs`). Le jeton est
   « chiffré » avec une clé que toute personne ayant le code connaît : la protection est illusoire.
2. **La session est éparpillée.**
   - Le jeton et l'utilisateur vivent à la fois dans `Preferences` (5 clés), dans `App.Token` et dans
     `App.UserDetails` (champs statiques, 11 usages).
   - `LoadingPageViewModel` et `AppShellViewModel` lisent et écrivent `Preferences` directement, sans passer par
     `PreferenceService`.
3. **La déconnexion est incomplète.** `AppShellViewModel.SignOut` efface `UserDetails`, mais **ni le jeton, ni
   `userId`, `userEmail` et `userRole`**.
4. **Des ViewModels sont des singletons** : `LoginPageViewModel`, `DashboardPageViewModel` et
   `LoadingPageViewModel`. Leur état survit à une déconnexion. Avec le multi-tenant, les données d'un site pourraient
   rester affichées après un changement de site.
5. **`LoginUseCase` navigue et touche à l'interface** (`Shell.Current.GoToAsync`, `FlyoutHeader`). Ce n'est pas le
   rôle d'un cas d'usage, et cela le rend impossible à tester.

### C. Écrans et ViewModels

1. **`BottomSheetPopupViewModel` fait tout.** Il sert les 7 formulaires : collection, commande, produit, variante,
   note de frais, fournisseur, frais de port.
   - Son constructeur compte 25 paramètres facultatifs.
   - Il porte 7 booléens `IsXxxForm`.
   - Il garde une référence directe à 7 ViewModels de liste, pour recharger leur liste après l'enregistrement.

   Chaque nouveau formulaire alourdit cette classe, et les couches dépendent les unes des autres dans tous les
   sens. Avec les popups v2, s'y ajoute probablement un **double rechargement** : le popup recharge la liste, puis
   la page la recharge dans `OnAppearing` (voir `gemini/lot-0-revue-addendum.md`, P5).
2. **Les ViewModels appellent directement Shell et la page principale** : 91 appels à `Shell.Current`,
   `AppShell.Current` ou `Application.Current.MainPage`, dans 25 fichiers. Conséquences :
   - aucun ViewModel n'est testable ;
   - les 66 avertissements CS0618 (`MainPage` obsolète) viennent en grande partie de là ;
   - les messages d'erreur sont écrits au cas par cas, en anglais (« Unable to get collections »).
3. **Des cas d'usage dépendent des modèles de formulaire de l'écran.** Les 6 `Save*UseCase` reçoivent un
   `XxxFormModel` (espace de noms `ViewModels.FormModels`) : la couche métier dépend donc de la couche écran.
4. **Des commandes en `async void`** : 18 cas signalés (MVVMTK0039), en plus des gestionnaires écrits à la main
   (`OnItemTapped`, `DeleteCollection`…). Une exception y fait planter l'application au lieu d'afficher un message.
5. **Écritures directes dans les champs** des `[ObservableProperty]` (40 cas, MVVMTK0034) : l'écran n'est pas
   prévenu du changement.

### D. Tests

Il n'y a **aucun projet de test**. De plus, un projet de tests classique (xUnit en `net10.0`) ne peut pas référencer
le projet de l'application, qui ne cible que `net10.0-android`, `-ios` et `-maccatalyst`. Il faut trancher ce point
avant le lot 4 (voir §3, point 4).

### E. Points mineurs, à ne pas traiter pour eux-mêmes

- Des fautes dans les noms de dossiers et de classes : `Dasboard`, `LisCommandeUseCase`,
  `ListProductVarianstService`, `NoteDeFraisForModel`, `Fourniseurs`, `GestListType…`. Les renommer touche des
  dizaines de fichiers pour aucun gain fonctionnel. **À laisser**, sauf pour un fichier qu'on réécrit de toute façon.
- Les 49 cas d'usage, dont beaucoup ne font que relayer le service : on garde la convention, pour la cohérence.
- `MauiProgram.cs` et ses 150 lignes d'enregistrements : on peut les regrouper par domaine (`AddListes()`,
  `AddStatistiques()`…), mais c'est sans urgence.
- Les 227 avertissements de nullabilité (CS86xx/CS87xx) et `Frame`, devenu obsolète : on les reprend quand on touche
  le fichier concerné.

## 3. Améliorations proposées et place dans le plan

### Dans les lots déjà prévus, sans coût notable : c'est leur sujet

| Lot | Amélioration | Problèmes réglés |
|---|---|---|
| 1 | **Un seul `HttpClient` configuré** : `BaseAddress` lue dans la configuration du site (`TenantContext`), services en chemins relatifs, fin des 24 constantes | A1 |
| 1 | **Gestionnaires d'en-têtes** (`DelegatingHandler`) : `X-Tenant-Host` dans le lot 1, puis `Authorization` dans le lot 2 ; plus aucune écriture dans `DefaultRequestHeaders` | A2 |
| 1 | **Durées de vie corrigées** : services sans état en transient, ou client obtenu par la fabrique ; plus de singleton qui capture un client | A3 |
| 1 | **Purge au changement de site** : les ViewModels singletons passent en transient, ou exposent une remise à zéro | B4 |
| 2 | **Un `SessionService` unique** sur `SecureStorage` : jeton, refresh token, utilisateur. Suppression d'`EncryptionHelper`, de `App.Token`, d'`App.UserDetails` et des accès directs à `Preferences`. La déconnexion efface tout | B1, B2, B3 |
| 2 | **`LoginUseCase` rend un résultat** (succès, OTP demandé, erreur) ; le ViewModel navigue | B5 |
| 2 | **Fin de la journalisation des réponses**, ou en-têtes et jetons masqués | A5 |
| 3 | **`HttpService` rend une erreur typée** (`ApiError` : `code`, `message`, `errors[]`, `retryAfterSeconds`) au lieu de `default` ; 2xx acceptés | A4 |
| 3 | **`IDialogService`** : un seul endroit pour les messages d'erreur, en français ; les 91 appels de dialogue et de navigation migrent progressivement, à commencer par les fichiers que touche le lot 3 | C2, en partie |
| 4 | **Projet de tests** : ajouter une cible `net10.0` au projet de l'application (méthode documentée par Microsoft pour tester une application MAUI) plutôt que d'extraire une bibliothèque à part | D |

Le lot 1 s'en trouve un peu plus large, d'environ une demi-journée, mais c'est le moment le moins cher : il réécrit
déjà l'accès HTTP. Je préciserai ces points dans la consigne du lot 1.

### Après la revue des fonctionnalités : la refonte des écrans

Les points C1, C2 (reste), C3, C4 et C5 touchent les écrans eux-mêmes. Or la revue des fonctionnalités avec le
propriétaire, sur le téléphone, risque de changer ces écrans. Il vaut donc mieux attendre ses conclusions, puis
traiter ces points dans les lots d'amélioration qui en sortiront :
- **un popup et un ViewModel par formulaire**, qui renvoient leur résultat à la fermeture (en v2,
  `CloseAsync(résultat)`) ; la page appelante recharge alors sa liste une seule fois ;
- **`INavigationService`**, à côté de l'`IDialogService` du lot 3 ;
- **les cas d'usage reçoivent des objets du domaine**, et le ViewModel s'occupe de la conversion depuis le
  formulaire ;
- **`async void` remplacé par `Task`**, et écritures par les propriétés générées.

Estimation : 2 à 3 jours, à revoir selon ce que la revue des fonctionnalités changera.

## 4. Décisions demandées au propriétaire

1. Intégrer les améliorations d'infrastructure aux lots 1 à 3, comme ci-dessus ? (recommandé)
2. Reporter la refonte des écrans après la revue des fonctionnalités ? (recommandé)
3. Pour les tests du lot 4, ajouter une cible `net10.0` au projet existant ? (recommandé, plutôt qu'une bibliothèque
   séparée)

**Décisions du propriétaire du 08/11/2026 : les trois recommandations sont acceptées.**
- Les améliorations d'infrastructure vont dans les lots 1 à 3.
- La refonte des écrans vient après la revue des fonctionnalités.
- Les tests du lot 4 passent par une cible `net10.0`.
