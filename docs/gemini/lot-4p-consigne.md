# Lot 4p : tests des ViewModels de gestion restants (stock bas, reprise, variantes, écran d'une période, listes)

Consigne de Claude du 12/11/2026. Lot de la file d'attente (`GEMINI.md` § 1 ter) : **plan** seulement après la fusion du lot 4o et un
message qui l'ouvre, **aucun code avant « plan validé, code »** dans la boîte aux lettres.

- Plan : `docs/gemini/lot-4p-plan.md`. Code sur `feature/lot-4p-tests`, depuis `main` à jour. Pas de commit. Référence : nombre réel de
  tests sur `main` ; Android **12 avertissements**.

## Ce que porte ce lot

Les ViewModels de gestion qui n'ont encore aucun test :
`StockLowViewModel`, `RepriseViewModel`, `ListNewProductsVariantViewModel`, `AllListsViewModel`, `DashBoardCollectionViewModel`
(l'écran d'une période).

Hors lot : les anciens écrans de statistiques (`ViewModels/Statistiques/` sauf `StatistiquesViewModel`, `YearStatisticsViewModel` et
ceux de location), qui seront remplacés.

Au plus **25 tests**, choisis et justifiés dans le plan, en commençant par ce qui écrit :
1. **Reprise d'historique** (`RepriseViewModel`) : simulation (aucune écriture), application avec confirmation (refusée = aucune requête),
   annulation avec confirmation, refus `REPRISE_ACTIVE` et `REPRISE_NOT_FOUND` avec le texte réellement montré, coût saisi à la main.
2. **Écran d'une période** (`DashBoardCollectionViewModel`) : clôture et réouverture avec confirmation, refus du serveur, chargement du
   résultat et du tableau de bord, période vide.
3. **Variantes** (`ListNewProductsVariantViewModel`) : création et suppression (confirmation, refus `PRODUCT_IN_USE` de la variante).
4. **Stock bas** et **toutes les listes** : chargement, état vide, erreur, navigation proposée.

Mêmes lectures préalables que 4o (tests et faux existants). Un cas inatteignable va dans les questions du plan ; **ne modifie jamais un
fichier de l'application.**

## Règles et vérification

Celles du lot 4o, mot pour mot. `[Collection("SiteCurrency")]` si un montant est formaté ; dates d'« aujourd'hui » depuis
`DateTime.Today`. Compte rendu `docs/gemini/lot-4p-compte-rendu.md` écrit en dernier, puis l'accusé de réception.
