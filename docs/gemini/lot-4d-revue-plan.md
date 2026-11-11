# Lot 4d : revue du plan, par Claude

Revue du 10/11/2026 de `docs/gemini/lot-4d-plan.md`.

## Verdict

**Validé avec 6 corrections (D1 à D6).** Applique-les en codant, sans réécrire le plan.

**Avant de coder :** lance `git merge main` sur ta branche, pour récupérer cette revue.

## Corrections

### D1. La référence a changé

`main` compte aujourd'hui **392 tests**, et non 223. Ton compte rendu doit donner 392 + tes nouveaux tests, 0 échec. Les avertissements Android restent à 325.

### D2. Pas de doublon avec les tests existants

Lis d'abord ces fichiers. Ne reteste pas ce qu'ils couvrent déjà :

- `Models/PeriodResultTests.cs` couvre déjà la lecture de `frozen`, `netRate` nul, `cogs.estimatedItems` / `estimatedAmount` et `otherRevenue`. Pour le sujet 1, ajoute seulement l'URL appelée par `PeriodResultService` et les textes qui ne sont pas encore testés.
- `UseCases/SaveUseCasesTests.cs` couvre déjà l'arrondi en centimes de `SaveFraisDePortUseCase` et `SaveNoteDeFraisUseCase`.
- `Models/ApiErrorTests.cs` et `Services/HttpServiceTests.cs` couvrent déjà la traduction de `PERIOD_CLOSED` et de `OUTSIDE_PERIOD`.

### D3. Sujet 4 : tester le vrai chemin de la saisie

Ne teste pas `MoneyHelper.ToCents(12.50)` : c'est déjà fait, et ce ne serait pas le chemin réel. Le bug d'origine venait du texte saisi. Teste donc la chaîne complète, en culture `fr-CA` fixée puis rétablie :

- texte « 12,50 » → `DecimalInputConverter.Parse` → modèle de formulaire → cas d'usage d'enregistrement → corps envoyé avec `1250` ;
- même chose avec « 1 234,50 », espace insécable compris : `123450` ;
- URLs d'ajout et de suppression des frais de port et des notes.

Pour la période fermée, ne reteste pas le message. Vérifie seulement que l'`ApiException` sort bien du service, avec le vrai `HttpService` et `TestHttpMessageHandler`.

### D4. Sujet 2 : les erreurs avec le vrai `HttpService`

`FakeHttpService` ne lève jamais d'`ApiException`. Pour les cas 409, monte le vrai `HttpService` sur `TestHttpMessageHandler`, comme dans `HttpServiceTests`. Pour les succès, `FakeHttpService` suffit. Vérifie que le corps envoyé est bien nul.

### D5. Sujet 6, manque n° 7 : constater avant d'affirmer

Pour la ligne validée avec une quantité à 0 ou un coût invalide :
- lis d'abord ce que fait `InvoiceConfirmationBuilder.Build` ;
- si le code refuse, teste ce refus ;
- s'il laisse passer une ligne à quantité 0, c'est un bug : écris le test en `Skip = "BUG: …"`, sans modifier le code de l'application.

Même règle pour les 8 autres manques : le test décrit le comportement constaté, ou un bug marqué.

### D6. Sujet 5 : textes en devise

`SalePriceText` et `QuantityCostText` dépendent de la culture et de `SiteCurrency`. Utilise `[Collection("SiteCurrency")]`, fixe la culture, puis rétablis-la ainsi que `SiteCurrency.Reset()`. Aucune comparaison exacte : vérifie le symbole ou le code de devise, l'absence de l'autre devise, les chiffres attendus, et la mention de l'absence de taux quand il n'y en a pas.

## Ce qui ne change pas

Le reste du plan est accepté. Mêmes règles qu'avant :
- aucun fichier XAML ni `Views/*.xaml.cs` ;
- aucun appel au backend, aucun lancement de l'application ;
- pas de reformatage, encodage intact, pas de commit ;
- sorties collées telles quelles dans `docs/gemini/lot-4d-compte-rendu.md` ;
- contrôle d'encodage contre `main` en dernier.
