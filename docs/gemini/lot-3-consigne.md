# Lot 3 : routes et erreurs

Consigne de Claude du 09/11/2026. Comme d'habitude : **tu écris d'abord ton plan** dans `docs/gemini/lot-3-plan.md`
(modèle de `GEMINI.md` §5), puis tu t'arrêtes. Tu ne codes rien avant que je l'aie validé.

- Branche : `feature/lot-3-erreurs`, créée depuis `feature/lot-2-session`.
- Pas de commit.

## Objectif

Aujourd'hui, toute erreur du serveur disparaît : `HttpService` rend `default`, l'écran affiche une liste vide ou « réussi ».
À la fin du lot :

1. une erreur du serveur **remonte avec son code et son message** jusqu'à l'écran ;
2. une suppression qui échoue **n'affiche plus « réussi »**, et une suppression réussie (200 ou 204) n'affiche plus
   « échec » ;
3. la suppression d'un frais de port fonctionne ;
4. une erreur imprévue **ne fait plus planter** l'application.

## À lire avant d'écrire le plan

1. `docs/backend-contrat-gestion.md` : « Codes d'erreur » et « Écarts connus ».
2. `docs/revue-architecture.md` : §2 A4, C2, C4, et les lignes « 3 » du tableau du §3.
3. `docs/gemini/lot-2-revue-code.md` : « Remarque pour le lot 3 » et le dernier paragraphe de la contre-vérification.
4. `Easymakemoney/Models/Api/ApiError.cs`, à réutiliser.

## Faits relevés dans le code (vérifie-les toi-même)

- **`HttpService`** rend `default` sur tout ce qui n'est pas 200 ou 201, et `DeleAsyncWithAuth` n'accepte que 200.
- **Les 7 services de suppression** font `return result != null` sur un `bool` : c'est toujours `true` (CS0472).
  Fichiers : `ListCollectionService`, `ListCommandService`, `ListFourniseursServices`, `ListFraisDePortService`,
  `ListNoteDeFraisServices`, `ListProductService`, `ListProductVarianstService`.
- **Les services de création** font aussi `result != null`, sur un `object` désérialisé : une réponse 2xx sans corps
  serait comptée comme un échec.
- **Frais de port (B4)** : `ListFraisDePortService.DeleteFraisDePort(int id)` appelle `frais-de-port/{id}`, qui répond 404. La
  route du backend est `DELETE /api/commandes/{commandId}/frais-de-port`. `ListNewFraisDePortViewModel` connaît `CommandId`.
- **`async void`** : 41 occurrences dans 20 fichiers de `ViewModels/`, dont les 18 `[RelayCommand]` signalés par
  MVVMTK0039 et des gestionnaires écrits à la main (`DeleteCollection`, `OnItemTapped`…). Une exception y arrête
  l'application.
- **`SessionExpiredException`** (lot 2) : l'écran qui faisait la requête affiche aussi sa propre erreur, en plus du
  message de session.
- **3 CS8602** dans `LoadingPageViewModel.cs`, lignes 135, 152 et 169 (lot 2).

## Ce que ton plan doit couvrir

### 1. `HttpService` : exception typée

- Toute réponse hors 2xx lève une **`ApiException`** qui porte un `ApiError` (`ApiError.FromHttpResponse`). Ajoute à `ApiError` la
  liste `Errors` (`[{path, message}]`, pour `VALIDATION_FAILED`).
- Pas de réponse (`HttpRequestException`, `TaskCanceledException`) : `ApiException` de code `NO_CONNECTION`, message
  « Pas de connexion ». Une annulation demandée par l'appelant n'est pas une erreur réseau.
- **Tous les 2xx sont des succès, 204 compris.** Un corps vide sur 2xx rend `default` sans exception.
- `DeleAsyncWithAuth` rend `true` sur 2xx et lève `ApiException` sinon.
- Garde les signatures publiques d'`IHttpService`. Pas de type `ApiResult<T>` : il faudrait réécrire 24 services et 49 cas
  d'usage.

### 2. Services

- Les 7 suppressions : `return await …` du booléen, ou `return result;`.
- Les créations `PostAsyncWithAuth<object>` : l'échec passe maintenant par l'exception, donc le service rend `true` après
  l'appel. Liste les fichiers touchés.
- **B4** : `DeleteFraisDePort(int commandId)` appelle `commandes/{commandId}/frais-de-port`. Le ViewModel passe `CommandId`. Vérifie
  dans le backend (`git show origin/dev:…`) le code de succès réel, et ce que supprime cette route.
- Les `Get…` qui rendaient une liste vide sur `null` gardent ce repli pour un 2xx vide, mais une erreur remonte en exception.

### 3. `IDialogService`

- Un seul service : `ShowErrorAsync(Exception)`, `ShowInfoAsync(string)`, `ConfirmAsync(titre, message)`. Il passe par le fil
  principal et utilise `DisplayAlertAsync`.
- `ShowErrorAsync` :
  - **ne montre rien** pour `SessionExpiredException` : le message de session suffit ;
  - pour `ApiException` : le message du serveur (français), avec les détails de `Errors` s'il y en a ;
  - sinon : « Une erreur est survenue, réessayez. », et l'exception dans le journal, sans jeton ni corps de réponse.

### 4. ViewModels

- **Tous les chemins d'erreur** passent par `IDialogService.ShowErrorAsync`. Fini « Unable to get collections », « Error »,
  et les `catch` qui avalent l'erreur.
- **Les 41 `async void`** :
  - `[RelayCommand]` : `async Task` ;
  - les `ICommand` écrits à la main : `AsyncRelayCommand<T>` et `Task` ;
  - **exception légitime** : les gestionnaires d'événements et `OnAppearing` restent `async void`, avec un `try/catch`
    complet. Liste-les.
- Dans chaque méthode touchée, l'exception est attrapée : plus aucune ne doit remonter jusqu'à l'application.
- **Ne migre que les messages d'erreur et d'information.** La navigation `Shell.Current.GoToAsync` reste telle quelle.
- `BottomSheetPopupViewModel.Save` : une erreur affiche le message et **laisse le popup ouvert**, avec les données saisies.
- Corrige les 3 CS8602 de `LoadingPageViewModel.cs`.

### 4 bis. Faits du backend reçus le 09/11/2026 (à intégrer)

Confirmés par la session « appli degestion backend », sur `origin/dev` :

- **`DELETE commandes/{commandId}/frais-de-port`** répond **204, sans corps**, même si la commande n'a aucun frais : l'appel est
  idempotent. 404 seulement si la commande n'existe pas. `frais-de-port/{id}` n'existe pas.
- **Une commande a au plus un frais de port.** `GET commandes/{id}/frais-de-port` rend un seul objet, et **404
  `{"error": "No shipping cost…"}` quand il n'y en a pas : ce n'est pas une erreur**. Avec `ApiException`, ce cas doit donner
  « aucun frais de port », sans message d'erreur.
- **`POST` sur une commande qui a déjà un frais** échoue en 500 (contrainte unique), et répond 201 sans l'objet créé. L'écran ne
  doit pas proposer l'ajout quand un frais existe : changer de frais, c'est supprimer puis créer.
- **Bug existant à corriger, prix d'achat des produits** : la plateforme stocke `purchase_price` et `price` **en centimes**, et
  `Product::setPurchasePrice` recalcule le prix de vente. Or `ProductFormModel` envoie le prix **tel que saisi**, et
  `HttpService.PostMultipartAsyncWithAuth` utilise `data.purchasePrice.ToString()`, qui dépend de la langue de l'appareil
  (« 12,5 » sur un appareil français : erreur 500). Corrige les deux :
  - envoyer un **entier en centimes** (`Math.Round(valeur * 100)`), avec `CultureInfo.InvariantCulture` ;
  - vérifier où le prix d'achat est relu et affiché, et diviser par 100 à cet endroit. Liste ces endroits dans le plan ;
  - **ne pas toucher au prix des frais de port** : le formulaire envoie déjà `Prix * 100`.

### 5. Vérification

- Compiler Android (`-t:Rebuild`), compter les avertissements, et contrôler l'encodage contre `feature/lot-2-session`.
  **On attend moins d'avertissements** : plus de CS0472 ni de MVVMTK0039.
- **Aucun lancement de l'application et aucun appel au backend.** Je ferai les essais.
- **Liste des essais** à mettre dans le plan : suppression réussie (200 et 204), suppression refusée, création refusée
  (`VALIDATION_FAILED`), liste en erreur, pas de réseau, session expirée pendant une liste, suppression d'un frais de port.

## Hors du lot (ne pas toucher)

- **Aucun fichier XAML ni aucune mise en page** : je m'occupe de la nouvelle interface, et les deux chantiers ne doivent pas se
  croiser. Les textes d'erreur écrits dans les ViewModels sont dans le lot.
- La navigation, la refonte des popups, `INavigationService`, les avertissements de nullabilité hors de ce qui est cité.
- Le rattachement des produits existants (`docs/lot-rattachement-produits.md`).
- Les routes du backend.
- **Mêmes règles que d'habitude** : pas de changement d'encodage, de BOM, de fins de ligne ; pas de reformatage (`git diff --stat`
  et `git diff -w --stat` doivent être proches) ; sorties de commandes collées telles quelles ; aucune clé, aucun identifiant
  ni aucune adresse de site ; pas de commit.
