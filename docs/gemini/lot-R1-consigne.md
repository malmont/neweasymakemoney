# Lot R1 : relecture à froid de l'application mobile de gestion (aucune modification)

Consigne de Claude du 12/11/2026. Lot de la file d'attente (`GEMINI.md` § 1 ter), ouvert par un message de la boîte après la fusion de
4q. **Ce lot ne modifie aucun fichier, ni de l'application ni des tests.** Pas de branche, pas de commit.

## Ce que tu produis

Un seul fichier : `docs/gemini/lot-R1-constats.md`. Pour chaque constat, une ligne de tableau :
`Gravité | Fichier:ligne | Ce qui se passe réellement | Quand cela arrive au gérant | Correction proposée (une phrase)`.

Gravité : **A** (perte ou écriture de données fausse, plantage, donnée d'un autre site ou d'un autre compte visible), **B** (écran faux,
erreur avalée, message trompeur ou en anglais, action en double possible), **C** (code mort, incohérence sans effet visible).

## Ce que tu cherches, dans `Easymakemoney/` (hors `Views/Statistiques` et `ViewModels/Statistiques` sauf `StatistiquesViewModel`)

1. `async void` hors des gestionnaires d'événements, tâches lancées sans `await` ni capture d'erreur, `.Result` ou `.Wait()`.
2. Accès à `Shell.Current`, `Application.Current`, `AppPage.Current` sans garde.
3. `catch` qui avale une erreur sans rien montrer au gérant alors qu'une écriture a échoué.
4. Double envoi possible : commande d'écriture sans garde `IsBusy` ou bouton non désactivé.
5. Montants : calcul en `double` ou `decimal` hors de `MoneyHelper` et `SiteCurrency`, arrondis maison, cents mal convertis.
6. Textes montrés au gérant en anglais ou techniques (`ex.Message` d'une exception non `ApiException`, « Failed to… »).
7. Données d'un site ou d'un compte qui pourraient survivre à un changement de site ou à une déconnexion.
8. Calculs métier faits dans l'application alors que la règle est « le serveur calcule » (sommes, marges, totaux, conversions).

Lis le code, ne suppose pas. Cite la ligne exacte. **Pas plus de 60 constats**, les plus graves d'abord. Si tu n'es pas sûr, dis-le dans
la colonne « Ce qui se passe réellement ».

## Ensuite

Accusé de réception, puis attends. Claude trie la liste et décide des corrections.
