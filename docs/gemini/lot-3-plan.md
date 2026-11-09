# Lot 3 : plan de Gemini (routes et erreurs)

## Ce que j'ai lu

### 1. Documents de référence
- `docs/gemini/lot-3-consigne.md` (y compris la section « 4 bis. Faits du backend ») : objectifs, faits relevés, couverture des 5 points et des faits backend, interdiction absolue de toucher aux fichiers XAML.
- `docs/backend-contrat-gestion.md` : contrat des codes d'erreur backend (codes stables en `MAJUSCULES_SOULIGNÉES`, `VALIDATION_FAILED` avec `errors[{path, message}]`, 429 `RATE_LIMITED`, routes gelées de gestion, DELETE commande en 204).
- `docs/revue-architecture.md` : §2 A4 (HttpService retournant default), C2 (appels directs UI / messages d'erreur dispersés), C4 (async void dans les commandes), tableau §3 ligne 3 (`ApiException`, `IDialogService`).
- `docs/gemini/lot-2-revue-code.md` : remarque sur `SessionExpiredException` qui doit être ignorée par les écrans pour ne pas doubler l'alerte de session, et constat des 3 CS8602 dans `LoadingPageViewModel.cs`.
- `Easymakemoney/Models/Api/ApiError.cs` : extracteur de code d'erreur HTTP existant, à enrichir avec la liste `Errors`.

### 2. Code analysé et constats relevés (avec chemin:ligne)
- **`HttpService` étouffe toutes les erreurs et ne gère pas 204** :
  - `Easymakemoney/Services/HttpService.cs:19-28` : `GetAsync` ne teste que `HttpStatusCode.OK` (200), renvoie `default` pour tout le reste sans exception.
  - `Easymakemoney/Services/HttpService.cs:45-54` : `PostAsyncWithAuth` ne teste que `OK` (200) ou `Created` (201), renvoie `default` sans exception pour tout le reste.
  - `Easymakemoney/Services/HttpService.cs:56-67` : `DeleAsyncWithAuth` ne teste que `OK` (200). Une réponse 204 No Content (pourtant standard sur les DELETE d'API Platform) renvoie `false` !
  - `Easymakemoney/Services/HttpService.cs:85-93` : `PostAsyncWithoutAuth` ne teste que 200/201.
  - `Easymakemoney/Services/HttpService.cs:129-137` : `PostMultipartAsyncWithAuth` ne teste que 200/201.
  - `Easymakemoney/Services/HttpService.cs:43,83,127` : résidus de `Debug.WriteLine($"Response Status Code: {response.StatusCode}");`.
- **Bug du prix d'achat des produits (4 bis)** :
  - `Easymakemoney/ViewModels/FormModels/ProductFormModel.cs:24-28, 142` : `ProductFormModel` stocke `PurchasePrice` sous forme de double en unités monétaires (ex. `12.5` pour 12,50 €) et l'affecte tel quel à `ListProduct.purchasePrice`.
  - `Easymakemoney/Services/HttpService.cs:102` : `content.Add(new StringContent(data.purchasePrice.ToString()), "purchasePrice");` sérialise le double avec la culture courante du téléphone. Sur un appareil en français, cela envoie `"12,5"`, ce qui provoque une erreur 500 du backend Symfony (qui attend un entier en centimes : `1250`).
  - *Inventaire des points de relecture et d'affichage du prix produit* :
    - `Easymakemoney/Models/ListProduct.cs:9-10` : porte `public double price { get; set; }` et `public double purchasePrice { get; set; }`. Reçus en centimes depuis le backend (`GET /api/commandes/{id}/products`).
    - `Easymakemoney/Converters/CurrencyConverter.cs:13` : effectue déjà la division par 100 : `string.Format(culture, "{0:C}", amount / 100);`.
    - `Easymakemoney/Views/Dasboard/DashBoardProductPage.xaml:103` : `TotalValue="{Binding DashBoardProduct.price, Converter={StaticResource CurrencyConverter}}"`.
    - `Easymakemoney/Views/Dasboard/DashBoardProductPage.xaml:105` : `ExpensesValue="{Binding DashBoardProduct.purchasePrice, Converter={StaticResource CurrencyConverter}}"`.
    - `Easymakemoney/Views/Lists/ListNewProductPage.xaml:155` : `Text="{Binding price, Converter={StaticResource CurrencyConverter}}"`.
    - *Frais de port intacts* : `Easymakemoney/UseCase/ListFraisDePortUseCase/SaveFraisDePortUseCase.cs:21` effectue déjà `price = FraisDePortForm.Prix * 100`.
- **Frais de port et faits backend (4 bis)** :
  - `Easymakemoney/Services/ListFraisDePortService.cs:24` : appelle `DELETE frais-de-port/{id}` qui n'existe pas (404). La route réelle confirmée sur `origin/dev` est `DELETE /api/commandes/{commandId}/frais-de-port` (répond 204 sans corps, idempotente).
  - `Easymakemoney/Services/ListFraisDePortService.cs:12-14` : `GetListFraisDePort` appelle `GET commandes/{commandId}/frais-de-port`. Quand la commande n'a pas de frais, le backend répond 404 (`{"error": "No shipping cost…"}`). Ce 404 ne doit pas être traité comme une erreur mais comme une absence de frais (retour `null` / liste vide sans dialogue).
  - `Easymakemoney/ViewModels/Lists/ListNewFraisDePortViewModel.cs:201-205` : comporte un contrôle `if (ListFraisDePorts.Count > 0)` interdisant d'ouvrir le formulaire si un frais est déjà présent (le backend interdit 2 frais sur une commande et répond 500 sur contrainte unique).
- **Les 7 services de suppression comparent un `bool` à `null` (CS0472)** :
  `result` est de type `bool`. La comparaison `result != null` vaut toujours `true`, masquant tout échec :
  - `Easymakemoney/Services/ListCollectionService.cs:28`
  - `Easymakemoney/Services/ListCommandService.cs:29`
  - `Easymakemoney/Services/ListFourniseursServices.cs:36`
  - `Easymakemoney/Services/ListFraisDePortService.cs:26`
  - `Easymakemoney/Services/ListNoteDeFraisServices.cs:35`
  - `Easymakemoney/Services/ListProductService.cs:32`
  - `Easymakemoney/Services/ListProductVarianstService.cs:31`
- **Les services de création comparent un `object` désérialisé à `null`** :
  - `Easymakemoney/Services/ListCollectionService.cs:20-21` (`result != null`)
  - `Easymakemoney/Services/ListCommandService.cs:21-22` (`result != null`)
  - `Easymakemoney/Services/ListFourniseursServices.cs:21-22` (`result != null`)
  - `Easymakemoney/Services/ListFraisDePortService.cs:19-20` (`result != null`)
  - `Easymakemoney/Services/ListNoteDeFraisServices.cs:20-21` (`result != null`)
  - `Easymakemoney/Services/ListProductVarianstService.cs:23-24` (`result != null`)
  - `Easymakemoney/Services/ListProductService.cs:23-25` (`result != null`)
- **Recensement exact des 41 `async void` dans `ViewModels/` (20 fichiers)** :
  - *Commandes `[RelayCommand]` générant l'avertissement MVVMTK0039 (18 occurrences)* :
    - `Easymakemoney/ViewModels/AppShellViewModel.cs:21` (`SignOut`)
    - `Easymakemoney/ViewModels/Dashboard/DashboardPageViewModel.cs:15, 22, 29` (`NavigateToAllListsPage`, `NavigateTDashboardPage`, `NavigateToStatistiquesPage`)
    - `Easymakemoney/ViewModels/Dashboard/DashBoardCollectionViewModel.cs:160` (`ItemTapped`)
    - `Easymakemoney/ViewModels/Dashboard/DashBoardCommandViewModel.cs:117` (`ItemTapped`)
    - `Easymakemoney/ViewModels/Lists/AllListsViewModel.cs:11, 17, 24, 31` (`NavigateToCollection`, `NavigateToNoteDeFrais`, `NavigateToNoteFournisseur`, `NavigateToFraisDePort`)
    - `Easymakemoney/ViewModels/FormModels/ProductFormModel.cs:112` (`OnSelectImage`)
    - `Easymakemoney/ViewModels/Statistiques/StatistiquesViewModel.cs:11, 16, 21, 26, 31, 36, 41, 47` (8 commandes de navigation vers les statistiques)
  - *Gestionnaires de commandes `ICommand` écrits à la main avec `new RelayCommand<T>` (14 occurrences)* :
    - `Easymakemoney/ViewModels/Components/BottomSheetPopupViewModel.cs:171` (`Save`)
    - `Easymakemoney/ViewModels/Lists/ListNewCollectionViewModel.cs:87, 96` (`OnItemTapped`, `DeleteCollection`)
    - `Easymakemoney/ViewModels/Lists/ListNewCommandViewModels.cs:58, 75` (`DeleteCommand`, `OnItemTapped`)
    - `Easymakemoney/ViewModels/Lists/ListNewFournisseurViewModel.cs:65, 89` (`DeleteFournisseur`, `OnItemTapped`)
    - `Easymakemoney/ViewModels/Lists/ListNewFraisDePortViewModel.cs:113, 134` (`DeleteFraisDePort`, `OnItemTapped`)
    - `Easymakemoney/ViewModels/Lists/ListNewNoteDeFraisViewModel.cs:80, 95` (`DeleteNoteDeFrais`, `OnItemTapped`)
    - `Easymakemoney/ViewModels/Lists/ListNewProductVariantViewModel.cs:76` (`DeleteProductVariant`)
    - `Easymakemoney/ViewModels/Lists/ListNewProductViewModels.cs:76, 85` (`OnItemTapped`, `DeleteProduct`)
  - *Méthodes de chargement asynchrone des listes déroulantes dans les FormModels (8 occurrences)* :
    - `Easymakemoney/ViewModels/FormModels/CommandFormModel.cs:51` (`LoadTransporteurs`)
    - `Easymakemoney/ViewModels/FormModels/FournisseurFormModel.cs:65` (`LoadTypeFournisseur`)
    - `Easymakemoney/ViewModels/FormModels/FraisDePortFormModel.cs:61` (`LoadTransporteur`)
    - `Easymakemoney/ViewModels/FormModels/ProductFormModel.cs:90, 101` (`LoadCategories`, `LoadStyles`)
    - `Easymakemoney/ViewModels/FormModels/ProductVariantFormModel.cs:45, 56` (`LoadColors`, `LoadSizes`)
  - *Initialisation asynchrone de démarrage (1 occurrence)* :
    - `Easymakemoney/ViewModels/Startup/LoadingPageViewModel.cs:38` (`InitStartup`)
- **Gestion actuelle des erreurs dans les ViewModels** :
  - Textes d'erreur en anglais dispersés (« Unable to get collections », « Failed to delete collection », « Error ») avec `Shell.Current.DisplayAlert` ou `Application.Current.MainPage.DisplayAlert`.
- **3 avertissements CS8602 dans `LoadingPageViewModel.cs`** :
  - Lignes 135, 152, 169 : `await Shell.Current.GoToAsync("//DashboardPage");` sans test de nullabilité sur `Shell.Current`.

---

## Prérequis à installer ou à vérifier (commandes exactes, rien n'est lancé)

Aucun outil ni paquet externe n'est requis. La solution utilise les bibliothèques déjà installées (`CommunityToolkit.Mvvm`, `Newtonsoft.Json`).

Vérifications de compilation prévues :
```bash
dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home
```

---

## Étapes

### Étape 1 : `ApiError` et création de `ApiException`
1. **Enrichissement de `ApiError` (`Easymakemoney/Models/Api/ApiError.cs`)** :
   - Création de la classe `ApiValidationError` portant `{ public string Path { get; set; } = string.Empty; public string Message { get; set; } = string.Empty; }`.
   - Ajout de la propriété `public List<ApiValidationError> Errors { get; set; } = new();` dans `ApiError`.
   - Dans `ApiError.FromHttpResponse`, extraction du tableau `errors` (format Symfony `{path, message}`) ou `violations` (format API Platform `{propertyPath, message}`).
2. **Création de `ApiException` (`Easymakemoney/Services/Api/ApiException.cs`)** :
   - Classe dérivée de `Exception`, portant :
     - `public ApiError ApiError { get; }`
     - `public HttpStatusCode? StatusCode { get; }`
   - Constructeur `ApiException(ApiError apiError, HttpStatusCode? statusCode = null, Exception? innerException = null)` passant `apiError.Message` au constructeur de base.
   - Méthode statique `public static ApiException NoConnection(Exception? inner = null)` initialisant un `ApiError` avec `Code = "NO_CONNECTION"` et `Message = "Pas de connexion."`.

### Étape 2 : Révision de `HttpService` (`Easymakemoney/Services/HttpService.cs`)
1. **Gestion réseau centralisée** :
   - Encapsulation des appels HTTP dans un bloc `try/catch`.
   - Si `OperationCanceledException` est levée alors que `cancellationToken.IsCancellationRequested` est vrai : relancer l'exception telle quelle (annulation volontaire de l'appelant).
   - Si `HttpRequestException`, `TaskCanceledException` (délai dépassé) ou `IOException` : lever `ApiException.NoConnection(ex)`.
2. **Traitement unifié des réponses HTTP** :
   - **Tous les 2xx (`response.IsSuccessStatusCode`) sont des succès** :
     - Pour un code 204 (No Content) ou un contenu vide/blanc : retourner `default` pour les méthodes typées sans lever d'exception.
     - Si contenu non vide : désérialisation JSON vers `T` et retour.
   - **Hors 2xx** :
     - Lecture du corps texte (`await response.Content.ReadAsStringAsync()`).
     - Construction de `ApiError` via `ApiError.FromHttpResponse(content, response.StatusCode, response.Headers)`.
     - Levée immédiate de `throw new ApiException(apiError, response.StatusCode);`.
3. **Cas de `DeleAsyncWithAuth<T>`** :
   - Si `response.IsSuccessStatusCode` (200, 204, etc.) : retourne `true`.
   - Sinon : lève `ApiException`.
4. **Correction du multipart produit (`PostMultipartAsyncWithAuth`)** :
   - `var purchasePriceCents = (long)Math.Round(data.purchasePrice * 100);`
   - `content.Add(new StringContent(purchasePriceCents.ToString(CultureInfo.InvariantCulture)), "purchasePrice");`
   - L'envoi d'un entier en centimes avec `CultureInfo.InvariantCulture` élimine l'erreur 500 sur les smartphones francophones.
5. **Nettoyage** :
   - Suppression des résidus `Debug.WriteLine($"Response Status Code: ...")`.
   - Conservation stricte des signatures publiques existantes d'`IHttpService`.

### Étape 3 : Correction des services métier et cas d'usage
1. **Les 7 services de suppression** :
   - Remplacement de `return result != null;` (CS0472) par `return result;` dans :
     - `Easymakemoney/Services/ListCollectionService.cs:28`
     - `Easymakemoney/Services/ListCommandService.cs:29`
     - `Easymakemoney/Services/ListFourniseursServices.cs:36`
     - `Easymakemoney/Services/ListFraisDePortService.cs:26`
     - `Easymakemoney/Services/ListNoteDeFraisServices.cs:35`
     - `Easymakemoney/Services/ListProductService.cs:32`
     - `Easymakemoney/Services/ListProductVarianstService.cs:31`
2. **Les 7 services de création** :
   - L'appel `PostAsyncWithAuth<object>` / `PostMultipartAsyncWithAuth<object>` levant désormais une `ApiException` en cas d'erreur serveur ou réseau, le fait que la réponse 200/201 contienne ou non un corps JSON n'est plus une condition d'échec.
   - Remplacement de `var result = await ...; return result != null;` par `await ...; return true;` dans :
     - `Easymakemoney/Services/ListCollectionService.cs:20-21`
     - `Easymakemoney/Services/ListCommandService.cs:21-22`
     - `Easymakemoney/Services/ListFourniseursServices.cs:21-22`
     - `Easymakemoney/Services/ListFraisDePortService.cs:19-20`
     - `Easymakemoney/Services/ListNoteDeFraisServices.cs:20-21`
     - `Easymakemoney/Services/ListProductVarianstService.cs:23-24`
     - `Easymakemoney/Services/ListProductService.cs:23-25`
3. **Correction de la route frais de port (B4 et faits backend 4 bis)** :
   - `Easymakemoney/Services/IListFraisDePortService.cs` :
     - `Task<FraisDePort?> GetListFraisDePort(int commandId);`
     - `Task<bool> DeleteFraisDePort(int commandId);`
   - `Easymakemoney/Services/ListFraisDePortService.cs` :
     - Dans `GetListFraisDePort(int commandId)` :
       ```csharp
       var url = $"commandes/{commandId}/frais-de-port";
       try
       {
           return await _httpService.GetAsync<FraisDePort>(url);
       }
       catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
       {
           // 404 : aucun frais de port pour cette commande, ce n'est pas une erreur
           return null;
       }
       ```
     - Dans `DeleteFraisDePort(int commandId)` :
       ```csharp
       var url = $"commandes/{commandId}/frais-de-port";
       return await _httpService.DeleAsyncWithAuth<object>(url);
       ```
       (Le serveur répond 204 sans corps : `DeleAsyncWithAuth` retourne `true`).
   - `Easymakemoney/UseCase/ListFraisDePortUseCase/GetListFraisDePortUseCase.cs` :
     - Si `GetListFraisDePort` retourne `null`, retourne une collection vide `ObservableCollection<FraisDePort>`.
   - `Easymakemoney/UseCase/ListFraisDePortUseCase/DeleteFraisDePortUseCase.cs` :
     - Prend `int commandId` et appelle `_listFraisDePortService.DeleteFraisDePort(commandId)`.
4. **Services de lecture (`Get...`)** :
   - Les méthodes `Get...` gardent leur repli sur une liste/objet vide si le corps d'une réponse 2xx est vide, mais toute erreur 4xx/5xx remonte désormais en `ApiException`.

### Étape 4 : Création de `IDialogService` et `DialogService`
1. **Interface `IDialogService` (`Easymakemoney/Services/Dialog/IDialogService.cs`)** :
   ```csharp
   namespace Easymakemoney.Services.Dialog;

   public interface IDialogService
   {
       Task ShowErrorAsync(Exception exception);
       Task ShowInfoAsync(string message, string title = "Information");
       Task<bool> ConfirmAsync(string title, string message, string accept = "Oui", string cancel = "Non");
   }
   ```
2. **Implémentation `DialogService` (`Easymakemoney/Services/Dialog/DialogService.cs`)** :
   - Dispatch systématique sur le fil d'interface principal via `MainThread.InvokeOnMainThreadAsync`.
   - Utilisation de `Shell.Current.DisplayAlertAsync(...)`.
   - Comportement de `ShowErrorAsync(Exception exception)` :
     - **`if (exception is SessionExpiredException)` : retour immédiat sans rien afficher** (la session a déjà émis son alerte unique via `SessionExpiryNotifier`).
     - **`if (exception is ApiException apiEx)`** :
       - Message principal : `apiEx.ApiError.Message`.
       - Si `apiEx.ApiError.Errors` contient des éléments : concaténation des erreurs de validation (ex. `\n• {path}: {message}`).
       - Affichage avec le titre `"Erreur"`.
     - **Sinon (exception imprévue)** :
       - Journalisation dans `Debug.WriteLine($"[DialogService] Exception : {exception.GetType().Name} - {exception.Message}")` (aucun jeton ni en-tête sensible).
       - Affichage du message générique en français : `"Une erreur est survenue, réessayez."` avec le titre `"Erreur"`.
   - `ShowInfoAsync` : affiche le dialogue avec le titre fourni et bouton `"OK"`.
   - `ConfirmAsync` : affiche une boîte de confirmation avec deux boutons et retourne le booléen choisi par l'utilisateur.
3. **Enregistrement dans `MauiProgram.cs`** :
   - `builder.Services.AddSingleton<IDialogService, DialogService>();`.

### Étape 5 : Refonte des ViewModels, élimination des `async void` et chemins d'erreur
1. **Correction des 3 avertissements CS8602 dans `LoadingPageViewModel.cs`** :
   - Lignes 135, 152, 169 : sécurisation par `if (Shell.Current is { } shell) await shell.GoToAsync("//DashboardPage");`.
2. **Traitement des 18 commandes `[RelayCommand]` avec MVVMTK0039** :
   - Remplacement de `async void` par `async Task` :
     - `AppShellViewModel.cs` : `SignOut` devient `async Task SignOutAsync()`
     - `DashboardPageViewModel.cs` : `NavigateToAllListsPage`, `NavigateTDashboardPage`, `NavigateToStatistiquesPage` deviennent `async Task`
     - `DashBoardCollectionViewModel.cs` : `ItemTapped` devient `async Task`
     - `DashBoardCommandViewModel.cs` : `ItemTapped` devient `async Task`
     - `AllListsViewModel.cs` : `NavigateToCollection`, `NavigateToNoteDeFrais`, `NavigateToNoteFournisseur`, `NavigateToFraisDePort` deviennent `async Task`
     - `FormModels/ProductFormModel.cs` : `OnSelectImage` devient `async Task`
     - `StatistiquesViewModel.cs` : 8 méthodes `NavigateTo...` deviennent `async Task`
   - Chacune de ces méthodes enveloppe ses opérations dans un `try/catch (Exception ex)` avec appel à `_dialogService.ShowErrorAsync(ex)` (ou redirection).
3. **Traitement des commandes manuelles `ICommand` dans les ViewModels de listes** :
   - Remplacement de `new RelayCommand<T>(...)` par `new AsyncRelayCommand<T>(...)` avec méthodes retournant `Task` :
     - `ListNewCollectionViewModel.cs` : `ItemTappedCommand`, `DeleteCollectionCommand`
     - `ListNewCommandViewModels.cs` : `DeleteCommand`, `ItemTappedCommand`
     - `ListNewFournisseurViewModel.cs` : `DeleteFournisseurCommand`, `ItemTappedCommand`
     - `ListNewFraisDePortViewModel.cs` : `DeleteFraisDePortCommand` (qui passe désormais `CommandId`), `ItemTappedCommand`
     - `ListNewNoteDeFraisViewModel.cs` : `DeleteNoteDeFraisCommand`, `ItemTappedCommand`
     - `ListNewProductVariantViewModel.cs` : `DeleteProductVariantCommand`
     - `ListNewProductViewModels.cs` : `DeleteProductCommand`, `ItemTappedCommand`
   - Dans chaque méthode de suppression :
     - Exécution du UseCase dans un `try/catch`.
     - Si succès : rafraîchissement de la liste et affichage du message en français via `_dialogService.ShowInfoAsync("... supprimé")`.
     - Si exception : `await _dialogService.ShowErrorAsync(ex);`.
   - Dans chaque méthode `GetList...` :
     - Remplacement des messages en anglais (`"Unable to get collections"`) par `await _dialogService.ShowErrorAsync(ex);`.
4. **Gestion des frais de port dans `ListNewFraisDePortViewModel.cs`** :
   - `GetListFraisDePort()` : en cas de 404 du serveur, `GetListFraisDePortUseCase` retourne une liste vide. L'écran affiche « aucun frais de port », sans message d'erreur.
   - `ShowBottomSheet()` : si `ListFraisDePorts.Count > 0`, refus d'ouvrir le formulaire avec avertissement `_dialogService.ShowInfoAsync("Un frais de port existe déjà pour cette commande.")`.
5. **`BottomSheetPopupViewModel.cs` (gestion des erreurs et maintien ouvert)** :
   - Transmission de `IDialogService`.
   - Méthode `Save` : transformation en `Task SaveAsync()` (ou commande asynchrone).
   - `await _popup.CloseAsync()` reste placé **après** le bloc des `ExecuteAsync`.
   - En cas d'exception, celle-ci est interceptée dans le `catch`, `await _dialogService.ShowErrorAsync(ex)` est appelé, et **le popup reste ouvert avec toutes les données saisies intactes**.
6. **Exceptions légitimes restant en `async void` (avec `try/catch` complet)** :
   - Les méthodes `Load...` appelées depuis les constructeurs de `FormModels` (`CommandFormModel.LoadTransporteurs`, `FournisseurFormModel.LoadTypeFournisseur`, `FraisDePortFormModel.LoadTransporteur`, `ProductFormModel.LoadCategories`, `ProductFormModel.LoadStyles`, `ProductVariantFormModel.LoadColors`, `ProductVariantFormModel.LoadSizes`) : elles ne peuvent pas être `Task` dans un constructeur C#. Elles sont sécurisées avec un bloc `try/catch (Exception ex)` complet appelant `_dialogService.ShowErrorAsync(ex)`.
   - `LoadingPageViewModel.InitStartup` : appelée depuis le constructeur, reste `async void` avec son `try/catch` complet.
   - Les gestionnaires d'événements UI de pages (`OnAppearing` dans les vues) : restent `async void` avec `try/catch`.
   - Aucune exception non gérée ne peut désormais remonter au runtime et faire planter l'application.
7. **Règle absolue sur le XAML** :
   - **Aucun fichier XAML ni aucune vue n'est modifié.** Seuls les fichiers C# de la couche applicative (`Services/`, `ViewModels/`, `UseCase/`, `Models/`, `Converters/`) sont touchés.

---

## Comment je vérifie

### 1. Compilation Android complète (`-t:Rebuild`)
Commande à exécuter :
```bash
dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -c Debug -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home
```
Résultats attendus :
- **0 Erreur(s)**.
- **Réduction significative des avertissements** :
  - **-7 CS0472** (les 7 comparaisons `bool != null` sont éliminées, passant de 7 à 0).
  - **-18 MVVMTK0039** (les 18 commandes `async void` converties en `async Task`, passant de 18 à 0).
  - **-3 CS8602** (les 3 déréférencements potentiels de `LoadingPageViewModel.cs` corrigés).
  - Soit une baisse attendue d'au moins **28 avertissements** par rapport aux 419 du lot 2 (attendu : ~391 avertissements).

### 2. Décompte réel des avertissements par code
```bash
grep -E 'warning (CS[0-9]+|XC[0-9]+|MVVMTK[0-9]+|XA[0-9]+)' <build.log> | sed 's/ \[.*//' | sort -u | grep -oE '\b(CS[0-9]+|XC[0-9]+|MVVMTK[0-9]+|XA[0-9]+)\b' | sort | uniq -c | sort -nr
```

### 3. Contrôle d'encodage et BOM contre `feature/lot-2-session`
```bash
for f in $(git diff --name-only feature/lot-2-session); do
    a=$(git show feature/lot-2-session:"$f" 2>/dev/null | file -b -)
    b=$(file -b "$f" 2>/dev/null)
    [ -e "$f" ] && [ "$a" != "$b" ] && echo "DIFF $f"
done; echo fin
```

### 4. Liste des essais fonctionnels (réalisés par Claude et le propriétaire sur le téléphone de test)
1. **Suppression réussie (200 et 204)** :
   Supprimer une commande fournisseur (qui répond 204 côté backend API Platform). Vérifier que la ligne disparaît de l'écran et que le message « Commande supprimée » s'affiche sans message d'erreur.
2. **Suppression refusée par le serveur (ex. 403 ou 400)** :
   Tenter de supprimer un élément non supprimable ou verrouillé. Vérifier que la ligne reste affichée, qu'aucun message de succès n'apparaît, et que le message d'erreur réel du serveur s'affiche dans une boîte de dialogue en français.
3. **Création refusée (`VALIDATION_FAILED`)** :
   Dans un formulaire de création (ex. collection ou produit), saisir des valeurs invalides provoquant une erreur de validation 422 côté serveur. Vérifier que le popup reste ouvert, que les champs saisis ne sont pas perdus, et que les détails des erreurs champ par champ (`Errors`) sont affichés.
4. **Liste en erreur** :
   Simuler ou provoquer une erreur lors du chargement d'une liste. Vérifier que le message d'erreur du serveur s'affiche via `IDialogService` et qu'aucune boîte d'alerte en anglais (« Unable to get ... ») n'apparaît.
5. **Pas de réseau / mode avion** :
   Activer le mode avion sur le téléphone et tenter une action (chargement, création ou suppression). Vérifier qu'aucun crash n'a lieu et que la boîte de dialogue affiche clairement « Pas de connexion ».
6. **Session expirée pendant la consultation d'une liste** :
   Lorsque le jeton et le refresh token expirent ou sont révoqués pendant qu'un écran effectue une requête. Vérifier que `AuthHandler` intercepte le 401, déclenche `SessionExpiryNotifier` (« Votre session a expiré, reconnectez-vous ») et redirige vers `//LoginPage`, et que l'écran appelant n'affiche **aucune** deuxième alerte redondante (`SessionExpiredException` ignorée par `IDialogService`).
7. **Suppression d'un frais de port (B4 et faits backend)** :
   Ouvrir la liste des frais de port d'une commande existante et cliquer sur l'icône de suppression. Vérifier que la requête émise est bien `DELETE /api/commandes/{commandId}/frais-de-port`, que la réponse 204 sans corps est traitée comme un succès, que le frais de port est retiré de la liste et que le message « Frais de port supprimé » s'affiche.
8. **Consultation d'une commande sans frais de port (faits backend)** :
   Ouvrir la page des frais de port d'une commande n'ayant aucun frais. Vérifier que la réponse 404 du serveur ne produit aucune boîte d'erreur et que l'écran affiche « aucun frais de port ». Vérifier que l'ajout est alors possible.
9. **Interdiction d'ajouter un second frais de port (faits backend)** :
   Sur une commande ayant déjà un frais de port, vérifier que l'ajout d'un second frais n'est pas proposé (ou bloque avec message clair), évitant l'erreur 500 sur contrainte unique.
10. **Création de produit avec prix d'achat à virgule (faits backend)** :
    Créer un produit avec un prix d'achat décimal (ex. `12,50` € sur téléphone français). Vérifier que la valeur envoyée dans le multipart est un entier centimes (`"1250"`) sous `CultureInfo.InvariantCulture`, sans erreur 500 du backend. Vérifier ensuite sur `DashBoardProductPage` que le prix d'achat et le prix de vente s'affichent correctement divisés par 100 (`12,50 €`).

---

## Risques et points incertains

1. **Injection de `IDialogService` dans `BottomSheetPopupViewModel`** :
   `BottomSheetPopupViewModel` n'est pas instancié par le conteneur DI mais créé manuellement avec `new BottomSheetPopupViewModel(...)` dans les ViewModels de listes (`ShowBottomSheetAsync`).
   *Solution* : Injecter `IDialogService` dans les ViewModels de listes (qui sont créés par le DI) et le transmettre en paramètre au constructeur de `BottomSheetPopupViewModel`.
2. **Liaisons des boutons en XAML vers les `ICommand`** :
   Les XAML référencent des propriétés comme `DeleteCollectionCommand`.
   *Solution* : Conserver exactement le nom des propriétés publiques `ICommand`, en remplaçant l'instanciation sous-jacente par `new AsyncRelayCommand<T>(...)`. Le XAML continue de se lier sans modification.
3. **Format des erreurs de validation du backend Symfony / API Platform** :
   Certains contrôleurs Symfony renvoient `{ code: "VALIDATION_FAILED", message: "...", errors: [ { path: "...", message: "..." } ] }` tandis que d'autres (violations API Platform) renvoient `violations: [ { propertyPath: "...", message: "..." } ]`.
   *Solution* : Dans `ApiError.FromHttpResponse`, tolérer à la fois la clé `"errors"` (`path` / `message`) et la clé `"violations"` (`propertyPath` / `message`) pour mapper vers `ApiValidationError`.

---

## Questions pour Claude ou le propriétaire

1. Pour la transmission de `IDialogService` à `BottomSheetPopupViewModel`, valides-tu l'ajout de `IDialogService` dans les constructeurs des ViewModels de listes (qui l'injectent ensuite dans le popup) ?
2. Pour les messages de succès des suppressions, confirmes-tu les libellés simples en français : « Collection supprimée », « Commande supprimée », « Fournisseur supprimé », « Frais de port supprimé », « Note de frais supprimée », « Produit supprimé », « Variante supprimée » ?
