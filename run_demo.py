#!/usr/bin/env python3
"""Run the local ErrandFlow demonstration with Docker Compose."""

from __future__ import annotations

import argparse
import os
from pathlib import Path
import secrets
import shutil
import socket
import subprocess
import sys
import time
import urllib.error
import urllib.request
import webbrowser

ROOT = Path(__file__).resolve().parent
STATE = ROOT / ".demo-generated"
COMPOSE_FILE = ROOT / "docker-compose.demo.yml"
SQL_SOURCE = ROOT / "backend" / "database" / "001_initial.sql"
SQL_GENERATED = STATE / "001_initial.sql"
ENV_FILE = STATE / ".env"
FRONTEND_URL = "http://localhost:4200"


def run(command: list[str], *, check: bool = True) -> subprocess.CompletedProcess[str]:
    return subprocess.run(command, cwd=ROOT, check=check, text=True)


def require_repository() -> None:
    required = [
        COMPOSE_FILE,
        SQL_SOURCE,
        ROOT / "backend" / "src" / "ErrandFlow.Api" / "ErrandFlow.Api.csproj",
        ROOT / "frontend" / "package-lock.json",
        ROOT / "docker" / "backend" / "Dockerfile",
        ROOT / "docker" / "frontend" / "Dockerfile",
    ]
    missing = [str(path.relative_to(ROOT)) for path in required if not path.is_file()]
    if missing:
        raise RuntimeError("Missing repository files: " + ", ".join(missing))


def require_docker() -> None:
    if shutil.which("docker") is None:
        raise RuntimeError("Docker is not installed or is not on PATH. Install Docker Engine/Desktop first.")
    for command in (["docker", "info"], ["docker", "compose", "version"]):
        result = subprocess.run(command, cwd=ROOT, capture_output=True, text=True)
        if result.returncode != 0:
            raise RuntimeError("Command failed: " + " ".join(command) + "\n" + result.stderr.strip())


def create_local_config() -> None:
    STATE.mkdir(mode=0o700, parents=True, exist_ok=True)
    (STATE / ".gitignore").write_text("*\n", encoding="utf-8")
    # Only the demo's local database is initialized. Existing Docker volumes are preserved.
    sql = SQL_SOURCE.read_text(encoding="utf-8-sig")
    SQL_GENERATED.write_text("USE `errandflow`;\n" + sql, encoding="utf-8")

    if not ENV_FILE.exists():
        values = {
            "DEMO_DB_PASSWORD": secrets.token_hex(24),
            "DEMO_DB_ROOT_PASSWORD": secrets.token_hex(24),
            "DEMO_CERT_PASSWORD": secrets.token_hex(24),
        }
        ENV_FILE.write_text(
            "".join(f"{key}={value}\n" for key, value in values.items()),
            encoding="utf-8",
        )
    if os.name == "posix":
        os.chmod(ENV_FILE, 0o600)


def compose_args(*args: str) -> list[str]:
    if not ENV_FILE.is_file():
        raise RuntimeError("Missing demo configuration. Run 'python3 run_demo.py' first.")
    return ["docker", "compose", "--env-file", str(ENV_FILE), "-f", str(COMPOSE_FILE), *args]


def local_port_available(port: int) -> bool:
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
        try:
            return sock.connect_ex(("127.0.0.1", port)) != 0
        except OSError:
            return False


def wait_until_ready(timeout_seconds: int = 120) -> bool:
    end = time.monotonic() + timeout_seconds
    while time.monotonic() < end:
        try:
            with urllib.request.urlopen(FRONTEND_URL, timeout=3) as response:
                frontend_ready = response.status == 200
            try:
                urllib.request.urlopen(FRONTEND_URL + "/api/auth/me", timeout=3)
                backend_ready = False
            except urllib.error.HTTPError as error:
                backend_ready = error.code == 401
            if frontend_ready and backend_ready:
                return True
        except (urllib.error.URLError, TimeoutError, ConnectionError):
            pass
        time.sleep(2)
    return False


def main() -> int:
    parser = argparse.ArgumentParser(description="Start the local ErrandFlow demo with HTTPS Kestrel.")
    action = parser.add_mutually_exclusive_group()
    action.add_argument("--stop", action="store_true", help="Stop containers but keep the database.")
    action.add_argument("--logs", action="store_true", help="Show container logs.")
    action.add_argument("--status", action="store_true", help="Show container status.")
    action.add_argument("--check", action="store_true", help="Check prerequisites without starting.")
    parser.add_argument("--no-browser", action="store_true", help="Do not open a browser.")
    args = parser.parse_args()

    try:
        require_repository()
        require_docker()
        if args.check:
            print("Docker and repository files are available.")
            return 0
        if args.stop:
            run(compose_args("down"))
            print("Containers stopped. Database data has been preserved.")
            return 0
        if args.logs:
            run(compose_args("logs", "--tail", "150", "-f"))
            return 0
        if args.status:
            run(compose_args("ps"))
            return 0

        if not local_port_available(4200):
            print("Port 4200 is in use. If the demo is already running, open " + FRONTEND_URL)
            print("Otherwise, stop the application using that port and try again.")
            return 2

        create_local_config()
        print("Building and starting the local demo (HTTPS inside Docker)...", flush=True)
        run(compose_args("up", "--build", "--detach"))
        if not wait_until_ready():
            print("The demo started, but the expected HTTP checks did not succeed.")
            print("Inspect logs using: python3 run_demo.py --logs")
            return 1

        print("ErrandFlow is ready: " + FRONTEND_URL)
        print("Kestrel runs over HTTPS on the private Docker network (port 8443).")
        print("Login uses the existing Auth0 tenant; the demo requires internet access.")
        if not args.no_browser:
            webbrowser.open(FRONTEND_URL)
        return 0
    except (OSError, RuntimeError, subprocess.CalledProcessError) as error:
        print(f"Error: {error}", file=sys.stderr)
        return 1
    except KeyboardInterrupt:
        print("\nInterrupted. Containers may remain running; use --stop to stop them.")
        return 130


if __name__ == "__main__":
    sys.exit(main())
