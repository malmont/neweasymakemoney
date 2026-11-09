# Lot 3 : revue du plan, par Claude

Revue du 09/11/2026 de `docs/gemini/lot-3-plan.md`, faite contre la consigne et le code de `feature/lot-3-erreurs`.

## Verdict

**Validé avec corrections (P1 à P4).** Le plan est complet sur `HttpService`, les services, B4, le prix en centimes et
`IDialogService`. **P1 est bloquante** : sans elle, le lot ferait planter des écrans qui, aujourd'hui, s'affichent vides.

## Réponses à tes questions

1. **`IDialogService` passé au popup par les ViewModels de listes : validé.**
2. **Libellés de suppression : validés**, tels que tu les proposes.

## Corrections

### P1. Toute lecture doit attraper l'exception, pas seulement les listes (bloquante)

Aujourd'hui, une erreur rend `default`. Après ce lot, elle lève une `ApiException`. Ton plan traite les `GetList…` des
ViewModels de listes, mais pas les autres appelants :

- **les 8 ViewModels de `ViewModels/Statistiques/`**, et **les 3 de `ViewModels/Dashboard/`** (collection, commande,
  produit), qui chargent leurs données sans passer par ces listes ;
- tout ViewModel dont la méthode de chargement est appelée depuis un `OnAppearing`.

Une exception non attrapée dans un `async void OnAppearing` arrête l'application. **Fais l'inventaire exact de tous les appelants**
(`grep` des cas d'usage dans `ViewModels/` et `Views/`), et mets-le dans le compte rendu.

**Attrape l'erreur dans le ViewModel, et ne modifie aucun `Views/*.xaml.cs`** : ces fichiers appartiennent au chantier de
l'interface. Chaque méthode publique de chargement doit donc être sûre, sans `try/catch` côté vue.

Vérifie aussi les endroits qui lisaient le résultat sans contrôle (`null` de `default`, par exemple des calculs sur un
objet de tableau de bord) : le chemin d'erreur doit laisser l'écran dans un état stable.

### P2. Un seul message d'erreur à la fois

Un formulaire produit charge plusieurs listes dès son ouverture (`LoadCategories`, `LoadStyles`…). Sans réseau, cela empilerait
autant de boîtes de dialogue. Dans `DialogService.ShowErrorAsync`, n'affiche pas de nouveau message tant qu'un autre est
ouvert : les erreurs arrivées pendant ce temps vont au journal. Protège aussi l'absence de `Shell.Current`.

### P3. Fin de session et site absent : silencieux aussi

Tu ne silences que `SessionExpiredException`. Après l'effacement de la session ou du site, une requête encore en cours lève
`AuthenticationMissingException` ou `TenantMissingException` : ces deux cas ne doivent pas non plus ouvrir de boîte de dialogue.
Journal seulement.

### P4. Recensement des `async void`

Ta liste annonce 41 occurrences, mais je compte 18 + 14 + 7 (tu écris « 8 » pour 7 lignes listées dans les `FormModels`) + 1 = 40.
Refais le compte par commande. Critère de fin : un `grep -rn "async void" ViewModels` ne rend **que** les exceptions que tu
listes, chacune avec son `try/catch`.

## Ce qui ne change pas

- Les étapes 1 à 5 sont acceptées, avec ces corrections, notamment :
  - `ApiException`, signatures publiques inchangées ;
  - 7 suppressions, 7 créations, B4 et 404 des frais de port ;
  - prix d'achat en centimes (`CultureInfo.InvariantCulture`), sans toucher aux frais de port ;
  - popup laissé ouvert en cas d'erreur.
- Les 10 essais sont acceptés.
- Rappels :
  - aucun fichier XAML ;
  - aucun lancement de l'application et aucun appel au backend ;
  - sorties de commandes collées telles quelles, après ta dernière modification ;
  - pas de reformatage, ni de changement d'encodage ;
  - pas de commit.
