# Adaptation aux livraisons G1 et G2 du backend

Consigne de Claude du 09/11/2026. **Écris d'abord ton plan** dans `docs/gemini/lot-g1-g2-plan.md`, puis arrête-toi. Tu ne codes rien avant que je l'aie validé.

- Branche : `feature/adaptation-g1-g2`, créée depuis `main` (`git switch -c feature/adaptation-g1-g2 main`).
- Pas de commit.

Contrat : `docs/backend-lots-gestion-G1-G4.md`. G1 et G2 sont **déjà en production** : ces corrections sont prioritaires.

## Ce que ton plan doit couvrir

### 1. Montants envoyés : entiers

Les 4 écritures ×100 (`SaveCollectionUseCase`, `SaveCommandUseCase`, `SaveFraisDePortUseCase`, `SaveNoteDeFraisUseCase`) doivent envoyer un
**entier arrondi** (`Math.Round(valeur * 100)`). `12,345` donnerait sinon `1234.5`, refusé par le backend (422 `MUST_BE_INTEGER_CENTS`). Dis si les
types des modèles doivent passer en `long`, et ce que cela change à la lecture.

### 2. Erreurs de validation : `fields`

Le backend renvoie maintenant `fields: {"budget": "MUST_BE_INTEGER_CENTS"}`. `ApiError.FromHttpResponse` doit le lire, en plus de `errors` et
`violations`, et produire des messages en français : `REQUIRED`, `MUST_BE_INTEGER_CENTS`, `MUST_BE_NUMBER`, `INVALID_DATE`, `INVALID_ID`.

### 3. Devise du site

Chaque réponse portant un montant contient `currency` (ISO 4217), à la racine ou sur chaque élément. Aujourd'hui, l'affichage dépend de la culture
de l'appareil (`{0:C}` dans `CurrencyConverter`, `.ToString("C")` dans `ChartGenerator`), donc la devise est fausse.

- Une classe statique simple, par exemple `SiteCurrency`, mémorise le dernier code reçu. `HttpService` le relève dans la réponse JSON, à la racine
  ou sur le premier élément d'un tableau, **sans changer les modèles**. `ISiteDataCleaner.ClearAllAsync()` le remet à zéro.
- Une **fonction pure** formate un montant en centimes : la devise vient de `SiteCurrency`, la culture de l'appareil ne sert qu'à la langue et aux séparateurs.
  Sans code connu : affiche le nombre sans symbole.
- `CurrencyConverter` et `ChartGenerator` l'utilisent. Aucun symbole écrit en dur.

### 4. Clôture d'une collection

`CollectionCloseService` lit la réponse en `string`. Depuis G1 et G2, c'est un objet JSON : la lecture échoue alors que la clôture a réussi. Ne lis plus le
corps ; en cas de succès, affiche « Collection clôturée » et recharge le tableau de bord. Un 409 `ALREADY_CLOSED` passe par `IDialogService`.

### 5. Vérification

Android (`-t:Rebuild`), décompte des avertissements, contrôle d'encodage contre `main` en dernier. Sorties collées telles quelles dans
`docs/gemini/lot-g1-g2-compte-rendu.md`. Liste des essais à prévoir dans le plan.

## Hors du lot

- **Aucun fichier XAML ni `Views/*.xaml.cs`** : l'interface est de mon ressort.
- G3 et G4 : pas encore livrés.
- **Mêmes règles** : pas d'appel au backend, pas de lancement de l'application, pas de reformatage, encodage intact, aucune clé ni adresse de site, pas de commit.
