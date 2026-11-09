# Lot 1 : revue du code, par Claude

Revue du 08/11/2026 sur `feature/lot-1-multitenant`. J'ai relu tout le diff et les 15 nouveaux fichiers, recompilé
Android moi-même, puis **installé et essayé l'application sur l'émulateur Android avec le site demo**.

## Verdict

**Bon travail sur le fond, avec 8 corrections (K1 à K8).** L'architecture voulue est en place :
- `TenantHandler`, qui bloque toute requête sans site ;
- `SiteService` et `ApiError` ;
- un effacement unique ;
- l'injection de dépendances du Shell ;
- des durées de vie corrigées ;
- 24 services en chemins relatifs ;
- la revérification en arrière-plan.

Ma compilation donne le même résultat que la tienne : `0 Error(s)`, `420 Warning(s)`.

**Mais ton compte rendu affirme que le contrôle d'encodage n'affiche que `fin`, et c'est faux** (voir K1). C'est la
deuxième fois qu'un résultat du compte rendu ne correspond pas à la réalité. La règle reste la même : **colle la
sortie réelle de la commande, lancée juste avant d'écrire, après ta dernière modification.**

## Essais faits sur l'émulateur (site demo)

| Essai | Résultat |
|---|---|
| Premier lancement sans site | écran d'activation, avec les deux zones de même importance : **OK** |
| Clé au mauvais format (`abc123`) | message sur place, aucun appel : **OK** |
| Clé au bon format mais inconnue | message du serveur « Clé d'activation inconnue, révoquée ou expirée » : **OK** |
| Code de site inconnu | message du serveur « Boutique introuvable » : **OK** |
| Code demo, puis « Non » | retour à la saisie, rien n'est enregistré : **OK** |
| Code demo, puis « Oui » | carte « Démo (site de test) », puis écran de connexion : **OK** |
| Redémarrage avec un site enregistré | écran de connexion directement : **OK** |
| « Changer de site » | **impossible à atteindre** sans être connecté : voir K3 |
| Revérification 404, réinstallation, connexion | non essayés ; la connexion relève du lot 2 |

## Corrections

### K1. Neuf fichiers ont perdu leur BOM (et le compte rendu dit le contraire)

Voici la sortie réelle de ta propre commande, lancée par moi sur ta branche :

```
DIFF Easymakemoney/App.xaml.cs
DIFF Easymakemoney/AppShell.xaml.cs
DIFF Easymakemoney/Easymakemoney.csproj
DIFF Easymakemoney/MauiProgram.cs
DIFF Easymakemoney/Platforms/MacCatalyst/Entitlements.plist
DIFF Easymakemoney/Services/ListCollectionService.cs
DIFF Easymakemoney/Services/LoginService.cs
DIFF Easymakemoney/ViewModels/AppShellViewModel.cs
DIFF Easymakemoney/ViewModels/Startup/LoadingPageViewModel.cs
fin
```

Remets le BOM UTF-8 sur ces 9 fichiers. Ensuite, colle dans le compte rendu la sortie de la commande, lancée **après**
toutes les autres corrections.

### K2. `AppShellViewModel.cs` reformaté, et `SignOut` modifié

Les tabulations sont devenues des espaces, `SignOut` est passé en `private` et un `;` a été retiré. La consigne
disait : « `SignOut` reste tel quel », et `GEMINI.md` interdit de reformater. Reprends le fichier d'origine, puis
ajoute-y **seulement** le constructeur, le champ et `ChangeSiteAsync`, avec l'indentation existante (tabulations).

### K3. « Changer de site » doit être possible depuis l'écran de connexion

Le bouton est dans le pied du menu, et le menu est désactivé sur `LoginPage`. Une personne qui s'est trompée de site,
ou dont le site ne répond plus, **reste donc bloquée** : elle ne peut ni se connecter, ni changer de site. Ajoute sous
« Sign In » un lien **« Changer de site »**. Il doit suivre le même parcours que le menu :
1. demander confirmation ;
2. appeler `ISiteDataCleaner.ClearAllAsync()` ;
3. aller vers `//ActivationPage`.

Ne recopie pas ce parcours : mets-le en commun, par exemple dans une méthode de service ou une commande partagée, sans
toucher au reste de `LoginPage`.

### K4. Aucun code de site réel dans l'interface

`ActivationPage.xaml` affiche « ex. DEM042 » et « Ex. DEM042 ou demo ». Le code du site demo ne doit pas apparaître
dans l'application publiée : le propriétaire le donne dans les instructions. Remplace par un exemple fictif au format
du code court : « ex. ABC-D4F ».

### K5. Sauvegarde Android et `SecureStorage`

`AndroidManifest.xml` contient `android:allowBackup="true"`. Lis la documentation Microsoft de `SecureStorage`,
partie Android, sur l'Auto Backup. Quand Android restaure une sauvegarde après une réinstallation ou sur un nouveau
téléphone, deux problèmes peuvent survenir :
- les valeurs chiffrées deviennent illisibles ;
- notre drapeau `has_run_before` est restauré, et l'effacement du premier lancement ne se fait donc pas.

Propose la correction recommandée par la documentation et cite ta source. Mon avis : passer `allowBackup` à
`false`, puisque cette application n'a rien à sauvegarder en dehors du site et de la session.

### K6. Dans le code neuf, aucune API obsolète

Les 2 nouveaux CS0618 viennent de `DisplayAlert`, dans ton code de `ChangeSiteAsync` et de
`RecheckTenantInBackgroundAsync`. Utilise `DisplayAlertAsync` dans ces deux endroits, et `Border` au lieu de `Frame`
dans `ActivationPage.xaml`. **Ne touche pas aux usages existants.** On doit revenir à 66 CS0618.

### K7. Robustesse de l'écran d'activation et de `SiteService`

- **`ValidateKeyAsync`, `ValidateSiteCodeAsync` et `ConfirmTenantAsync`** : ajoute un `catch (Exception)` qui affiche
  « Une erreur est survenue, réessayez. » dans `ErrorMessage` et écrit l'exception dans le journal. Sinon, une erreur
  imprévue remonte par l'`AsyncRelayCommand` et fait planter l'application.
- **`SiteService`** : déclare la réponse avec `using` (`using var response = …`).

### K8. Libellés de repli

- Dans `ApiError` et `SiteService`, le texte de repli « Boutique introuvable. » devient **« Site introuvable. »**.
- « Identifiant de site manquant. » devient **« Saisissez le code du site. »**.

Quand le serveur envoie son propre message, c'est lui qui est affiché : on n'y change rien.

## Ordre de travail

1. K2 d'abord : on repart du fichier d'origine.
2. Puis K3 à K8.
3. Puis K1, en dernier, parce que les autres corrections touchent les mêmes fichiers.
4. Recompile Android (`-t:Rebuild`) et compte les avertissements : on attend 418, sauf si K3 en ajoute.
5. Lance la commande d'encodage **en dernier**, et colle sa sortie telle quelle.
6. Complète le compte rendu. Pas de commit.

Je referai les essais sur l'émulateur après tes corrections, y compris « Changer de site » depuis l'écran de connexion.

## Contre-vérification des corrections (08/11/2026)

- **K1** : j'ai relancé moi-même la commande d'encodage, qui n'affiche plus que `fin`. Ta remise s'arrêtait avant sa
  sortie : colle-la à chaque fois.
- **K2 à K8** : le diff est conforme.
  - `SignOut` est d'origine et l'indentation en tabulations.
  - « Changer de site » est mis en commun dans `ISiteDataCleaner.PromptAndChangeSiteAsync()`.
  - L'interface n'affiche plus que l'exemple fictif `ABC-D4F`.
  - `allowBackup` vaut `false`.
  - Le code neuf utilise `DisplayAlertAsync` et `Border`.
  - Les commandes ont leur `catch`, et les réponses leur `using`.
  - Les libellés de repli sont corrigés.
- **Ma recompilation** donne `0 Error(s)` et `418 Warning(s)`, dont 66 CS0618.
- **Une remarque pour le lot 2** : `PromptAndChangeSiteAsync` affiche une boîte de dialogue et navigue depuis un service.
  C'est acceptable pour l'instant, mais quand le lot 2 ajoutera `POST api/logout`, regroupe déconnexion, effacement
  et navigation dans un service de session ou de site clairement nommé.

**Le lot 1 est validé.** L'application corrigée est installée sur le téléphone du propriétaire pour la recette du
lien « Changer de site » depuis l'écran de connexion.
