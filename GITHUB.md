# Mise en ligne sur GitHub (une seule fois)

Dépôt : https://github.com/Achilba/DofusOrganizer

## 1. Envoyer le code
Dans le dossier du projet (celui qui contient DofusTeamOrganizer.csproj), ouvre un terminal :

    git init
    git add .
    git commit -m "Version 0.4.0"
    git branch -M main
    git remote add origin https://github.com/Achilba/DofusOrganizer.git
    git push -u origin main

## 2. Publier la version (GitHub compile pour toi)

    git tag v0.4.0
    git push origin v0.4.0

Onglet « Actions » du dépôt : la compilation prend 2 à 3 minutes.
Ensuite, onglet « Releases » : tu y trouves DofusTeamOrganizer.exe.

## 3. Les fois suivantes
Je te donne les fichiers modifiés (ou le zip). Tu remplaces les fichiers du dossier, puis :

    git add .
    git commit -m "Version 0.4.1"
    git push
    git tag v0.4.1
    git push origin v0.4.1

Les appli déjà installées proposent la mise à jour dans Paramètres > Mises à jour
(et affichent « Mise à jour disponible » au lancement).

## Dépôt privé ?
Le plus simple est un dépôt public (le code ne contient aucune donnée personnelle).
S'il reste privé : crée un jeton GitHub en lecture seule (Settings > Developer settings >
Fine-grained tokens > accès au dépôt, permission « Contents : Read ») et colle-le dans le fichier
%AppData%\DofusTeamOrganizer\update-token.txt
