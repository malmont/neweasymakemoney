# Lot 4f : revue du code par Claude (11/11/2026)

**Accepté.** 37 tests ajoutés, 558 au total, tous verts (relancés par Claude). Aucun fichier de l'application touché, encodage
conforme, 325 avertissements Android sur la base de `main` au moment du lot.

Points vérifiés :
- **Corrections de la revue du plan, toutes appliquées** :
  - route des locations `gestion/locations/statistiques` ;
  - faux services dédiés pour les erreurs : 429 `COMMENT_LIMIT` construit par `ApiError.FromHttpResponse` ;
  - `ShownErrors` vide vérifié dans les cas « succès » ;
  - drapeau statique `FiscalYearChanged` remis à faux dans le constructeur et dans `Dispose` ;
  - même collection pour les deux classes concernées ;
  - aucun accès aux membres privés.
- Les tests vérifient des URL exactes et des effets visibles : bornes 2000 et exercice en cours sans requête, période contenant
  aujourd'hui construite depuis `DateTime.Today`, corps JSON avec et sans `fixedCost`, case « frais fixe » gardée quand la catégorie
  change après un choix du gérant.
- Rien de fragile : pas de `Task.Delay`, de `Thread.Sleep`, de réflexion ni de test ignoré.

À noter pour les lots suivants : la référence Android passe à **270 avertissements**. La suppression des anciens écrans de statistiques
(Chiffre d'affaires, Achats, Panier) a retiré 55 avertissements MVVMTK.
