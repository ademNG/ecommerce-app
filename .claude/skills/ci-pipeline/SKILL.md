---
name: ci-pipeline
description: Génère ou met à jour les workflows GitHub Actions de l'ecommerce-app (CI build/test/scan + CD avec gate manuel prod). Explique chaque étape et n'active jamais un déploiement prod automatique.
---

# Skill : ci-pipeline

Objectif : produire des **pipelines GitHub Actions** clairs et sûrs pour l'ecommerce-app,
en séparant l'**intégration continue (CI)** du **déploiement continu (CD)**.

## Étapes à suivre

1. **Comprendre le contexte**
   - Identifier la stack (.NET 8, microservices, Docker) et les fichiers de projet/tests.
   - Repérer où vivent les Dockerfiles des services.

2. **Générer le workflow CI** (`.github/workflows/ci.yml`)
   - Déclencheurs : `push` et `pull_request` sur `main`.
   - Étapes : `checkout` → `setup-dotnet` → `restore` → `build` (Release) → `test`.
   - Job séparé : build de l'image Docker + **scan Trivy** (échec si faille CRITICAL/HIGH).

3. **Générer le workflow CD** (`.github/workflows/cd.yml`)
   - Déclenché après un CI réussi sur `main`.
   - Déploiement **pré-prod** automatique.
   - Déploiement **prod** derrière un **environnement protégé** (`environment: production`)
     qui exige une **approbation humaine** (Required reviewers).

4. **Expliquer**
   - Commenter chaque étape pour un public débutant CI/CD.
   - Rappeler comment activer le gate : Settings > Environments > production > Required reviewers.

## Garde-fous
- ⚠️ **Jamais** de déploiement prod sans gate manuel / approbation humaine.
- Ne jamais mettre de secret en clair dans le YAML : utiliser les **GitHub Secrets**
  (`${{ secrets.NOM }}`).
- Le scan de sécurité doit **bloquer** la chaîne en cas de faille critique.
