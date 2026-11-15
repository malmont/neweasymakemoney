# Contrat L2-b : étiquettes des pièces, assistant d'installation, gamme de matériel

Reçu le 15/11/2026 de « prevention des pertes backend ». Pas encore en ligne. Détail : docs/prevention-pertes.md § 13.10 du dépôt backend. Aucune migration.

1. Mêmes droits que L2-a. Chaque route a son jumeau d'exercice sous `/api/gestion/pertes/exercices/…`, ouvert sans le droit du module.
2. `POST /pieces/etiquettes/pdf { pieceIds, format? (a4-50x30|roll-50x30), showPrice? }` rend un PDF, avec les en-têtes X-Labels-Count et X-Labels-Format.
   - Une étiquette par pièce : le code-barres de la variante, le numéro de la pièce, et le prix si on le demande.
   - Au plus 200 étiquettes en rouleau, 500 en planche A4.
3. Refus des étiquettes :
   - 422 VALIDATION_FAILED, LABELS_LIMIT_EXCEEDED `{ max, requested }` ou PIECES_NOT_FOUND `{ pieceIds }` ;
   - 409 PIECE_RETIRED `{ pieceIds }` ou BARCODE_MISSING `{ pieceIds, variantIds }`.
4. `GET /installation` rend `{ steps [{ code, title, text, image, status (done|current|todo), verify { kind (data|none), label } }], progress { done, total }, complete, exercise, screen: "pertes.installation" }`.
   - Quatre étapes : hardware, test_label, test_tag, first_piece.
   - L'avancement se calcule d'après ce qui a été fait : rien à cocher, rien de stocké.
5. `POST /installation/etapes/{code}/verifier` rend `{ code, verified, hint }`, ou 404 STEP_NOT_FOUND. La route relit les données sans rien écrire.
6. `POST /installation/puce-test { identifier, technology (uhf|nfc) }` rend `{ ok, identifier, technology, refusal (TAG_INVALID|TAG_IS_BARCODE|null), hint, tag, installation }`.
   - La lecture d'essai prouve l'étape mais n'enregistre pas la puce.
7. `POST /installation/etiquette-test { format? }` rend le PDF d'une étiquette d'essai, dont le code réservé ne désigne aucun article.
8. `GET /materiel` rend `{ needs [{ code (read|print|attach), label, levels [{ level (pro|standard|diy), label, technology, what, allows, limits }] }], levels [{ code, label, summary, soldWithHardware }], note, screen }`.
   - Aucun prix. `soldWithHardware` vaut false pour la gamme bricoleur.
9. Jumeaux d'exercice : `/exercices/pieces/etiquettes/pdf`, `/exercices/installation…` (sur `puce-test`, `identifier` est facultatif et la puce est alors simulée), `/exercices/materiel`.
   - `DELETE /exercices/pieces` remet aussi l'assistant d'exercice à zéro : on le propose même quand il n'y a aucune pièce.
   - Codes d'écran : pertes.pieces et pertes.installation.
   - 39 exemples JSON dans tests/Fixtures/pertes/pieces/.
