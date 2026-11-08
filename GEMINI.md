# Consignes pour l'agent Gemini : application de gestion IIZIMANAGER (.NET MAUI)

Tu es l'agent développeur de ce dépôt. Une session Claude (« new easymakemoney ») joue le rôle de responsable
technique : elle valide tes plans avant que tu codes, puis relit ton code. Le propriétaire du projet fait passer les
messages entre vous deux et décide de tout. **Réponds et écris toujours en français.**

## 1. Le circuit, pour chaque lot

1. **Plan, sans code.** Tu lis la consigne du lot (`docs/gemini/lot-<n>-consigne.md`) et le code concerné, puis tu
   écris ton plan dans `docs/gemini/lot-<n>-plan.md` (modèle en §5). Tu ne modifies aucun autre fichier, tu
   n'installes rien, tu ne lances aucune compilation qui télécharge des paquets.
2. **Validation.** Claude écrit sa revue du plan dans `docs/gemini/lot-<n>-revue-plan.md` : « validé », « validé
   avec corrections » ou « à refaire ». Tu ne codes pas avant « validé » ou « validé avec corrections », et tu
   appliques ces corrections.
3. **Code.** Tu travailles sur la branche indiquée dans la consigne, tu suis le plan validé et rien d'autre. Si tu
   dois t'en écarter, tu t'arrêtes et tu l'écris dans le compte rendu au lieu de décider seul.
4. **Compte rendu.** Tu écris `docs/gemini/lot-<n>-compte-rendu.md` : fichiers modifiés, commandes lancées et
   résultat **réel** (copie des erreurs, pas de résumé optimiste), écarts au plan, points non faits.
5. **Revue du code.** Claude relit le diff et écrit `docs/gemini/lot-<n>-revue-code.md`. Tu corriges chaque point
   numéroté, puis tu complètes le compte rendu (section « Corrections après revue »).

Un seul lot à la fois. Ne commence jamais le lot suivant de toi-même.

## 2. Règles absolues

- **Ne lance jamais `git remote -v`, `git config --get remote.origin.url`, ni `cat .git/config`**, et n'affiche
  jamais l'adresse du remote : elle contient un jeton GitHub en clair.
- **Pas de `git commit`, pas de `git push`, pas de `git fetch`/`pull`**, pas de `git reset --hard`, `git clean`,
  `git checkout -- .` ni de suppression de fichiers non suivis. Le propriétaire commite lui-même, sur sa demande.
  Tu as le droit de créer la branche du lot (`git switch -c <branche>`) et de lancer `git status` et `git diff`.
- **N'installe rien toi-même** (SDK .NET, workloads, JDK, Xcode, outils Android, outils globaux). Écris dans ton plan
  les commandes exactes ; le propriétaire les lance ou t'autorise explicitement après validation.
- **Aucun appel au backend** (`curl`, lancement de l'application connectée, tests réseau) sans accord du propriétaire
  pour ce lot. Le backend n'a pas de préproduction : tout appel écrit dans des bases réelles. Les essais se font
  **uniquement sur le site `demo`** (`demo.arkanoa-media.com`, code court `DEM042`).
- **Aucune clé, aucun identifiant, aucun mot de passe, aucune clé d'activation, aucune URL de site** dans le code, les
  fichiers ou tes réponses. Ne saisis jamais les identifiants du propriétaire : il les tape lui-même.
- **Le backend ne se modifie jamais** depuis ce dépôt, et tu n'as pas à le lire : le contrat est dans `docs/`. Une
  question sur une route se pose dans ton plan ou ton compte rendu (section « Questions ») ; Claude la transmet.
- **Ne modifie pas** `README.md` ni les documents de `docs/` en dehors de tes propres fichiers `docs/gemini/lot-*-plan.md`
  et `docs/gemini/lot-*-compte-rendu.md`. Claude tient l'audit et l'état des lieux à jour.
- Ne change pas le comportement de l'application en dehors de ce que demande le lot (pas de « nettoyage » au passage,
  pas de renommage gratuit, pas de reformatage de fichiers entiers).

## 3. À lire avant le premier lot, dans cet ordre

1. `docs/etat-des-lieux-2026-11-05.md` : état de l'application, écarts avec le backend, plan en lots 0 à 4.
2. `docs/topo-demarrage.md` : contexte de la suite Izzipro et règles du propriétaire.
3. `docs/backend-contrat-gestion.md` : contrat backend propre à l'application de gestion.
4. `docs/strategie-multitenant-mobile.md` : modèle commun aux applications mobiles (site, clé d'activation, session).
5. `docs/audit-multitenant.md` : audit du 15/10/2026 (en partie dépassé : l'état des lieux fait foi).

## 4. Le projet en bref

- Solution `Easymakemoney1.sln`, projet unique `Easymakemoney/Easymakemoney.csproj` (.NET MAUI 8 aujourd'hui).
- Couches : `Services/` (appels HTTP, un service par liste ou écran, `HttpService` commun) → `UseCase/` →
  `ViewModels/` → `Views/` (XAML). Injection dans `MauiProgram.cs`. `GlobalUsings.cs` porte les `using` globaux.
- Aucun test automatique aujourd'hui.
- Plan des lots : 0 Plateforme (.NET 10), 1 Socle multi-tenant, 2 Connexion / OTP / session, 3 Routes et erreurs,
  4 Tests et recette. Détail : état des lieux, §6.

## 5. Modèle de plan (`docs/gemini/lot-<n>-plan.md`)

```
# Lot <n> : plan de Gemini

## Ce que j'ai lu
Fichiers et constats, avec chemin:ligne.

## Prérequis à installer ou à vérifier (commandes exactes, rien n'est lancé)

## Étapes
1. … (fichiers touchés, changement prévu, pourquoi)

## Comment je vérifie
Commandes de compilation ou de test, et le résultat attendu.

## Risques et points incertains

## Questions pour Claude ou le propriétaire
```
