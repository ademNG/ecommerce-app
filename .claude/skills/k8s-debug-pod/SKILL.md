---
name: k8s-debug-pod
description: Diagnostique un pod Kubernetes en échec (CrashLoopBackOff, ImagePullBackOff, OOMKilled, probes qui échouent). Collecte logs, events et describe, formule une hypothèse de cause racine et propose un correctif. N'applique aucune modification destructive sans validation humaine.
---

# Skill : k8s-debug-pod

Objectif : trouver **pourquoi** un pod ne tourne pas, de façon méthodique et lisible.

## Étapes à suivre
1. État global : `kubectl -n <ns> get pods -o wide` (repérer le statut : CrashLoopBackOff, Pending, etc.).
2. Détail du pod : `kubectl -n <ns> describe pod <pod>` (events, raison du redémarrage, probes, ressources).
3. Logs : `kubectl -n <ns> logs <pod> [--previous]` (`--previous` = logs du conteneur qui a crashé).
4. Events du namespace : `kubectl -n <ns> get events --sort-by=.lastTimestamp`.
5. Formuler 1 à 3 **hypothèses** de cause racine, classées, avec la preuve associée.
6. Proposer un **correctif** (manifest, ressources, image, probe) et l'EXPLIQUER ; appliquer seulement après validation.

## Garde-fous
- Lecture seule par défaut (`get`/`describe`/`logs`) : on diagnostique avant de modifier.
- Toute modification (`apply`, `edit`, `scale`, `rollout restart`) passe par une validation humaine.
- Ne jamais exposer de secret présent dans les logs ou les variables d'environnement.
