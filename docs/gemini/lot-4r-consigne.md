# Lot 4r : tests du guide des séquences (IA-SEQ) dans l'application mobile de gestion

Consigne de Claude du 13/11/2026. Lot de la file d'attente (`GEMINI.md` § 1 ter), **en attente du backend** : ne commence le plan qu'après un
message de la boîte qui l'ouvre (quand les routes du guide des séquences sont en ligne et que Claude a écrit les écrans).

- Plan : `docs/gemini/lot-4r-plan.md`. Code sur `feature/lot-4r-tests`, depuis `main` à jour. Pas de commit. Référence : nombre réel de
  tests sur `main` ; Android **12 avertissements**.

## Ce que porte ce lot

Le guide des séquences (backend : `GET /api/gestion/sequences` et `GET /api/gestion/sequences/{key}?instance=`, fiche
`docs/sequences-gestion.md` du backend, lue avec `git show origin/dev:docs/sequences-gestion.md` dans le dépôt du backend s'il est
accessible, sinon par la copie que Claude mettra dans `docs/`). Claude écrit dans l'application : le modèle, le service, la page
« Où j'en suis », la page détail d'une séquence et la carte de l'Accueil. Toi : les tests.

Au plus **25 tests**, justifiés dans le plan :
1. **Modèle** : lecture de la liste et du détail tels que le backend les construit (séquence unique et récurrente, `next` absent, instances,
   chaque statut d'étape, `translated: false`, `figures`) ; **un champ absent reste absent** (jamais un statut ni un nombre inventé).
2. **Textes affichés** : avancement « n sur m » et pourcentage repris du serveur (aucun calcul local), « indispensable » ou « conseillé »,
   séquence « ne vous concerne pas ».
3. **Liens d'étape** : écran de l'application quand le code est connu, aucun lien sinon (jamais une page introuvable), écran du
   back-office annoncé sans lien.
4. **ViewModels** : chargement, erreur avec réessai, changement d'instance (période en cours puis suivante), refus `SEQUENCE_NOT_FOUND` et
   `INSTANCE_NOT_FOUND` avec le texte réellement montré, carte de l'Accueil cachée quand tout est prêt.

Mêmes règles que les lots 4o à 4q : lis le code, aucun fichier de l'application modifié (cas inatteignable → question du plan), aucun appel
au backend, aucune attente réelle, encodage et fins de ligne intacts, compte rendu écrit en dernier.
