# Contrat L3-a2 : jumelage du boîtier par code (prévention des pertes)

Reçu le 15/11/2026. La bascule vient après l'étape 83. Détail : docs/prevention-pertes.md § 16.17 du dépôt backend. Les exemples sont dans tests/Fixtures/pertes/boitier/canal-jumelage-*.json et admin-boitier-jumele*.json.

- Administrateur : `POST /api/gestion/pertes/boitiers/jumeler { code, name, place? }` rend 201 `{ box }`, avec le statut pending tant que le boîtier attend sa machine. Le site est celui de la session.
  - Le code a la forme `KX7-42P-9QM` : trois groupes de trois caractères, lu par le QR code ou tapé. Côté appli : majuscules, tirets ajoutés.
  - 404 PAIRING_CODE_INVALID, toujours avec le même texte neutre : « Code inconnu, ou boîtier pas encore branché à Internet. Branchez le boîtier, attendez que son voyant clignote, puis réessayez. »
  - 422 fields ; 409 BOX_LIMIT_REACHED, et le code n'est pas consommé ; 429 RATE_LIMITED `{ retryAfterSeconds }`.
  - Si la réponse d'un jumelage réussi se perd, un second essai reçoit le refus neutre : il faut relire la liste des boîtiers.
- Chaque fiche de boîtier gagne `paired`. `keyHint` vaut null pour un boîtier jumelé. « Détacher » est la révocation existante.
- Côté boîtier, sans jeton : `POST /api/pertes/boitier/jumelage` et `POST /api/pertes/boitier/jumelage/etat`.
- Mobile, par moi : « Ajouter un boîtier » scanne le QR code avec l'appareil photo (ZXing, format QR), ou laisse taper le code, puis montre la pastille.
