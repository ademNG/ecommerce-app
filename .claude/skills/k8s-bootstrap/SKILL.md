---
name: k8s-bootstrap
description: Crée un cluster Kubernetes local avec k3d (multi-nœuds, registre local, ingress Traefik), importe les images de l'ecommerce-app et applique les manifests (Deployments, Services, Ingress, probes liveness/readiness, HPA). Montre les commandes avant d'agir et demande confirmation avant toute action destructive (delete cluster/namespace).
---

# Skill : k8s-bootstrap

Objectif : obtenir un cluster k3d **reproductible** et y déployer l'`ecommerce-app` proprement.

## Étapes à suivre
1. Créer le cluster : `k3d cluster create --config k8s/k3d-cluster.yaml` (serveurs + agents, registre, ingress).
2. Vérifier : `kubectl cluster-info` puis `kubectl get nodes` (tous `Ready`).
3. Rendre les images disponibles : `k3d image import <image> -c <cluster>` (ou push vers le registre local).
4. Appliquer les manifests : `kubectl apply -f k8s/` (namespace, Deployments, Services, Ingress, HPA).
5. Vérifier le rollout : `kubectl -n ecommerce rollout status deploy/<svc>` ; pods `Running`, probes OK.
6. Tester l'accès via l'ingress (`curl` sur le host configuré) et l'endpoint `/health`.

## Garde-fous
- Afficher chaque commande `k3d`/`kubectl` AVANT exécution ; pas d'action à l'aveugle.
- Confirmation humaine avant `k3d cluster delete`, `kubectl delete ns`, `kubectl delete -f`.
- Probes `readiness` (/health) et `liveness` (/alive) sur chaque service ; `requests/limits` définis.
- Aucun secret en clair dans un manifest : `Secret`/variables d'env, jamais en dur.
