#!/usr/bin/env python3

import sys
import argparse
import requests
import os
import subprocess
import time
from typing import Iterable

from requests.adapters import HTTPAdapter

DRY_RUN = os.environ.get("DRY_RUN", "").lower() in ("1", "true", "yes")
VERSION = os.environ["GITHUB_SHA"]
FORK_ID = os.environ["FORK_ID"]

RELEASE_DIR = "release"
MAX_RETRIES = 8
BASE_DELAY = 1.0
MAX_DELAY = 20.0
REQUEST_TIMEOUT = 600

#
# CONFIGURATION PARAMETERS
# Forks should change these to publish to their own infrastructure.
#
ROBUST_CDN_URL = "https://cdn.station14.ru/"

def get_publish_token() -> str:
    try:
        return os.environ["PUBLISH_TOKEN"]
    except KeyError:
        raise RuntimeError("PUBLISH_TOKEN is not set")


def get_delay(attempt: int) -> float:
    """Exponential backoff capped at MAX_DELAY; short pauses on the first attempts."""
    if attempt <= 2:
        return 0.5
    return min(BASE_DELAY * (2 ** (attempt - 3)), MAX_DELAY)


def simulate_publish(fork_id: str, version: str) -> None:
    print(f"[DRY RUN] Would publish version {version} to fork '{fork_id}'")
    print(f"[DRY RUN] Engine version: {get_engine_version()}")
    files = list(get_files_to_publish())
    print(f"[DRY RUN] Files to upload ({len(files)}):")
    for f in files:
        print(f"[DRY RUN]   - {f}")
    print("[DRY RUN] Skipping all network calls.")


def make_session() -> requests.Session:
    """Create a session for the start/finish requests."""
    session = requests.Session()
    adapter = HTTPAdapter(pool_connections=1, pool_maxsize=1, max_retries=0)
    session.mount("https://", adapter)
    session.mount("http://", adapter)
    session.headers.update({
        "Authorization": f"Bearer {get_publish_token()}",
    })
    return session


def upload_one_file(file_path: str, fork_id: str, version: str) -> str:
    """
    Upload a single file via curl instead of requests.
    Curl avoids the multi-threaded TLS bug in requests/urllib3 that
    produces SSLEOFError behind the SYN-proxy.
    """
    filename = os.path.basename(file_path)
    url = f"{ROBUST_CDN_URL}fork/{fork_id}/publish/file"
    token = get_publish_token()

    last_error = None
    for attempt in range(1, MAX_RETRIES + 1):
        cmd = [
            "curl",
            "-X", "POST",
            "-H", f"Authorization: Bearer {token}",
            "-H", "Content-Type: application/octet-stream",
            "-H", f"Robust-Cdn-Publish-File: {filename}",
            "-H", f"Robust-Cdn-Publish-Version: {version}",
            "-H", "Connection: close",
            "--data-binary", f"@{file_path}",
            "--max-time", str(REQUEST_TIMEOUT),
            "--fail",
            "--silent",
            "--show-error",
            url,
        ]
        print(f"  [{filename}] attempt {attempt}/{MAX_RETRIES}")
        try:
            result = subprocess.run(cmd, capture_output=True, text=True)
            if result.returncode != 0:
                err = result.stderr.strip() or f"curl exited with code {result.returncode}"
                raise RuntimeError(err)
            print(f"  ✔ [{filename}] uploaded")
            return filename
        except Exception as e:
            last_error = e
            print(f"  ✘ [{filename}] attempt {attempt} failed: {e}")
            if attempt < MAX_RETRIES:
                delay = get_delay(attempt)
                print(f"     sleeping {delay:.1f}s before retry")
                time.sleep(delay)

    raise RuntimeError(f"Failed to upload {filename} after {MAX_RETRIES} attempts: {last_error}")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--fork-id", default=FORK_ID)
    args = parser.parse_args()
    fork_id = args.fork_id

    if DRY_RUN:
        simulate_publish(fork_id, VERSION)
        return

    print(f"Starting publish on Robust.Cdn for version {VERSION}")
    print(f"Retries per file: {MAX_RETRIES}")

    start_session = make_session()
    start_session.headers["Connection"] = "close"
    data = {"version": VERSION, "engineVersion": get_engine_version()}
    resp = start_session.post(
        f"{ROBUST_CDN_URL}fork/{fork_id}/publish/start",
        json=data,
        headers={"Content-Type": "application/json"},
        timeout=60,
    )
    resp.raise_for_status()
    start_session.close()
    print("Publish started.")

    files = list(get_files_to_publish())
    failed: list[str] = []

    # Sequential upload: Robust.Cdn returns HTTP 500 on concurrent
    # /publish/file requests, so files must be sent one at a time.
    for file_path in files:
        try:
            upload_one_file(file_path, fork_id, VERSION)
        except Exception as e:
            print(f"  ✘ {os.path.basename(file_path)}: {e}", file=sys.stderr)
            failed.append(file_path)

    if failed:
        print(f"\n{len(failed)} file(s) failed to upload. Aborting publish.", file=sys.stderr)
        sys.exit(1)

    print("\nAll files uploaded, finishing publish...")

    finish_session = make_session()
    finish_session.headers["Connection"] = "close"
    resp = finish_session.post(
        f"{ROBUST_CDN_URL}fork/{fork_id}/publish/finish",
        json={"version": VERSION},
        headers={"Content-Type": "application/json"},
        timeout=60,
    )
    resp.raise_for_status()
    finish_session.close()

    print("SUCCESS!")


def get_files_to_publish() -> Iterable[str]:
    for f in os.listdir(RELEASE_DIR):
        yield os.path.join(RELEASE_DIR, f)


def get_engine_version() -> str:
    proc = subprocess.run(
        ["git", "describe", "--tags", "--abbrev=0"],
        stdout=subprocess.PIPE,
        cwd="RobustToolbox",
        check=True,
        encoding="UTF-8",
    )
    tag = proc.stdout.strip()
    assert tag.startswith("v")
    return tag[1:] # Cut off v prefix.


if __name__ == '__main__':
    main()
