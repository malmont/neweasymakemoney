# Lot 4k : tests du ViewModel de lecture d'un document (note de frais, transport, campagne)

Consigne de Claude du 12/11/2026. Lot de la file d'attente (`GEMINI.md` § 1 ter) : tu peux en écrire le **plan** dès que tu n'as rien
d'autre à faire. **Aucun code avant que Claude ait validé le plan dans la boîte aux lettres.**

- Plan : `docs/gemini/lot-4k-plan.md`, sur ta branche du moment, sans en créer une autre.
- Branche du code : `feature/lot-4k-tests`, créée depuis `main` à jour, **seulement quand la boîte aux lettres te le dit**.
- Pas de commit.
- Référence : lance `dotnet test Easymakemoney.Tests` sur `main` avant de commencer et note le nombre réel. Android : **216 avertissements**.

## Ce qui est déjà testé : ne le reteste pas

Lis d'abord, dans `Easymakemoney.Tests/Models/` : `GestionDocumentTests.cs`, `CampaignDocumentPrefillTests.cs`,
`InvoiceReviewTests.cs` et `InvoiceConfirmationBuilderMissingTests.cs`. Ton plan cite, pour chaque test prévu, ce qu'il ajoute.
Les faux `FakeDocumentReaderService` et autres créés au lot 4j sont à réutiliser, pas à dupliquer.

## Ce que porte ce lot

Le comportement de `Easymakemoney/ViewModels/Lists/DocumentReadViewModel.cs` **seulement** (la vérification d'une facture fournisseur,
`InvoiceReviewViewModel`, viendra dans un lot à part) :
- `InitAsync` selon le type (`note`, `facture`, transport) et la commande ;
- l'envoi d'un document (fichiers, type, locale) et ce qui ne part pas (aucun fichier, trop de fichiers, mauvais type) ;
- le suivi de la lecture (relectures successives jusqu'à `extracted` ou `failed`, sans boucle infinie) : **sans attente réelle**. Si le code
  attend avec `Task.Delay`, ne teste pas le minutage. Teste ce qui est atteignable sans attendre, et signale dans le plan ce qui ne l'est pas ;
- la confirmation en note de frais, en transport et en campagne : le corps envoyé ;
- les codes traités à part : `PERIOD_CLOSED` (proposition d'enregistrer dans la période en cours) et `INVOICE_ALREADY_CONFIRMED`
  (doublon, avec un second envoi `force` après confirmation du gérant, et aucun envoi si le gérant refuse) ;
- relire (avec confirmation, puisque cela consomme une unité IA : refus = aucune requête) et supprimer un document non confirmé.

**Lis le code et décris dans le plan ce qu'il fait réellement**, sans inventer de comportement. Si une navigation (`Shell.Current`) ou
une sélection de fichier (`FilePicker`, appareil photo) empêche de tester un cas, teste ce qui peut l'être et signale-le.

## Règles, les mêmes que pour les lots 4 à 4j

- Aucun fichier XAML ni `Views/*.xaml.cs`, aucun fichier de l'application modifié.
- Aucun appel au backend, aucun lancement de l'application. Aucun vrai fichier d'image ou de PDF : des octets factices en mémoire.
- Pas de reformatage. Encodage et fins de ligne intacts (CRLF reste CRLF).
- Aucune clé ni adresse réelle. Pas de commit.
- `SiteCurrency` : les tests qui formatent un montant vont dans la `[Collection("SiteCurrency")]`, avec la culture fixée puis rétablie.
- Pas de comparaison exacte des montants formatés. Les dates « aujourd'hui » viennent de `DateTime.Today`.
- Aucun accès aux membres privés, ni par réflexion.
- Un bug trouvé donne un test en `Skip` avec la mention BUG, et une ligne dans le compte rendu.

## Vérification

Colle les sorties telles quelles dans `docs/gemini/lot-4k-compte-rendu.md`.
1. `dotnet test Easymakemoney.Tests`.
2. Android : `dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home`. Ligne des avertissements telle quelle (référence 216).
3. En dernier, le contrôle d'encodage contre `main`.
