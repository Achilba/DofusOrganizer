namespace DofusTeamOrganizer.Models;

public record PatchNote(string Version, string Date, string[] Lines);

public static class PatchNotes
{
    /// <summary>Version courante de l'appli (à incrémenter à chaque compilation livrée).</summary>
    public const string Current = "0.4.0";

    /// <summary>Du plus récent au plus ancien.</summary>
    public static readonly PatchNote[] All =
    {
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
