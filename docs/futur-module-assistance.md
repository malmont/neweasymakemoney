# Module « Assistance » (à venir)

Décision du propriétaire du 07/11/2026, transmise par la session « posflutter backend ». **À construire plus tard :
rien à coder maintenant, rien à entamer avant le signal du propriétaire.** Le cadrage complet sera dans
`docs/assistance.md` du backend app_v2 (branche `dev`), à lire au démarrage. Les routes exactes viendront avec le
lot 1 du backend. Concerne l'application de gestion MAUI et son futur clone web. Essais sur le site **demo**
uniquement.

## 1. Ce que fera le module (lots du backend, dans l'ordre)

1. **Boîte de demandes unique (tickets).** Chaque ticket est rattaché à une commande, un client, une vente de caisse
   ou un site. Les demandes arrivent de trois sources :
   - le formulaire du site boutique ;
   - le courriel entrant, avec une adresse par site ;
   - le bouton « Signaler un bug » de l'application Caisse, qui joint automatiquement le contexte technique.
2. **IA assistante du répondeur humain.** Elle résume le ticket, lit la commande et propose une réponse. Elle peut
   aussi proposer une action, que l'humain confirme : rembourser, renvoyer un reçu, enregistrer un retour. Ces
   actions passent par des routes qui existent déjà côté backend.
3. **Chat client autonome et borné.** Il répond sur le statut de commande, la politique de retour et les horaires,
   et passe la main à un humain au moindre doute.

## 2. Périmètre de l'application de gestion : onglet « Assistance »

- **Liste des tickets**, avec des filtres par état, canal et site.
- **Fiche d'un ticket**, avec la commande ou la vente liée.
- **Zone de réponse par courriel**, préremplie avec la proposition de l'IA.
- **Boutons d'action** qui appellent les routes du backend, par exemple le remboursement : l'équivalent API de
  `POST /admin/orders/{id}/rembourser` est à venir.
- **Historique** du ticket, **prise en charge** par un agent et **fermeture**.

Les autres applications ont leur propre part :
- **Caisse** (posflutter) : un écran « Signaler un problème » qui envoie le contexte technique.
- **Site Next.js** : un formulaire « Un problème avec ma commande ».

## 3. Ce que fournira le backend

- Une entité ticket et ses messages.
- Les routes `POST` et `GET /api/support/tickets`.
- La réception des courriels.
- Les outils d'IA : `summarize_ticket`, `draft_reply`, et les actions confirmées par l'humain.
- Les notifications.

## 4. Dépendances côté application de gestion

- Lots 1 (site) et 2 (session administrateur) terminés.
- Lot 3 : lecture des erreurs à codes stables.
- Lecture des commandes de vente (`GET /api/orders`), que MAUI ne fait pas aujourd'hui. Il faudra alors appliquer
  les règles F18 (taxes par taux) et lire les champs de livraison (`shipment`, suivi).
