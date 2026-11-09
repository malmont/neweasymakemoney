# Adaptation G3 : plan de Gemini

Branche : `feature/adaptation-g3` (créée depuis `main`)  
Base de comparaison : `main`

---

## Ce que j'ai lu

1. `docs/backend-lots-gestion-G1-G4.md:38-44, 75-77, 111-121` : annonce et livraison de G3 sur `dev` (334f41e9) ; `month` devient un entier JSON 1..12 dans 6 statistiques (`chiffre-affaires`, `panier-moyen`, `nombre-commandes`, `frais/total`, `taxes/monthly`, `carrier/statistics`), et n'est plus une chaîne en anglais.
2. `Easymakemoney/Models/RevenueData.cs:39-54` : `MonthlyRevenueData` (Chiffre d'affaires) possède `public string Month { get; set; }` et `public string Data1 => Month;`.
3. `Easymakemoney/Models/RevenueData.cs:77-86` : `MonthlyOrderData` (Nombre de commandes) possède `public string Month { get; set; }` et `public string Data1 => Month;`.
4. `Easymakemoney/Models/RevenueData.cs:110-119` : `MonthlyAverageOrderValueData` (Panier moyen) possède `public string Month { get; set; }` et `public string Data1 => Month;`.
5. `Easymakemoney/Models/FraisEvolution.cs:25-41` : `MonthlyFraisData` (Frais / total) possède `[JsonProperty("month")] public string Month { get; set; }` et `public string PeriodLabel => Month;`.
6. `Easymakemoney/Models/FraisClientTotal.cs:5-16` : `MonthlyTaxData` (Taxes / monthly) possède `[JsonProperty("month")] public string Month { get; set; }` et `public string Data1 => Month;`.
7. `Easymakemoney/Models/FraisClientTotal.cs:18-29` : `MonthlyCarrierData` (Carrier / statistics) possède `[JsonProperty("month")] public string Month { get; set; }` et `public string Data1 => Month;`.
8. `Easymakemoney/Utilities/ChartGenerator.cs:21-23` : génération des étiquettes de graphique (`ChartEntry.Label`) à partir de `data.Data1`.
9. `Easymakemoney/Views/Statistiques/ChiffresAffairePage.xaml:49` : badge XAML lié à `Date="{Binding Data1}"`.
10. `Easymakemoney/Views/Statistiques/PanierMoyenPage.xaml:49` : badge XAML lié à `Date="{Binding Data1}"`.
11. `Easymakemoney/Views/Statistiques/OrderNumberPage.xaml:43` : badge XAML lié à `Date="{Binding Data1}"`.
12. `Easymakemoney/Views/Statistiques/FraisClientTotalViewPage.xaml:39` : badge XAML lié à `Date="{Binding Data1}"`.
13. `Easymakemoney/Views/Statistiques/FraisEvolutionEntreprisePage.xaml:62` : libellé XAML lié à `Text="{Binding PeriodLabel}"`.

---

## Prérequis à installer ou à vérifier (commandes exactes, rien n'est lancé)

Aucun outil ni paquet à installer. Le SDK .NET 10 est opérationnel.

Commandes de vérification initiale :
```bash
git branch --show-current
git status
```

---

## Étapes

### 1. Fonction pure et testable de formatage du nom de mois (`MonthHelper`)

- **Création du fichier** : `Easymakemoney/Utilities/MonthHelper.cs`
- **Implémentation prévue** :
  ```csharp
  namespace Easymakemoney.Utilities;

  public static class MonthHelper
  {
      private static readonly string[] FrenchMonths = new[]
      {
          "janvier", "février", "mars", "avril", "mai", "juin",
          "juillet", "août", "septembre", "octobre", "novembre", "décembre"
      };

      /// <summary>
      /// Fonction pure et testable.
      /// Convertit un mois (entier 1..12 ou chaîne convertible "1".."12") en nom de mois français (janvier, février…).
      /// Une valeur non numérique ou hors bornes reste affichée telle quelle.
      /// </summary>
      public static string FormatMonthName(string? monthValue)
      {
          if (string.IsNullOrWhiteSpace(monthValue))
              return string.Empty;

          var trimmed = monthValue.Trim();

          if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var monthNumber))
          {
              if (monthNumber >= 1 && monthNumber <= 12)
              {
                  return FrenchMonths[monthNumber - 1];
              }
          }

          // Valeur non numérique (ou hors bornes 1..12) : reste affichée telle quelle
          return monthValue;
      }
  }
  ```
- **Caractéristiques de la fonction** :
  - **Pure** : aucun effet de bord, aucun état mutable, résultat strictement déterministe.
  - **Testable** : facilement vérifiable avec `"1"`..`"12"`, `"0"`, `"13"`, `"mars"`, `""`, `null`.
  - **Règle respectée** : « Une valeur non numérique reste affichée telle quelle. »

### 2. Proposition pour le type des modèles concernés

- **Proposition : Conserver le type `string` (ou `string?`) pour la propriété `Month` dans les 6 modèles.**
  - *Justification* :
    1. **Désérialisation universelle** : Avec `Newtonsoft.Json`, une propriété C# `public string Month { get; set; }` désérialise sans aucune erreur aussi bien un entier JSON `3` (il devient la chaîne `"3"`), qu'une chaîne `"3"` ou une chaîne textuelle `"mars"`.
    2. **Résilience et non-régression** : Si le type `Month` était passé en `int`, l'arrivée d'une valeur non numérique (ex: ancien format, libellé textuel, ou test unitaire mocké) lèverait une exception `JsonSerializationException` au lieu d'être « affichée telle quelle ». Le type `string` est le seul qui garantit le respect de la règle « une valeur non numérique reste affichée telle quelle ».
    3. **Aucun impact sur le contrat HTTP** : Le backend G3 envoie un entier JSON `1..12`, qui est directement et fidèlement lu dans la propriété `Month`.

- **Mise à jour des propriétés d'exposition UI dans les 6 modèles** :
  - `Easymakemoney/Models/RevenueData.cs` :
    - `MonthlyRevenueData` (Chiffre d'affaires) :
      `public string Data1 => MonthHelper.FormatMonthName(Month);`
    - `MonthlyOrderData` (Nombre de commandes) :
      `public string Data1 => MonthHelper.FormatMonthName(Month);`
    - `MonthlyAverageOrderValueData` (Panier moyen) :
      `public string Data1 => MonthHelper.FormatMonthName(Month);`
  - `Easymakemoney/Models/FraisEvolution.cs` :
    - `MonthlyFraisData` (Frais entreprise / total) :
      `public string PeriodLabel => MonthHelper.FormatMonthName(Month);`
  - `Easymakemoney/Models/FraisClientTotal.cs` :
    - `MonthlyTaxData` (Taxes mensuelles) :
      `public string Data1 => MonthHelper.FormatMonthName(Month);`
    - `MonthlyCarrierData` (Frais transporteur) :
      `public string Data1 => MonthHelper.FormatMonthName(Month);`

- **Mise à jour de `Easymakemoney/Utilities/ChartGenerator.cs`** :
  - Dans la boucle de génération des entrées du graphique :
    ```csharp
    string label = DateTime.TryParseExact(data.Data1, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
        ? date.ToString("dd/MM")
        : MonthHelper.FormatMonthName(data.Data1);
    ```
  - **Pourquoi** : Assure que le libellé sous chaque barre ou point de graphique mensuel affiche le nom du mois en français (janvier, février…), qu'il provienne directement d'un modèle ou d'une source tierce.

---

## Comment je vérifie

1. **Compilation Android Rebuild** :
   ```bash
   dotnet build Easymakemoney/Easymakemoney.csproj -f net10.0-android -c Debug -t:Rebuild -p:JavaSdkDirectory=/Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home
   ```
   Résultat attendu : `0 Error(s)`, et un décompte des avertissements $\le 318$.

2. **Contrôle d'encodage et BOM contre `main`** :
   ```bash
   for f in $(git diff --name-only main); do a=$(git show main:"$f" 2>/dev/null | file -b -); b=$(file -b "$f" 2>/dev/null); [ -e "$f" ] && [ "$a" != "$b" ] && echo "DIFF $f: '$a' vs '$b'"; done; echo fin
   ```
   Résultat attendu : aucun BOM ajouté ni retiré ; encodage UTF-8 conforme.

3. **Contrôle des `async void`** :
   ```bash
   grep -rn "async void" Easymakemoney/ViewModels
   ```
   Résultat attendu : strictement les 8 méthodes d'exception autorisées.

4. **Liste des essais à prévoir (recette et tests lot 4)** :
   - *Tests unitaires de `MonthHelper.FormatMonthName`* :
     - Entrée `"1"` → `"janvier"`
     - Entrée `"2"` → `"février"`
     - Entrée `"12"` → `"décembre"`
     - Entrée avec espaces `" 3 "` → `"mars"`
     - Entrée avec zéro initial `"04"` → `"avril"`
     - Valeur non numérique `"janvier"` → `"janvier"` (reste telle quelle)
     - Valeur non numérique `"T1"` → `"T1"` (reste telle quelle)
     - Valeur hors bornes `"0"` → `"0"`
     - Valeur hors bornes `"13"` → `"13"`
     - Entrée vide `""` ou `null` → `string.Empty`
   - *Tests d'intégration des 6 écrans de statistiques* :
     - `ChiffreAffaire` : vérifier que la liste et le graphique affichent « janvier », « février », etc.
     - `PanierMoyen` : vérifier que la liste et le graphique affichent les noms de mois français.
     - `OrderNumber` (Nombre de commandes) : vérifier que la liste et le graphique affichent les mois en français.
     - `FraisEvolutionEntreprise` (Frais entreprise) : vérifier que les lignes du tableau affichent `PeriodLabel` en français.
     - `FraisClientTotal` (Taxes et Transporteurs) : vérifier que les listes et graphiques affichent les mois en français.
   - *Test de non-régression XAML* :
     - Vérifier qu'aucun fichier XAML ni code-behind `Views/*.xaml.cs` n'a été modifié et que les liaisons `{Binding Data1}` et `{Binding PeriodLabel}` fonctionnent sans accroc.

---

## Risques et points incertains

1. **Casse du nom de mois (minuscule vs majuscule)** :
   La consigne mentionne explicitement `janvier, février…` en minuscules (convention standard de la langue française). Si une majuscule est souhaitée (ex: « Janvier »), la table `FrenchMonths` peut être adaptée en un seul endroit sans toucher au reste de l'application.

---

## Questions pour Claude ou le propriétaire

1. **Casse des noms de mois** :
   Préférez-vous les noms de mois strictement en minuscules (« janvier, février… » comme dans la consigne) ou avec la première lettre en majuscule (« Janvier, Février… ») ?

