# Adaptation G1-G2 : plan de Gemini

Branche : `feature/adaptation-g1-g2` (créée depuis `main`)  
Base de comparaison : `main`

---

## Ce que j'ai lu

1. `docs/gemini/lot-g1-g2-consigne.md:1-48` : consignes des 5 points pour adapter l'application aux livraisons G1 et G2 déjà en production.
2. `docs/backend-lots-gestion-G1-G4.md:1-109` : contrats G1 (centimes entiers, lecture de `fields`, `currency` dans les réponses) et G2 (clôture 200 avec objet JSON, 409 `ALREADY_CLOSED`).
3. `Easymakemoney/UseCase/ListCollectionUseCase/SaveCollectionUseCase.cs:25` : calcul `(collectionForm.BudgetCollection ?? 0) * 100` sans arrondi entier explicite (`long`).
4. `Easymakemoney/UseCase/LisCommandeUseCase/SaveCommandUseCase.cs:17` : calcul `(commandForm.Budget ?? 0) * 100` sans arrondi entier explicite (`long`).
5. `Easymakemoney/UseCase/ListFraisDePortUseCase/SaveFraisDePortUseCase.cs:21` : calcul `FraisDePortForm.Prix * 100` sans arrondi entier explicite (`long`).
6. `Easymakemoney/UseCase/ListNoteDeFraisUseCase/SaveNoteDeFraisUseCase.cs:21` : calcul `NoteDeFraisForm.Montant * 100` sans arrondi entier explicite (`long`).
7. `Easymakemoney/Models/ListCollection.cs:6` : propriété `budgetCollection` déclarée en `double?`.
8. `Easymakemoney/Models/ListCommand.cs:8` : propriété `budget` déclarée en `double?`.
9. `Easymakemoney/Models/FraisDePort.cs:7` : propriété `price` déclarée en `double?`.
10. `Easymakemoney/Models/NoteDeFrais.cs:9` : propriété `montant` déclarée en `double?`.
11. `Easymakemoney/Models/Api/ApiError.cs:85-113` : extraction d'erreurs de validation limitée à `errors` et `violations` ; `fields` n'est pas lu.
12. `Easymakemoney/Models/Api/ApiError.cs:139-156` : `GetFallbackMessage` ne gère pas les codes `VALIDATION_FAILED` ni `ALREADY_CLOSED`.
13. `Easymakemoney/Services/HttpService.cs:162-184` : `HandleResponseAsync<T>` ne lit pas `currency` dans le JSON.
14. `Easymakemoney/Converters/CurrencyConverter.cs:9-16` : utilise `{0:C}` avec `value is double amount`, sans prise en compte de la devise du site ni du type `long`.
15. `Easymakemoney/Utilities/ChartGenerator.cs:28` : `ValueLabel = (data.Data2 / 100).ToString("C")` utilise la culture locale de l'appareil et non la devise du site.
16. `Easymakemoney/Services/Tenant/SiteDataCleaner.cs:18-36` : nettoie SecureStorage, Preferences, Session et TenantStore, mais aucune devise du site mémorisée.
17. `Easymakemoney/Services/CollectionCloseService.cs:13-17` : `PostAsyncWithAuth<string>(url, null)` attendait une chaîne et échoue sur un objet JSON retourné par G1/G2.
18. `Easymakemoney/Services/ICollectionCloseService.cs:5` : signature renvoyant `Task<string>`.
19. `Easymakemoney/UseCase/CollectionCloseUseCase/GetCollectionCloseUseCase.cs:13-16` : relaye le retour `string`.
20. `Easymakemoney/ViewModels/Dashboard/DashBoardCollectionViewModel.cs:68-91` : attend `result != null` (chaîne) pour afficher l'info, ne recharge pas le tableau de bord en cas de succès, et ne gère pas le 409 `ALREADY_CLOSED`.

---

## Prérequis à installer ou à vérifier (commandes exactes, rien n'est lancé)

Aucun outil ni paquet à installer. Le SDK .NET 10 et les workloads MAUI sont déjà en place.

Commandes de contrôle d'état initial :
```bash
git branch --show-current
git status
```

---

## Étapes

### 1. Montants envoyés en entiers et passage des modèles en `long`

- **Écritures ×100 dans les 4 UseCases** :
  - `Easymakemoney/UseCase/ListCollectionUseCase/SaveCollectionUseCase.cs` :
    `budgetCollection = (long)Math.Round((collectionForm.BudgetCollection ?? 0) * 100)`
  - `Easymakemoney/UseCase/LisCommandeUseCase/SaveCommandUseCase.cs` :
    `budget = (long)Math.Round((commandForm.Budget ?? 0) * 100)`
  - `Easymakemoney/UseCase/ListFraisDePortUseCase/SaveFraisDePortUseCase.cs` :
    `price = (long)Math.Round((FraisDePortForm.Prix ?? 0) * 100)`
  - `Easymakemoney/UseCase/ListNoteDeFraisUseCase/SaveNoteDeFraisUseCase.cs` :
    `montant = (long)Math.Round((NoteDeFraisForm.Montant ?? 0) * 100)`
  - **Pourquoi** : Garantit qu'un montant décimal saisi (ex. `12,345`) devient `1235` centimes entiers et non `1234.5`, ce qui provoquerait une erreur HTTP 422 `MUST_BE_INTEGER_CENTS`.

- **Analyse du passage des types des modèles en `long?` (ou `long`)** :
  - *Fichiers modèles touchés* :
    - `Easymakemoney/Models/ListCollection.cs` : `public long? budgetCollection { get; set; }`
    - `Easymakemoney/Models/ListCommand.cs` : `public long? budget { get; set; }`
    - `Easymakemoney/Models/FraisDePort.cs` : `public long? price { get; set; }`
    - `Easymakemoney/Models/NoteDeFrais.cs` : `public long? montant { get; set; }`
  - *Pourquoi les passer en `long` ?*
    1. À l'écriture, la sérialisation JSON via `JsonConvert.SerializeObject` produit un entier JSON pur (ex: `125000`) et élimine tout risque de sérialisation flottante `125000.0`.
    2. C'est conforme à la note G1 de `docs/backend-lots-gestion-G1-G4.md` : « Passer les modèles en entiers (`long`) : les réponses rendront des entiers. Les ÷100 en lecture et ×100 en écriture restent valables. »
  - *Ce que cela change à la lecture* :
    - La désérialisation JSON (`JsonConvert.DeserializeObject<T>`) accepte directement les entiers renvoyés par le backend G1 sans conversion intermédiaire.
    - Dans le XAML, les bindings envoient désormais un `long?` (ou `long`) à `CurrencyConverter`. Si `CurrencyConverter` ne testait que `value is double`, le formatage échouerait et afficherait le nombre brut. Il est donc impératif que `CurrencyConverter` gère `long` (et tous types numériques).

### 2. Erreurs de validation : lecture de `fields` dans `ApiError`

- **Fichier modifié** : `Easymakemoney/Models/Api/ApiError.cs`
- **Changements prévus** :
  - Dans `FromHttpResponse`, après l'extraction des listes `errors` et `violations`, ajouter la lecture de l'objet JSON `fields` :
    ```csharp
    var fieldsToken = jObject["fields"];
    if (fieldsToken is JObject jFields)
    {
        foreach (var prop in jFields.Properties())
        {
            var fieldName = prop.Name;
            var errorCode = prop.Value?.ToString() ?? string.Empty;
            var translated = TranslateValidationCode(errorCode);
            validationErrors.Add(new ApiValidationError { Path = fieldName, Message = translated });
        }
    }
    ```
  - Ajouter la méthode statique de traduction des codes :
    ```csharp
    public static string TranslateValidationCode(string errorCode)
    {
        return errorCode switch
        {
            "REQUIRED" => "Ce champ est obligatoire.",
            "MUST_BE_INTEGER_CENTS" => "Le montant doit être un entier en centimes.",
            "MUST_BE_NUMBER" => "Ce champ doit être un nombre valide.",
            "INVALID_DATE" => "La date est invalide.",
            "INVALID_ID" => "L'identifiant est invalide.",
            _ => !string.IsNullOrWhiteSpace(errorCode) ? errorCode : "Valeur invalide."
        };
    }
    ```
  - Dans `GetFallbackMessage`, ajouter les cas spécifiques :
    - `"VALIDATION_FAILED" => "Données non valides."`
    - `"ALREADY_CLOSED" => "Cet élément est déjà clôturé."`
- **Pourquoi** : Le backend G1 envoie `422 {"error":"VALIDATION_FAILED","fields":{"budget":"MUST_BE_INTEGER_CENTS"}}`. `DialogService` sait déjà afficher les `Errors` sous forme de liste à puces (`• budget: Le montant doit être un entier en centimes.`). L'utilisateur bénéficie ainsi d'un message d'erreur clair en français.

### 3. Devise du site (`SiteCurrency`), `CurrencyConverter` et `ChartGenerator`

- **Création de `Easymakemoney/Utilities/SiteCurrency.cs`** :
  - Classe statique simple thread-safe :
    - `SetCurrency(string? currencyCode)` : mémorise le code ISO (ex: `"EUR"`, `"USD"`), normalisé en majuscules.
    - `CurrentCurrency` : accesseur du code mémorisé.
    - `Reset()` : remet `_currentCurrency = null`.
    - `GetCurrencySymbol(string isoCurrencyCode)` : fonction pure interrogeant dynamiquement les cultures du système (`CultureInfo.GetCultures(CultureTypes.SpecificCultures)`) via `RegionInfo.ISOCurrencySymbol` pour récupérer le `RegionInfo.CurrencySymbol`. Si aucun symbole spécifique n'est trouvé, renvoie le code ISO lui-même. **Aucun symbole écrit en dur**.
    - `FormatCents(double cents, CultureInfo? culture = null, string? currencyCode = null)` : fonction pure qui :
      - prend le montant en centimes et le divise par 100 ;
      - utilise `currencyCode ?? CurrentCurrency` ;
      - **sans code connu** : affiche le nombre sans symbole via `amount.ToString("N2", culture)` ;
      - **avec code connu** : clone le `NumberFormatInfo` de la culture de l'appareil (`culture ?? CultureInfo.CurrentCulture`), remplace `nfi.CurrencySymbol` par le symbole découvert, et retourne `amount.ToString("C", nfi)`. La culture de l'appareil gère la langue, les séparateurs et le positionnement du symbole monétaire.
- **Extraction dans `Easymakemoney/Services/HttpService.cs`** :
  - Dans `HandleResponseAsync<T>`, inspecter `responseJson` via `JsonDocument` :
    - si objet JSON à la racine avec clé `currency` (ou `Currency`) de type string : appel `SiteCurrency.SetCurrency(val)`.
    - si tableau JSON non vide dont le 1er élément est un objet portant la clé `currency` : appel `SiteCurrency.SetCurrency(val)`.
    - opération encapsulée dans un `try/catch` silencieux pour ne jamais perturber la désérialisation métier, **sans modifier les modèles**.
- **Réinitialisation dans `Easymakemoney/Services/Tenant/SiteDataCleaner.cs`** :
  - Dans `ClearAllAsync()`, ajouter l'appel `SiteCurrency.Reset()`.
- **Adaptation de `Easymakemoney/Converters/CurrencyConverter.cs`** :
  - Support de tous les types numériques (`long`, `double`, `int`, `decimal`, `float`) et parsing de chaînes.
  - Appel de `SiteCurrency.FormatCents(cents, culture)`.
- **Adaptation de `Easymakemoney/Utilities/ChartGenerator.cs`** :
  - Remplacement de `(data.Data2 / 100).ToString("C")` par `SiteCurrency.FormatCents(data.Data2, CultureInfo.CurrentCulture)`.

### 4. Clôture d'une collection (G2)

- **Fichiers touchés** :
  - `Easymakemoney/Services/ICollectionCloseService.cs` :
    Nouvelle signature : `Task<bool> CloseCollectionAsync(int collectionId);`
  - `Easymakemoney/Services/CollectionCloseService.cs` :
    ```csharp
    public async Task<bool> CloseCollectionAsync(int collectionId)
    {
        var url = $"dashboard/collection/{collectionId}/close";
        await _httpService.PostAsyncWithAuth<object>(url, null);
        return true;
    }
    ```
    Ne lit plus le corps comme une chaîne : `PostAsyncWithAuth<object>` réussit sur le statut 200 renvoyant l'objet JSON du tableau de bord (ou `{"message":...}`).
  - `Easymakemoney/UseCase/CollectionCloseUseCase/GetCollectionCloseUseCase.cs` :
    Adapte la méthode pour appeler `await _collectionCloseService.CloseCollectionAsync(collectionId);` et renvoyer `Task<bool>`.
  - `Easymakemoney/ViewModels/Dashboard/DashBoardCollectionViewModel.cs` :
    Mise à jour de `CloseCollection()` :
    ```csharp
    [RelayCommand]
    public async Task CloseCollection()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        try
        {
            await _getCollectionCloseUseCase.CloseCollectionAsync(CollectionId);
            await _dialogService.ShowInfoAsync("Collection clôturée");
            await GetDashBoardCollectionAsync(CollectionId);
            IsClosed = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await _dialogService.ShowErrorAsync(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }
    ```
    En cas de succès : affiche « Collection clôturée » et recharge le tableau de bord.  
    En cas de 409 `ALREADY_CLOSED` : l'exception `ApiException` est levée et passée à `_dialogService.ShowErrorAsync(ex)`, affichant « Cet élément est déjà clôturé. ».

---

## Comment je vérifie

1. **Compilation Android Rebuild** :
   ```bash
   dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -c Debug -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home
   ```
   Résultat attendu : `0 Error(s)`, et un décompte des avertissements inférieur ou égal à 320 (aucun avertissement supplémentaire introduit).

2. **Contrôle d'encodage et BOM contre `main`** :
   ```bash
   for f in $(git diff --name-only main); do a=$(git show main:"$f" 2>/dev/null | file -b -); b=$(file -b "$f" 2>/dev/null); [ -e "$f" ] && [ "$a" != "$b" ] && echo "DIFF $f: '$a' vs '$b'"; done; echo fin
   ```
   Résultat attendu : aucun BOM ajouté ni retiré ; encodage UTF-8 préservé.

3. **Vérification `async void`** :
   ```bash
   git grep -n "async void" Easymakemoney/ViewModels
   ```
   Résultat attendu : uniquement les 8 méthodes d'exception autorisées.

4. **Liste des essais fonctionnels à prévoir (tests automatisés lot 4 / recette sur `demo`)** :
   - *Montants entiers* :
     - Envoi d'un montant décimal (ex. `12.345`) dans les 4 formulaires -> vérifier que la valeur envoyée en centimes est `1235` et que le backend renvoie 201/200 sans 422.
   - *Validation `fields`* :
     - Simuler un 422 avec payload `{"error":"VALIDATION_FAILED","fields":{"budget":"MUST_BE_INTEGER_CENTS"}}` -> vérifier que le dialogue affiche « Données non valides.\n• budget: Le montant doit être un entier en centimes. ».
     - Simuler les codes `REQUIRED`, `MUST_BE_NUMBER`, `INVALID_DATE`, `INVALID_ID`.
   - *Devise du site* :
     - Réponse portant `"currency": "EUR"` -> formatage `1 250,00 €` en culture `fr-FR`.
     - Réponse portant `"currency": "USD"` -> formatage `1 250,00 $` en culture `fr-FR` ou `$1,250.00` en culture `en-US`.
     - Sans devise mémorisée (`null`) -> formatage `1 250,00` sans symbole.
     - Appel `SiteDataCleaner.ClearAllAsync()` -> réinitialisation de `SiteCurrency.CurrentCurrency` à `null`.
     - Graphiques `ChartGenerator` : étiquettes formatées avec la devise du site.
   - *Clôture de collection* :
     - Appel de `CloseCollection()` sur collection ouverte -> succès 200 objet JSON -> affichage du dialogue « Collection clôturée » et rechargement des données du tableau de bord.
     - Appel sur collection déjà fermée -> 409 `ALREADY_CLOSED` -> dialogue d'erreur affiché.

---

## Risques et points incertains

1. **Typage des modèles et liaisons XAML** :
   Passer `budgetCollection`, `budget`, `price` et `montant` en `long?` au lieu de `double?` pourrait impacter des converters s'ils n'acceptaient que `double`. Pour neutraliser totalement ce risque, `CurrencyConverter` sera doté d'une conversion universelle acceptant `long`, `int`, `double`, `decimal` et `float`.
2. **Performances de l'extraction de `currency` dans `HttpService`** :
   L'inspection JSON avec `JsonDocument` est très rapide et peu consommatrice en mémoire, mais elle sera strictement limitée à la détection de la clé `currency` à la racine ou sur le premier élément d'un tableau pour un surcoût négligeable.

---

## Questions pour Claude ou le propriétaire

1. **Nom de la méthode dans `GetCollectionCloseUseCase`** :
   Aujourd'hui la méthode s'appelle `GetCollectionClose(int CollectionId)`. Pour respecter les conventions de nommage et refléter une action sans retour de chaîne, préférez-vous la renommer en `CloseCollectionAsync(int collectionId)` ou conserver le nom historique `GetCollectionClose` ?
2. **Affichage du symbole ou code ISO** :
   La fonction pure découverte dynamiquement via `RegionInfo` extrait le symbole usuel (`€`, `$`, `£`). Si une devise n'a pas de symbole reconnu dans le système, le code ISO (ex: `CHF`) sera affiché. Est-ce conforme à vos attentes ?

