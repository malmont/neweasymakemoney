# Lot 2 : revue du plan, par Claude

Revue du 08/11/2026 de `docs/gemini/lot-2-plan.md`, faite contre la consigne et le code de `feature/lot-2-session`.

## Verdict

**Validé avec corrections (M1 à M10).** Le plan est complet, les faits relevés sont exacts, et la répartition en
`SessionService`, `JwtService` (fonctions pures), `AuthHandler` et `AuthFlowService` est la bonne. Deux corrections
sont **bloquantes**, parce que le code tel que prévu ne démarrerait pas ou capturerait un client HTTP pour toujours :
**M1** et **M2**. Applique toutes les corrections en codant ; il n'est pas nécessaire de réécrire le plan.

## Réponses à tes questions

1. **`OtpPage` en `ShellContent` absolu (`//OtpPage`) : validé.** Ajoute `Shell.FlyoutBehavior="Disabled"`, comme sur
   `LoginPage` et `ActivationPage`. « Retour » va vers `//LoginPage`.
2. **« Se déconnecter » : validé.**
3. **Identifiants gardés en mémoire seulement : validé, sous cette forme.** Un `ShellContent` ne reçoit pas de
   paramètre, et le mot de passe ne doit jamais passer dans une URL de navigation. Crée donc un petit service
   **singleton, en mémoire seulement**, par exemple `IPendingLoginStore` : il garde le courriel et le mot de passe, et
   n'a ni stockage ni journal. On le vide :
   - après une connexion réussie ;
   - sur « Retour » ;
   - au bout de 10 minutes, si l'écran OTP est abandonné.

   Si `OtpPage` s'ouvre alors qu'il est vide, elle renvoie vers `//LoginPage`.

## Corrections

### M1. Dépendance circulaire : `AuthHandler` ne doit pas passer par `HttpService` (bloquant)

Pour rafraîchir le jeton, `AuthHandler` appellerait `LoginService`, qui utilise `HttpService`, dont le client contient
`AuthHandler`. Construire le client exige donc de construire `AuthHandler`, qui exige le client, et ainsi de suite :
l'application planterait au démarrage, ou à la première requête.

Solution :
- un **client nommé à part**, par exemple `"auth"`, qui a la même `BaseAddress` et **seulement `TenantHandler`**, sans
  `AuthHandler` ;
- les appels publics (`login`, `otp-verify`, `token/refresh`, `logout`) passent par ce client, obtenu par
  **`IHttpClientFactory.CreateClient("auth")` au moment de chaque appel** ;
- `AuthHandler` ne dépend que de `ISessionService` et d'un `ITokenRefresher` (voir M3), qui utilise lui-même ce client
  `"auth"`.

### M2. Aucun singleton ne garde un client HTTP (bloquant)

`AuthFlowService` est prévu en singleton, et `AppShellViewModel` (singleton) en dépend. S'il reçoit `ILoginService`,
c'est-à-dire un client typé transient, ce client reste capturé à vie : c'est exactement le problème A3 corrigé au
lot 1. Règle : **tout singleton qui doit appeler le serveur passe par `IHttpClientFactory.CreateClient("auth")`**, au
moment de l'appel, jamais par un service typé reçu dans son constructeur. Ce sont `ITokenRefresher` et
`AuthFlowService`. Donne dans le compte rendu la liste des durées de vie de tout ce que tu enregistres.

### M3. Un seul code de rafraîchissement

Le plan rafraîchit le jeton à deux endroits : dans `AuthHandler` et au démarrage
(`AuthFlowService.RefreshTokenAsync`). Il faut **un seul** `ITokenRefresher`, en singleton, qui porte le
`SemaphoreSlim` et rend un résultat clair : **réussi**, **refusé** (400 ou 401) ou **injoignable**. `AuthHandler` et
`LoadingPageViewModel` l'appellent tous les deux. Ainsi, deux rafraîchissements ne peuvent pas se croiser au
démarrage.

Pour savoir si « une autre requête a déjà rafraîchi », compare le jeton envoyé avec la requête au jeton actuel de la
session. S'ils diffèrent, rejoue directement avec le nouveau jeton.

### M4. Session expirée : un seul message, et pas d'interface dans le gestionnaire HTTP

Quand plusieurs requêtes reçoivent un 401 en même temps, le plan afficherait plusieurs fois « Votre session a
expiré ». De plus, `AuthHandler` n'a pas à afficher de dialogue. Solution :
- `AuthHandler` efface la session, puis signale l'expiration à un service (par exemple
  `ISessionExpiryNotifier.NotifyExpired()`) ;
- ce service affiche **une seule fois** le message et navigue vers `//LoginPage` sur le fil principal, avec un
  drapeau remis à zéro à la prochaine connexion ;
- la requête d'origine échoue ensuite normalement, avec une exception dédiée.

### M5. Messages du serveur en anglais

Le serveur répond en anglais pour `PLATFORM_NOT_ALLOWED` (« This account is not allowed… »). Dans `LoginUseCase`, ce
code doit donner **le texte français de la consigne**, quel que soit le message du serveur. Pour les autres codes
(`LOGIN_INVALID`, `ACCOUNT_NOT_VERIFIED`, `OTP_INVALID`, `RATE_LIMITED`), le message du serveur est en français : on
l'affiche.

### M6. `LoginResponse.Token` peut être absent

Aujourd'hui, `public required string Token`. Avec `otp_required`, la réponse n'a pas de jeton. `Token` et
`RefreshToken` deviennent `string?`. Si l'un d'eux est vide sans `otp_required`, c'est une réponse incomplète : on
affiche une erreur et on n'enregistre rien.

### M7. Envoi sans authentification : construire la requête

`PostAsyncWithoutAuth` appelle `_httpClient.PostAsync(url, content)`, qui ne permet pas de poser une option. Pour
marquer la requête « sans authentification », construis un `HttpRequestMessage`, comme `SiteService` au lot 1. Les
routes publiques de M1 utilisent de toute façon le client `"auth"`, qui n'a pas d'`AuthHandler`. Garde quand même
l'option : elle protège tout autre appel public qui passerait par `HttpService`.

### M8. `FlyoutHeaderControl` reçoit l'utilisateur en paramètre

Il est créé par `new FlyoutHeaderControl()` : il ne peut donc pas recevoir de service. Choisis
`new FlyoutHeaderControl(UserInfo user)`, la deuxième option de ton plan, sans dépendance à un service.

### M9. Contrôle d'encodage contre la bonne branche, et aucun secret dans le plan

- **Le contrôle d'encodage doit comparer avec `feature/lot-1-multitenant`.** Les fichiers créés au lot 1, comme
  `SiteDataCleaner.cs`, n'existent pas dans `feature/lot-0-plateforme` : la commande prévue les signalerait à tort.
  Commande :

  ```bash
  for f in $(git diff --name-only feature/lot-1-multitenant); do a=$(git show feature/lot-1-multitenant:"$f" 2>/dev/null | file -b -); b=$(file -b "$f" 2>/dev/null); [ -e "$f" ] && [ "$a" != "$b" ] && echo "DIFF $f"; done; echo fin
  ```

- **Ton plan recopie la clé AES d'`EncryptionHelper`.** Même si ce fichier va disparaître, retire-la du plan :
  `GEMINI.md` interdit toute clé dans les documents. Écris « une clé écrite dans le code ».

### M10. Mode sombre : forcer le thème clair (ajout du 08/11/2026)

**Problème vu par le propriétaire sur son téléphone, réglé en mode sombre** : le texte saisi s'affiche en blanc sur
fond blanc.
- `Resources/Styles/Styles.xaml` donne à `Entry` la couleur `TextColor` = `{AppThemeBinding … Dark={StaticResource
  White}}`.
- Or les écrans ont des fonds blancs écrits en dur.

L'application n'a jamais été conçue pour le mode sombre. Dans le constructeur d'`App` (`App.xaml.cs`), ajoute
`UserAppTheme = AppTheme.Light;` **avant** l'affectation de la page principale. Ne change aucune couleur ni aucun
style : la prise en charge du mode sombre sera traitée à la revue des fonctionnalités.

## Ce qui ne change pas

- **Les étapes 1 à 10 sont acceptées**, avec les corrections ci-dessus, et notamment :
  - suppression d'`EncryptionHelper`, de `PreferenceService` et des champs statiques d'`App` ;
  - traduction de `LoginPage` et retrait de « Sign Up here » ;
  - comparaison des dates en UTC avec une marge de 60 secondes.
- **Le format des rôles** : ta prudence est juste. Accepte à la fois un tableau JSON et une chaîne unique.
- **Les 11 essais** sont acceptés. Pour l'essai 7, décris précisément la simulation dans le compte rendu.
- **Rappels** :
  - aucun lancement de l'application ;
  - aucun appel au backend ;
  - sorties de commandes collées telles quelles, après ta dernière modification ;
  - pas de commit.
