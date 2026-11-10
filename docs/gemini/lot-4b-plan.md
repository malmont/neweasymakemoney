# Lot 4b : plan de Gemini

## Ce que j'ai lu

1. `docs/gemini/lot-4b-consigne.md` (1-25) : les 8 sujets à tester pour le code ajouté depuis le lot 4, maintien des 2 tests ignorés d'activation avec proposition d'une abstraction de `DeviceInfo` sans l'appliquer, Rebuild Android avec 315 avertissements de référence, contrôle d'encodage contre `main`, aucun commit, arrêt après le plan.
2. `docs/periodes-gestion.md` :
   - P1 (70-103) : modèle période, champs `kind`, `periodKey`, `status`, `closedAt`, `reopenedAt`, codes d'erreur `PERIOD_CLOSED`, `PERIOD_NOT_DELETABLE`, `NOT_CLOSED`, `OUTSIDE_PERIOD`.
   - P2 (105-117) : contrat JSON de `GET /api/periodes/{id}/resultat` (`sales`, `cogs`, `expenses`, `result`, `investment`, gestion des articles sans coût, part estimée, perte nette).
   - P3 (120-124) : contrat de reprise d'historique (`gestion/reprise/simulation`, `gestion/reprise/appliquer`, `gestion/reprise/annuler`).
3. `docs/lot-rattachement-produits.md` (1-71) : contrat des routes de rattachement (`GET commandes/products/disponibles?q=...`, `POST commandes/{id}/products/rattacher`, `DELETE commandes/{id}/products/{productId}`).
4. `Easymakemoney/Services/HttpService.cs` (218-246) : `TryExtractCurrency` vérifie en priorité `siteCurrency`, ignore si `purchaseCurrency` est présent (l. 227-228), puis lit `currency` ou `Currency` (l. 230-231).
5. `Easymakemoney/Utilities/SiteCurrency.cs` (42-109) : `GetCurrencySymbol` avec cache `_symbolCache` et recherche déterministe `FindSymbol` triée par longueur puis ordre ordinal, et repli sur le code ISO si inconnu.
6. `Easymakemoney/Services/ProductAttachmentService.cs` (12-36) :
   - `GetAvailableAsync` échappe la chaîne de recherche via `Uri.EscapeDataString(query?.Trim() ?? string.Empty)` (l. 14-15).
   - `AttachAsync` n'envoie la clé `purchasePrice` que si `EditedCostCents.HasValue` (l. 24-27).
   - `DetachAsync` appelle `commandes/{commandId}/products/{productId}` en suppression (l. 34-35).
7. `Easymakemoney/Services/RepriseService.cs` (15-43) : `BaseBody` n'inclut `estimateCoefficient` que s'il est renseigné (l. 38-41) ; envoi de `costs` sur `appliquer` ; annulation sur `gestion/reprise/annuler` avec corps nul.
8. `Easymakemoney/Models/PeriodResult.cs` (7-96) : désérialisation du contrat P2, propriétés calculées `IsLoss` (`result.net < 0`), `HasEstimatedCogs` (`cogs.estimatedItems > 0`), `EstimatedCogsText`, `HasUnknownCost` (`cogs.unknownCostItems > 0`), `UnknownCostTitle`, `UnknownCostDetail`, `HasNetRate` (`result.netRate.HasValue`), `NetRateText`.
9. `Easymakemoney/Models/ListCollection.cs` (16-35) : `IsPeriod` (`kind == "period"`), `CanDelete` (`!IsPeriod`), `StatusText` (`"Clôturée"`, `"À venir"`, `"En cours"`).
10. `Easymakemoney/Models/FraisDePortCommandeRow.cs` (10-35) : `PeriodClosed` verrouillant `CanAddFrais` (`Frais == null && !PeriodClosed`) et `CanDeleteFrais` (`Frais != null && !PeriodClosed`).
11. `Easymakemoney/Models/Api/ApiError.cs` (196-198) : messages de repli pour `PERIOD_CLOSED`, `PERIOD_NOT_DELETABLE`, `NOT_CLOSED`, et fallback HTTP pour `OUTSIDE_PERIOD`.
12. `Easymakemoney/Services/Site/SiteService.cs` (93) : appel direct à `DeviceInfo.Manufacturer` et `DeviceInfo.Model` provoquant `NotImplementedInReferenceAssemblyException` sous `net10.0`.

---

## Prérequis à installer ou à vérifier (commandes exactes, rien n'est lancé)

Aucun outil ni paquet à installer. Le projet `Easymakemoney.Tests` est déjà configuré en `net10.0` et intégré à la solution.

Commandes de vérification prévues :
- `dotnet test Easymakemoney.Tests`
- `dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -c Debug -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home`
- `for f in $(git diff --name-only main); do a=$(git show main:"$f" 2>/dev/null | file -b -); b=$(file -b "$f" 2>/dev/null); [ -e "$f" ] && [ "$a" != "$b" ] && echo "DIFF $f: '$a' vs '$b'"; done; echo fin`

---

## Étapes

### 1. `HttpService` : devise (`Easymakemoney.Tests/Services/HttpServiceTests.cs`)
- **Cas 1 : `siteCurrency` prioritaire sur `currency`** :
  Envoyer une réponse JSON `{ "siteCurrency": "EUR", "currency": "USD" }`. Vérifier que `SiteCurrency.CurrentCurrency` vaut `"EUR"`.
- **Cas 2 : objet avec `purchaseCurrency` ignoré** :
  Positionner `SiteCurrency.SetCurrency("EUR")`. Envoyer une réponse JSON `{ "purchaseCurrency": "USD", "currency": "GBP" }`. Vérifier que `SiteCurrency.CurrentCurrency` reste `"EUR"`.
- **Cas 3 : premier élément d'un tableau avec `siteCurrency` et `purchaseCurrency`** :
  Vérifier les mêmes règles sur la lecture du premier élément d'un tableau JSON.

### 2. `SiteCurrency.GetCurrencySymbol` (`Easymakemoney.Tests/Utilities/SiteCurrencyTests.cs`)
- **Cas 1 : déterminisme et constance** :
  Vérifier que des appels consécutifs à `SiteCurrency.GetCurrencySymbol("CAD")`, `GetCurrencySymbol("EUR")`, `GetCurrencySymbol("USD")` renvoient exactement le même résultat à chaque appel (sans dépendre de l'ordre d'énumération des cultures grâce au cache et au tri ordinal stable).
- **Cas 2 : repli sur le code ISO** :
  Vérifier que `SiteCurrency.GetCurrencySymbol("XYZ")` renvoie `"XYZ"`, et qu'un code fictif sans correspondance régionale retourne le code ISO en majuscules sans lever d'exception.
- **Cas 3 : codes vides ou nuls** :
  Vérifier que `SiteCurrency.GetCurrencySymbol("")` et `GetCurrencySymbol(null!)` renvoient `string.Empty`.

### 3. `ProductAttachmentService` (`Easymakemoney.Tests/Services/ProductAttachmentServiceTests.cs`)
Création de cette nouvelle classe de tests branchée sur `FakeHttpService` :
- **Cas 1 : URL de recherche échappée** :
  Appel à `GetAvailableAsync("robe & lin #1", 1, 30)` : vérifier que l'URL appelée commence par `commandes/products/disponibles?q=robe%20%26%20lin%20%231&page=1&limit=30&locale=fr`.
  Appel avec `query = null` ou `""` : `q=` vide.
- **Cas 2 : `purchasePrice` envoyé seulement si saisi** :
  Appel à `AttachAsync(10, products)` avec :
  - Produit 1 : `id = 101`, `EditedCostCents = 2500`
  - Produit 2 : `id = 102`, `EditedCostCents = null`
  Vérifier que le payload envoyé à `commandes/10/products/rattacher` contient une liste où le premier élément a `productId = 101` et `purchasePrice = 2500`, tandis que le deuxième élément a `productId = 102` sans la clé `purchasePrice`.
- **Cas 3 : route de retrait** :
  Appel à `DetachAsync(10, 101)` : vérifier que la méthode appelle `DeleAsyncWithAuth` sur `commandes/10/products/101`.

### 4. `RepriseService` (`Easymakemoney.Tests/Services/RepriseServiceTests.cs`)
Création de cette nouvelle classe de tests branchée sur `FakeHttpService` :
- **Cas 1 : simulation avec coefficient** :
  `SimulateAsync(12, 2.5, 1, 25)` appelle `gestion/reprise/simulation` avec un payload contenant `months = 12`, `estimateCoefficient = 2.5`, `page = 1`, `limit = 25`.
- **Cas 2 : simulation sans coefficient** :
  `SimulateAsync(12, null, 1, 25)` appelle `gestion/reprise/simulation` avec `months = 12`, `page = 1`, `limit = 25` et la clé `estimateCoefficient` est absente du dictionnaire.
- **Cas 3 : application avec et sans coefficient** :
  `ApplyAsync(12, 2.0, editedProducts)` appelle `gestion/reprise/appliquer` avec `months = 12`, `estimateCoefficient = 2.0` et `costs` contenant les paires `{ productId, purchasePrice }`. Sans coefficient, la clé est absente.
- **Cas 4 : annulation** :
  `CancelAsync()` appelle `POST` sur `gestion/reprise/annuler` avec corps `null`.
- **Cas 5 : état** :
  `GetStateAsync()` appelle `GET` sur `gestion/reprise`.

### 5. `PeriodResult` (`Easymakemoney.Tests/Models/PeriodResultTests.cs`)
Création de cette nouvelle classe de tests désérialisant le contrat P2 :
- **Cas 1 : lecture du JSON complet contrat P2** :
  Désérialisation d'une réponse P2 complète (`frozen`, `sales`, `otherRevenue`, `cogs`, `expenses`, `result`, `investment`). Vérifier que toutes les valeurs et propriétés associées sont peuplées.
- **Cas 2 : `netRate` nul** :
  Quand `result.netRate = null` : `HasNetRate` est `false`, `NetRateText` est `string.Empty`.
  Quand `result.netRate = 15.2` : `HasNetRate` est `true`, `NetRateText` contient `15.2 % des ventes`.
- **Cas 3 : articles sans coût** :
  Quand `cogs.unknownCostItems = 0` : `HasUnknownCost` est `false`.
  Quand `cogs.unknownCostItems = 1` : `HasUnknownCost` est `true`, `UnknownCostTitle` vaut `"1 article sans coût"`.
  Quand `cogs.unknownCostItems = 4` : `HasUnknownCost` est `true`, `UnknownCostTitle` vaut `"4 articles sans coût"`.
  `UnknownCostDetail` inclut le montant de `sales.unknownCostRevenue`.
- **Cas 4 : perte (`IsLoss`)** :
  Quand `result.net = -5000` : `IsLoss` est `true`.
  Quand `result.net = 0` ou `5000` : `IsLoss` est `false`.
- **Cas 5 : part estimée** :
  Quand `cogs.estimatedItems = 0` : `HasEstimatedCogs` est `false`.
  Quand `cogs.estimatedItems = 3` et `estimatedAmount = 15000` : `HasEstimatedCogs` est `true`, `EstimatedCogsText` contient `"dont ... estimés"`.

### 6. `ListCollection` (`Easymakemoney.Tests/Models/ListCollectionTests.cs`)
Création de cette nouvelle classe de tests :
- **Cas 1 : `kind` et `IsPeriod`** :
  `kind = "period"` -> `IsPeriod` vaut `true`.
  `kind = "manual"` ou `null` -> `IsPeriod` vaut `false`.
- **Cas 2 : `CanDelete`** :
  `kind = "period"` -> `CanDelete` vaut `false` (protection contre la suppression d'une période).
  `kind = "manual"` -> `CanDelete` vaut `true`.
- **Cas 3 : `StatusText`** :
  `IsClosed = true` -> `"Clôturée"`.
  `IsClosed = false` et `startDateCollection` dans le futur (ex: dans 15 jours) -> `"À venir"`.
  `IsClosed = false` et `startDateCollection` dans le passé (ex: il y a 15 jours) -> `"En cours"`.

### 7. `FraisDePortCommandeRow` (`Easymakemoney.Tests/Models/FraisDePortCommandeRowTests.cs`)
Création de cette nouvelle classe de tests :
- **Cas 1 : période clôturée (`PeriodClosed = true`)** :
  Avec ou sans frais de port : `CanAddFrais` vaut `false`, `CanDeleteFrais` vaut `false`.
- **Cas 2 : période ouverte (`PeriodClosed = false`) sans frais** :
  `Frais = null` -> `CanAddFrais` vaut `true`, `CanDeleteFrais` vaut `false`, `DetailText` vaut `"Aucun frais de port"`.
- **Cas 3 : période ouverte (`PeriodClosed = false`) avec frais** :
  `Frais` présent avec transporteur -> `CanAddFrais` vaut `false`, `CanDeleteFrais` vaut `true`, `DetailText` vaut `"{transporteur} · {name}"`.

### 8. `ApiError` (`Easymakemoney.Tests/Models/ApiErrorTests.cs`)
Ajout des tests pour les codes d'erreur de gestion des périodes :
- **Codes testés** : `PERIOD_CLOSED`, `PERIOD_NOT_DELETABLE`, `NOT_CLOSED`, `OUTSIDE_PERIOD`.
- Vérifier pour chaque code que :
  1. `error.Code` conserve exactement le code serveur.
  2. `error.Message` ne contient jamais le code brut.
  3. `PERIOD_CLOSED` traduit par `"Cette période est clôturée : plus rien ne peut y être ajouté."`.
  4. `PERIOD_NOT_DELETABLE` traduit par `"Une période ne peut pas être supprimée."`.
  5. `NOT_CLOSED` traduit par `"Cette période est déjà ouverte."`.
  6. `OUTSIDE_PERIOD` retourne un message propre (serveur ou fallback) sans jamais afficher `"OUTSIDE_PERIOD"` brut.

### 9. Proposition d'abstraction de `DeviceInfo` (sans l'appliquer)
Pour résoudre le `NotImplementedInReferenceAssemblyException` sur `ActivateWithKeyAsync` sans impacter le runtime MAUI :
1. Interface :
```csharp
namespace Easymakemoney.Services.Device;

public interface IDeviceInfoService
{
    string GetDeviceName();
}
```
2. Implémentation MAUI par défaut :
```csharp
public class MauiDeviceInfoService : IDeviceInfoService
{
    public string GetDeviceName() => $"{DeviceInfo.Manufacturer} {DeviceInfo.Model}".Trim();
}
```
3. Injection dans `SiteService` :
```csharp
public class SiteService : ISiteService
{
    private readonly HttpClient _httpClient;
    private readonly IDeviceInfoService _deviceInfoService;

    public SiteService(HttpClient httpClient, IDeviceInfoService? deviceInfoService = null)
    {
        _httpClient = httpClient;
        _deviceInfoService = deviceInfoService ?? new MauiDeviceInfoService();
    }
...
```
Dans `ActivateWithKeyAsync`, remplacer `var deviceName = $"{DeviceInfo.Manufacturer} {DeviceInfo.Model}".Trim();` par `var deviceName = _deviceInfoService.GetDeviceName();`.
Dans les tests, passer une doublure `FakeDeviceInfoService` qui retourne `"iPhone Test"`.
Conformément à la consigne, les 2 tests de `ActivationTests` restent ignorés (`Skip`) dans ce lot et ce changement n'est pas appliqué.

---

## Comment je vérifie

1. `dotnet test Easymakemoney.Tests` :
   - Tous les tests existants (127) + nouveaux tests doivent être verts.
   - Les 2 tests d'activation restent ignorés (`Skipped: 2`).
   - 0 échec.
2. Rebuild Android :
   `dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -c Debug -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home`
   - Résultat attendu : 0 erreur, comparaison avec la référence de 315 avertissements.
3. Contrôle d'encodage et BOM contre `main` :
   `for f in $(git diff --name-only main); do a=$(git show main:"$f" 2>/dev/null | file -b -); b=$(file -b "$f" 2>/dev/null); [ -e "$f" ] && [ "$a" != "$b" ] && echo "DIFF $f: '$a' vs '$b'"; done; echo fin`
   - Résultat attendu : aucun fichier modifié par le lot ne doit avoir de différence d'encodage, de BOM ou de fins de ligne.

---

## Risques et points incertains

- `SiteCurrency` : comme les tests de devises touchent à l'état global partagé, toutes les classes qui modifient la devise doivent être intégrées à `[Collection("SiteCurrency")]` et exécuter `SiteCurrency.Reset()` à l'initialisation et à la libération (`IDisposable`).
- `PeriodResult` : certains champs comme `other` dans `expenses` peuvent être nuls selon la réponse ; s'assurer que les calculs de propriétés comme `OtherExpensesAmount` gèrent les listes nulles sans exception.

---

## Questions pour Claude ou le propriétaire

Aucune question bloquante : les contrats P1, P2, P3 et rattachement sont clairs et déjà en place dans le code.
J'attends ta validation du plan avant de commencer le code.

