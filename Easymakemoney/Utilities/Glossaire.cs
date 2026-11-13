namespace Easymakemoney.Utilities
{
    /// <summary>
    /// Lexique unique de l'application : chaque terme technique affiché en lien (UiTerm, UiAmountRow.Term) y trouve sa définition.
    /// Les définitions sont écrites pour un gérant, sans jargon comptable, avec un exemple quand c'est utile.
    /// </summary>
    public static class Glossaire
    {
        public sealed record Entry(string Title, string Definition);

        private static readonly Dictionary<string, Entry> Entries = new(StringComparer.OrdinalIgnoreCase)
        {
            // Résultat et marge
            ["ventes"] = new("Ventes", "Ce que vous avez vendu, hors taxes, remises déduites et retours retirés. Les locations et les abonnements sont comptés à part."),
            ["ventes-sans-cout"] = new("Ventes sans coût connu", "Des ventes d'articles dont on ne connaît pas le prix d'achat. Elles restent dans les ventes mais sont exclues de la marge, faute de pouvoir la calculer. Rattachez ces articles à une commande pour les compter."),
            ["cout-articles-vendus"] = new("Coût des articles vendus", "Ce que vous avaient coûté à l'achat les articles vendus pendant la période : prix d'achat moyen de chaque article × quantité vendue."),
            ["marge"] = new("Marge brute", "Ventes moins le coût d'achat des articles vendus. Exemple : un article acheté 20 $ et vendu 50 $ donne 30 $ de marge."),
            ["taux-de-marque"] = new("Taux de marque", "La part de marge dans le prix de vente. À 40 %, sur 100 $ vendus, il vous reste 40 $ après le coût d'achat."),
            ["coefficient"] = new("Coefficient", "Prix de vente ÷ prix d'achat. Un article acheté 20 $ avec un coefficient de 2,5 se vend 50 $. Plus le coefficient est haut, plus la marge est grande."),
            ["frais"] = new("Frais de la période", "Ce que la période vous coûte en plus des articles : voyage, frais de port, publicité et autres notes de frais."),
            ["resultat"] = new("Résultat", "Marge brute moins frais : ce que la période vous rapporte réellement. Un résultat négatif est une perte. Les achats de stock n'en font pas partie : ils deviennent du stock."),
            ["point-mort"] = new("Point mort", "Le montant de ventes à atteindre pour ne pas perdre d'argent : frais ÷ taux de marque. Au-dessus vous gagnez, en dessous vous perdez."),
            ["investi-stock"] = new("Investi en stock", "Le total des achats de marchandises de la période (commandes fournisseurs). Ce n'est pas une dépense du résultat : c'est du stock qui sera vendu."),
            ["autres-revenus"] = new("Locations et abonnements", "Revenus des locations et des abonnements, comptés à part du résultat de la période d'articles."),

            // Budget et périodes
            ["periode"] = new("Période", "Une période de vente, créée automatiquement : un trimestre par défaut (4 par an), ou un semestre ou un mois selon le réglage du site. Chaque dépense compte dans la période de sa date. Une période fermée est en lecture seule."),
            ["budget-global"] = new("Budget global", "Le montant total que vous vous autorisez à dépenser sur la période, tous postes confondus."),
            ["prevu"] = new("Prévu", "Le montant que vous prévoyez de dépenser sur un poste (stock, voyage, port, publicité, autres) pendant la période."),
            ["reel"] = new("Dépensé (réel)", "Ce qui est déjà dépensé sur le poste, d'après vos commandes, notes de frais, frais de port et campagnes."),
            ["reste"] = new("Reste", "Prévu moins dépensé : ce qu'il vous reste à dépenser sur le poste sans dépasser le prévu."),

            // Trésorerie
            ["solde"] = new("Solde saisi", "Le solde de votre compte en banque à la fin de la journée, que vous saisissez vous-même. L'application n'est pas reliée à la banque."),
            ["point-bas"] = new("Point bas", "Le solde le plus bas que votre compte devrait atteindre sur la durée indiquée, d'après vos mouvements prévus et vos dépenses saisies."),
            ["seuil"] = new("Seuil d'alerte", "Le montant sous lequel vous voulez être prévenu. À 0, vous êtes prévenu seulement si le compte risque de passer à découvert."),
            ["ventes-a-venir"] = new("Ventes à venir", "Si c'est activé, la projection ajoute vos encaissements : les ventes réelles depuis votre dernier solde, puis la moyenne encaissée par jour sur les 30 derniers jours. Éteint, la projection ne compte que ce qui est sûr."),
            ["rapprochement"] = new("Rapprochement bancaire", "Comparer le solde réel de votre compte à celui que l'application attendait (dernier solde, dépenses saisies, mouvements prévus, encaissements). Un écart révèle souvent une dépense oubliée : retrouvez-la, ou enregistrez l'écart comme non justifié ou expliqué."),
            ["frais-fixes"] = new("Frais fixes", "Les dépenses qui reviennent chaque mois quel que soit le niveau des ventes : loyer, salaires, assurances, abonnements, électricité. Ils comptent dans le résultat au prorata des jours de la période. Un remboursement de prêt n'en fait pas partie par défaut : c'est du capital, pas une charge. Ne les saisissez pas aussi en note de frais, sinon ils comptent deux fois."),
            ["resultat-apres-frais-fixes"] = new("Résultat après frais fixes", "Le résultat de la période moins sa part de frais fixes (loyer, salaires, assurances…), comptée au prorata des jours. C'est ce que l'activité vous rapporte une fois l'entreprise payée."),
            ["point-mort-apres-frais-fixes"] = new("Point mort après frais fixes", "Les ventes à atteindre pour couvrir les frais de la période et sa part de frais fixes : (frais + frais fixes) ÷ taux de marque."),
            ["exercice"] = new("Exercice comptable", "Les 12 mois sur lesquels vous faites vos comptes : de janvier à décembre, ou par exemple d'avril à mars. Il se règle dans Gestion › Exercice comptable."),
            ["resultat-entreprise"] = new("Résultat de l'entreprise", "Marge + locations et abonnements − frais variables (port, publicité, notes de frais, voyages) − frais fixes (loyer, salaires…). C'est ce que l'entreprise gagne ou perd vraiment sur l'exercice."),
            ["frais-variables"] = new("Frais variables", "Les frais qui suivent l'activité : frais de port des achats, publicité, notes de frais et voyages, chacun à sa date. Les achats de stock n'en font pas partie : ils comptent dans le coût des articles vendus."),
            ["projection"] = new("Fin d'exercice estimée", "Le réalisé, plus une estimation du reste : les mêmes mois de l'exercice précédent s'ils existent, sinon le rythme de vente des 30 derniers jours. Les frais viennent de vos budgets de période, les frais fixes sont connus d'avance."),
            ["leviers"] = new("Leviers", "Des actions chiffrées par l'application sur vos propres chiffres : ce qu'elles rapporteraient d'ici la fin de l'exercice. « Comble l'écart » : à elle seule, l'action suffirait à atteindre le point mort."),
            ["mouvement-prevu"] = new("Mouvement prévu", "Une entrée ou une sortie d'argent connue à l'avance : loyer, salaires, prêt, taxes… Vos commandes et notes de frais sont déjà comptées : ne les ajoutez pas ici."),
            ["decouvert"] = new("Découvert", "Un solde négatif : le compte est passé sous zéro."),

            // Voyage
            ["budget-voyage"] = new("Budget du voyage", "Ce que vous prévoyez de dépenser en frais pendant le voyage : déplacement, hôtel, repas. Les achats de marchandises restent dans le budget « Stock »."),
            ["budget-a-ce-jour"] = new("Budget prévu à ce jour", "Budget par jour × jours déjà écoulés : ce que vous auriez dû dépenser au plus jusqu'à aujourd'hui pour tenir le budget."),

            // Prix et promotions
            ["arrondi"] = new("Arrondi", "Ajuste le nouveau prix pour qu'il « sonne » bien : finir en ,99, aux 0,50 ou à l'unité. Pour une promotion, l'arrondi se fait vers le bas, pour ne jamais annoncer moins de remise."),
            ["sous-le-cout"] = new("Sous le coût", "Le nouveau prix serait inférieur au prix d'achat : chaque vente ferait perdre de l'argent. Il faut l'autoriser explicitement."),
            ["promotion"] = new("Promotion datée", "Un prix réduit pendant des dates choisies, en boutique et en caisse. Le prix normal n'est jamais modifié et revient tout seul à la fin."),
            ["promotion-variante"] = new("Promotion par variante", "Une remise datée sur une taille ou une couleur précise d'un article (par exemple le XL noir à −20 %), même si la variante a son propre prix. Pendant ses dates, elle remplace la promotion de l'article, sans cumul. Le prix barré reste le prix normal ; le prix normal revient tout seul à la fin."),
            ["variante-prix-propre"] = new("Variante à prix propre", "Une taille ou une couleur qui a son propre prix (par exemple un XL plus cher). Les promotions ne s'y appliquent pas pour l'instant."),

            // Guide
            ["confiance"] = new("Niveau de confiance", "Élevée : le chiffre est constaté. Moyenne : il est estimé sur des données solides. Prudente : c'est une estimation à vérifier."),
            ["effet"] = new("Effet estimé", "L'argent que le conseil peut rapporter ou libérer, calculé par l'application à partir de vos chiffres. L'IA ne fait que le rédiger."),
            ["mesure"] = new("Mesure", "Après un conseil appliqué, le guide compare les 14 jours suivants aux 14 jours précédents pour voir ce qu'il a vraiment rapporté."),

            // Statistiques et publicité
            ["panier-moyen"] = new("Panier moyen", "Le montant moyen d'une commande, hors taxes : ventes avant retours ÷ nombre de commandes. Un retour ne fait pas baisser le panier du jour où il arrive."),
            ["valeur-stock"] = new("Valeur du stock", "Le stock en magasin valorisé au prix de vente affiché : quantité × prix de chaque variante. La comparaison dans le temps vient d'une photo du stock prise chaque jour à 10 h."),
            ["commandes"] = new("Commandes", "Le nombre de ventes contenant au moins un article, en boutique en ligne, en caisse et dans l'application. Les commandes annulées ou incomplètes ne comptent pas."),
            ["taux-retour"] = new("Taux de retour", "Le montant des articles retournés ÷ les ventes avant retours. À 5 %, sur 100 $ vendus, 5 $ ont été rendus."),
            ["evolution"] = new("Évolution", "La variation par rapport à la plage précédente (par exemple le mois dernier, aux mêmes jours) et à la même plage l'an dernier. Pour un taux, l'écart est en points."),
            ["gain-marge"] = new("Gain en marge", "La marge gagnée pendant la campagne par rapport à avant, pour chaque dollar dépensé en publicité. Au-dessus de 1 $, la campagne rapporte plus qu'elle ne coûte."),
            ["grille-location"] = new("Grille de tarifs", "Les tarifs de location (à l'heure, à la demi-journée, à la journée, à la semaine, au mois) rattachés à des catégories : la changer change le prix de tous les articles louables de ces catégories. Les réservations déjà faites gardent leur prix. Pour un seul article, une promotion de location est plus adaptée."),
            ["promotion-location"] = new("Promotion de location", "Une remise sur le tarif de location, pendant des dates et des jours de la semaine choisis. Elle compte selon le jour où la location commence, en boutique et en caisse. La caution et le supplément passagers ne sont jamais remisés, et une réservation déjà faite garde son prix."),
            ["revenu-location"] = new("Revenu des locations", "Ce que les locations ont rapporté, compté à la date de la vente, comme dans le résultat de période. Une réservation annulée mais non remboursée reste comptée : l'argent est gardé."),
            ["revenu-date-location"] = new("Revenu à la date de location", "Le même revenu, réparti sur les jours où l'article est réellement loué : une location du 30 au 2 compte pour moitié dans chaque mois."),
            ["occupation"] = new("Taux d'occupation", "Le temps où un article est loué, rapporté au temps où il pourrait l'être. À 80 %, l'article est loué 8 heures sur 10 d'ouverture."),
            ["recurrent"] = new("Revenu mensuel récurrent", "Ce que vos abonnements actifs rapportent chaque mois, ramené au mois quelle que soit leur fréquence."),
            ["taux-depart"] = new("Taux de départ", "La part des abonnés qui arrêtent leur abonnement chaque mois."),
        };

        public static Entry? Find(string? key) =>
            key != null && Entries.TryGetValue(key, out var entry) ? entry : null;

        public static IReadOnlyCollection<string> Keys => Entries.Keys;
    }
}
