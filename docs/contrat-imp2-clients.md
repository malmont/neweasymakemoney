# Contrat IMP-2 : import des clients Shopify, avec leur consentement

Reçu le 15/11/2026 de « appli degestion backend ». Pas encore en ligne : relecture indépendante, mutations, puis une migration d'une table sur les 8 bases. Transmettre à « appli de gestion web » (partie A) et à « boutique next » (partie B) quand le lot sera en ligne.

En attente des réponses du propriétaire :
- le texte de l'invitation ;
- la case des courriels commerciaux, précochée (proposition du backend) ou vide (ma recommandation) ;
- la reprise de « boutique next » pour la page du lien.

## A. Appli de gestion web (commerçant)

1. **Déposer** : `POST /api/site/imports`, en multipart, avec `file`, `source=shopify` et `kind=customers`. Sans `kind`, c'est le catalogue. La route rend 201.
   - 422 IMPORT_KIND_UNKNOWN, IMPORT_NOT_A_CUSTOMER_FILE, IMPORT_TOO_MANY_ROWS (5 000), IMPORT_NOT_UTF8, IMPORT_EMPTY_FILE ou IMPORT_FILE_TOO_BIG.
   - 409 IMPORT_ALREADY_ACTIVE.
2. **L'import** porte `kind: "catalog" | "customers"`.
   - `report.customers` : `{ total, importable, alreadyCustomer, alreadyImported, duplicates, refused, withAddress, marketingClaimed }`.
   - `report.issues` : `[{ line, code }]`, avec les codes NO_EMAIL, EMAIL_INVALID, DUPLICATE_IN_FILE, ALREADY_CUSTOMER, ALREADY_IMPORTED et ADDRESS_INCOMPLETE.
   - `eraseDays` vaut 90 et `linkDays` vaut 30.
   - `guests` : `{ imported, invited, accepted, kept, invitable }`.
   - `result` : `{ created, withAddress, marketingClaimed, skipped, failed, failures, invitations?: { sent, failed, lastRunAt }, cancel?: { erased, kept } }`.
   - `options` : `{ attestMarketingConsent, attestedBy, attestedAt }`.
   - Le rapport ne contient aucune adresse ni aucun nom.
3. **Confirmer** : `POST …/{id}/confirm { attestMarketingConsent? }` rend 202.
   - La case : « J'atteste avoir obtenu le consentement de ces clients à recevoir mes courriels commerciaux ». Sans elle, le consentement n'est repris pour personne.
   - 409 IMPORT_NOTHING_TO_IMPORT ou IMPORT_NOT_READY.
4. **Envoyer les invitations** : `POST …/{id}/invitations` rend 202 `{ importId, queued }`. Un seul courriel par invité, jamais de relance.
   - 409 IMPORT_NOT_READY, IMPORT_NOTHING_TO_INVITE ou IMPORT_INVITATION_LINK_UNKNOWN.
5. **Les clients repris** : `GET …/{id}/customers?page=&state=` rend `{ customers, page, pages, total }`, 50 par page.
   - Un client : `{ userId, email, firstname, lastname, state, marketingClaimed, importedAt, invitedAt, linkExpiresAt, eraseAt, acceptedAt }`.
   - États : imported (repris), invited (invitation envoyée), accepted (devenu client), kept (n'a pas accepté, mais une commande tient son compte).
6. **Annuler** : `POST …/{id}/cancel` rend 200 avec `result.cancel { erased, kept }`. 409 IMPORT_NOT_CANCELLABLE s'il ne reste aucun invité en attente.
   - `GET …/{id}/issues.csv` rend les lignes signalées (`line;code`).

## B. Boutique publique (page du lien)

7. **Le lien** : `https://<site>/invitation?token=…&locale=fr`, avec `&decline=1` pour « Ne pas rouvrir mon compte ». Les routes sont publiques, et le jeton passe dans le corps.
8. **Lire** : `POST /api/import-invitation/verify { token, locale? }` rend `{ firstname, email, site, expiresAt, marketing, terms { version, privacyPolicy, termsOfUse, legalNotice, … } }`.
9. **Accepter** : `POST /api/import-invitation/accept { token, password (8 caractères au moins), termsVersion, accepted: true, marketing? }` rend `{ accepted: true, email, marketing }`. Le client se connecte ensuite normalement.
   - La case des courriels commerciaux dépend de la décision du propriétaire.
   - 422 VALIDATION_FAILED (password REQUIRED, TOO_SHORT ou TOO_LONG) ou TERMS_NOT_ACCEPTED.
   - 409 TERMS_VERSION_CHANGED `{ terms }` : réafficher les conditions.
10. **Refuser** : `POST /api/import-invitation/decline { token }` rend `{ declined: true }` et efface tout ce qui a été repris.
    - Un lien inconnu, expiré ou déjà servi rend 404 IMPORT_INVITATION_INVALID.
    - Le débit est limité comme pour la connexion (429).

## Retouches après la relecture indépendante (étape 79)

- a. Le jeton passe dans le FRAGMENT de l'adresse : `https://<site>/invitation?locale=fr#token=…`, avec `&decline=1` placé avant le `#`. La page lit le jeton et l'envoie dans le corps de la requête.
- b. La page `decline=1` demande un clic avant d'appeler le refus. Un antivirus de messagerie qui ouvre les liens ne doit rien effacer.
- c. `accept` peut rendre 503 IMPORT_INVITATION_FAILED. Rien n'est écrit et le lien reste valable : la page affiche « réessayez ».
- d. Les invitations partent par lots de 300 par demande ; `guests.invitable` dit combien il en reste. « Envoyé » veut dire remis au serveur d'envoi du site.

Limites acceptées :
- en s'inscrivant avec l'adresse d'un invité, on efface sa fiche reprise ;
- la console de Mémoires Vivantes liste les invités ;
- l'anti-pourriel et la Loi 25 sont à faire vérifier par l'avocat.
