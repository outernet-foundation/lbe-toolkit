# ruff: noqa: PLC2701 — opentelemetry's log exporter wiring has no public import
# path (opentelemetry._logs et al.); this module is the auditable wrapper boundary.
import logging
import logging.config
import os
import socket
import sys

from opentelemetry._logs import set_logger_provider
from opentelemetry.exporter.otlp.proto.grpc._log_exporter import OTLPLogExporter
from opentelemetry.sdk._logs import LoggerProvider, LoggingHandler
from opentelemetry.sdk._logs.export import BatchLogRecordProcessor
from opentelemetry.sdk.resources import Resource


def configure_logging() -> None:
    instance_id = socket.gethostname()
    handlers: dict[str, dict[str, object]] = {
        "stream": {
            "class": "logging.StreamHandler",
            "formatter": "human",
            "stream": sys.stderr,
        },
    }
    handler_names = ["stream"]

    otlp_endpoint = os.environ.get("OTEL_EXPORTER_OTLP_ENDPOINT")
    if otlp_endpoint:
        resource = Resource.create({
            "service.name": "livekit-token",
            "service.namespace": "lbetoolkit",
            "service.instance.id": instance_id,
            "service.version": os.environ.get("SERVICE_VERSION", "unknown"),
            "deployment.environment.name": os.environ.get("DEPLOYMENT_ENVIRONMENT", "development"),
            "container.name": os.environ.get("HOSTNAME", instance_id),
        })
        provider = LoggerProvider(resource=resource)
        provider.add_log_record_processor(
            BatchLogRecordProcessor(OTLPLogExporter(endpoint=otlp_endpoint, insecure=True))
        )
        set_logger_provider(provider)
        handlers["otlp"] = {
            "()": lambda: LoggingHandler(level=logging.NOTSET, logger_provider=provider),
        }
        handler_names.append("otlp")

    logging.config.dictConfig({
        "version": 1,
        "disable_existing_loggers": False,
        "formatters": {
            "human": {
                "format": "%(asctime)s [%(levelname)s] %(message)s",
                "datefmt": "%H:%M:%S",
            },
        },
        "handlers": handlers,
        "root": {"handlers": handler_names, "level": "INFO"},
    })
