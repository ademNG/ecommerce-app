---
name: git-commit
description: Crée un commit Git propre au format Conventional Commits. Analyse les changements, propose un message clair, et demande TOUJOURS validation avant de commiter.
---

# Skill : git-commit

Objectif : produire un **commit propre et lisible** sans que l'utilisateur ait à rédiger
le message lui-même. Le commit doit respecter le format **Conventional Commits**.

## Étapes à suivre

1. **Regarder ce qui a changé**
   - Lancer `git status` puis `git diff` (et `git diff --staged` si des fichiers sont déjà indexés).
   - Résumer en une phrase ce que font les changements.

2. **Indexer les bons fichiers**
   - Si rien n'est indexé, proposer `git add` sur les fichiers pertinents.
   - **Ne jamais** ajouter de fichiers de secret, `.env`, clés, ou artefacts de build.

3. **Rédiger le message au format Conventional Commits**
   - Forme : `type(scope): description courte à l'impératif`
   - Types autorisés : `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, `ci`, `perf`.
   - Exemple : `feat(catalog): ajoute un endpoint /health pour les probes`
   - Si le changement est important, ajouter un corps qui explique le **pourquoi**.

4. **Valider avant de commiter** ⚠️
   - **Afficher le message proposé et demander confirmation à l'utilisateur.**
   - Ne lancer `git commit` qu'après accord explicite.

5. **Confirmer**
   - Afficher le hash court du commit créé (`git log -1 --oneline`).

## Garde-fous
- Jamais de `git push` dans ce skill (le push est une étape séparée et validée).
- Jamais de secret dans le diff committé : si un secret est détecté, **stopper** et alerter.
