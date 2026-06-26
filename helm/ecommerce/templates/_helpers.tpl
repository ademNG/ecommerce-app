{{/*
Construit le nom complet de l'image pour un service.
Avec registry : ghcr.io/owner/ecommerce-app-catalog:sha-abc
Sans registry  : ecommerce-app-catalog:latest  (k3d local)
*/}}
{{- define "ecommerce.image" -}}
{{- $svc := .svc -}}
{{- $global := .global -}}
{{- if $global.imageRegistry -}}
{{ $global.imageRegistry }}/{{ $svc.image }}:{{ $global.imageTag }}
{{- else -}}
{{ $svc.image }}:{{ $global.imageTag }}
{{- end -}}
{{- end }}
