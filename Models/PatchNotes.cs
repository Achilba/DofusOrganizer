namespace DofusTeamOrganizer.Models;

public record PatchNote(string Version, string Date, string[] Lines);

public static class PatchNotes
{
    /// <summary>Version courante de l'appli (à incrémenter à chaque compilation livrée).</summary>
    public const string Current = "0.8.3";

    /// <summary>Du plus récent au plus ancien.</summary>
    public static readonly PatchNote[] All =
    {
        new("0.8.3", "25/10/2026", new[]
        {
            "CORRIGÉ — Donjons : les noms de boss s'affichent en entier (retour à la ligne si besoin), la colonne Niveau est centrée et plus étroite, et la barre d'outils passe à la ligne au lieu d'être coupée quand la fenêtre est étroite.",
        }),
        new("0.8.2", "25/10/2026", new[]
        {
            "CHANGÉ — Donjons : la colonne « Chall 3 » est retirée (seuls les deux premiers challenges de chaque boss sont affichés) la colonne « Manque aussi chez » indique « Tous » quand il manque encore quelque chose à tous les autres personnages, et la colonne Niveau affiche seulement le niveau du donjon (relevé sur DofusDB pour les 144 boss).",
        }),
        new("0.8.1", "25/10/2026", new[]
        {
            "CHANGÉ — Donjons : la vue Liste est retirée, la page affiche directement les succès par boss et par personnage. Tes coches « Fait » de l'ancienne liste restent enregistrées mais ne sont plus affichées.",
        }),
        new("0.8.0", "25/10/2026", new[]
        {
            "NOUVEAU — Donjons, vue Succès : la liste des 144 boss est maintenant remplie, avec le nom des challenges de chaque boss affiché au-dessus de sa case (jusqu'à 3 challenges), le succès Duo (ou Trio / Quatuor selon le boss) et le succès Spécial. Relevé sur DofusDB : 122 succès Duo et 130 succès Spécial, conformes au total annoncé par la base.",
            "CHANGÉ — La vue Succès est maintenant organisée par boss (« Blops Royaux », « Gelées Royales »…) et non plus par donjon ; les lignes à ajouter à la main et la saisie des noms de challenges disparaissent. Les cases cochées en 0.7.x dans cette vue sont perdues (la vue Liste n'est pas touchée).",
                    }),
        new("0.7.1", "25/10/2026", new[]
        {
            "NOUVEAU — Donjons, vue Succès : une ligne par boss. Le bouton « + » à droite d'un donjon ajoute une ligne pour un autre boss (nom modifiable, avec ses 4 cases propres à chaque personnage) ; « × » la supprime avec ses coches.",
        }),
        new("0.7.0", "25/10/2026", new[]
        {
            "NOUVEAU — Donjons : une vue « Succès » (bouton « Vue : Liste / Succès » en haut de la page). Chaque donjon a 4 cases : Challenge 1, Challenge 2, Duo et Spécial. Les cases sont propres à chaque personnage (choix du personnage en haut) et une colonne indique chez quels autres personnages il manque encore quelque chose. Filtre « Manquants seulement ».",
            "Les noms des deux challenges (qui changent d'un donjon à l'autre) se saisissent une fois, au-dessus de la case, et valent pour tous les personnages. La vue Liste existante est inchangée.",
        }),
        new("0.6.1", "25/10/2026", new[]
        {
            "NOUVEAU — Almanax : le type de l'offrande est affiché à côté de la quantité (« Ressource · Os », « Équipement · Anneau », « Consommable · Pain »…), en couleur pour les équipements et les consommables. Source : DofusDB, vérifié sur les 366 offrandes (trois noms approchants sont à confirmer en jeu : Botte trouée, Incisive de Verglisseur, Œil de Fédorme).",
        }),
        new("0.6.0", "25/10/2026", new[]
        {
            "NOUVEAU — Recherche globale : Ctrl+K (ou « Rechercher » en haut de la barre latérale). Tape un nom pour trouver un personnage, un Dofus, un donjon, une légende, un Zaap (un clic le copie), une note ou une page ; ↑ ↓ pour choisir, Entrée pour ouvrir, Échap pour fermer. Les accents sont ignorés (« ebene » trouve « Ébène »).",
            "NOUVEAU — Thèmes de couleurs inspirés de DOFUS (Paramètres > Apparence) : Classique, Bonta, Brâkmar, Cawotte, Émeraude, Pourpre, Turquoise et Ébène. Le changement est immédiat et retenu au prochain lancement.",
            "NOUVEAU — Démarrage automatique : Paramètres > Démarrage, un interrupteur pour lancer l'appli avec Windows (elle s'ouvre comme à ta dernière fermeture).",
        }),
        new("0.5.1", "24/10/2026", new[]
        {
            "CORRIGÉ — Almanax : le rappel de l'accueil (et le point orange de la mini-barre) reste affiché tant que l'Almanax du jour n'est pas réalisé, même s'il est déjà préparé.",
        }),
        new("0.5.0", "23/10/2026", new[]
        {
            "NOUVEAU — Raccourcis modifiables (Paramètres > Raccourcis) : clique sur une touche puis appuie sur la nouvelle. Chaque raccourci est contrôlé (pas de doublon, pas de lettre seule qui gênerait le chat de DOFUS) et un bouton rétablit les valeurs par défaut.",
            "NOUVEAU — Dofus : trois statuts au clic, « À faire », puis « En cours » (orange), puis « Terminé » (vert), écrits sous le niveau. Les compteurs ne comptent que les Dofus terminés.",
        }),
        new("0.4.19", "23/10/2026", new[]
        {
            "AMÉLIORÉ — Barre latérale : « Paramètres » est en bas de la liste ; le bloc « Listes intégrées / Ctrl+Alt+O Masquer / version » est retiré (la version reste visible ici, dans les notes de version).",
        }),
        new("0.4.18", "23/10/2026", new[]
        {
            "NOUVEAU — Mini-barre : la position est mémorisée pour chaque configuration d'écrans (changement d'écran ou de résolution : la barre retrouve sa place).",
            "NOUVEAU — Donjons : filtre par niveau (1 à 50, 51 à 100, 101 à 150, 151 et plus).",
            "NOUVEAU — Paramètres : bouton « Copier ailleurs » pour copier tes données dans un autre dossier (autre disque, clé USB, cloud).",
        }),
        new("0.4.17", "23/10/2026", new[]
        {
            "AMÉLIORÉ — Mini-barre : la pastille de mise à jour est sur le bouton de retour au format normal (en barre compacte, qui n'a pas ce bouton, elle reste sur le triangle).",
        }),
        new("0.4.16", "23/10/2026", new[]
        {
            "AMÉLIORÉ — Mini-barre complète raccourcie (800 px) : la liste Zaap est plus courte pour que la barre tienne entre les éléments de l'interface DOFUS.",
            "AMÉLIORÉ — La fenêtre de la mini-barre a exactement la taille de la barre : l'aperçu pendant le déplacement correspond à sa taille (ombre portée retirée).",
        }),
        new("0.4.15", "23/10/2026", new[]
        {
            "AMÉLIORÉ — Mini-barre plus fine : 44 px de haut au lieu de 68, boutons de 32 px.",
            "CORRIGÉ — Majuscules aux noms propres : « Zaap : Cania, Sufokia… » dans le champ de recherche. Relecture de tous les textes de l'appli.",
        }),
        new("0.4.14", "23/10/2026", new[]
        {
            "NOUVEAU — Mini-barre : pastille orange sur le bouton de gauche quand une mise à jour est disponible.",
            "CORRIGÉ — Mini-barre : l'annonce de mise à jour n'affiche plus « COPIÉ ».",
        }),
        new("0.4.13", "23/10/2026", new[]
        {
            "AMÉLIORÉ — Mini-barre : le bouton compacte/complète est un triangle plein (◀ réduire, ▶ agrandir).",
            "AMÉLIORÉ — Mini-barre : les indications de touches (* et F12) ne sont plus affichées.",
        }),
        new("0.4.12", "23/10/2026", new[]
        {
            "CORRIGÉ — Personnages : le niveau (ex. 300) n'était plus affiché en entier, le champ est élargi.",
        }),
        new("0.4.11", "23/10/2026", new[]
        {
            "NOUVEAU — Nouvelle icône de l'appli (logo recyclage avec le Dofus Émeraude) pour le fichier .exe et la barre des tâches.",
        }),
        new("0.4.10", "23/10/2026", new[]
        {
            "NOUVEAU — Dofus : le niveau d'équipement est affiché sous chaque Dofus (données DofusDB).",
            "AMÉLIORÉ — Donjons : la bande « Annuler » après un clic est supprimée (il suffit de décocher).",
        }),
        new("0.4.9", "23/10/2026", new[]
        {
            "AMÉLIORÉ — Mini-barre compacte : la fenêtre s'ajuste exactement à son contenu, plus d'espace vide à droite.",
        }),
        new("0.4.8", "23/10/2026", new[]
        {
            "AMÉLIORÉ — Les listes et champs (personnage, classe, niveau, faction, ordre, métier, version, tri) sont ajustés à la taille du plus long mot qu'ils peuvent contenir. Les Zaaps ne changent pas.",
        }),
        new("0.4.7", "23/10/2026", new[]
        {
            "NOUVEAU — Icônes des classes dans Personnages (liste des personnages et choix de la classe). Steamer et Forgelance n'ont pas encore d'icône.",
            "CORRIGÉ — Fermer la fenêtre des notes de version ne réduit plus l'appli.",
            "AMÉLIORÉ — Dofus : phrase sur les icônes provisoires retirée.",
        }),
        new("0.4.6", "23/10/2026", new[]
        {
            "NOUVEAU — Icônes officielles de tous les Dofus (principaux et particuliers). DDE et DDE Cristallin gardent l'œuf coloré, car ils ne sont pas encore sortis.",
        }),
        new("0.4.5", "23/10/2026", new[]
        {
            "AMÉLIORÉ — Vérification des mises à jour toutes les 5 minutes (au lieu de 30) : la pastille « Paramètres » apparaît plus vite, sans relancer l'appli.",
        }),
        new("0.4.4", "23/10/2026", new[]
        {
            "AMÉLIORÉ — L'appli revérifie les mises à jour toutes les 30 minutes : la pastille orange apparaît sur « Paramètres » sans avoir à relancer.",
        }),
        new("0.4.3", "23/10/2026", new[]
        {
            "NOUVEAU — Icône officielle du Dofus Pourpre. Les autres icônes s'afficheront dès que leur fichier sera ajouté (dossier Assets).",
        }),
        new("0.4.2", "23/10/2026", new[]
        {
            "NOUVEAU — Pastille orange sur « Paramètres » quand une nouvelle version est disponible.",
            "AMÉLIORÉ — Personnages > Alignement : le niveau se règle avec une saisie et des flèches − / + (1 à 100), comme les compteurs.",
        }),
        new("0.4.1", "23/10/2026", new[]
        {
            "NOUVEAU — Onglet Almanax dans la barre latérale (entre Dofus et Donjons).",
        }),
        new("0.4.0", "23/10/2026", new[]
        {
            "NOUVEAU — Mises à jour automatiques : l'appli vérifie au lancement s'il existe une nouvelle version sur GitHub et l'installe en un clic (Paramètres > Mises à jour).",
            "NOUVEAU — Compilation automatique sur GitHub : plus besoin de compiler sur ton PC.",
        }),
        new("0.3.3", "23/10/2026", new[]
        {
            "CORRIGÉ — Personnages : la liste affichait « DofusTeamOrganizer.Models… » au lieu du nom du personnage.",
            "AMÉLIORÉ — Stuffs : les champs indiquent maintenant « Nom du stuff » et « Colle ton lien ici ».",
            "AMÉLIORÉ — Mini-barre : plus de pastille verte, bouton compact à gauche à côté de la poignée, la barre Zaap occupe tout l'espace libre.",
            "AMÉLIORÉ — Almanax : calendrier aligné sur les jours de la semaine, « À préparer » sur les jours à venir.",
            "AMÉLIORÉ — Dofus : phrase retirée sous le titre.",
        }),
        new("0.3.2", "23/10/2026", new[]
        {
            "CORRIGÉ — Erreur de compilation de la 0.3.1 (page Almanax, bouton « Préparer la ressource »).",
        }),
        new("0.3.1", "23/10/2026", new[]
        {
            "CORRIGÉ — Liste des métiers mise à jour pour DOFUS 3 : Boucher, Boulanger et Poissonnier n'existent plus (Pêcheur, Chasseur, Paysan… conservés).",
            "AMÉLIORÉ — Affichage recalé sur la maquette en ligne : Accueil (cartes, raccourcis, touche *, favoris), Personnages (liste déroulante du personnage, alignement sur une ligne, filtres en pastilles), Dofus (grille de 4, pastille ✓), Almanax (boutons rapides, légende, calendrier), Donjons et Songes en listes, Notes, Paramètres, mini-barre (touche *, Invitations, Almanax, mode compact).",
            "AMÉLIORÉ — Couleurs des boutons (principal orange, Valider vert, Tout préparé orange), curseur d'opacité et interrupteurs comme la maquette.",
            "NOUVEAU — Bannière d'alerte avec « Réessayer » si l'enregistrement échoue, mémoire utilisée dans Paramètres > Diagnostic.",
            "PAS ENCORE — Raccourcis modifiables, thème clair / taille du texte, son des alertes, contraste renforcé, vérification en ligne des listes, icônes officielles.",
        }),
        new("0.3.0", "23/10/2026", new[]
        {
            "NOUVEAU — Refonte de l'affichage pour correspondre à la maquette : barre latérale avec icônes, cartes, pastilles d'état, touches en « keycaps ».",
            "NOUVEAU — Tous les textes sont en gras (polices Manrope et Bricolage intégrées à l'appli).",
            "NOUVEAU — Accueil : 3 cartes personnages, raccourcis, invitations & Zaaps favoris (★), bouton Mode Mini.",
            "NOUVEAU — Dofus : œufs colorés, compteur et barre de progression, section « spéciaux » repliable.",
            "NOUVEAU — Almanax : cartes de progression semaine / année, légende, calendrier avec jours de la semaine.",
            "NOUVEAU — Notes : onglets, recherche dans toutes les notes, panneau de tâches. Paramètres : interrupteurs, opacité de la mini-barre, ordre des personnages (↑ ↓), diagnostic.",
            "NOUVEAU — Barre de titre sombre (Windows 10/11) et bulle « Annuler » en bas de la fenêtre.",
            "PAS ENCORE — Icônes officielles Dofus (il faut me fournir les PNG), thème clair, raccourcis modifiables, mises à jour automatiques.",
        }),
        new("0.2.1", "23/10/2026", new[]
        {
            "CORRIGÉ — Deux erreurs de compilation de la version 0.2.0 (aucun changement visible).",
        }),
        new("0.2.0", "23/10/2026", new[]
        {
            "NOUVEAU — Page Personnages : détails (classe, niveau, Oméga), alignement, métiers avec +/− maintenu, stuffs (liens, tags PvM/PvP/Songes, version du jeu), bouton « Copier le résumé ».",
            "NOUVEAU — Page Almanax : semaine et calendrier mensuel, offrande du jour (clic = copie), quête, modificateur du jour (2026), préparer / valider / annuler.",
            "NOUVEAU — Page Donjons : 140 donjons, recherche, tri par niveau ou nom, masquer les terminés, annulation après coche.",
            "NOUVEAU — Page Songes : bouclier, coffret, légendes animales et classiques avec compteurs.",
            "NOUVEAU — Accueil : bandeau « Almanax du jour ».",
            "NOUVEAU — Notes de version affichées à chaque mise à jour (Paramètres > Notes de version pour les relire).",
            "CORRIGÉ — F12 ne fonctionnait pas (Windows réserve cette touche) : nouvelle méthode de capture, active uniquement quand DOFUS est au premier plan.",
            "AMÉLIORÉ — Listes déroulantes et fenêtres au thème sombre de la maquette.",
            "PAS ENCORE — Vérification des listes en ligne au lancement, icônes officielles, thème clair, raccourcis modifiables, raccourci bureau / lancement avec DOFUS."
        }),
        new("0.1.0", "23/10/2026", new[]
        {
            "Première version : Accueil (personnages, invitations, Zaaps), Dofus, Notes, Paramètres (sauvegardes, export / import), mini-barre, Ctrl+Alt+O.",
        })
    };
}
