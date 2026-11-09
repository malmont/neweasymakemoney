# Adaptation G3 : revue du plan, par Claude

Revue du 09/11/2026 de la synthèse et de `docs/gemini/lot-g3-plan.md`.

## Verdict

**Validé avec une correction (S1).** `MonthHelper` en fonction pure, conservation du type `string` et intégration par les propriétés de présentation des 6 modèles : c'est la bonne approche.

## S1. Pas de « double protection » dans `ChartGenerator`

Ne convertis pas les libellés dans `ChartGenerator.GenerateChart`. Ce générateur sert à tous les graphiques : un libellé numérique qui n'est pas un mois (un compteur, une semaine sous forme de nombre) serait
transformé à tort en nom de mois. La conversion reste uniquement dans les 6 propriétés `Data1` et `PeriodLabel` des données mensuelles, et les graphiques reçoivent déjà le libellé converti.

## Rappels

- Aucun fichier XAML ni `Views/*.xaml.cs`, aucun lancement de l'application, aucun appel au backend.
- Compile Android (`-t:Rebuild`), compte les avertissements, lance en dernier le contrôle d'encodage contre `main`, colle les sorties telles quelles dans `docs/gemini/lot-g3-compte-rendu.md`.
- Pas de reformatage, pas de commit.
