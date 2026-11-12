# Lot 4o : tests des ViewModels du démarrage et de la connexion

Consigne de Claude du 12/11/2026. Lot de la file d'attente (`GEMINI.md` § 1 ter) : **plan** dès que tu n'as rien d'autre à faire,
**aucun code avant « plan validé, code »** dans la boîte aux lettres.

- Plan : `docs/gemini/lot-4o-plan.md`, sur ta branche du moment. Code sur `feature/lot-4o-tests`, depuis `main` à jour, quand la boîte
  te le dit. Pas de commit. Référence : nombre réel de tests sur `main` (1 020 au 12/11/2026) ; Android **12 avertissements** (Debug,
  Rebuild).

## Pourquoi ce lot

Un défaut au démarrage ou à la connexion bloque toute l'application. Ces écrans n'ont encore aucun test de ViewModel.

## Ce que porte ce lot

Le comportement de `Easymakemoney/ViewModels/Startup/` et de `AppShellViewModel` :
`LoadingPageViewModel`, `ActivationPageViewModel`, `LoginPageViewModel`, `OtpPageViewModel`, `AppShellViewModel`.

Au plus **25 tests**, choisis et justifiés dans le plan, dans cet ordre :
1. **Démarrage** (`LoadingPageViewModel`) : quel écran est choisi selon l'état (aucun site enregistré, site sans session, session valide,
   session expirée, pas de réseau). Ce qui est lu et ce qui n'est jamais effacé par erreur.
2. **Activation d'un site** (`ActivationPageViewModel`) : code vide ou mal formé (aucune requête), code refusé par le serveur (texte
   réellement montré), code accepté (site enregistré, écran suivant).
3. **Connexion** (`LoginPageViewModel`) : champs vides (aucune requête), identifiants refusés, compte sans le rôle administrateur
   (l'application mobile est réservée aux administrateurs, voir `MobileRolesTests`), passage au code à usage unique.
4. **Code à usage unique** (`OtpPageViewModel`) : code incomplet, code faux, code expiré, renvoi du code (et son délai d'attente, sans
   attente réelle), succès (session enregistrée, Accueil).
5. **Déconnexion et changement de site** (`AppShellViewModel` et ce qu'il appelle) : ce qui est effacé (session, préférences, copies
   hors connexion) et ce qui est gardé.

Lis d'abord les tests existants : `Easymakemoney.Tests/Services/` (`AuthHandlerTests`, `TenantHandlerTests`, `ActivationTests`) et
`Easymakemoney.Tests/Models/MobileRolesTests.cs`. Réutilise les faux existants (`FakeSessionService`, `FakeTenantStore`,
`FakeTokenRefresher`, `FakeDialogService`…) ; complète-les si besoin, **dans le projet de tests seulement**.

**Lis le code et décris ce qu'il fait réellement**, sans supposer. Un cas important inatteignable (navigation du Shell, fenêtre native,
`SecureStorage` ou `Preferences` de l'appareil, membre privé, attente réelle) va dans les **questions du plan** : Claude ajoutera un point
d'entrée public dans l'application. **Ne modifie jamais un fichier de l'application toi-même.**

## Règles, les mêmes que pour les lots 4 à 4n

Aucun fichier XAML ni de l'application modifié ; aucun appel au backend ; aucun vrai identifiant, mot de passe, code ou adresse de site
dans les tests (valeurs inventées évidentes : `demo.example`, `admin@example.test`) ; pas de reformatage, encodage et fins de ligne
intacts ; pas de commit ; aucune attente réelle ; un bug trouvé = test en `Skip` BUG et une ligne au compte rendu.

## Vérification

Dans `docs/gemini/lot-4o-compte-rendu.md`, écrit **en dernier** : `dotnet test Easymakemoney.Tests`, Rebuild Android (ligne des
avertissements telle quelle), contrôle d'encodage contre `main`. Puis l'accusé de réception.
