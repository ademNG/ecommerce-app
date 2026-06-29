---
name: containerize
description: Génère ou optimise les Dockerfiles des microservices de l'ecommerce-app (build multi-étapes, image runtime chiselée/distroless, exécution non-root) et un docker-compose pour le dev. Construit l'image, la scanne (Trivy) et propose des correctifs. Ne met aucun secret en clair et demande confirmation avant toute commande destructive (docker system prune, rmi).
---

# Skill : containerize

Objectif : conteneuriser proprement les services .NET (`Catalog`, `Ordering`, `Gateway`, `Web`) —
images **petites, non-root et scannées**.

## Étapes à suivre
1. Identifier le service cible et son `.csproj` (+ sa dépendance `ECommerce.ServiceDefaults`).
2. Générer un `src/<Service>/Dockerfile` **multi-étapes** : `sdk` pour build/publish, image **runtime
   chiselée** (`aspnet:10.0-*-chiseled`) pour l'exécution → plus petite et moins de surface d'attaque.
3. Exécuter en **non-root** (`USER $APP_UID`) et exposer le port `8080`.
4. Construire depuis la **racine du repo** : `docker build -t <image> -f src/<Service>/Dockerfile .`.
5. **Scanner l'image** : `trivy image --severity HIGH,CRITICAL <image>` ; EXPLIQUER puis corriger les findings.
6. (Dev) Générer/mettre à jour `docker-compose.yml` pour lancer les services ensemble (service discovery).
7. Vérifier l'endpoint `/health` du conteneur ; expliquer comment arrêter/nettoyer.

## Garde-fous
- Contexte de build = **racine du repo** (les `COPY` commencent par `src/`).
- JAMAIS de secret en clair dans un Dockerfile ou un compose (ni `ARG`/`ENV` sensible) — variables d'env injectées au runtime ou coffre.
- Images **non-root** et **runtime-only** (jamais le SDK dans l'image finale).
- Confirmation humaine avant toute commande destructive : `docker rmi`, `docker system prune`, `docker volume rm`.
