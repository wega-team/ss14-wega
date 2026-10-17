#!/usr/bin/env python3
import json
import os
import re
import sys
from datetime import datetime, timedelta, timezone
from pathlib import Path

import requests
import yaml

CHANGELOG_FILE = Path("Resources/Changelog/ChangelogAWega.yml")
NEW_ENTRIES_FILE = Path("new_changelog_entries.json")

GITHUB_API_URL = os.environ.get("GITHUB_API_URL", "https://api.github.com")
GITHUB_TOKEN = os.environ["GITHUB_TOKEN"]
GITHUB_REPOSITORY = os.environ["GITHUB_REPOSITORY"]
GITHUB_RUN_ID = os.environ["GITHUB_RUN_ID"]

# If there is no previous successful run, only look back this many days.
# Prevents flooding the Discord channel on the very first execution.
FIRST_RUN_WINDOW_DAYS = 7

# Matches a single changelog line: `- add: ...`, `- fix: ...`, etc.
# Group 1 = type (case-insensitive), group 2 = message.
CL_LINE_RE = re.compile(
    r"^-\s*(add|fix|tweak|remove)\s*:\s*(.+)$",
    re.IGNORECASE,
)

# Match `:cl:` line only; author comes from the PR, text after `:cl:` is ignored.
CL_MARKER_RE = re.compile(r"^[ \t]*:cl:[^\n]*$", re.MULTILINE | re.IGNORECASE)

# GitHub user-attachments host. Images are usually wrapped in markdown
# (`![alt](url)`) or HTML (`<img src="...">`); videos are bare URLs.
USER_ATTACHMENT_RE = re.compile(
    r"https?://github\.com/user-attachments/assets/[A-Za-z0-9-]+"
)
MD_IMAGE_RE = re.compile(r"!\[[^\]]*\]\((https?://[^)\s]+)\)")
HTML_IMAGE_RE = re.compile(r"<img[^>]+src=[\"'](https?://[^\"']+)[\"']", re.I)
VIDEO_EXTENSIONS = (".mp4", ".webm", ".mov", ".m4v")

YOUTUBE_RE = re.compile(
    r"https?://(?:www\.|m\.)?"
    r"(?:youtube\.com/(?:watch\?v=|shorts/|embed/|live/|v/)[A-Za-z0-9_-]+"
    r"|youtu\.be/[A-Za-z0-9_-]+)"
    r"(?:[?&][^\s)\"'<>]*)?",
    re.IGNORECASE,
)

MAX_URL_LENGTH = 2048


def make_session() -> requests.Session:
    """Build a requests session pre-configured for the GitHub REST API."""
    s = requests.Session()
    s.headers["Authorization"] = f"Bearer {GITHUB_TOKEN}"
    s.headers["Accept"] = "application/vnd.github+json"
    s.headers["X-GitHub-Api-Version"] = "2022-11-28"
    return s


def strip_html_comments(text: str) -> str:
    """
    Remove all `<!-- ... -->` blocks.

    The PR template ships the `:cl:` example inside an HTML comment, so this
    step is what distinguishes "user actually filled in the changelog" from
    "user left the template as is".
    """
    return re.sub(r"<!--.*?-->", "", text, flags=re.DOTALL)


def extract_media(body: str | None) -> dict:
    """
    Extract images and videos from a PR body.

    Returns {"images": [...], "videos": [...]} with absolute URLs.

    - Markdown (`![](url)`) and HTML (`<img src=...>`) images -> images.
    - GitHub user-attachment URLs (bare, no extension) -> videos.
    - YouTube links (youtube.com, youtu.be) -> videos.

    Anything longer than Discord's 2048-char URL limit is dropped.
    Duplicates are removed; an URL is only counted once.
    """
    if not body:
        return {"images": [], "videos": []}

    body = strip_html_comments(body)

    images: list[str] = []
    videos: list[str] = []
    seen: set[str] = set()

    for url in MD_IMAGE_RE.findall(body) + HTML_IMAGE_RE.findall(body):
        url = url.strip()
        if url and url not in seen and len(url) <= MAX_URL_LENGTH:
            images.append(url)
            seen.add(url)

    for url in USER_ATTACHMENT_RE.findall(body):
        if url in seen or len(url) > MAX_URL_LENGTH:
            continue
        lower = url.lower()
        if lower.endswith(VIDEO_EXTENSIONS):
            videos.append(url)
            seen.add(url)
        elif not any(
            lower.endswith(ext)
            for ext in (".png", ".jpg", ".jpeg", ".gif", ".webp")
        ):
            # No image extension and not captured as markdown image:
            # GitHub's video attachments are bare URLs -> treat as video.
            videos.append(url)
            seen.add(url)

    for url in YOUTUBE_RE.findall(body):
        url = url.rstrip(".,;:!?")
        if url in seen or len(url) > MAX_URL_LENGTH:
            continue
        videos.append(url)
        seen.add(url)

    return {"images": images, "videos": videos}


def get_last_successful_run(sess: requests.Session) -> dict | None:
    """Return the most recent successful run of this workflow before the current one."""
    resp = sess.get(
        f"{GITHUB_API_URL}/repos/{GITHUB_REPOSITORY}/actions/runs/{GITHUB_RUN_ID}"
    )
    resp.raise_for_status()
    current = resp.json()

    resp = sess.get(
        f"{current['workflow_url']}/runs",
        params={
            "status": "success",
            "created": f"<={current['created_at']}",
            "per_page": 10,
        },
    )
    resp.raise_for_status()
    for run in resp.json().get("workflow_runs", []):
        if run["id"] != current["id"]:
            return run
    return None


def get_merged_prs_since(sess: requests.Session, since_iso: str) -> list[dict]:
    """Return all PRs merged strictly after `since_iso` (ISO-8601 timestamp)."""
    url = f"{GITHUB_API_URL}/search/issues"
    params = {
        "q": f"repo:{GITHUB_REPOSITORY} is:pr is:merged merged:>{since_iso}",
        "per_page": 100,
        "sort": "updated",
        "order": "asc",
    }
    prs: list[dict] = []
    while url:
        resp = sess.get(url, params=params)
        resp.raise_for_status()
        prs.extend(resp.json().get("items", []))
        url = resp.links.get("next", {}).get("url")
        params = None
    return prs


def get_pr_details(sess: requests.Session, number: int) -> dict:
    """Fetch full PR data (body, labels, user, merged_at, etc.)."""
    resp = sess.get(f"{GITHUB_API_URL}/repos/{GITHUB_REPOSITORY}/pulls/{number}")
    resp.raise_for_status()
    return resp.json()


def parse_cl_block(body: str | None) -> list[dict]:
    """
    Parse an SS14-style `:cl:` block from a PR body.

    Expected format (matches the repo's PR template):

        :cl:
        - add: something new
        - fix: something fixed

    The author is intentionally NOT read from the block — it always
    comes from the PR's GitHub user (see main()). Anything after `:cl:`
    on the same line (legacy `:cl: SomeUser` form) is ignored.

    Returns the list of changes; empty if the block is missing or empty.
    """
    if not body:
        return []

    # Drop commented-out template content first: only a filled-in `:cl:`
    # block counts as a real changelog entry.
    body = strip_html_comments(body)

    marker = CL_MARKER_RE.search(body)
    if not marker:
        return []

    changes: list[dict] = []
    for raw_line in body[marker.end():].splitlines():
        line = raw_line.strip()
        if not line:
            continue                       # skip blank lines between entries
        if line.startswith("#"):
            break                          # reached the next markdown section
        m = CL_LINE_RE.match(line)
        if m:
            # Normalize to the casing used in the YAML file.
            changes.append(
                {"type": m.group(1).capitalize(), "message": m.group(2).strip()}
            )
        elif changes:
            # Non-changelog line after we started collecting: stop, we're
            # past the changelog block.
            break

    return changes


def next_entry_id(entries: list[dict]) -> int:
    """Highest numeric `id` currently present in the file (0 if empty)."""
    return max(
        (e["id"] for e in entries if isinstance(e.get("id"), int)),
        default=0,
    )


def write_github_output(changed: bool) -> None:
    """Expose `changed=true|false` to subsequent workflow steps."""
    output_file = os.environ.get("GITHUB_OUTPUT")
    if output_file:
        with open(output_file, "a") as f:
            f.write(f"changed={'true' if changed else 'false'}\n")


def main() -> None:
    sess = make_session()

    # Load the current changelog file, tolerating a missing file.
    if CHANGELOG_FILE.exists():
        with open(CHANGELOG_FILE, "r", encoding="utf-8-sig") as f:
            changelog = yaml.safe_load(f) or {"Entries": []}
    else:
        changelog = {"Entries": []}
    entries: list[dict] = changelog.setdefault("Entries", [])

    # Determine the start of the search window.
    last_run = get_last_successful_run(sess)
    if last_run:
        since = last_run["created_at"]
        print(f"Looking for PRs merged after {since} (run {last_run['id']})")
    else:
        since = (
            datetime.now(timezone.utc) - timedelta(days=FIRST_RUN_WINDOW_DAYS)
        ).strftime("%Y-%m-%dT%H:%M:%SZ")
        print(f"No previous successful run found; using window since {since}")

    prs = get_merged_prs_since(sess, since)
    if not prs:
        print("No merged PRs found in the window.")
        write_github_output(changed=False)
        return

    # Deduplicate against whatever is already in the YAML.
    existing_urls = {e.get("url") for e in entries if e.get("url")}
    current_id = next_entry_id(entries)
    new_entries: list[dict] = []
    media_by_url: dict[str, dict] = {}

    for pr_stub in prs:
        number = pr_stub["number"]
        pr_url = pr_stub["html_url"]
        if pr_url in existing_urls:
            continue  # already recorded by a previous run

        pr = get_pr_details(sess, number)
        changes = parse_cl_block(pr.get("body"))
        if not changes:
            # No filled-in `:cl:` block: skip, by design.
            continue

        current_id += 1
        entry = {
            # Author is always the PR's GitHub user, never the `:cl:` block.
            "author": pr["user"]["login"],
            "changes": changes,
            "id": current_id,
            "time": pr["merged_at"],
            "url": pr_url,
        }
        entries.append(entry)
        new_entries.append(entry)
        # Media is kept OUT of the YAML on purpose: the game's changelog
        # format doesn't need it, only the Discord embed does.
        media_by_url[pr_url] = extract_media(pr.get("body"))
        existing_urls.add(pr_url)

    if not new_entries:
        print("No new changelog entries found.")
        write_github_output(changed=False)
        return

    # Keep the file sorted by id so entries stay chronological.
    entries.sort(key=lambda e: e.get("id", 0))

    with open(CHANGELOG_FILE, "w", encoding="utf-8") as f:
        yaml.safe_dump(
            changelog,
            f,
            allow_unicode=True,
            sort_keys=False,
            default_flow_style=False,
            width=100000,
        )

    print(f"Added {len(new_entries)} new changelog entries.")

    # Attach media to each entry for the Discord step.
    entries_for_discord = []
    for e in new_entries:
        item = dict(e)
        item["media"] = media_by_url.get(e["url"], {"images": [], "videos": []})
        entries_for_discord.append(item)

    NEW_ENTRIES_FILE.write_text(
        json.dumps(entries_for_discord, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )

    write_github_output(changed=True)


if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        print(f"update_changelog.py failed: {e}", file=sys.stderr)
        sys.exit(1)
