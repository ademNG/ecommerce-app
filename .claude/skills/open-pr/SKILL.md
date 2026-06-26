---
name: open-pr
description: Pousse la branche courante et ouvre une Pull Request GitHub avec un titre clair, une description structurée et une checklist de revue. Demande validation avant de pousser.
---

# Skill : open-pr

Objectif : transformer une branche de travail en **Pull Request bien documentée**, prête
à être revue, en utilisant la CLI GitHub (`gh`).

## Pré-requis
- Être authentifié : `gh auth status` (sinon `gh auth login`).
- Avoir au moins un commit sur une branche **autre que** `main`/`master`.

## Étapes à suivre

1. **Vérifier la branche**
   - `git branch --show-current`. Si on est sur `main`/`master`, **refuser** et proposer
     de créer une branche (`git switch -c feat/<sujet>`).

2. **Récapituler les changements**
   - Comparer avec la branche par défaut : `git log --oneline main..HEAD` et `git diff main...HEAD --stat`.

3. **Pousser la branche** ⚠️
   - **Demander confirmation**, puis `git push -u origin <branche>`.

4. **Rédiger la PR**
   - Titre : court, format Conventional Commits (ex. `feat(catalog): endpoint /health`).
   - Corps structuré :
     ```
     ## Contexte
     <pourquoi ce changement>

     ## Changements
     - <point 1>
     - <point 2>

     ## Tests
     <comment ça a été vérifié>

     ## Checklist de revue
     - [ ] Pas de secret ni de donnée sensible
     - [ ] Tests passent
     - [ ] Impact infra / déploiement vérifié
     ```

5. **Créer la PR**
   - `gh pr create --title "<titre>" --body "<corps>"`.
   - Afficher l'URL de la PR créée.

## Garde-fous
- Ne **jamais** merger automatiquement (`gh pr merge`) : le merge est une décision humaine.
- Vérifier qu'aucun secret n'est inclus avant le push.
