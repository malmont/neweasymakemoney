# Contrat L3-a : boîtiers et tableau de santé du matériel (prévention des pertes)

Reçu le 15/11/2026 de « prevention des pertes backend ». Pas encore en ligne. Détail : docs/prevention-pertes.md § 16.13 du dépôt backend ; 38 exemples JSON dans tests/Fixtures/pertes/boitier/.

1. Routes de l'administrateur, sous /api/gestion/pertes :
   - `GET /boitiers` ;
   - `POST /boitiers { name, place? }` rend 201 avec la clé `IZ-BOX-…`, montrée une seule fois, et `enrolment { host, key, code }` ;
   - `GET|PUT /boitiers/{id}` ;
   - `POST /boitiers/{id}/cle { confirm: true }` crée une nouvelle clé et détache la machine d'avant ;
   - `POST /boitiers/{id}/revoquer { confirm: true }` révoque le boîtier, définitivement.

   Au plus 20 boîtiers non révoqués par site.
2. La fiche d'un boîtier donne :
   - son état (`pending|active|revoked`) et `online` ;
   - `silentSeconds`, sa version et l'écart de son horloge ;
   - les appareils branchés et leur état ;
   - ses 50 derniers faits.

   Aucune lecture ne rend la clé.
3. `GET /sante` rend `devices [{ code, kind, label, zone, cashRegisterId, state (ok|pending|down|absent), stateLabel, lastSeenAt, silentSince, silentSeconds, hint }]`, `summary { ok, pending, down, absent, allOk, label }` et `thresholdSeconds`.
   - `PUT /sante/seuil { silentAfterSeconds }` : de 180 à 3600 secondes, 300 par défaut.
4. `summary.label` vaut « Tout est en ordre », « N appareil(s) à vérifier » ou « Aucun appareil installé ». `allOk` n'est vrai que si au moins un appareil est installé et qu'aucun n'est à vérifier.
5. La réponse de `/installation` gagne `optionalSteps`. L'étape `box` porte `state` et `hint`, et n'est faite que si un boîtier inscrit donne signe de vie.
6. Exercice, ouvert sans le droit du module :
   - `GET|POST|DELETE /exercices/boitiers` et `GET /exercices/boitiers/{id}` ;
   - `POST /exercices/boitiers/{id}/scenarios { scenario }`, avec les scénarios signe de vie, coupure du réseau, lecteur débranché puis rebranché, contrôle au lecteur à main, lecture à la sortie ;
   - `GET /exercices/sante`.
7. Routes du boîtier, sous /api/pertes/boitier, avec l'en-tête `X-Loss-Box` : `POST /inscription`, `POST /jeton`, `POST /signe-de-vie`, `GET /reglages`, `POST /faits` (1 à 200 faits, rejouables).
8. Le jeton du boîtier vaut 24 h et n'ouvre que ces cinq routes. Aucune réponse faite au boîtier ne contient de nom.
9. Code d'écran : `pertes.boitiers`, avec son guide.

À relire par l'appli : relire `/sante` toutes les 5 s tant que l'écran est ouvert, et afficher une pastille par appareil.

Précisions du 15/11 :
- Il y a 45 exemples JSON : les fichiers `admin-…` servent aux écrans, les fichiers `canal-…` au logiciel du boîtier.
- IMPORTANT pour les écrans : relire `/sante` toutes les 5 s ne doit PAS rafraîchir le jeton de l'utilisateur. Sinon, une session laissée ouverte sur cet écran ne s'éteint jamais. Cela vaut pour le web comme pour le mobile.
