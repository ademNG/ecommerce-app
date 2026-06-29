{{/*
Construit le nom complet de l'image pour un service.
Avec registry : ghcr.io/owner/ecommerce-app-catalog:sha-abc
Sans registry  : ecommerce-app-catalog:latest  (k3d local)
Fix #2 : imageTag vide déclenche un échec explicite (sentinel guard).
*/}}
{{- define "ecommerce.image" -}}
{{- $svc := .svc -}}
{{- $global := .global -}}
{{- if and $global.imageRegistry (not $global.imageTag) -}}
  {{- required "global.imageTag is required when imageRegistry is set — pass --set global.imageTag=$GITHUB_SHA" nil -}}
{{- end -}}
{{- if $global.imageRegistry -}}
{{- printf "%s/%s:%s" $global.imageRegistry $svc.image $global.imageTag -}}
{{- else -}}
{{- printf "%s:%s" $svc.image ($global.imageTag | default "latest") -}}
{{- end -}}
{{- end }}
