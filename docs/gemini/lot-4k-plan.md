# Lot 4k : plan de Gemini

## Ce que j'ai lu

### Consigne et règles
- `docs/gemini/lot-4k-consigne.md` : consigne du lot 4k (tests du ViewModel `DocumentReadViewModel.cs`, réutilisation des faux existants dont `FakeDocumentReaderService`, aucun test sur le minutage réel de `Task.Delay`, codes d'erreur `PERIOD_CLOSED` et `INVOICE_ALREADY_CONFIRMED`, référence Android 216 avertissements).
- `GEMINI.md` : circuit, § 1 bis (boîte aux lettres), § 1 ter (file d'attente : un seul plan d'avance, aucun code avant validation).

### Tests existants dans `Easymakemoney.Tests/Models/`
- `GestionDocumentTests.cs:8-48` : teste la désérialisation du modèle `GestionDocument` et de son objet d'extraction JObject.
- `CampaignDocumentPrefillTests.cs:8-69` : teste la logique pure d'extraction `CampaignDocumentPrefill.Read` pour une campagne.
- `InvoiceReviewTests.cs:9-45` : teste la classe de modèle `InvoiceExtraction` et les brouillons de vérification de facture fournisseur.
- `InvoiceConfirmationBuilderMissingTests.cs:9-35` : teste la construction du JSON de confirmation de facture fournisseur.

### Code de production concerné
- `Easymakemoney/ViewModels/Lists/DocumentReadViewModel.cs` (lignes 15-563) :
  - `InitAsync` (lignes 109-145) : configuration des types (`DocType = "note" | "transport" | "facture"`), commandeId, purpose (`"campaign"`). Si `IsInvoice`, chargement des factures en attente (`PendingInvoices` via `_documents.ListAsync("facture", "extracted")`). Si note non-campagne, chargement des types (`_notes.GetTypeNoteDeFrais()`). Si transport, chargement des transporteurs (`_transporteurs.GetListTransporteur()`). Propriétés dérivées (`PageTitle`, `IntroText`, `AmountLabel`, `CanEnterManually`).
  - Sélection de fichiers et gestion des pages (lignes 230-264) : `ClearPagesCommand`, `PagesText` (`"Aucune page"`, `"1 page"`, `"1 document PDF"`, `"{n} pages"`), limite à `MaxPages` (6), exclusion mutuelle PDF / photos.
  - Commande `ReadCommand` (lignes 275-310) : vérifie `!IsBusy` et `_files.Count > 0`, convertit les fichiers en `UploadFile` avec Content-Type déduit de l'extension, appelle `_documents.UploadAsync(DocType, uploads)`, enregistre `_documentId` et déclenche `WaitForReadingAsync()`.
  - Commande `RereadCommand` (lignes 312-330) : vérifie `_documentId > 0`, appelle `_documents.RereadAsync(_documentId)` et relance l'attente de lecture.
  - Suivi de la lecture `WaitForReadingAsync()` (lignes 332-397) : boucle avec `Task.Delay(3000)`. Quand le statut devient `"extracted"` : si facture fournisseur -> navigation vers `InvoiceReviewPage` ; si campagne -> navigation de retour vers la page campagne avec `?DocumentId={_documentId}` ; si note ou transport -> appel de `Prefill(document.extraction)` et passage à `Step = "verify"`. Quand le statut est `"failed"` : message d'échec `FailureText` selon le code d'erreur (`DOCUMENT_UNREADABLE`, `AI_TOO_MANY_FAILURES`) et `Step = "failed"`.
  - Commande `ManualEntryCommand` (lignes 399-407) : ignorée si facture ou campagne ; sinon appelle `Prefill(null)` et passe `Step = "verify"`.
  - Préremplissage `Prefill(JObject? extraction)` (lignes 409-439) : lecture du montant (converti en décimal), date, marchand/fournisseur, description, type de note (pour note) ou numéro facture / numéro suivi / transporteur (pour transport), alertes de confiance basse (< 0.7) et de détection de doublon (`"Ce document semble déjà enregistré."`).
  - Commande `ConfirmCommand` (lignes 446-543) : validation locale du montant (> 0), du type (si note) ou du transporteur (si transport) via `ShowInfoAsync`. Si `_documentId == 0`, alerte « Lisez d'abord un document... ». Envoi du corps JSON via `_documents.ConfirmAsync(_documentId, body)`.
    - Gestion de `PERIOD_CLOSED` (note de frais) : dialogue de confirmation proposant d'enregistrer sur la période en cours (`classificationDate = Today`). Si accepté, second envoi avec `classificationDate`. Si refusé, abandon.
    - Gestion de `INVOICE_ALREADY_CONFIRMED` (transport) : dialogue de confirmation pour forcer l'enregistrement. Si accepté, second envoi avec `force = true`. Si refusé, abandon.
    - En cas de succès : affichage du message de confirmation (« Note de frais enregistrée. » / « Frais de port enregistré. ») et navigation retour sans Shell.

### Services sous-jacents
- `Easymakemoney/UseCase/DocumentUseCase/DocumentReaderUseCase.cs` (utilise `IDocumentReaderService`, déjà mocké dans `FakeDocumentReaderService.cs`).
- `Easymakemoney/Models/IListNoteDeFraisServices.cs` (méthode `GetTypeNoteDeFrais()`).
- `Easymakemoney/Services/IListTransporteurService.cs` (méthode `GetListTransporteur()`).

## Prérequis à installer ou à vérifier (commandes exactes, rien n'est lancé)

Aucun outil ni paquet à installer.
Vérification de la base de tests avant de coder le lot :
```
dotnet test Easymakemoney.Tests
```
Référence actuelle constatée : **881 tests passants**, 0 échec.
Référence Android : **216 avertissements**, 0 erreur.

## Étapes

### Étape 1 : Faux dans `Easymakemoney.Tests/Fakes/`
1. Réutiliser `FakeDocumentReaderService.cs` (créé au lot 4j) et y ajouter si nécessaire `UploadResult`, `ListResult`, `RereadResult` pour simuler `UploadAsync`, `ListAsync` et `RereadAsync`.
2. Créer `FakeListNoteDeFraisServices.cs` : implémente `IListNoteDeFraisServices` et fournit une liste configurable de `TypeNoteDeFrais`.
3. Créer `FakeListTransporteurService.cs` : implémente `IListTransporteurService` et fournit une liste configurable de `Transporteur`.

### Étape 2 : Tests dans `Easymakemoney.Tests/ViewModels/DocumentReadViewModelTests.cs`
Fichier placé dans la collection `[Collection("SiteCurrency")]`.

1. **`InitAsync` et propriétés selon le type de document** :
   - `InitAsync_Note_ConfiguresPageTitleAndLoadsTypes` : configure `DocType = "note"`, vérifie `PageTitle == "Lire un reçu"`, `AmountLabel == "Montant toutes taxes"`, `CanEnterManually == true`, et charge les types dans `Types`. *(Ajoute par rapport à `GestionDocumentTests` : configuration UI et chargement des types).*
   - `InitAsync_Transport_ConfiguresPageTitleAndLoadsCarriers` : configure `DocType = "transport"`, vérifie `PageTitle == "Lire une facture de transport"`, `AmountLabel == "Montant hors taxes"`, et charge les transporteurs dans `Transporteurs`. *(Ajoute : configuration UI transport).*
   - `InitAsync_Campaign_ConfiguresPageTitleAndDisablesManualEntry` : configure `DocType = "note"`, `Purpose = "campaign"`, vérifie `PageTitle == "Lire la facture de la régie"`, `CanEnterManually == false`. *(Ajoute : restriction de saisie manuelle pour campagne).*
   - `InitAsync_Invoice_LoadsPendingInvoices` : configure `DocType = "facture"`, vérifie `PageTitle == "Lire une facture fournisseur"` et peuple `PendingInvoices` avec les factures au statut `extracted`. *(Ajoute : historique des factures en attente).*

2. **Sélection et affichage des pages** :
   - `PagesText_FormatsCountsCorrectly` : vérifie l'état initial `"Aucune page"`, et le formatage selon le nombre de pages (`"1 page"`, `"1 document PDF"`, `"{n} pages"`). *(Ajoute : affichage du compteur de pièces jointes).*
   - `ManualEntry_Note_SetsVerifyStepWithDefaultDate` : pour une note, `ManualEntryCommand` bascule `Step == "verify"`, `Date == Today`, `Merchant == null`. Pour facture ou campagne, la commande est ignorée. *(Ajoute : entrée manuelle alternative).*

3. **Envoi et relecture de document** :
   - `Read_WithoutFiles_DoesNotCallUpload` : si aucun fichier sélectionné, `ReadCommand` ne lance aucun appel `UploadAsync`. *(Ajoute : garde-fou d'envoi).*
   - `Reread_WithoutDocumentId_DoesNotCallService` : si `_documentId == 0`, `RereadCommand` ne fait rien. *(Ajoute : garde-fou de relecture).*

4. **Préremplissage et vérification des champs (`Prefill`)** :
   - `Prefill_Note_PopulatesAmountDateDescriptionAndType` : extraction de reçu (montant 45,50 $, date 2026-11-05, marchand « Uber », type 3), vérifie `AmountText == "45,50"`, `Merchant == "Uber"`, `Description == "Uber"`, `SelectedType.id == 3`. *(Ajoute : hydratation UI d'une note).*
   - `Prefill_Transport_PopulatesCarrierTrackingAndInvoiceNumber` : extraction transport (montant 120,00 $, facture « FT-10 », tracking « TRK123 », transporteur 2), vérifie les propriétés correspondantes. *(Ajoute : hydratation UI d'un transport).*
   - `Prefill_LowConfidenceAndDuplicate_AddsWarningRemarks` : confiance < 0.7 et présence de `duplicate`, vérifie que `RemarksText` contient « Lecture incertaine : vérifiez chaque champ. » et « Ce document semble déjà enregistré. ». *(Ajoute : remontée des alertes OCR).*

5. **Validation locale lors de `ConfirmCommand`** :
   - `Confirm_ValidationErrors_ShowsInfoAndDoesNotSend` (`[Theory]`) :
     - Montant manquant ou invalide
     - Note de frais sans type sélectionné
     - Facture de transport sans transporteur sélectionné
     - Absence de `_documentId` (« Lisez d'abord un document... »)
     Vérifie que le dialogue informatif correspondant est affiché et qu'aucun appel à `ConfirmAsync` n'est envoyé. *(Ajoute : validation des champs requis avant enregistrement).*

6. **Confirmation et corps envoyé (`ConfirmAsync`)** :
   - `Confirm_Note_SendsExpectedBodyAndShowsSuccess` : note confirmée avec montant en centimes, date formatée, description, typeId, merchant ; vérifie l'appel `ConfirmAsync(_documentId, body)` et le message « Note de frais enregistrée. ». *(Ajoute : contrat de confirmation note).*
   - `Confirm_Transport_SendsExpectedBodyAndShowsSuccess` : transport confirmé avec `commandeId`, `transporteurId`, montant, numéro de facture, tracking ; vérifie le corps et le message « Frais de port enregistré. ». *(Ajoute : contrat de confirmation transport).*

7. **Codes d'erreur spécifiques (`PERIOD_CLOSED` et `INVOICE_ALREADY_CONFIRMED`)** :
   - `Confirm_Note_409PeriodClosed_Refused_DoesNotRetry` : serveur renvoie `PERIOD_CLOSED`, dialogue de confirmation « Période clôturée » affiché, gérant clique Annuler -> aucun second appel. *(Ajoute : traitement de rejet période clôturée).*
   - `Confirm_Note_409PeriodClosed_Accepted_RetriesWithCurrentPeriodClassificationDate` : gérant clique « Période en cours » -> second appel `ConfirmAsync` envoyé avec `classificationDate == DateTime.Today.ToString("yyyy-MM-dd")`. *(Ajoute : repli automatique vers la période courante).*
   - `Confirm_Transport_409InvoiceAlreadyConfirmed_Refused_DoesNotRetry` : serveur renvoie `INVOICE_ALREADY_CONFIRMED`, dialogue « Facture déjà enregistrée », gérant clique Annuler -> aucun second appel. *(Ajoute : garde-fou doublon transport).*
   - `Confirm_Transport_409InvoiceAlreadyConfirmed_Accepted_RetriesWithForceTrue` : gérant clique « Enregistrer » -> second appel `ConfirmAsync` envoyé avec `force = true`. *(Ajoute : confirmation forcée par le gérant).*

Total des tests prévus : environ **18 à 20 tests unitaires** (avec Theories).

## Comment je vérifie

1. **Exécution des tests xUnit** :
   ```
   dotnet test Easymakemoney.Tests
   ```
   Résultat attendu : 881 + ~20 = **~901 tests passants**, 0 échec, 0 ignoré.
2. **Rebuild Android** :
   ```
   dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home
   ```
   Résultat attendu : **216 avertissements**, 0 erreur.
3. **Contrôle d'encodage et fins de ligne** :
   ```
   git diff --check main
   ```
   Résultat attendu : aucune sortie, code de sortie 0.

## Risques et points incertains

1. **Gestion de `Task.Delay` dans `WaitForReadingAsync`** :
   Comme indiqué dans la consigne de Claude, aucun test ne doit attendre réellement avec `Task.Delay`. Pour tester les transitions d'état de lecture, nous initialiserons `_documentId` et testerons les méthodes directes `Prefill`, `ManualEntry`, `Confirm`, ou appellerons `Stop()` pour arrêter immédiatement la boucle si `WaitForReadingAsync` est déclenché.
2. **Sélecteurs de fichiers et appareil photo (`MediaPicker`, `FilePicker`)** :
   Ces API système MAUI requièrent des autorisations et un contexte d'application. Le test des commandes `TakePhotoCommand`, `PickPhotosCommand`, `PickPdfCommand` se concentrera sur l'absence de plantage et le comportement quand la capture n'est pas supportée.
3. **Navigation Shell (`Shell.Current`)** :
   Comme pour les autres lots, les commandes de navigation (`ResumeInvoice`, redirection vers `InvoiceReviewPage`, retour `..`) s'exécutent en toute sécurité en environnement de test xUnit sans Shell.

## Questions pour Claude ou le propriétaire

Aucune question bloquante : la consigne est précise, le code source de `DocumentReadViewModel.cs` a été analysé en détail.
Le plan est prêt pour validation dans la boîte aux lettres.

