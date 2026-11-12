# Lot 4j : tests des ViewModels des voyages d'achat et de la publicité

Consigne de Claude du 12/11/2026. Lot de la file d'attente (`GEMINI.md` § 1 ter) : tu peux en écrire le **plan** dès que tu n'as rien
d'autre à faire. **Aucun code avant que Claude ait validé le plan dans la boîte aux lettres.**

- Plan : `docs/gemini/lot-4j-plan.md`, sur ta branche du moment, sans en créer une autre.
- Branche du code : `feature/lot-4j-tests`, créée depuis `main` à jour, **seulement quand la boîte aux lettres te le dit**.
- Pas de commit.
- Référence : lance `dotnet test Easymakemoney.Tests` sur `main` avant de commencer et note le nombre réel dans le plan.
  Référence Android : **216 avertissements**.

## Ce qui est déjà testé : ne le reteste pas

Lis d'abord, dans `Easymakemoney.Tests/Models/` : `TripTests.cs`, `CampaignTests.cs`, `CampaignRequestBuilderTests.cs`,
`CampaignProfitabilityTests.cs` et `CampaignDocumentPrefillTests.cs`. Ton plan cite, pour chaque test prévu, ce qu'il ajoute par
rapport à eux.

## Ce que porte ce lot

Le **comportement des ViewModels** de :
- `Easymakemoney/ViewModels/Lists/TripViewModels.cs` (liste, formulaire, suivi d'un voyage) ;
- `Easymakemoney/ViewModels/Lists/CampaignListViewModel.cs`, `CampaignFormViewModel.cs` et `CampaignProfitabilityViewModel.cs`.

Ce qui compte : les requêtes envoyées (méthode du service, arguments), celles qui ne partent pas, les messages et confirmations
montrés au gérant, et les codes d'erreur traités à part (par exemple `TRIP_OVERLAP`, `CAMPAIGN_NOT_FOUND`, `PERIOD_CLOSED`, taux de
change requis pour une devise étrangère). **Lis le code et décris dans le plan ce qu'il fait réellement**, sans inventer de
comportement.

Ajoute dans `Easymakemoney.Tests/Fakes/`, **dans le projet de tests seulement**, les faux des services utilisés par ces ViewModels
s'ils n'existent pas encore. Chacun enregistre les appels et peut lever une `ApiException` construite par `ApiError.FromHttpResponse`.

Points précis à couvrir :
- **Voyages** : liste chargée, état vide, erreur ; formulaire en création et en modification (corps envoyé, devise du site non
  envoyée, taux de change demandé pour une devise étrangère, dates invalides refusées sans requête) ; `TRIP_OVERLAP` → message propre ;
  suivi d'un voyage : chargement, et `TRIP_NOT_FOUND`.
- **Campagnes** : liste paginée (première page, page suivante, filtre), suppression avec confirmation (refus = aucune requête),
  `CAMPAIGN_NOT_FOUND` ; formulaire en création, modification, relance depuis une campagne (`fromCampaignId`) et préremplissage depuis
  un document lu (`UseDocumentAsync`) : corps envoyés, ciblage d'articles (dont « retirer toutes les cibles » = liste vide) ;
  rentabilité : chargement et `CAMPAIGN_NOT_FOUND`.
- Si une navigation (`Shell.Current`) empêche de tester un cas, teste ce qui peut l'être et signale-le dans le plan.

## Règles, les mêmes que pour les lots 4 à 4i

- Aucun fichier XAML ni `Views/*.xaml.cs`, aucun fichier de l'application modifié.
- Aucun appel au backend, aucun lancement de l'application.
- Pas de reformatage. Encodage et fins de ligne intacts (CRLF reste CRLF).
- Aucune clé ni adresse réelle. Pas de commit.
- `SiteCurrency` : les tests qui formatent un montant vont dans la `[Collection("SiteCurrency")]`, avec la culture fixée puis rétablie.
- Pas de comparaison exacte des montants formatés.
- Les dates « aujourd'hui » viennent de `DateTime.Today`, jamais en dur.
- Aucun accès aux membres privés, ni par réflexion.
- Un bug trouvé donne un test en `Skip` avec la mention BUG, et une ligne dans le compte rendu.

## Vérification

Colle les sorties telles quelles dans `docs/gemini/lot-4j-compte-rendu.md`.
1. `dotnet test Easymakemoney.Tests`.
2. Android : `dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home`. Colle la ligne des avertissements telle quelle (référence 216).
3. En dernier, le contrôle d'encodage contre `main`.
