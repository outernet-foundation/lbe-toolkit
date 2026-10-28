# The ingress publishes its host port through a ${GATEWAY_PORT:?err} hole:
# seam publishes are publish-pinned literals and cannot carry env holes, and
# the tier's port contract makes the published port consumer topology, so the
# hole must survive into the consumer's include-time interpolation (patch
# values pass through score-compose verbatim). The depends_on preserves the
# hand tier's start ordering on the token sidecar — score has no depends_on
# concept, and the reference names the post-collapse service name.
{{- range $workloadName, $workload := .Workloads }}
{{- if eq $workloadName "ingress" }}
{{- range $containerName, $_ := $workload.containers }}
- op: set
  path: services.{{ $workloadName }}-{{ $containerName }}.ports
  value:
    - target: 8443
      published: "${GATEWAY_PORT:?err}"
      protocol: tcp
      mode: ingress
  description: Publish the ingress host port from the consumer's GATEWAY_PORT
- op: set
  path: services.{{ $workloadName }}-{{ $containerName }}.depends_on
  value:
    livekit-token:
      condition: service_started
      required: true
  description: Order the ingress after the token sidecar
{{- end }}
{{- end }}
{{- end }}
