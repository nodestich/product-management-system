{{/* Chart name, overridable through nameOverride. */}}
{{- define "pms.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "pms.fullname" -}}
{{- if .Values.fullnameOverride -}}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- $name := default .Chart.Name .Values.nameOverride -}}
{{- if contains $name .Release.Name -}}
{{- .Release.Name | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" -}}
{{- end -}}
{{- end -}}
{{- end -}}

{{- define "pms.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/* Expects a dict with "root" (the top-level context) and "component" (api, web, postgres). */}}
{{- define "pms.selectorLabels" -}}
app.kubernetes.io/name: {{ include "pms.name" .root }}-{{ .component }}
app.kubernetes.io/instance: {{ .root.Release.Name }}
{{- end -}}

{{- define "pms.labels" -}}
{{ include "pms.selectorLabels" . }}
app.kubernetes.io/managed-by: {{ .root.Release.Service }}
helm.sh/chart: {{ include "pms.chart" .root }}
{{- end -}}

{{/* The postgres Service name doubles as the database host, so both come from one helper. */}}
{{- define "pms.postgresHost" -}}
{{- printf "%s-postgres" (include "pms.fullname" .) -}}
{{- end -}}

{{- define "pms.postgresSecretName" -}}
{{- if .Values.postgres.existingSecret -}}
{{- .Values.postgres.existingSecret -}}
{{- else -}}
{{- printf "%s-db" (include "pms.fullname" .) -}}
{{- end -}}
{{- end -}}

{{- define "pms.connectionString" -}}
{{- printf "Host=%s;Port=5432;Database=%s;Username=%s;Password=%s" (include "pms.postgresHost" .) .Values.postgres.database .Values.postgres.username .Values.postgres.password -}}
{{- end -}}
