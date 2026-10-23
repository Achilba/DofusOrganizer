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

## 2. Publier une version
Rien à faire de plus : à chaque envoi sur la branche main, GitHub compile la version écrite dans
DofusTeamOrganizer.csproj et la publie dans « Releases » (onglet Actions : 2 à 3 minutes).

## 3. Les fois suivantes
Remplace les fichiers du dossier, puis : git add . / git commit / git push.
Les appli déjà installées proposent la mise à jour dans Paramètres > Mises à jour.

## Dépôt privé ?
Le plus simple est un dépôt public (le code ne contient aucune donnée personnelle).
S'il reste privé : crée un jeton GitHub en lecture seule (Settings > Developer settings >
Fine-grained tokens > accès au dépôt, permission « Contents : Read ») et colle-le dans le fichier
%AppData%\DofusTeamOrganizer\update-token.txt
