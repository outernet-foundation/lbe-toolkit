ARG UV_BASE_IMAGE
FROM ${UV_BASE_IMAGE}

WORKDIR /app

ENV \
    PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    UV_COMPILE_BYTECODE=1 \
    UV_NO_INSTALLER_METADATA=1 \
    UV_LINK_MODE=copy \
    UV_PROJECT_ENVIRONMENT=/opt/uv/venv \
    VIRTUAL_ENV=/opt/uv/venv

RUN uv venv $UV_PROJECT_ENVIRONMENT

COPY workloads/livekit-token/pyproject.toml workloads/livekit-token/pylock.toml ./
RUN --mount=type=cache,id=uvcache,sharing=locked,target=/root/.cache/uv \
    uv pip install -r pylock.toml

COPY workloads/livekit-token/src ./src

ENTRYPOINT ["uv", "run", "--no-sync", "uvicorn", "src.main:app", "--host", "0.0.0.0", "--port", "8000"]
