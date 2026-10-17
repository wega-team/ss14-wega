#!/usr/bin/env python3

import sys
import argparse
import requests
import os
import subprocess
from concurrent.futures import ThreadPoolExecutor, as_completed
from typing import Iterable

DRY_RUN = os.environ.get("DRY_RUN", "").lower() in ("1", "true", "yes")
VERSION = os.environ["GITHUB_SHA"]
FORK_ID = os.environ['FORK_ID']

RELEASE_DIR = "release"
MAX_WORKERS = 4

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


def simulate_publish(fork_id: str, version: str) -> None:
    print(f"[DRY RUN] Would publish version {version} to Robust.CDN fork '{fork_id}'")
    print(f"[DRY RUN]   engine version: {get_engine_version()}")
    files = list(get_files_to_publish())
    print(f"[DRY RUN]   files to upload ({len(files)}):")
    for file in files:
        print(f"[DRY RUN]     - {file}")
    print("[DRY RUN] Skipping all network calls to Robust.CDN.")

def upload_one_file(session: requests.Session, file_path: str, fork_id: str, version: str) -> str:
    """Uploads a single file. Returns the filename upon success."""
    print(f"Publishing {file_path}")
    with open(file_path, "rb") as f:
        headers = {
            "Content-Type": "application/octet-stream",
            "Robust-Cdn-Publish-File": os.path.basename(file_path),
            "Robust-Cdn-Publish-Version": version,
        }
        resp = session.post(
            f"{ROBUST_CDN_URL}fork/{fork_id}/publish/file",
            data=f,
            headers=headers,
            timeout=600,
        )
    resp.raise_for_status()
    return os.path.basename(file_path)

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--fork-id", default=FORK_ID)
    parser.add_argument("--workers", type=int, default=MAX_WORKERS, help=f"Number of parallel uploads (default: {MAX_WORKERS})")

    args = parser.parse_args()
    fork_id = args.fork_id
    workers = args.workers

    if DRY_RUN:
        simulate_publish(fork_id, VERSION)
        return

    adapter = requests.adapters.HTTPAdapter(pool_connections=10, pool_maxsize=workers + 2)
    session = requests.Session()
    session.mount("https://", adapter)
    session.mount("http://", adapter)
    session.headers = {
        "Authorization": f"Bearer {get_publish_token()}",
    }

    print(f"Starting publish on Robust.Cdn for version {VERSION}")
    print(f"Using {workers} parallel upload workers")

    data = {
        "version": VERSION,
        "engineVersion": get_engine_version(),
    }
    resp = session.post(
        f"{ROBUST_CDN_URL}fork/{fork_id}/publish/start",
        json=data,
        headers={"Content-Type": "application/json"},
    )
    resp.raise_for_status()
    print("Publish successfully started, adding files in parallel...")

    files = list(get_files_to_publish())
    failed = []

    with ThreadPoolExecutor(max_workers=workers) as pool:
        future_to_file = {
            pool.submit(upload_one_file, session, f, fork_id, VERSION): f
            for f in files
        }
        for future in as_completed(future_to_file):
            file_path = future_to_file[future]
            try:
                name = future.result()
                print(f"  ✔ {name}")
            except Exception as e:
                print(f"  ✘ {os.path.basename(file_path)}: {e}", file=sys.stderr)
                failed.append(file_path)

    if failed:
        print(f"\n{len(failed)} file(s) failed to upload. Aborting publish.", file=sys.stderr)
        sys.exit(1)

    print("\nAll files uploaded, finishing publish...")

    resp = session.post(
        f"{ROBUST_CDN_URL}fork/{fork_id}/publish/finish",
        json={"version": VERSION},
        headers={"Content-Type": "application/json"},
    )
    resp.raise_for_status()

    print("SUCCESS!")


def get_files_to_publish() -> Iterable[str]:
    for file in os.listdir(RELEASE_DIR):
        yield os.path.join(RELEASE_DIR, file)


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
