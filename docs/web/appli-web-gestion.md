# Appli de gestion web : vision, IA et apprentissage (décisions du 11/11/2026)

Destinataires : la session « appli de gestion web », qui construit l'interface, et la session « appli degestion backend », qui écrit les
routes. Rédigé par Claude (session « new easymakemoney », application mobile), sur décisions du propriétaire. À lire avec
`docs/web/dossier-fin-exercice.md`.

## 1. Décisions du propriétaire

1. **Module de gestion payant et activable**, comme les autres modules de la plateforme, **pour le web et pour le mobile**. Clé dans
   `/api/site/features` ; sans le module : 403 `MODULE_INACTIVE`. La transition des sites existants est à proposer par le backend
   (l'application mobile appelle déjà `/api/gestion` pour tous les sites) et à valider avant le code.
2. **L'utilisation de l'IA est cadrée comme dans les autres modules** : elle consomme les **unités IA mensuelles** du site (le même
   système d'unités par mois que les autres projets). Cela couvre :
   - le guide de la période, l'avis du simulateur et de la préparation, l'avis de l'exercice ;
   - la console IA ;
   - la lecture des relevés PDF ;
   - l'assistant comptable.
   Quand les unités sont épuisées, la règle des autres modules s'applique. Ce qui fonctionne sans IA (calculs, textes des règles) reste
   disponible.
3. **L'appli web est un clone complet de l'appli mobile, en plus complet**, avec des modules impossibles sur mobile. C'est un espace
   d'administration **à plusieurs modules activables** : gestion, **support boutique**, et d'autres à venir. Elle se construit en
   reprenant ce que les applications web actuelles font déjà bien : connexion, multi-sites, modules, navigation.
4. Français et anglais ; pas d'espagnol pour l'instant (un site en espagnol affiche l'anglais).
5. **L'IA est au cœur de l'appli web** : console IA, assistant comptable, apprentissage du chef d'entreprise (§ 2 à 4).

## 2. La console IA : un copilote sur tous les écrans

- **Panneau latéral**, ouvert d'un clic sur tous les écrans et dans tous les modules. Il connaît le module et l'écran en cours :
  « Explique-moi ce tableau » parle de l'écran affiché.
- **Questions en langage courant sur les chiffres du site**, par exemple :
  - « Pourquoi ma marge a baissé en octobre ? »
  - « Est-ce que je peux me payer 2 000 $ ce mois-ci ? »
  - « Quel article me fait perdre de l'argent ? »
- **Réponses sourcées** :
  - chaque chiffre vient d'un calcul du backend (routes de gestion), jamais d'un texte de l'IA ;
  - mêmes règles que le guide IA : seuls les nombres fournis sont admis, sinon texte de repli ;
  - chaque chiffre a un lien vers l'écran d'où il vient.
- **Actions proposées, jamais faites en douce** : « Je peux créer ce frais fixe, voici l'aperçu » ; le gérant valide ; c'est annulable.
  Même schéma que partout ailleurs : aperçu, empreinte, application, journal, annulation.
- **Historique** des échanges par site ; **consommation d'unités IA** affichée (restantes ce mois-ci).
- **Garde-fou** : pour une décision fiscale ou légale, elle renvoie vers le comptable.

## 3. L'assistant comptable

- **Routine du mois, en 20 minutes guidées** :
  1. importer les relevés ;
  2. rapprocher ;
  3. classer les dépenses ;
  4. retrouver les justificatifs manquants ;
  5. clôturer le mois.
  À chaque étape, l'assistant explique pourquoi c'est utile.
- **Rappels** : échéances de taxes (TPS, TVQ, TVH, sales tax américaine), relevé manquant, écart bancaire non expliqué, justificatif
  absent.
- **« Préparer mon rendez-vous comptable »** : l'assistant rassemble le dossier (`dossier-fin-exercice.md`) et rédige les questions à
  poser au comptable, par exemple sur une dépense difficile à classer.
- **Limite** : il prépare, il ne remplace pas le comptable ; seul le comptable valide le bilan.

## 4. Apprendre en utilisant l'appli

La plupart des entrepreneurs n'ont pas de formation en gestion : l'appli doit les faire progresser.

1. **Chaque terme technique est un lien vers sa définition**, et la définition est **illustrée avec les chiffres du site**. Par exemple :
   « Votre taux de marque ce mois-ci : 44 %. Sur 100 $ vendus, il vous reste 44 $ après le prix d'achat. »
2. **« D'où vient ce chiffre ? »** sur chaque montant : le calcul détaillé avec les valeurs du site.
3. **Mini-leçons d'une minute**, montrées la première fois qu'une notion compte pour le gérant. Par exemple, le point mort le jour où il
   passe dessous.
4. **Parcours de compétences**, sans gadget :
   - les notions clés : marge, taux de marque, point mort, trésorerie, frais fixes et variables, taxes, stock dormant, rapprochement,
     exercice, résultat ;
   - pour chacune, un état : « vue », « comprise » (une question simple réussie) ou « utilisée » (une décision prise avec elle) ;
   - le gérant voit sa progression.
5. **Deux niveaux d'affichage** : « essentiel » au début, « détaillé » à mesure que le gérant progresse, ou quand il le choisit.
6. **Bilan pédagogique du mois** : ce que le mois a appris au gérant (3 points) et une notion à découvrir ensuite.
7. **Mode « apprendre » de la console** : « Explique-moi le fonds de roulement » donne une explication avec les données du site, et une
   petite question de vérification si le gérant le souhaite.
8. **Le simulateur comme outil d'apprentissage** : « Et si je baissais mes prix de 10 % ? » montre l'effet sur la marge et le point mort.

L'application mobile reprendra ce qui s'y prête : liens illustrés, « d'où vient ce chiffre », mini-leçons.

## 5. Lots

| Lot | Contenu | Côté |
|---|---|---|
| MOD | Module de gestion payant et activable (web et mobile), transition des sites existants ; unités IA mensuelles pour toutes les fonctions IA de la gestion | backend d'abord ; écran « module non activé » et unités restantes dans les applications |
| LEX | Lexique servi par le backend (`GET /api/gestion/lexique?locale=fr|en`), mêmes clés que l'application mobile | backend |
| W0 | Cadre à modules : registre des modules, connexion et multi-sites réutilisés, menu, en-tête (site, exercice), place de la console IA, fr/en, erreurs, composants communs | web |
| W1 | Reproduction des écrans de l'application mobile, avec ce que le bureau fait mieux (tableaux larges, saisie en masse, glisser-déposer, exports) | web |
| W2 | Console IA : questions et explications, chiffres sourcés, unités affichées | backend (route de la console à chiffres vérifiés) et web |
| W3 | Console IA : actions proposées, avec aperçu, validation et annulation | backend et web |
| W4 | Assistant comptable : routine du mois, rappels, rendez-vous comptable | backend et web |
| W5 | Apprentissage : définitions illustrées, « d'où vient ce chiffre », mini-leçons, parcours de compétences (suivi par utilisateur côté backend), deux niveaux d'affichage, bilan du mois | backend et web, puis mobile |
| ensuite | Dossier de fin d'exercice, import des relevés (IA3), connexion bancaire (IA4) | voir `dossier-fin-exercice.md` |

Règles communes : contrat backend avant tout code, plan relu avant de coder, tout additif, aucune régression des autres projets, essais
sur demo uniquement.
