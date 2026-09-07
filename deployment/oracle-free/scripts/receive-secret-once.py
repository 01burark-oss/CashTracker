#!/usr/bin/env python3
"""Receive one small secret over a pinned, one-time HTTPS endpoint."""

from __future__ import annotations

import argparse
import os
import ssl
from http.server import BaseHTTPRequestHandler, HTTPServer
from pathlib import Path


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--cert", required=True)
    parser.add_argument("--key", required=True)
    parser.add_argument("--token", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--port", type=int, default=443)
    args = parser.parse_args()

    output = Path(args.output).resolve()
    token_path = f"/{args.token}"

    class Handler(BaseHTTPRequestHandler):
        def do_POST(self) -> None:  # noqa: N802
            if self.path != token_path:
                self.send_error(404)
                return

            length = int(self.headers.get("Content-Length", "0"))
            if length < 1 or length > 4096:
                self.send_error(413)
                return

            payload = self.rfile.read(length)
            temporary = output.with_suffix(output.suffix + ".partial")
            descriptor = os.open(temporary, os.O_WRONLY | os.O_CREAT | os.O_TRUNC, 0o600)
            with os.fdopen(descriptor, "wb") as stream:
                stream.write(payload)
                stream.flush()
                os.fsync(stream.fileno())
            os.replace(temporary, output)

            self.send_response(204)
            self.end_headers()
            self.server.received = True  # type: ignore[attr-defined]

        def log_message(self, _format: str, *_args: object) -> None:
            return

    server = HTTPServer(("0.0.0.0", args.port), Handler)
    server.received = False  # type: ignore[attr-defined]
    context = ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER)
    context.load_cert_chain(args.cert, args.key)
    server.socket = context.wrap_socket(server.socket, server_side=True)

    while not server.received:  # type: ignore[attr-defined]
        server.handle_request()


if __name__ == "__main__":
    main()
