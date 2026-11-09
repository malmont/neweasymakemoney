# Adaptation G1 et G2 : revue du plan, par Claude

Revue du 09/11/2026 de `docs/gemini/lot-g1-g2-plan.md`.

## Verdict

**Validé avec 2 corrections (R1 et R2).** Le plan est juste sur les montants entiers, `SiteCurrency`, le passage en `long?`, `fields` et la clôture. Applique-les en codant, sans réécrire le plan.

## Réponses à tes questions

1. **Nom de la méthode** : renomme en `CloseCollectionAsync(int collectionId)`. Le nom de la classe reste tel quel.
2. **Code ISO si aucun symbole n'est trouvé** : validé.

## Corrections

### R1. Mettre le symbole en cache

`GetCurrencySymbol` parcourt toutes les cultures du système. Elle sera appelée pour **chaque montant affiché**, y compris dans les listes et les graphiques. Mets le résultat en
cache par code ISO (un dictionnaire, ou la dernière valeur connue), et prévois qu'une recherche puisse échouer ou ne rien trouver : repli sur le code ISO, sans exception.

### R2. Messages de validation lisibles

`DialogService` afficherait « • budgetCollection: Le montant doit être un entier en centimes. » : c'est du jargon.

- Traduis les noms de champs avec une petite table : `budget` et `budgetCollection` → « Budget », `nomCollection` et `name` → « Nom », `date` → « Date », `montant` → « Montant », `price` → « Prix »,
  `transporteur` et `transporteur.id` → « Transporteur », `fournisseur.id` → « Fournisseur », `typeNoteDeFraisId` → « Type », `coefficientMultiplier` → « Coefficient ». Un champ inconnu : le message seul, sans le nom technique.
- `MUST_BE_INTEGER_CENTS` devient « Montant invalide. ».

## Ce qui ne change pas

Les autres étapes et la vérification sont acceptées. Rappels : aucun fichier XAML ni `Views/*.xaml.cs`, aucun lancement de l'application, aucun appel au backend, sorties de commandes collées telles
quelles après ta dernière modification, contrôle d'encodage contre `main` en dernier, pas de commit.
