#!/usr/bin/env bash
# Hook PreToolUse — bloque un `git commit` si un secret est détecté dans les fichiers indexés.
# Reçoit l'appel d'outil en JSON sur stdin. Retourne un code != 0 pour BLOQUER la commande.
#
# Câblé dans .claude/settings.json (voir 02-demo-claude-code-git.md).
# Pédagogie : montre comment un hook impose un garde-fou DÉTERMINISTE (ce n'est pas l'IA
# qui décide, c'est le harnais qui exécute ce script).

set -euo pipefail

# Récupère la commande shell que l'agent veut lancer
payload="$(cat)"
cmd="$(printf '%s' "$payload" | grep -o '"command"[^,]*' | head -1 || true)"

# On ne s'intéresse qu'aux commits
if ! printf '%s' "$cmd" | grep -qi 'git commit'; then
  exit 0
fi

# Motifs de secrets fréquents (simplifié pour la démo ; en vrai : gitleaks/trufflehog)
patterns='(password|passwd|secret|api[_-]?key|token|BEGIN (RSA|OPENSSH|EC) PRIVATE KEY|connectionstring|aws_secret_access_key)'

# Scanne les fichiers indexés
hits="$(git diff --cached -U0 2>/dev/null | grep -iE "^\+.*$patterns" || true)"

if [ -n "$hits" ]; then
  echo "⛔ COMMIT BLOQUÉ par le hook secret-scan : un secret potentiel a été détecté." >&2
  echo "Lignes suspectes :" >&2
  printf '%s\n' "$hits" | head -5 >&2
  echo "Retire le secret (utilise un coffre / variable d'environnement) puis recommence." >&2
  exit 2   # code != 0 => la commande est bloquée
fi

exit 0
