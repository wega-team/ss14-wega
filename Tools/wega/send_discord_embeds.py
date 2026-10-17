#!/usr/bin/env python3
import json
import os
import sys
from pathlib import Path

import requests

NEW_ENTRIES_FILE = Path("new_changelog_entries.json")
DISCORD_WEBHOOK_URL = os.environ.get("DISCORD_WEBHOOK_URL")

EMBED_COLOR = 0x3498DB  # embed accent color (blue)

# Emoji per change type. Chosen to match SS14's visual style.
TYPES_TO_EMOJI = {
    "Add": "✨",
    "Fix": "🐛",
    "Tweak": "⚒️",
    "Remove": "❌",
}


def group_by_pr(entries: list[dict]) -> dict[str, list[dict]]:
    """Group entries by PR URL while preserving insertion order."""
    grouped: dict[str, list[dict]] = {}
    for entry in entries:
        url = entry.get("url")
        if not url:
            continue
        grouped.setdefault(url, []).append(entry)
    return grouped


def build_embed(pr_url: str, entries: list[dict]) -> dict:
    """
    Build a single embed for one PR.

    Title shows the author, title is clickable and points at the PR,
    description lists every change with an emoji prefix.
    """
    author = entries[0].get("author", "unknown")
    lines: list[str] = []
    for entry in entries:
        for change in entry.get("changes", []):
            emoji = TYPES_TO_EMOJI.get(change.get("type"), "•")
            lines.append(f"{emoji} {change.get('message', '')}")

    description = "\n".join(lines)
    # Discord hard-limits embed descriptions to 4096 characters.
    if len(description) > 4096:
        description = description[:4093] + "..."

    return {
        "title": f"PR by {author}",
        "url": pr_url,
        "description": description,
        "color": EMBED_COLOR,
    }


def send_embed(embed: dict) -> None:
    """Post a single embed to the configured Discord webhook."""
    payload = {
        "embeds": [embed],
        # Never ping anyone, even if the message text contains @mentions.
        "allowed_mentions": {"parse": []},
    }
    resp = requests.post(DISCORD_WEBHOOK_URL, json=payload, timeout=15)
    resp.raise_for_status()
    print(f"Sent embed for {embed['url']}")


def main() -> None:
    if not DISCORD_WEBHOOK_URL:
        print("DISCORD_WEBHOOK_URL is not set; skipping Discord publish.")
        return

    if not NEW_ENTRIES_FILE.exists():
        print("No new entries file found; nothing to publish.")
        return

    entries = json.loads(NEW_ENTRIES_FILE.read_text(encoding="utf-8"))
    if not entries:
        print("No new entries to publish.")
        return

    grouped = group_by_pr(entries)
    for pr_url, pr_entries in grouped.items():
        send_embed(build_embed(pr_url, pr_entries))


if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        print(f"send_discord_embeds.py failed: {e}", file=sys.stderr)
        sys.exit(1)
