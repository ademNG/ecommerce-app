---
name: helm-package
description: Transforme les manifests Kubernetes de l'ecommerce-app (UC5) en un chart Helm paramétrable, avec des values par environnement (dev/preprod). Valide le chart (helm lint, helm template), installe/met à jour via helm upgrade --install, et recommande une revue (/code-review) des templates. Ne met aucun secret en clair et demande confirmation avant tout helm uninstall/rollback destructif.
---

# Skill : helm-package

Objectif : packager le déploiement K8s en un **chart Helm** clair, **paramétré** et réutilisable.

## Étapes à suivre
1. Repartir des manifests UC5 (Deployments, Services, Ingress, HPA) et identifier ce qui **varie** par env (image, réplicas, ressources, host, autoscaling).
2. Créer la structure : `helm/ecommerce/Chart.yaml`, `values.yaml` (défauts), `templates/` (resources paramétrées via `.Values`).
3. Externaliser les variations dans `values-<env>.yaml` (ex. `values-dev.yaml`, `values-preprod.yaml`).
4. Valider : `helm lint helm/ecommerce` puis `helm template ecommerce helm/ecommerce -f values-<env>.yaml` (rendu local, rien d'appliqué).
5. Installer/mettre à jour : `helm upgrade --install ecommerce helm/ecommerce -n ecommerce --create-namespace -f values-<env>.yaml`.
6. Vérifier le déploiement (`helm status`, `kubectl get pods`) ; proposer une revue `/code-review` des templates.

## Garde-fous
- Toujours `helm lint` + `helm template` (dry-run) AVANT `helm upgrade --install`.
- Aucun secret en clair dans les values/templates : `Secret` K8s ou coffre, jamais en dur.
- Confirmation humaine avant `helm uninstall` et `helm rollback` (actions destructives).
- Un seul jeu de templates ; les différences d'environnement vivent dans les `values-<env>.yaml`.
