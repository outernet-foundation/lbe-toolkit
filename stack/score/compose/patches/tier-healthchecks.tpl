# score-compose validates and drops workload livenessProbes (httpGet is not a
# supported compose concept), so the generated compose would gate nothing on
# workload health. This patch-template class re-derives the probe as a compose
# healthcheck against the computed <workload>-<container> service name. The
# probe runs via python urllib because the livekit-token image is python-based
# and ships neither wget nor curl.
{{- range $workloadName, $workload := .Workloads }}
{{- range $containerName, $container := $workload.containers }}
{{- with $container.livenessProbe.httpGet }}
- op: set
  path: services.{{ $workloadName }}-{{ $containerName }}.healthcheck
  value:
    test:
      [
        "CMD",
        "python",
        "-c",
        "import urllib.request; urllib.request.urlopen('http://localhost:{{ .port }}{{ .path }}')",
      ]
    interval: 5s
    start_period: 5s
    timeout: 5s
    retries: 3
  description: Healthcheck from livenessProbe for {{ $workloadName }}-{{ $containerName }}
{{- end }}
{{- end }}
{{- end }}
