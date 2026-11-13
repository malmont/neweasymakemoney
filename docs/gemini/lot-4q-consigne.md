# Lot 4q : tests des formulaires de création et des écrans d'une commande et d'un article

Consigne de Claude du 12/11/2026. Lot de la file d'attente (`GEMINI.md` § 1 ter) : **plan** dès qu'un message l'ouvre, **aucun code
avant « plan validé, code »** dans la boîte aux lettres.

- Plan : `docs/gemini/lot-4q-plan.md`. Code sur `feature/lot-4q-tests`, depuis `main` à jour. Pas de commit. Référence : nombre réel de
  tests sur `main` (1 083 au 12/11/2026) ; Android **12 avertissements**.

## Pourquoi ce lot

Le formulaire commun de création (`BottomSheetPopupViewModel`) écrit plus de données que n'importe quel autre écran : périodes,
commandes, articles, variantes, notes de frais, fournisseurs et frais de port. Il n'a aucun test.

## Ce que porte ce lot

`ViewModels/Components/BottomSheetPopupViewModel.cs` (enregistrement de chacun des 7 formulaires, puis rechargement de la liste
appelante, fermeture, erreurs), les modèles de formulaire de `ViewModels/FormModels/` en ce qu'ils produisent comme données envoyées, et
les écrans `DashBoardCommandViewModel` et `DashBoardProductViewModel`.

Au plus **25 tests**, choisis et justifiés dans le plan :
1. **Pour chaque formulaire** : le corps exact envoyé au service (montants en cents, devise, dates, identifiants de la période ou de la
   commande appelante), le rechargement de la bonne liste après succès, et ce qui se passe quand le service refuse (texte réellement
   montré au gérant, formulaire non fermé, liste non rechargée).
2. Les champs obligatoires et les saisies invalides : ce qui part, ce qui ne part pas.
3. Les deux écrans d'une commande et d'un article : chargement, état vide, erreur.

Lis le code et décris ce qu'il fait réellement. Si un texte montré au gérant est en anglais ou technique, c'est un **constat** : écris un
test qui le montre en `Skip` BUG et une ligne au compte rendu, ne le corrige pas. Un cas inatteignable (`Popup` de MAUI, Shell,
`AppPage.Current`, membre privé) va dans les **questions du plan** ; Claude ajoutera le point d'entrée. **Ne modifie jamais un fichier de
l'application.**

## Règles et vérification

Celles des lots 4o et 4p. Compte rendu `docs/gemini/lot-4q-compte-rendu.md` écrit en dernier, puis l'accusé de réception.
