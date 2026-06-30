---
name: init-repo
description: Initialise le dépôt Git du projet et le publie sur GitHub en une fois (init, commit initial, creation du repo, push main). Demande confirmation avant de creer le repo distant et de pousser.
---

# Skill : init-repo

Objectif : éviter de taper les 5 commandes d'initialisation à la main. Enchaîne tout, mais
**demande validation** avant l'action irréversible (créer le repo distant + pousser).

## Pré-requis
- GitHub CLI connectée : `gh auth status` (sinon **STOPPER** et proposer `gh auth login`).

## Étapes à suivre

1. **Vérifier l'état**
   - Si le dossier est **déjà** un dépôt git avec un remote (`git remote -v` non vide),
     **STOPPER** et l'indiquer (rien à initialiser).

2. **Demander les paramètres**
   - Nom du repo (par défaut : nom du dossier courant).
   - Visibilité : **`--private` par défaut** (proposer `--public` seulement si demandé).

3. **Initialiser en local**
   ```bash
   git init
   git branch -M main
   git add .
   git commit -m "chore: commit initial du projet"
   ```
   - ⚠️ Avant le commit, s'assurer qu'aucun **secret** n'est indexé (le hook secret-scan
     bloquera de toute façon).

4. **Créer le repo distant et pousser** ⚠️ *(action irréversible — DEMANDER CONFIRMATION)*
   ```bash
   gh repo create <nom> --public --source=. --remote=origin --push
   ```

5. **Confirmer**
   - Afficher l'URL du repo (`gh repo view --web` ou l'URL renvoyée).

## Garde-fous
- **Visibilité privée par défaut** (ne jamais publier en public sans demande explicite).
- **Confirmation obligatoire** avant `gh repo create` / push (on crée un dépôt distant).
- Jamais de secret dans le commit initial.
- Ne touche pas à un dépôt déjà initialisé.
