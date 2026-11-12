# Lot 4n : tests des ViewModels des listes de gestion (périodes, commandes, fournisseurs, frais de port, notes de frais, à classer)

Consigne de Claude du 12/11/2026. Lot de la file d'attente (`GEMINI.md` § 1 ter) : **plan** dès que tu n'as rien d'autre à faire,
**aucun code avant « plan validé, code »** dans la boîte aux lettres.

- Plan : `docs/gemini/lot-4n-plan.md`, sur ta branche du moment. Code sur `feature/lot-4n-tests`, depuis `main` à jour, quand la boîte
  te le dit. Pas de commit. Référence : nombre réel de tests sur `main` ; Android **216 avertissements**.

## Ce que porte ce lot

Le comportement des ViewModels de `Easymakemoney/ViewModels/Lists/` qui n'ont encore aucun test :
`ListNewCollectionViewModel`, `ListNewCommandViewModels`, `ListNewFournisseurViewModel`, `ListNewFraisDePortViewModel`,
`ListNewNoteDeFraisViewModel`, `AttachProductsViewModel` et `ToReceiveViewModel`.

Au plus **25 tests**, choisis et justifiés dans le plan, en commençant par ce qui écrit ou supprime des données :
1. **Suppressions** (commande, fournisseur, note de frais, frais de port, article détaché d'une commande) : confirmation refusée = aucune
   requête ; acceptée = la bonne route avec le bon identifiant ; et les refus du serveur traités à part, avec le texte réellement montré :
   `PERIOD_CLOSED`, `COMMANDE_CLOSED`, `FOURNISSEUR_IN_USE`, `LINE_RECEIVED`, `PRODUCT_IN_USE`.
2. **Créations et rattachements** : corps envoyé, et ce qui ne part pas (champs vides, période clôturée).
3. **À recevoir** : réception d'une ligne, choix de variante (422 `VARIANT_REQUIRED`), annulation.
4. Chargements, états vides, erreurs, relecture après une écriture.

Lis d'abord les tests de modèles existants (`Easymakemoney.Tests/Models/`) et réutilise les faux existants ; complète-les si besoin, dans
le projet de tests seulement. **Lis le code et décris ce qu'il fait réellement.** Un cas important inatteignable (fenêtre native, Shell,
membre privé, attente réelle) va dans les **questions du plan** : Claude ajoutera un point d'entrée public dans l'application. Ne modifie
jamais un fichier de l'application toi-même.

## Règles, les mêmes que pour les lots 4 à 4m

Aucun fichier XAML ni de l'application modifié ; aucun appel au backend ; pas de reformatage, encodage et fins de ligne intacts ; aucune
clé ni adresse réelle ; pas de commit ; `[Collection("SiteCurrency")]` si un montant est formaté ; dates d'« aujourd'hui » depuis
`DateTime.Today` ; aucune attente réelle ; un bug trouvé = test en `Skip` BUG et une ligne au compte rendu.

## Vérification

Dans `docs/gemini/lot-4n-compte-rendu.md`, écrit **en dernier** : `dotnet test Easymakemoney.Tests`, Rebuild Android (ligne des
avertissements telle quelle), contrôle d'encodage contre `main`. Puis l'accusé de réception.
