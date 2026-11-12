# Lot 4l : tests du ViewModel de vérification d'une facture fournisseur

Consigne de Claude du 12/11/2026. Lot de la file d'attente (`GEMINI.md` § 1 ter) : tu peux en écrire le **plan** dès que tu n'as rien
d'autre à faire. **Aucun code avant que Claude ait validé le plan dans la boîte aux lettres.**

- Plan : `docs/gemini/lot-4l-plan.md`, sur ta branche du moment, sans en créer une autre.
- Branche du code : `feature/lot-4l-tests`, créée depuis `main` à jour, **seulement quand la boîte aux lettres te le dit**.
- Pas de commit. Référence : `dotnet test Easymakemoney.Tests` sur `main`, nombre réel noté. Android : **216 avertissements**.

## Ce qui est déjà testé : ne le reteste pas

Lis d'abord : `Easymakemoney.Tests/Models/InvoiceReviewTests.cs`, `InvoiceConfirmationBuilderMissingTests.cs`,
`Easymakemoney.Tests/ViewModels/InvoiceLineItemViewModelTests.cs`. Réutilise les faux existants (`FakeDocumentReaderService`,
`FakeProductAttachmentService`…) au lieu d'en créer de nouveaux ; complète-les si besoin, dans le projet de tests seulement.

## Ce que porte ce lot

Le comportement de `Easymakemoney/ViewModels/Lists/InvoiceReviewViewModel.cs` (environ 870 lignes : c'est le plus gros écran de l'appli).
Ton plan doit **choisir** au plus 25 tests, les plus utiles, et dire pourquoi. Priorités, dans l'ordre :
1. **Ce qui part au serveur à la validation** : le corps de confirmation (fournisseur existant ou nouveau, lignes, articles à créer,
   quantités, coûts), et ce qui ne part pas (lignes non validées, fournisseur manquant, champs obligatoires vides).
2. **Les codes d'erreur traités à part** : `INVOICE_CONFIRMATION_PENDING`, `PERIOD_CLOSED` (proposition de la période en cours, puis
   second envoi avec la date de classement ; refus = aucun envoi), `INVOICE_ALREADY_CONFIRMED` (doublon : second envoi `force` après
   accord, aucun envoi si refus), `DOCUMENT_CONFIRMED`, `DOCUMENT_EXPIRED` : le message montré au gérant, et ce qui est renvoyé ou non.
3. **Le brouillon** : quand il est enregistré (après une modification, pas à l'ouverture), sa forme (version 1), et sa reprise à
   l'ouverture (`InitAsync`) : une facture commencée doit se reprendre là où elle en était. Les champs inconnus du brouillon (écrits par
   une autre version ou par le web) doivent être conservés à la réécriture : si le code ne le fait pas, c'est un test en `Skip` BUG.
4. **La réception juste après la validation** (« Marquer comme reçue maintenant ») : appels envoyés, choix de variante sur 422
   `VARIANT_REQUIRED`, et le cas 404 traité à la ligne 844 environ.
5. `ValidateAll`, le choix fournisseur existant ou nouveau, et `ValidateLineAsync`.

**Lis le code et décris dans le plan ce qu'il fait réellement**, sans inventer de comportement. Si une sélection de photo (`MediaPicker`)
ou une navigation (`Shell.Current`) empêche de tester un cas, teste ce qui peut l'être et signale-le. Ne contourne jamais par un membre
privé ou par réflexion : si un cas important n'est pas atteignable, écris-le dans les questions du plan, Claude ajoutera un point
d'entrée public dans l'application.

## Règles, les mêmes que pour les lots 4 à 4k

- Aucun fichier XAML ni `Views/*.xaml.cs`, aucun fichier de l'application modifié.
- Aucun appel au backend, aucun lancement de l'application. Aucun vrai fichier d'image : octets factices, fichiers temporaires supprimés.
- Pas de reformatage. Encodage et fins de ligne intacts (CRLF reste CRLF). Aucune clé ni adresse réelle. Pas de commit.
- `SiteCurrency` : `[Collection("SiteCurrency")]`, culture fixée puis rétablie. Pas de comparaison exacte des montants formatés.
- Les dates « aujourd'hui » viennent de `DateTime.Today`. Aucune attente réelle (`Task.Delay`) dans les tests.
- Un bug trouvé donne un test en `Skip` avec la mention BUG, et une ligne dans le compte rendu.

## Vérification

Colle les sorties telles quelles dans `docs/gemini/lot-4l-compte-rendu.md` : `dotnet test Easymakemoney.Tests`, le Rebuild Android
(ligne des avertissements telle quelle, référence 216), puis le contrôle d'encodage contre `main`.
