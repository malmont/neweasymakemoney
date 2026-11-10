# Lot 4 : plan de Gemini (tests automatiques)

Branche : `feature/lot-4-tests` (créée depuis `main`)  
Base de comparaison : `main`

---

## Ce que j'ai lu

1. `docs/gemini/lot-4-consigne.md:1-56` : directives pour le lot 4 ; ciblage `net10.0`, création de `Easymakemoney.Tests`, isolation totale sans appels backend, doublures en mémoire pour les API MAUI (`SecureStorage`, `Preferences`), `HttpMessageHandler` fait main, règle stricte « [Fact(Skip = "BUG : …")] » en cas d'anomalie détectée sans modifier le code de l'application.
2. `docs/revue-architecture.md:85-90, 117, 140-146` : validation de la décision d'ajouter la cible `net10.0` au projet de l'application MAUI pour permettre le référencement direct depuis une suite de tests unitaires xUnit.
3. `Easymakemoney/Easymakemoney.csproj:4, 6` : `TargetFrameworks` actuels (`net10.0-android;net10.0-ios;net10.0-maccatalyst`) et `OutputType` (`Exe` global).
4. `Easymakemoney/Utilities/MonthHelper.cs:5-39` : fonction pure `FormatMonthName(string?)` à éprouver sur toutes les combinaisons.
5. `Easymakemoney/Utilities/SiteCurrency.cs:8-109` : cache de symboles ISO, `FormatCents`, `SetCurrency`, `Reset`.
6. `Easymakemoney/UseCase/ListCollectionUseCase/SaveCollectionUseCase.cs:25` : calcul `budgetCollection = (long)Math.Round((collectionForm.BudgetCollection ?? 0) * 100)`.
7. `Easymakemoney/UseCase/LisCommandeUseCase/SaveCommandUseCase.cs:17` : calcul `budget = (long)Math.Round((commandForm.Budget ?? 0) * 100)`.
8. `Easymakemoney/UseCase/ListFraisDePortUseCase/SaveFraisDePortUseCase.cs:21` : calcul `price = (long)Math.Round((FraisDePortForm.Prix ?? 0) * 100)`.
9. `Easymakemoney/UseCase/ListNoteDeFraisUseCase/SaveNoteDeFraisUseCase.cs:21` : calcul `montant = (long)Math.Round((NoteDeFraisForm.Montant ?? 0) * 100)`.
10. `Easymakemoney/Models/Api/ApiError.cs:23-188` : parsing de `fields`, `errors`, `violations`, codes de statut, `TranslateFieldName`, `TranslateValidationCode`.
11. `Easymakemoney/Services/Auth/TenantHandler.cs:24-48` : injection de `X-Tenant-Host` ou levée de `TenantMissingException`.
12. `Easymakemoney/Services/Auth/AuthHandler.cs:28-115` : gestion 401, verrou de rafraîchissement unique, notification expiration, bufferisation multipart.
13. `Easymakemoney/Services/HttpService.cs:20-188` : gestion des 200, 201, 204, extraction de `currency`.
14. `Easymakemoney/Models/ListProduct.cs:10-11` : types `purchasePrice` et `coefficientMultiplier`.

---

## Prérequis à installer ou à vérifier (commandes exactes, rien n'est lancé)

Aucun outil système ni workload à installer (le SDK .NET 10 est en place).
Pour la restauration du nouveau projet de tests et de ses paquets NuGet :
```bash
dotnet restore Easymakemoney1.sln
```

---

## Étapes

### 1. Mise en place du multi-ciblage dans `Easymakemoney.csproj`

#### Changement exact de `Easymakemoney/Easymakemoney.csproj` :
```xml
--- a/Easymakemoney/Easymakemoney.csproj
+++ b/Easymakemoney/Easymakemoney.csproj
@@ -3,5 +3,5 @@
     <PropertyGroup>
-        <TargetFrameworks>net10.0-android;net10.0-ios;net10.0-maccatalyst</TargetFrameworks>
+        <TargetFrameworks>net10.0;net10.0-android;net10.0-ios;net10.0-maccatalyst</TargetFrameworks>
         <TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('windows'))">$(TargetFrameworks);net10.0-windows10.0.19041.0</TargetFrameworks>
-        <OutputType>Exe</OutputType>
+        <OutputType Condition="'$(TargetFramework)' != 'net10.0'">Exe</OutputType>
         <RootNamespace>Easymakemoney</RootNamespace>
@@ -27,2 +27,6 @@
     </PropertyGroup>
+
+    <ItemGroup Condition="'$(TargetFramework)' == 'net10.0'">
+        <Compile Remove="Platforms\**" />
+    </ItemGroup>
```

#### Vérification que `Platforms/` n'est pas compilé en `net10.0` :
1. `Platforms/` contient du code spécifique aux plateformes (`Android.App`, `Foundation`, `UIKit`, etc.). Si ces fichiers étaient inclus lors de la compilation sous `net10.0`, celle-ci échouerait immédiatement avec des erreurs de compilation CS0246 (types introuvables).
2. Vérification par commande explicite :
   ```bash
   dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0
   ```
   Un succès avec 0 erreur prouve l'exclusion totale de `Platforms/`.
3. Contrôle des éléments compilés via MSBuild :
   ```bash
   dotnet msbuild Easymakemoney/Easymakemoney.csproj -f net10.0 -getItem:Compile | grep Platforms
   ```
   La sortie doit être vide.

---

### 2. Création du projet de tests `Easymakemoney.Tests`

- **Fichier créé** : `Easymakemoney.Tests/Easymakemoney.Tests.csproj`
- **Versions exactes des paquets** :
  - `Microsoft.NET.Test.Sdk` : `17.13.0`
  - `xunit` : `2.9.3`
  - `xunit.runner.visualstudio` : `3.0.2`
  - `coverlet.collector` : `6.0.4`
- **Contenu du fichier `.csproj`** :
  ```xml
  <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
          <TargetFramework>net10.0</TargetFramework>
          <ImplicitUsings>enable</ImplicitUsings>
          <Nullable>enable</Nullable>
          <IsPackable>false</IsPackable>
          <IsTestProject>true</IsTestProject>
      </PropertyGroup>

      <ItemGroup>
          <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.13.0" />
          <PackageReference Include="xunit" Version="2.9.3" />
          <PackageReference Include="xunit.runner.visualstudio" Version="3.0.2">
              <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
              <PrivateAssets>all</PrivateAssets>
          </PackageReference>
          <PackageReference Include="coverlet.collector" Version="6.0.4">
              <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
              <PrivateAssets>all</PrivateAssets>
          </PackageReference>
      </ItemGroup>

      <ItemGroup>
          <ProjectReference Include="..\Easymakemoney\Easymakemoney.csproj" />
      </ItemGroup>
  </Project>
  ```
- **Ajout à la solution** :
  ```bash
  dotnet sln Easymakemoney1.sln add Easymakemoney.Tests/Easymakemoney.Tests.csproj
  ```

---

### 3. Doublures et outillage de test (aucun appel backend, aucune dépendance API MAUI)

Création du dossier `Easymakemoney.Tests/Fakes/` :
- `TestHttpMessageHandler.cs` : `HttpMessageHandler` fait main permettant d'enregistrer les requêtes reçues (URL, méthode, en-têtes, corps) et de retourner des réponses simulées programmées (par file d'attente ou fonction de rappel).
- `FakeTenantStore.cs` : doublure en mémoire de `ITenantStore` (aucun accès à `Preferences`).
- `FakeSessionService.cs` : doublure en mémoire de `ISessionService` (aucun accès à `SecureStorage`).
- `FakeSessionExpiryNotifier.cs` : doublure de `ISessionExpiryNotifier` comptabilisant les appels à `NotifySessionExpired()`.
- `FakeDialogService.cs` : doublure de `IDialogService` stockant les alertes et erreurs sans appel à `Shell.Current`.

---

### 4. Liste détaillée des cas de test par groupe

#### Groupe 1 : `MonthHelper` (`Easymakemoney.Tests/Utilities/MonthHelperTests.cs`)
1. `FormatMonthName_With1_ReturnsJanvier` : entrée `"1"` → `"janvier"`.
2. `FormatMonthName_With12_ReturnsDecembre` : entrée `"12"` → `"décembre"`.
3. `FormatMonthName_WithValues2Through11_ReturnsExpectedFrenchMonths` : théorie testant `"2"` à `"11"` (`"février"`, `"mars"`, `"avril"`, `"mai"`, `"juin"`, `"juillet"`, `"août"`, `"septembre"`, `"octobre"`, `"novembre"`).
4. `FormatMonthName_With0_ReturnsZero` : entrée `"0"` → `"0"` (hors bornes, non altéré).
5. `FormatMonthName_With13_ReturnsTreize` : entrée `"13"` → `"13"` (hors bornes, non altéré).
6. `FormatMonthName_WithNegativeNumber_ReturnsUnchanged` : entrée `"-5"` → `"-5"`.
7. `FormatMonthName_WithNonNumericText_ReturnsUnchanged` : entrées `"janvier"`, `"mars"`, `"T1"`, `"March"` → restent inchangées.
8. `FormatMonthName_WithNullOrEmpty_ReturnsEmptyString` : `null` → `""`, `""` → `""`.
9. `FormatMonthName_WithLeadingZerosAndWhitespace_ReturnsCorrectMonth` : entrées `" 03 "`, `" 10 "` → `"mars"`, `"octobre"`.

#### Groupe 2 : `SiteCurrency` et formatage (`Easymakemoney.Tests/Utilities/SiteCurrencyTests.cs`)
1. `FormatCents_ConvertsCentsToAmount` : `1250` centimes avec devise `"EUR"` et culture `fr-FR` → `"12,50 €"` (division par 100).
2. `FormatCents_UsesSiteCurrencyRegardlessOfDeviceCulture` :
   - Devise du site `"USD"` avec culture de l'appareil `fr-FR` → le symbole utilisé est `$` (ex: `12,50 $` ou `12,50 US$`), et non `€`.
   - Devise du site `"EUR"` avec culture de l'appareil `en-US` → le symbole utilisé est `€` (ex: `€12.50`), et non `$`.
3. `FormatCents_UnknownIsoCode_FallsBackToIsoCodeWithoutException` :
   - Code inconnu `"XYZ"` → formatage avec le code `"XYZ"` en repli, aucune exception levée.
4. `FormatCents_AbsentCurrency_DisplaysNumberWithoutSymbol` :
   - Devise `null` ou vide → affiche `"12,50"` (en `fr-FR`) sans aucun symbole monétaire.
5. `Reset_ClearsCurrentCurrencyToNull` :
   - Après `SetCurrency("EUR")` puis `Reset()` → `CurrentCurrency` est `null`, et le formatage redevient sans symbole.

#### Groupe 3 : Montants envoyés (`Easymakemoney.Tests/UseCases/SaveUseCasesTests.cs`)
*Justification de l'arrondi* : En C# / .NET, l'opération `Math.Round(valeur * 100)` sans argument `MidpointRounding` applique l'arrondi bancaire IEEE 754 (`ToEven`). Pour la saisie `12,345`, `12.345 * 100 = 1234.5`. Le nombre pair le plus proche de 1234.5 est **1234**. La valeur réellement attendue par le code actuel est donc **1234**. (Si une règle métier exige 1235, un bug sera constaté et documenté).
1. `SaveCollectionUseCase_SendsRoundedIntegerCents` :
   - `collectionForm.BudgetCollection = 12.345` → `newCollection.budgetCollection` envoyé est `1234L`.
   - `BudgetCollection = 100.50` → `10050L`.
   - `BudgetCollection = null` → `0L`.
2. `SaveCommandUseCase_SendsRoundedIntegerCents` :
   - `commandForm.Budget = 12.345` → `newCommand.budget` envoyé est `1234L`.
   - `Budget = 250.75` → `25075L`.
3. `SaveFraisDePortUseCase_SendsRoundedIntegerCents` :
   - `fraisDePortForm.Prix = 12.345` → `newFraisDePort.price` envoyé est `1234L`.
   - `Prix = 15.00` → `1500L`.
4. `SaveNoteDeFraisUseCase_SendsRoundedIntegerCents` :
   - `noteDeFraisForm.Montant = 12.345` → `newNoteDeFrais.montant` envoyé est `1234L`.
   - `Montant = 42.80` → `4280L`.

#### Groupe 4 : `ApiError.FromHttpResponse` (`Easymakemoney.Tests/Models/ApiErrorTests.cs`)
1. `FromHttpResponse_WithErrorsArray_ParsesPathAndMessage` :
   - JSON `{"errors": [{"path": "email", "message": "Email invalide"}]}` → `Errors` contient `Path == "email"`, `Message == "Email invalide"`.
2. `FromHttpResponse_WithViolationsArray_ParsesPropertyPathAndMessage` :
   - JSON `{"violations": [{"propertyPath": "name", "message": "Le nom est obligatoire"}]}` → `Errors` contient `Path == "name"`.
3. `FromHttpResponse_WithFieldsObject_TranslatesKnownFieldsAndCodes` :
   - JSON `{"error": "VALIDATION_FAILED", "fields": {"budget": "MUST_BE_INTEGER_CENTS", "date": "REQUIRED"}}` → traduit en `Path: "Budget"`, `Message: "Montant invalide."` et `Path: "Date"`, `Message: "Ce champ est obligatoire."`.
4. `FromHttpResponse_WithFieldsObject_UnknownField_TranslatesMessageOnlyWithoutTechnicalName` :
   - JSON `{"fields": {"champTechniqueInconnu": "INVALID_ID"}}` → `Path` est vide (`""`), `Message == "L'identifiant est invalide."`.
5. `FromHttpResponse_StandardErrorCodeAndMessage_ExtractsBoth` :
   - JSON `{"error": "COMMANDE_CLOSED", "message": "Commande fermée"}` → `Code == "COMMANDE_CLOSED"`, `Message == "Commande fermée"`.
6. `FromHttpResponse_EmptyBody_ProvidesFallbackMessage` :
   - Corps vide, statut 404 → `Code == "NotFound"`, message de repli présent.
   - Corps vide, statut 500 → `Message == "Erreur du serveur. Veuillez réessayer plus tard."`.
7. `FromHttpResponse_NonJsonBody_HandlesGracefullyWithoutThrowing` :
   - Corps HTML `"<html>502 Bad Gateway</html>"` → aucune exception, `Code == "BadGateway"`.
8. `FromHttpResponse_Contract404And409Codes_ProvideFrenchFallbacks` :
   - Statut 409 avec code `"ALREADY_CLOSED"` → `Message == "Cet élément est déjà clôturé."`.
   - Statut 404 avec code `"TENANT_NOT_FOUND"` → `Message == "Site introuvable."`.
   - Statut 422 avec code `"VALIDATION_FAILED"` → `Message == "Données non valides."`.
   - Statut 429 avec en-tête `Retry-After: 45` → `RetryAfterSeconds == 45`, message mentionnant 45 secondes.

#### Groupe 5 : `TenantHandler` (`Easymakemoney.Tests/Services/TenantHandlerTests.cs`)
1. `SendAsync_WhenSiteIsConfigured_InjectsXTenantHostHeader` :
   - Tenant configuré avec `host = "magasin.exemple.test"` → la requête HTTP transmise contient l'en-tête `X-Tenant-Host: magasin.exemple.test`.
2. `SendAsync_WhenNoSiteConfigured_ThrowsTenantMissingException` :
   - Aucun tenant configuré → `TenantMissingException` levée, aucun envoi réseau en aval.
3. `SendAsync_WhenSkipTenantConfigured_DoesNotInjectHeader` :
   - Requête configurée pour ignorer le tenant → transmise sans en-tête `X-Tenant-Host`.

#### Groupe 6 : `AuthHandler` (`Easymakemoney.Tests/Services/AuthHandlerTests.cs`)
1. `SendAsync_AuthenticatedRoute_AttachesBearerToken` :
   - Session active avec `token = "token-abc"` → en-tête `Authorization: Bearer token-abc`.
2. `SendAsync_PublicRouteWithSkipAuth_DoesNotAttachToken` :
   - Requête marquée `SetSkipAuth(true)` → aucun en-tête `Authorization`.
3. `SendAsync_On401Response_RefreshesTokenAndRetriesRequest` :
   - 1re réponse : 401 Unauthorized.
   - Le rafraîchisseur fournit `"token-refreshed"`.
   - 2e réponse (rejeu) : 200 OK avec le nouveau jeton dans `Authorization`.
4. `SendAsync_MultipleConcurrent401Responses_ExecutesOnlyOneRefresh` :
   - 3 requêtes simultanées reçoivent 401.
   - Le rafraîchisseur n'est invoqué qu'une seule fois. Les 3 requêtes sont rejouées et réussissent.
5. `SendAsync_WhenRefreshFails_NotifiesSessionExpiredAndThrowsSessionExpiredException` :
   - 401 reçu, le rafraîchisseur échoue → `ISessionExpiryNotifier.NotifySessionExpired()` est appelé une seule fois, puis `SessionExpiredException` est levée.
6. `SendAsync_OnMultipart401Response_BuffersContentAndRetriesSuccessfully` :
   - Requête `MultipartFormDataContent` recevant un 401 initial → le contenu est bufferisé en amont et peut être relu intégralement lors du rejeu sans `ObjectDisposedException`.

#### Groupe 7 : Activation et code de site (`Easymakemoney.Tests/Services/ActivationTests.cs`)
1. `Activation_NormalizesInput_TrimsAndConvertsToUpper` :
   - Code saisi `"  dem042  "` → normalisé en `"DEM042"`.
2. `Activation_WhenAppKeyInvalid404_ExtractsAppKeyInvalidCodeAndFrenchMessage` :
   - Réponse simulée 404 avec JSON `{"error": "APP_KEY_INVALID"}` → `ApiException` avec message français « Cette clé d'activation est invalide, inconnue ou expirée. ».
3. `Activation_WhenSuccess200_ConstructsTenantContext` :
   - Réponse 200 simulée avec `{"host": "demo.exemple.test", "name": "Démo"}` → contexte de site correctement renseigné.

#### Groupe 8 : `HttpService` (`Easymakemoney.Tests/Services/HttpServiceTests.cs`)
1. `GetAsync_200Ok_DeserializesObject` :
   - Réponse 200 avec objet JSON → retour de l'objet désérialisé.
2. `PostAsyncWithAuth_201Created_ReturnsDeserializedObject` :
   - Réponse 201 avec objet JSON créé → retour de l'objet désérialisé.
3. `HandleResponse_204NoContent_ReturnsDefaultWithoutThrowing` :
   - Réponse 204 No Content → renvoie `default` sans lever d'exception.
4. `GetListFraisDePort_404Or200Null_ReturnsNullWithoutException` :
   - Requête frais de port recevant 404 ou 200 avec corps `null` → renvoie `null` sans erreur.
5. `HttpService_ExtractsCurrencyFromRootObject` :
   - Réponse JSON `{"currency": "USD", "budget": 1000}` → `SiteCurrency.CurrentCurrency` est mis à jour à `"USD"`.
6. `HttpService_ExtractsCurrencyFromFirstArrayElement` :
   - Réponse JSON `[{"currency": "GBP", "id": 1}, {"id": 2}]` → `SiteCurrency.CurrentCurrency` est mis à jour à `"GBP"`.

#### Groupe 9 : Lecture des modèles (`Easymakemoney.Tests/Models/ModelDeserializationTests.cs`)
1. `ListProduct_Deserialization_WithNullPurchasePriceAndDecimalCoefficient` :
   - JSON : `{"id": 1, "name": "Produit Test", "purchasePrice": null, "coefficientMultiplier": 2.5}`.
   - Désérialisation sans crash, gestion sûre de `purchasePrice`.
2. `MonthlyModels_Deserialization_WithIntegerMonth_FormulasData1InFrench` :
   - `MonthlyRevenueData` (`{"month": 3}`) → `Data1 == "mars"`.
   - `MonthlyOrderData` (`{"month": 11}`) → `Data1 == "novembre"`.
   - `MonthlyAverageOrderValueData` (`{"month": 1}`) → `Data1 == "janvier"`.
   - `MonthlyFraisData` (`{"month": 7}`) → `PeriodLabel == "juillet"`.
   - `MonthlyTaxData` (`{"month": 8}`) → `Data1 == "août"`.
   - `MonthlyCarrierData` (`{"month": 12}`) → `Data1 == "décembre"`.
3. `Models_WithLongNullableBudgets_DeserializesAndSerializesCorrectly` :
   - `ListCollection` (`budgetCollection: 150000`) → `150000L`.
   - `ListCommand` (`budget: 50000`) → `50000L`.
   - `FraisDePort` (`price: 1200`) → `1200L`.
   - `NoteDeFrais` (`montant: 4500`) → `4500L`.
   - Passage dans `CurrencyConverter` pour vérifier le formatage sans régression.

---

## Comment je vérifie

1. **Exécution des tests xUnit** :
   ```bash
   dotnet test Easymakemoney.Tests --verbosity normal
   ```
   Résultat attendu : tous les tests passent (Passed), aucun échec (sauf si un bug est révélé et skippé).
2. **Rebuild Android** :
   ```bash
   dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -c Debug -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home
   ```
   Résultat attendu : `0 Error(s)`, décompte des avertissements $\le 318$ (prouvant que la cible `net10.0` n'a rien dégradé sur Android).
3. **Contrôle d'encodage et BOM contre `main`** :
   ```bash
   for f in $(git diff --name-only main); do a=$(git show main:"$f" 2>/dev/null | file -b -); b=$(file -b "$f" 2>/dev/null); [ -e "$f" ] && [ "$a" != "$b" ] && echo "DIFF $f: '$a' vs '$b'"; done; echo fin
   ```
   Résultat attendu : aucun écart de BOM ni d'encodage.

---

## Risques et points incertains

1. **Compilation sous `net10.0` de dépendances MAUI tierces** :
   Si une dépendance comme `Microcharts.Maui` ou `CommunityToolkit.Maui` avait des cibles restreintes, `net10.0` pourrait nécessiter des ajustements de packages conditionnels. Cependant, ces bibliothèques fournissent généralement une cible `net10.0` neutre. Nous vérifierons immédiatement avec `dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0`.
2. **Gestion des bugs découverts** :
   Si un test met en évidence un comportement non conforme du code existant, la consigne sera rigoureusement appliquée : **pas de correction sauvage du code applicatif**, marquage du test en `[Fact(Skip = "BUG : description")]` et compte rendu détaillé.

---

## Questions pour Claude ou le propriétaire

1. **Restauration des paquets de test** :
   La commande `dotnet restore Easymakemoney1.sln` téléchargera les paquets NuGet de test (`xunit`, `Microsoft.NET.Test.Sdk`...). Confirmez-vous l'autorisation de lancer cette restauration lors de la phase de code après validation du plan ?
2. **Arrondi bancaire (`1234`) vs arithmétique (`1235`)** :
   Pour `12,345 * 100 = 1234.5`, `Math.Round` par défaut en .NET produit `1234` (arrondi au pair le plus proche). Si vous souhaitez que `12,345` produise `1235`, cela nécessiterait `MidpointRounding.AwayFromZero` dans le code de l'application. Validez-vous que le test vérifie le comportement actuel (`1234`) ou préférez-vous un signalement en bug ?

