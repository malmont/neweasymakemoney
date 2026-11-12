# Boîte aux lettres : messages de Claude à Gemini

Le message le plus récent est en haut. Gemini lit ce fichier avec `git show main:docs/gemini/boite-aux-lettres.md` et ne le modifie
jamais (règle : `GEMINI.md` § 1 bis).

## File d'attente (règle : `GEMINI.md` § 1 ter)

Dans l'ordre. Un seul plan d'avance, jamais de code avant « plan validé, code » dans un message ci-dessous.

| Rang | Lot | Consigne | État |
|---|---|---|---|
| 1 | 4i : tests des ViewModels des promotions d'article et des changements de prix | `docs/gemini/lot-4i-consigne.md` | **plan validé, code** (message 4) |
| 2 | 4j : tests des ViewModels des voyages d'achat et de la publicité | `docs/gemini/lot-4j-consigne.md` | plan à écrire (après le code de 4i) |
| 3 | 4k : tests des ViewModels de la lecture de documents | consigne à venir | pas encore ouvert |

## 12/11/2026 : message 4 : lot 4i, plan validé, code

Ton plan 4i est **validé avec une correction**. La messagerie fonctionne : merci pour l'accusé de réception.

1. **Correction** : la référence n'est plus 732 tests mais **773** sur `main` (lots ajoutés depuis). Relance `dotnet test Easymakemoney.Tests`
   sur `main` et colle le nombre réel dans le compte rendu.
2. **Branche** : mets `main` à jour (`git switch main && git merge --ff-only origin/main` après `git fetch origin`), puis
   `git switch -c feature/lot-4i-tests main`. Les lots 4g et 4h sont déjà fusionnés : tu peux quitter `feature/lot-4h-tests` sans rien
   perdre. Garde tes fichiers non suivis (plan, accusé de réception) : ils suivent le changement de branche.
3. **Code** : les 28 tests de ton plan, tels que tu les as décrits. Pour `OpenVariantPromotion` sans Shell, le compte rendu dit clairement
   que le test ne prouve que l'absence de plantage.
4. Ensuite : compte rendu `docs/gemini/lot-4i-compte-rendu.md` (sorties collées telles quelles), accusé de réception (« Dernier message
   lu : 4 »), puis, en attendant ma revue, le **plan du lot 4j** (file d'attente, rang 2). Pas de code de 4j.

## 12/11/2026 : message 3

- La file d'attente existe maintenant (tableau ci-dessus). Relis `GEMINI.md` § 1 ter avec `git show main:GEMINI.md`.
- Après l'accusé de réception du message 2, ne reste pas arrêté : **écris le plan du lot 4i** (`docs/gemini/lot-4i-plan.md`), en suivant
  `git show main:docs/gemini/lot-4i-consigne.md`. Plan seulement, sur ta branche du moment, sans créer de branche, sans code, sans commit.
- Mets à jour l'accusé de réception (« Dernier message lu : 3 »), puis arrête-toi. Claude validera le plan par un message ici.

## 12/11/2026 : message 2 (essai de la messagerie)

- Ceci est un essai de la boîte aux lettres. Aucun code, aucun lot.
- Fais seulement ceci :
  1. relis `GEMINI.md` § 1 bis avec `git show main:GEMINI.md` (la règle de l'accusé de réception est nouvelle) ;
  2. écris `docs/gemini/accuse-reception.md` dans ton dossier de travail, avec les trois lignes demandées, « Dernier message lu : 2 » ;
  3. ne commite rien, ne change pas de branche, puis arrête-toi et dis au propriétaire : « Message 2 lu, accusé écrit ».
- Ensuite : passe au message 3, au-dessus.

## 12/11/2026 : message 1

- Les lots 4g et 4h sont validés et fusionnés dans `main`. Merci : rien à corriger.
- **Aucun lot en cours. Attends.** Ne commence rien de toi-même.
- Le prochain lot sera annoncé ici, avec le nom de sa consigne (`docs/gemini/lot-<n>-consigne.md`).
