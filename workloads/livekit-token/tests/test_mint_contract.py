import base64
import json

import pytest
from litestar.testing import TestClient

from src.main import app


def decode_jwt_payload(token: str) -> dict[str, object]:
    payload = token.split(".")[1]
    padded = payload + "=" * (-len(payload) % 4)
    return json.loads(base64.urlsafe_b64decode(padded))


def test_spec_advertises_mounted_mint_route() -> None:
    schema = app.openapi_schema
    assert schema.paths is not None
    assert "/livekit-token/connection-details" in schema.paths


def test_mint_contract_round_trip(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setenv("LIVEKIT_API_KEY", "test-key")
    monkeypatch.setenv("LIVEKIT_API_SECRET", "test-secret-long-enough-for-hs256")
    monkeypatch.setenv("LIVEKIT_SERVER_URL", "ws://livekit:7880")

    with TestClient(app=app) as client:
        response = client.post(
            "/livekit-token/connection-details",
            json={
                # Field names and response shape are the LiveKit SDK's
                # TokenSource wire contract (TokenSourceData.cs): snake_case
                # keys, participant_name tolerated as an extra.
                "room_name": "room-1",
                "participant_identity": "participant-1",
                "participant_name": "participant-1",
            },
        )

    assert response.status_code == 200
    body = response.json()
    assert set(body) == {"server_url", "participant_token"}
    assert body["server_url"] == "ws://livekit:7880"

    claims = decode_jwt_payload(body["participant_token"])
    assert claims["sub"] == "participant-1"
    video = claims["video"]
    assert video["room"] == "room-1"
    assert video["roomJoin"] is True
    assert video["canPublishData"] is True
    assert video["canPublish"] is False
    attributes = claims["attributes"]
    assert isinstance(attributes, dict)
    assert str(attributes["id"]).isdigit()
