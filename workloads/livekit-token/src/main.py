import os
import threading

from litestar import Litestar, Router, get, post
from litestar.params import JSONBody
from livekit.api import AccessToken, VideoGrants
from pydantic import BaseModel

from src.otel_logging import configure_logging

configure_logging()


class TokenRequest(BaseModel, extra="allow"):
    room_name: str
    participant_identity: str


class ConnectionDetails(BaseModel):
    server_url: str
    participant_token: str


_next_id_lock = threading.Lock()
_next_id = 1


def _allocate_id() -> int:
    global _next_id
    with _next_id_lock:
        allocated = _next_id
        _next_id += 1
    return allocated


@post("/connection-details", status_code=200)
async def issue_token(data: JSONBody[TokenRequest]) -> ConnectionDetails:
    participant_id = _allocate_id()
    token = (
        AccessToken(os.environ["LIVEKIT_API_KEY"], os.environ["LIVEKIT_API_SECRET"])
        .with_identity(data.participant_identity)
        .with_attributes({"id": str(participant_id)})
        .with_grants(
            VideoGrants(
                room_join=True,
                room=data.room_name,
                can_publish=False,
                can_publish_data=True,
                can_subscribe=True,
                can_update_own_metadata=True,
            )
        )
        .to_jwt()
    )

    return ConnectionDetails(
        server_url=os.environ["LIVEKIT_SERVER_URL"],
        participant_token=token,
    )


@get("/health", include_in_schema=False)
async def health() -> dict[str, str]:
    return {"status": "ok"}


api = Router(path="/livekit-token", route_handlers=[issue_token, health])

app = Litestar([api], logging_config=None)
