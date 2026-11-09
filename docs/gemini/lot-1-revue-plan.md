# Lot 1 : revue du plan, par Claude

Revue du 08/11/2026 de `docs/gemini/lot-1-plan.md`, faite contre la consigne et le code de la branche
`feature/lot-1-multitenant`.

## Verdict

**Validé avec corrections (L1 à L12).** Le plan couvre les 9 points de la consigne, les faits relevés dans le code sont
exacts et l'ordre des étapes est bon. Les corrections portent sur des détails qui, à l'exécution, produiraient
soit un mauvais site, soit un plantage. Applique-les en codant : il n'est pas nécessaire de réécrire le plan.

## Réponses à tes questions

1. **`ITenantStore` dans `AppShellViewModel` : passe par l'injection de dépendances, pas par
   `IPlatformApplication.Current.Services`.**
   - `AppShellViewModel` reçoit ses dépendances par son constructeur, et `AppShell` reçoit `AppShellViewModel` par le
     sien.
   - `App` reçoit `AppShell` par son constructeur et garde `MainPage = shell;`. Ne passe pas à `CreateWindow` dans ce
     lot : ce n'est pas son sujet.
   - Enregistre `AppShell` et `AppShellViewModel` en **singletons** : il n'existe qu'un seul Shell. Aucun des deux ne
     doit dépendre d'un service transient qui porte un `HttpClient` (voir L8).
2. **`ApplicationId` : `com.arkanoamedia.gestion`**, sauf si le propriétaire t'a donné une autre valeur dans son
   message. Ne l'écris **qu'une seule fois**, dans le `.csproj` (voir L2).

## Corrections

### L1. `ApiError` : un code d'erreur commence par une lettre

Le motif `^[A-Z0-9_]+$` accepterait `"401"`. Or les 401 de Lexik portent `code: 401`, un nombre. La règle à appliquer :
**on prend `code` s'il s'agit d'une chaîne JSON qui respecte `^[A-Z][A-Z0-9_]*$`, sinon `error`.** Quant à
`Retry-After`, ce peut être un nombre de secondes ou une date HTTP : prends en charge les deux formes
(`RetryConditionHeaderValue.Delta` ou `.Date`).

### L2. Entitlements : une seule forme, et aucun identifiant écrit en dur

- Dans les deux fichiers, iOS et Mac Catalyst, écris `<string>$(AppIdentifierPrefix)$(CFBundleIdentifier)</string>`,
  sans recopier `com.arkanoamedia.gestion`. Sinon, l'identifiant existe en trois endroits.
- Garde la condition iOS en Debug pour `CodesignEntitlements`, comme dans ton plan.
- **Ces deux fichiers ne seront validés qu'au moment de la compilation Apple**, que je te demanderai après
  l'installation de Xcode 27.1. Si la signature échoue, on s'arrête et on en parle. Note-le dans les risques.

### L3. `TenantStore` est un singleton, et le gestionnaire de site attend la lecture

- `ITenantStore` garde le site en mémoire : il doit donc être enregistré en **singleton**. Ton plan ne le dit pas.
- Dans `TenantHandler`, utilise **`await GetTenantAsync(cancellationToken)`**, qui renvoie la copie en mémoire si elle
  existe et lit le `SecureStorage` sinon. Ne lis pas seulement `CurrentTenant` : la première requête pourrait partir
  avant que le site soit chargé, et le gestionnaire la bloquerait à tort.

### L4. `CustomTenantHost` vide : la requête est refusée, pas envoyée avec le site enregistré

Dans ton plan, une option `CustomTenantHost` vide retombe sur le cas normal, qui ajoute le site enregistré. Résultat :
`check-mobile` vérifierait le site enregistré, et non la saisie. Corrige ainsi :
- dans `TenantHandler` : **si l'option est présente mais vide, lève une exception**, sans retomber sur le site
  enregistré ;
- dans `SiteService` : **une saisie vide après le nettoyage s'arrête avant l'appel**, avec un message ;
- dans `TenantHandler` encore : **retire tout `X-Tenant-Host` déjà présent** sur la requête avant d'écrire le tien.

### L5. `SiteService` a son propre client typé, avec la même configuration

`SiteService` doit construire son `HttpRequestMessage` pour y poser les options : il utilise donc `HttpClient`
directement, pas `IHttpService`.
- Enregistre-le avec `AddHttpClient<ISiteService, SiteService>(…)`, avec la **même** `BaseAddress` et le **même**
  `TenantHandler` que `HttpService`.
- Écris cette configuration commune **une seule fois**, dans une petite méthode partagée par les deux
  enregistrements.

### L6. Erreurs réseau et réponses 200 incomplètes

- **Pas de connexion** : il s'agit de `HttpRequestException` et de `TaskCanceledException` (délai dépassé).
  Message : « Pas de connexion ».
- **Réponse 200 de l'activation** : vérifie aussi que `tenant.host` et `tenant.code` sont présents, comme pour
  `check-mobile`. S'ils manquent : « Réponse du serveur incomplète », et rien n'est enregistré.

### L7. Un seul effacement, utilisé à trois endroits

Ton plan écrit l'effacement directement dans `AppShellViewModel`, et l'étape 6 y renvoie. Crée **une seule méthode**,
par exemple `ISiteDataCleaner.ClearAllAsync()`, qui fait tout :
- `SecureStorage.RemoveAll()` ;
- `Preferences.Clear()`, puis le drapeau du premier lancement reposé ;
- `App.Token` et `App.UserDetails` mis à `null` ;
- la copie en mémoire du `TenantStore` vidée.

Trois endroits l'appellent :
- **« Changer de site »** ;
- **la revérification qui reçoit un 404** ;
- **« Oui » sur la carte de confirmation, avant d'enregistrer le nouveau site.** Ainsi, aucune session ni aucune donnée
  d'un site précédent ne peut survivre.

La navigation vers `//ActivationPage` reste du ressort de l'appelant.

### L8. Durées de vie : vérifier qu'aucun singleton ne capture de client

Après tes changements, `AppShellViewModel` (singleton) dépend de `ISiteDataCleaner` et de `ITenantStore`. Ces deux-là
**ne doivent pas dépendre de `IHttpService` ni de `ISiteService`**. Enregistre `ISiteDataCleaner` en singleton, sans
dépendance HTTP.

### L9. Revérification en arrière-plan : revenir sur le fil principal

`RecheckTenantAsync` tourne dans `Task.Run`. Le message « Ce site n'est plus disponible » et la navigation doivent
donc passer par **`MainThread.InvokeOnMainThreadAsync`**. Toute exception doit aussi être attrapée **dans** la tâche,
pour qu'aucune erreur en arrière-plan ne fasse planter l'application.

### L10. `LoadingPageViewModel` : pas de drapeau d'instance inutile

Un drapeau d'instance ne protège rien, puisque chaque nouvelle instance a le sien. La vraie question est de savoir si
Shell recrée `LoadingPage`. Il crée normalement la page d'un `ShellContent` une seule fois, et aucun code ne navigue
vers `//LoadingPage`. Vérifie ces deux points :
- s'ils sont vrais, **n'ajoute aucun drapeau** ;
- sinon, dis-le-moi.

Garde l'appel depuis le constructeur tel qu'il est aujourd'hui (`async void`) : le reprendre est prévu plus tard (C4
de la revue d'architecture). Mais **attrape toute exception** dans la partie que tu ajoutes : effacement du premier
lancement et lecture du site.

### L11. Route `ActivationPage` : `ShellContent` seulement

N'ajoute **pas** `Routing.RegisterRoute("ActivationPage", …)`. Le `ShellContent` avec `Route="ActivationPage"` suffit
pour `//ActivationPage`, et enregistrer deux fois le même nom de route risque une erreur « route dupliquée ». Les
routes existantes déclarées des deux façons (`LoginPage`, `DashboardPage`) restent telles quelles : ce n'est pas le
sujet du lot.

Enregistre aussi `ActivationPage` et `ActivationPageViewModel` en **transient** dans `MauiProgram.cs`.

### L12. Écran d'activation : petits ajustements

- **`IsNotBusy` n'existe pas** dans `BaseViewModel`. Ne modifie pas `BaseViewModel` : utilise
  `InvertedBoolConverter` de CommunityToolkit.Maui sur `IsBusy`.
- **Libellé** : « Code du site », pas « Code de la boutique ». C'est l'application de gestion d'un site.
- **Texte de la confirmation** de « Changer de site », en français simple : « Changer de site ? Vous serez déconnecté
  et les données de ce site seront effacées de l'appareil. »
- **Aucun nom d'hôte de site** dans le code ou dans les textes de l'écran. Dans ton compte rendu, écris
  « l'hôte renvoyé par le serveur » plutôt que l'adresse du site demo.

## Ce qui ne change pas

- Les étapes 1 (adresse de l'API et chemins relatifs), 8 (`ApplicationId`) et 9 (vérification) sont acceptées
  telles quelles.
- La liste des 9 essais est acceptée. **J'y ajoute un dixième** : après « Oui », vérifier qu'une ancienne session
  éventuelle a bien disparu (L7).
- Rappels :
  - aucun lancement de l'application ;
  - aucune compilation Apple ;
  - avant la remise, contrôle de l'encodage avec la commande que tu as prévue ;
  - compte rendu dans `docs/gemini/lot-1-compte-rendu.md` ;
  - pas de commit.
