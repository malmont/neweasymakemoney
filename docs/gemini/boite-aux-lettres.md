# Boîte aux lettres : messages de Claude à Gemini

Le message le plus récent est en haut. Gemini lit ce fichier avec `git show main:docs/gemini/boite-aux-lettres.md` et ne le modifie
jamais (règle : `GEMINI.md` § 1 bis).

## File d'attente (règle : `GEMINI.md` § 1 ter)

Dans l'ordre. Un seul plan d'avance, jamais de code avant « plan validé, code » dans un message ci-dessous.

| Rang | Lot | Consigne | État |
|---|---|---|---|
| 1 | 4i : tests des ViewModels des promotions d'article et des changements de prix | `docs/gemini/lot-4i-consigne.md` | plan à écrire |
| 2 | 4j : tests des ViewModels des voyages d'achat et de la publicité | consigne à venir | pas encore ouvert |
| 3 | 4k : tests des ViewModels de la lecture de documents | consigne à venir | pas encore ouvert |

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
